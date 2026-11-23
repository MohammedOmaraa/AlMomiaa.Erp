using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMomiaa.Infrastructure.Authentication;

/// <summary>
/// Development-only local SUPER_ADMIN bootstrapper driven strictly by environment variables.
/// Never runs in Production and never embeds hardcoded passwords or users in source control.
/// </summary>
public static class DevelopmentAdminBootstrapper
{
    public static async Task EnsureDevAdminFromEnvironmentAsync(
        AlMomiaaDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var code = Environment.GetEnvironmentVariable("ALMOMIAA_DEV_ADMIN_CODE");
        var phone = Environment.GetEnvironmentVariable("ALMOMIAA_DEV_ADMIN_PHONE");
        var password = Environment.GetEnvironmentVariable("ALMOMIAA_DEV_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await IdentitySeedData.EnsureCanonicalRolesAndPermissionsAsync(dbContext, cancellationToken);

        var normalizedCode = AuthenticationService.NormalizeEmployeeCode(code);
        var normalizedPhone = AuthenticationService.NormalizePhone(phone);

        var superAdminRole = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.SuperAdmin, cancellationToken);

        if (superAdminRole is null)
        {
            return;
        }

        var existingUser = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.EmployeeCode == normalizedCode || u.Phone == normalizedPhone, cancellationToken);

        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                existingUser.Activate();
            }

            existingUser.AssignRole(superAdminRole.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = User.Create(
            employeeCode: normalizedCode,
            fullNameAr: "مدير النظام المحلي",
            fullNameEn: "Local Super Admin",
            phone: normalizedPhone,
            email: null,
            passwordHash: passwordHasher.HashPassword(password),
            repType: null,
            defaultWarehouseId: null,
            isActive: true);

        user.AssignRole(superAdminRole.Id);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Development bootstrap created local SUPER_ADMIN employee '{EmployeeCode}'.", normalizedCode);
    }
}
