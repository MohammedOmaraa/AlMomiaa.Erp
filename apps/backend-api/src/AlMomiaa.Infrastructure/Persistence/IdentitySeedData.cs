using AlMomiaa.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence;

public static class IdentitySeedData
{
    /// <summary>
    /// Idempotently seeds canonical roles and role-permission mappings from 07-Security.md.
    /// Never creates default users or passwords.
    /// </summary>
    public static async Task EnsureCanonicalRolesAndPermissionsAsync(
        AlMomiaaDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var existingRoles = await dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        foreach (var definition in CanonicalRolesAndPermissions.All)
        {
            if (!existingRoles.ContainsKey(definition.Id))
            {
                dbContext.Roles.Add(Role.Create(
                    definition.Id,
                    definition.RoleCode,
                    definition.NameAr,
                    definition.NameEn));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingPermissions = (await dbContext.RolePermissions
            .AsNoTracking()
            .Select(rp => new { rp.RoleId, rp.PermissionClaim })
            .ToListAsync(cancellationToken))
            .Select(x => (x.RoleId, x.PermissionClaim))
            .ToHashSet();

        foreach (var definition in CanonicalRolesAndPermissions.All)
        {
            foreach (var permission in definition.Permissions)
            {
                if (!existingPermissions.Contains((definition.Id, permission)))
                {
                    dbContext.RolePermissions.Add(RolePermission.Create(definition.Id, permission));
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
