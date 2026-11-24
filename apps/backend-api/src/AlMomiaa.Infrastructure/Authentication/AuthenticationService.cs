using System.Data;
using System.Security.Cryptography;
using System.Text;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Commands.ChangePassword;
using AlMomiaa.Application.Authentication.Commands.Login;
using AlMomiaa.Application.Authentication.Commands.RefreshSession;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.SystemOps;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Authentication;

public sealed class AuthenticationService(
    AlMomiaaDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ILoginAttemptLimiter attemptLimiter,
    ISecurityStampStore securityStampStore,
    TimeProvider? timeProvider = null) : IAuthenticationService
{
    private const int AccessTokenExpiresInSeconds = 600;
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly string _dummyPasswordHash = passwordHasher.HashPassword("AlMomiaa-Timing-Pad-DoNotUse!");

    public static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    public static string GenerateOpaqueToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(48)).ToLowerInvariant();

    public static string NormalizeEmployeeCode(string value) =>
        value.Trim().ToUpperInvariant();

    public static string NormalizePhone(string value)
    {
        var trimmed = value.Trim();
        var sb = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsDigit(ch) || (ch == '+' && sb.Length == 0))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static bool FixedTimeHashEquals(string leftHex, string rightHex)
    {
        var leftBytes = Encoding.ASCII.GetBytes(leftHex);
        var rightBytes = Encoding.ASCII.GetBytes(rightHex);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        string clientIp,
        CancellationToken cancellationToken)
    {
        var rawIdentifier = request.Identifier.Trim();
        var employeeCodeLookup = NormalizeEmployeeCode(rawIdentifier);
        var phoneLookup = NormalizePhone(rawIdentifier);
        var limiterKey = !string.IsNullOrEmpty(phoneLookup) && rawIdentifier.All(c => char.IsDigit(c) || c is '+' or '-' or ' ' or '(' or ')')
            ? phoneLookup
            : employeeCodeLookup;

        if (await attemptLimiter.IsBlockedAsync(clientIp, limiterKey, cancellationToken))
        {
            return new LoginResult(null, LoginFailure.TooManyAttempts);
        }

        var user = await LoadUserForLoginAsync(employeeCodeLookup, rawIdentifier, phoneLookup, cancellationToken);

        if (user is null)
        {
            // Constant-work mitigation against timing enumeration.
            _ = passwordHasher.Verify(_dummyPasswordHash, request.Password);
            await attemptLimiter.RecordFailureAsync(clientIp, limiterKey, cancellationToken);
            return new LoginResult(null, LoginFailure.Unauthorized);
        }

        var passwordValid = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (!user.IsActive || !passwordValid)
        {
            await attemptLimiter.RecordFailureAsync(clientIp, limiterKey, cancellationToken);
            return new LoginResult(null, LoginFailure.Unauthorized);
        }

        await attemptLimiter.ResetAsync(clientIp, limiterKey, cancellationToken);
        await securityStampStore.SetAsync(user.Id, user.SecurityStamp, cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.Add(RefreshTokenSession.MaxSessionLifetime);
        var familyId = Guid.CreateVersion7();
        var rawRefreshToken = GenerateOpaqueToken();
        var session = RefreshTokenSession.Issue(
            user.Id,
            familyId,
            HashToken(rawRefreshToken),
            now,
            expiresAtUtc);

        dbContext.RefreshTokenSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = BuildAuthenticationResponse(user);
        return new LoginResult(
            response,
            LoginFailure.None,
            rawRefreshToken,
            new DateTimeOffset(expiresAtUtc, TimeSpan.Zero));
    }

    public async Task<RefreshSessionResult> RefreshAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new RefreshSessionResult(null, false);
        }

        var computedHash = HashToken(rawRefreshToken.Trim());
        var now = _clock.GetUtcNow().UtcDateTime;

        await using var transaction = await BeginSerializableIfNeededAsync(cancellationToken);
        try
        {
            var session = await dbContext.RefreshTokenSessions
                .FirstOrDefaultAsync(x => x.TokenHash == computedHash, cancellationToken);

            if (session is null || !FixedTimeHashEquals(session.TokenHash, computedHash))
            {
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new RefreshSessionResult(null, false);
            }

            if (session.RevokedAtUtc is not null)
            {
                await RevokeFamilyAsync(session.FamilyId, now, "replay_detected", cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new RefreshSessionResult(null, false);
            }

            if (session.ExpiresAtUtc <= now)
            {
                await RevokeFamilyAsync(session.FamilyId, now, "expired", cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new RefreshSessionResult(null, false);
            }

            var user = await LoadUserByIdAsync(session.UserId, cancellationToken);
            if (user is null || !user.IsActive)
            {
                await RevokeFamilyAsync(session.FamilyId, now, "user_inactive", cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new RefreshSessionResult(null, false);
            }

            // Fail closed if Redis is unavailable (SecurityStampUnavailableException propagates).
            var cachedStamp = await securityStampStore.GetAsync(user.Id, cancellationToken);
            if (cachedStamp is null
                || !Guid.TryParse(cachedStamp, out var parsedStamp)
                || parsedStamp != user.SecurityStamp)
            {
                await RevokeFamilyAsync(session.FamilyId, now, "security_stamp_invalid", cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return new RefreshSessionResult(null, false);
            }

            var newRawToken = GenerateOpaqueToken();
            var replacementSession = RefreshTokenSession.Issue(
                session.UserId,
                session.FamilyId,
                HashToken(newRawToken),
                now,
                session.ExpiresAtUtc);

            session.Rotate(replacementSession.Id, now);
            dbContext.RefreshTokenSessions.Add(replacementSession);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var response = BuildAuthenticationResponse(user);
            return new RefreshSessionResult(
                response,
                true,
                newRawToken,
                new DateTimeOffset(replacementSession.ExpiresAtUtc, TimeSpan.Zero));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
            var concurrentSession = await dbContext.RefreshTokenSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenHash == computedHash, cancellationToken);

            if (concurrentSession is not null)
            {
                await RevokeFamilyAsync(concurrentSession.FamilyId, now, "concurrent_replay", cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new RefreshSessionResult(null, false);
        }
    }

    public async Task LogoutAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var computedHash = HashToken(rawRefreshToken.Trim());
        var session = await dbContext.RefreshTokenSessions
            .FirstOrDefaultAsync(x => x.TokenHash == computedHash, cancellationToken);

        if (session is null || !FixedTimeHashEquals(session.TokenHash, computedHash))
        {
            return;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        await RevokeFamilyAsync(session.FamilyId, now, "logout", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.Roles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (!user.IsActive)
        {
            throw new ForbiddenDomainException("Inactive accounts cannot change password.");
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new BusinessRuleException("Current password is incorrect.", "invalid-current-password");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        user.ChangePasswordHash(passwordHasher.HashPassword(request.NewPassword));

        var activeSessions = await dbContext.RefreshTokenSessions
            .Where(s => s.UserId == user.Id && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeSession in activeSessions)
        {
            activeSession.Revoke(now, "password_changed");
        }

        var roleSummary = string.Join(',', user.Roles.Select(r => r.Role.RoleCode).DefaultIfEmpty("USER"));
        dbContext.AuditLogs.Add(AuditLog.Append(
            user.Id,
            roleSummary.Length > 50 ? roleSummary[..50] : roleSummary,
            "ChangePassword",
            "User",
            user.Id.ToString("D"),
            null,
            "{\"passwordChanged\":true,\"revokedAllSessions\":true}",
            null,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await securityStampStore.SetAsync(user.Id, user.SecurityStamp, cancellationToken);
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginSerializableIfNeededAsync(
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    private async Task<User?> LoadUserForLoginAsync(
        string employeeCodeLookup,
        string rawIdentifier,
        string phoneLookup,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.Permissions)
            .Include(x => x.Warehouses)
            .FirstOrDefaultAsync(
                x => x.EmployeeCode == employeeCodeLookup
                     || x.Phone == rawIdentifier
                     || (phoneLookup != string.Empty && x.Phone == phoneLookup),
                cancellationToken);
    }

    private async Task<User?> LoadUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.Permissions)
            .Include(x => x.Warehouses)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        DateTime utcNow,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeRows = await dbContext.RefreshTokenSessions
            .Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var row in activeRows)
        {
            row.Revoke(utcNow, reason);
        }
    }

    private AuthenticationResponse BuildAuthenticationResponse(User user)
    {
        var roleCodes = user.Roles
            .Select(r => r.Role.RoleCode)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();

        var permissions = user.Roles
            .SelectMany(r => r.Role.Permissions)
            .Select(p => p.PermissionClaim)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var assignedWarehouses = user.Warehouses
            .Select(w => w.WarehouseId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var userDto = new AuthenticatedUserDto(
            user.Id,
            user.EmployeeCode,
            user.FullNameAr,
            user.FullNameEn,
            roleCodes,
            permissions,
            user.RepType,
            user.DefaultWarehouseId,
            assignedWarehouses);

        var accessToken = jwtTokenService.CreateAccessToken(userDto, user.SecurityStamp);
        return new AuthenticationResponse(accessToken, "Bearer", AccessTokenExpiresInSeconds, userDto);
    }
}
