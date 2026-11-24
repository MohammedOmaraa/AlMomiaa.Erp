namespace AlMomiaa.Domain.Identity;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FullNameAr { get; private set; } = string.Empty;
    public string? FullNameEn { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string? RepType { get; private set; }
    public int? DefaultWarehouseId { get; private set; }
    public Guid SecurityStamp { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public ICollection<UserRole> Roles { get; private init; } = new List<UserRole>();
    public ICollection<UserWarehouse> Warehouses { get; private init; } = new List<UserWarehouse>();

    public static User Create(
        string employeeCode,
        string fullNameAr,
        string? fullNameEn,
        string phone,
        string? email,
        string passwordHash,
        string? repType = null,
        int? defaultWarehouseId = null,
        bool isActive = true,
        DateTime? createdAtUtc = null,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullNameAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = id ?? Guid.CreateVersion7(),
            EmployeeCode = employeeCode.Trim().ToUpperInvariant(),
            FullNameAr = fullNameAr.Trim(),
            FullNameEn = string.IsNullOrWhiteSpace(fullNameEn) ? null : fullNameEn.Trim(),
            Phone = phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = passwordHash,
            RepType = string.IsNullOrWhiteSpace(repType) ? null : repType.Trim(),
            DefaultWarehouseId = defaultWarehouseId,
            SecurityStamp = Guid.CreateVersion7(),
            IsActive = isActive,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };
    }

    public void RotateSecurityStamp()
    {
        SecurityStamp = Guid.CreateVersion7();
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        RotateSecurityStamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        RotateSecurityStamp();
    }

    public void Activate()
    {
        IsActive = true;
        RotateSecurityStamp();
    }

    public void AssignRole(int roleId)
    {
        if (Roles.All(r => r.RoleId != roleId))
        {
            Roles.Add(UserRole.Create(Id, roleId));
        }
    }

    public void AssignWarehouse(int warehouseId)
    {
        if (Warehouses.All(w => w.WarehouseId != warehouseId))
        {
            Warehouses.Add(UserWarehouse.Create(Id, warehouseId));
        }
    }

    public void SetDefaultWarehouse(int warehouseId)
    {
        DefaultWarehouseId = warehouseId;
        AssignWarehouse(warehouseId);
    }

    public void ReplaceRoles(IEnumerable<int> roleIds)
    {
        Roles.Clear();
        foreach (var roleId in roleIds.Distinct())
        {
            Roles.Add(UserRole.Create(Id, roleId));
        }

        RotateSecurityStamp();
    }

    public void ReplaceWarehouses(IEnumerable<int> warehouseIds, int? defaultWarehouseId)
    {
        Warehouses.Clear();
        var merged = warehouseIds.Distinct().ToList();
        if (defaultWarehouseId.HasValue && !merged.Contains(defaultWarehouseId.Value))
        {
            merged.Add(defaultWarehouseId.Value);
        }

        foreach (var warehouseId in merged)
        {
            Warehouses.Add(UserWarehouse.Create(Id, warehouseId));
        }

        DefaultWarehouseId = defaultWarehouseId;
        RotateSecurityStamp();
    }
}
