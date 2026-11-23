using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AlMomiaa.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 10;
}

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    TimeProvider? timeProvider = null) : IJwtTokenService
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public string CreateAccessToken(AuthenticatedUserDto user, Guid securityStamp)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Issuer)
            || string.IsNullOrWhiteSpace(settings.Audience)
            || string.IsNullOrWhiteSpace(settings.SigningKey)
            || Encoding.UTF8.GetByteCount(settings.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT security configuration is incomplete or signing key is shorter than 32 bytes.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString("D")),
            new("UserId", user.Id.ToString("D")),
            new("EmployeeCode", user.EmployeeCode),
            new("SecurityStamp", securityStamp.ToString("D")),
            new("RoleCodes", string.Join(',', user.RoleCodes)),
            new("RepType", user.RepType ?? string.Empty),
            new("DefaultWarehouseId", user.DefaultWarehouseId?.ToString() ?? string.Empty),
            new("AssignedWarehouseIds", string.Join(',', user.AssignedWarehouseIds))
        };

        foreach (var roleCode in user.RoleCodes)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
        }

        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(settings.AccessTokenMinutes > 0 ? settings.AccessTokenMinutes : 10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
