using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Customers;
using AlMomiaa.Application.Users;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Finance;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence.Services;

public sealed class IamModuleService(
    AlMomiaaDbContext db,
    IPasswordHasher passwordHasher,
    ISecurityStampStore securityStampStore,
    ICurrentUserService currentUser,
    TimeProvider? clock = null) : IIamModuleService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<PagedResult<UserSummaryDto>> GetUsersAsync(GetUsersQuery query, CancellationToken ct)
    {
        var baseQuery = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            baseQuery = baseQuery.Where(u =>
                u.EmployeeCode.Contains(term) ||
                u.FullNameAr.Contains(term) ||
                (u.FullNameEn != null && u.FullNameEn.Contains(term)) ||
                u.Phone.Contains(term));
        }

        if (query.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleCode))
        {
            var code = query.RoleCode.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(u => u.Roles.Any(r => r.Role.RoleCode == code));
        }

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.EmployeeCode,
                u.FullNameAr,
                u.FullNameEn,
                u.Phone,
                u.Email,
                u.RepType,
                u.DefaultWarehouseId,
                u.IsActive,
                u.CreatedAtUtc,
                u.Roles.Select(r => r.Role.RoleCode).ToArray(),
                u.Warehouses.Select(w => w.WarehouseId).ToArray()))
            .ToListAsync(ct);

        var nextCursor = (query.Page * query.PageSize) < total ? (query.Page + 1).ToString() : null;
        return new PagedResult<UserSummaryDto>(items, total, nextCursor);
    }

    public async Task<UserDetailDto> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles).ThenInclude(ur => ur.Role).ThenInclude(r => r.Permissions)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        return MapUserDetail(user);
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var now = _clock.GetUtcNow().UtcDateTime;
        var normalizedCode = AuthenticationService.NormalizeEmployeeCode(req.EmployeeCode);
        var normalizedPhone = AuthenticationService.NormalizePhone(req.Phone);

        if (await db.Users.AsNoTracking().AnyAsync(u => u.EmployeeCode == normalizedCode, ct))
            throw new ConflictException($"EmployeeCode '{normalizedCode}' already exists.");
        if (await db.Users.AsNoTracking().AnyAsync(u => u.Phone == normalizedPhone, ct))
            throw new ConflictException($"Phone '{normalizedPhone}' already exists.");

        var roleIds = req.RoleIds.Distinct().ToArray();
        var roles = await db.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);
        if (roles.Count != roleIds.Length)
            throw new NotFoundException("Role", string.Join(',', roleIds));

        var warehouseIds = (req.AssignedWarehouseIds ?? []).Distinct().ToList();
        if (req.DefaultWarehouseId.HasValue && !warehouseIds.Contains(req.DefaultWarehouseId.Value))
        {
            warehouseIds.Add(req.DefaultWarehouseId.Value);
        }

        var isWarehouseKeeper = roles.Any(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.WarehouseKeeper);
        int? effectiveDefaultWarehouseId = req.DefaultWarehouseId;
        if (isWarehouseKeeper)
        {
            if (warehouseIds.Count != 1)
            {
                throw new BusinessRuleException(
                    "Warehouse Keeper (WH_KEEPER) must be assigned to exactly one warehouse.",
                    "wh-keeper-single-warehouse");
            }

            effectiveDefaultWarehouseId = warehouseIds[0];
        }

        if (warehouseIds.Count > 0)
        {
            var foundCount = await db.Warehouses.AsNoTracking().CountAsync(w => warehouseIds.Contains(w.Id), ct);
            if (foundCount != warehouseIds.Count)
                throw new NotFoundException("Warehouse", string.Join(',', warehouseIds));
        }

        var user = User.Create(
            normalizedCode,
            req.FullNameAr,
            req.FullNameEn,
            normalizedPhone,
            req.Email,
            passwordHasher.HashPassword(req.Password),
            req.RepType,
            effectiveDefaultWarehouseId,
            isActive: true,
            createdAtUtc: now);

        foreach (var roleId in roleIds)
        {
            user.AssignRole(roleId);
        }

        foreach (var whId in warehouseIds)
        {
            user.AssignWarehouse(whId);
        }

        db.Users.Add(user);

        var isSalesRep = roles.Any(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.SalesRep);
        if (isSalesRep)
        {
            var walletCode = $"WALLET-{normalizedCode}";
            if (walletCode.Length > 30) walletCode = walletCode[..30];
            db.FinancialAccounts.Add(FinancialAccount.Create(
                walletCode,
                $"عهدة المندوب {req.FullNameAr}",
                $"Rep Custody Wallet - {req.FullNameEn ?? normalizedCode}",
                "RepCustodyWallet",
                ownerRepId: user.Id));
        }

        Warehouse? createdVan = null;
        if (req.CreateMobileVan)
        {
            var vanCode = $"VAN-{normalizedCode}";
            if (vanCode.Length > 20) vanCode = vanCode[..20];
            createdVan = Warehouse.Create(
                vanCode,
                $"سيارة المندوب {req.FullNameAr}",
                $"Mobile Van - {req.FullNameEn ?? normalizedCode}",
                "MobileVan",
                assignedRepId: user.Id,
                vehiclePlateNumber: req.VehiclePlateNumber);
            db.Warehouses.Add(createdVan);
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateUser", "User", user.Id.ToString("D"), null, new { user.EmployeeCode, RoleCodes = roles.Select(r => r.RoleCode) }, now);
        await db.SaveChangesAsync(ct);

        if (createdVan is not null)
        {
            user.SetDefaultWarehouse(createdVan.Id);
            if (!warehouseIds.Contains(createdVan.Id))
            {
                warehouseIds.Add(createdVan.Id);
            }
            await db.SaveChangesAsync(ct);
        }

        return new UserSummaryDto(
            user.Id,
            user.EmployeeCode,
            user.FullNameAr,
            user.FullNameEn,
            user.Phone,
            user.Email,
            user.RepType,
            user.DefaultWarehouseId,
            user.IsActive,
            user.CreatedAtUtc,
            roles.Select(r => r.RoleCode).OrderBy(x => x).ToArray(),
            warehouseIds.OrderBy(x => x).ToArray());
    }

    public async Task<UserSummaryDto> UpdateUserStatusAsync(UpdateUserStatusCommand command, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(r => r.Role)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        var oldState = new { user.IsActive, user.SecurityStamp };
        if (command.IsActive) user.Activate();
        else user.Deactivate();

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateUserStatus", "User", user.Id.ToString("D"), oldState, new { user.IsActive, user.SecurityStamp }, now);
        await db.SaveChangesAsync(ct);
        await securityStampStore.InvalidateAsync(user.Id, ct);

        return new UserSummaryDto(
            user.Id, user.EmployeeCode, user.FullNameAr, user.FullNameEn, user.Phone, user.Email,
            user.RepType, user.DefaultWarehouseId, user.IsActive, user.CreatedAtUtc,
            user.Roles.Select(r => r.Role.RoleCode).ToArray(),
            user.Warehouses.Select(w => w.WarehouseId).ToArray());
    }

    public async Task<UserDetailDto> UpdateUserRolesAsync(UpdateUserRolesCommand command, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role).ThenInclude(r => r.Permissions)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        var nextRoleIds = command.RoleIds.Distinct().ToArray();
        var roles = await db.Roles
            .Include(r => r.Permissions)
            .Where(r => nextRoleIds.Contains(r.Id))
            .ToListAsync(ct);

        if (roles.Count != nextRoleIds.Length)
        {
            throw new NotFoundException("Role", string.Join(',', nextRoleIds));
        }

        var oldRoleCodes = user.Roles.Select(ur => ur.Role.RoleCode).OrderBy(x => x).ToArray();
        var newRoleCodes = roles.Select(r => r.RoleCode).OrderBy(x => x).ToArray();

        if (roles.Any(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.WarehouseKeeper))
        {
            var assignedIds = user.Warehouses.Select(w => w.WarehouseId).Distinct().ToList();
            if (assignedIds.Count != 1)
            {
                throw new BusinessRuleException(
                    "Warehouse Keeper (WH_KEEPER) must have exactly one assigned warehouse before assigning this role.",
                    "wh-keeper-single-warehouse");
            }

            if (user.DefaultWarehouseId != assignedIds[0])
            {
                user.SetDefaultWarehouse(assignedIds[0]);
            }
        }

        user.ReplaceRoles(nextRoleIds);

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateUserRoles", "User", user.Id.ToString("D"), oldRoleCodes, newRoleCodes, now);
        await db.SaveChangesAsync(ct);
        await securityStampStore.InvalidateAsync(user.Id, ct);

        return await GetUserByIdAsync(user.Id, ct);
    }

    public async Task<UserDetailDto> UpdateUserWarehousesAsync(UpdateUserWarehousesCommand command, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Roles).ThenInclude(ur => ur.Role).ThenInclude(r => r.Permissions)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        var merged = command.AssignedWarehouseIds.Distinct().ToList();
        if (command.DefaultWarehouseId.HasValue && !merged.Contains(command.DefaultWarehouseId.Value))
        {
            merged.Add(command.DefaultWarehouseId.Value);
        }

        var effectiveDefaultWarehouseId = command.DefaultWarehouseId;
        var isWarehouseKeeper = user.Roles.Any(ur => ur.Role.RoleCode == CanonicalRolesAndPermissions.Roles.WarehouseKeeper);
        if (isWarehouseKeeper)
        {
            if (merged.Count != 1)
            {
                throw new BusinessRuleException(
                    "Warehouse Keeper (WH_KEEPER) must be assigned to exactly one warehouse.",
                    "wh-keeper-single-warehouse");
            }

            effectiveDefaultWarehouseId = merged[0];
        }

        if (merged.Count > 0)
        {
            var foundCount = await db.Warehouses.AsNoTracking().CountAsync(w => merged.Contains(w.Id), ct);
            if (foundCount != merged.Count)
            {
                throw new NotFoundException("Warehouse", string.Join(',', merged));
            }
        }

        var oldState = new { user.DefaultWarehouseId, Warehouses = user.Warehouses.Select(w => w.WarehouseId).OrderBy(x => x).ToArray() };
        user.ReplaceWarehouses(merged, effectiveDefaultWarehouseId);

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateUserWarehouses", "User", user.Id.ToString("D"), oldState, new { user.DefaultWarehouseId, Warehouses = merged.OrderBy(x => x).ToArray() }, now);
        await db.SaveChangesAsync(ct);
        await securityStampStore.InvalidateAsync(user.Id, ct);

        return await GetUserByIdAsync(user.Id, ct);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        return await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(
                r.Id,
                r.RoleCode,
                r.NameAr,
                r.NameEn,
                r.Permissions.Select(p => p.PermissionClaim).OrderBy(c => c).ToArray()))
            .ToListAsync(ct);
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var normalizedCode = req.RoleCode.Trim().ToUpperInvariant();
        if (await db.Roles.AsNoTracking().AnyAsync(r => r.RoleCode == normalizedCode, ct))
        {
            throw new ConflictException($"RoleCode '{normalizedCode}' already exists.");
        }

        var role = Role.CreateCustom(normalizedCode, req.NameAr, req.NameEn);
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        var claims = req.Permissions.Select(p => p.Trim()).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
        foreach (var claim in claims)
        {
            db.RolePermissions.Add(RolePermission.Create(role.Id, claim));
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateRole", "Role", role.Id.ToString(), null, new { role.RoleCode, Permissions = claims }, now);
        await db.SaveChangesAsync(ct);

        return new RoleDto(role.Id, role.RoleCode, role.NameAr, role.NameEn, claims);
    }

    private static UserDetailDto MapUserDetail(User user) =>
        new(
            user.Id,
            user.EmployeeCode,
            user.FullNameAr,
            user.FullNameEn,
            user.Phone,
            user.Email,
            user.RepType,
            user.DefaultWarehouseId,
            user.IsActive,
            user.CreatedAtUtc,
            user.Roles.Select(ur => ur.Role.RoleCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray(),
            user.Warehouses.Select(w => w.WarehouseId).Distinct().OrderBy(x => x).ToArray(),
            user.Roles.SelectMany(ur => ur.Role.Permissions).Select(p => p.PermissionClaim).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray());

    public async Task<RoleDto> UpdateRolePermissionsAsync(UpdateRolePermissionsCommand command, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, ct)
            ?? throw new NotFoundException("Role", command.RoleId);

        var oldPermissions = role.Permissions.Select(p => p.PermissionClaim).OrderBy(x => x).ToArray();
        var nextPermissions = command.Permissions.Select(p => p.Trim()).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();

        db.RolePermissions.RemoveRange(role.Permissions);
        foreach (var claim in nextPermissions)
        {
            db.RolePermissions.Add(RolePermission.Create(role.Id, claim));
        }

        var affectedUsers = await db.Users
            .Where(u => u.Roles.Any(ur => ur.RoleId == role.Id))
            .ToListAsync(ct);

        foreach (var affectedUser in affectedUsers)
        {
            affectedUser.RotateSecurityStamp();
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateRolePermissions", "Role", role.Id.ToString(), oldPermissions, nextPermissions, now);
        await db.SaveChangesAsync(ct);

        foreach (var affectedUser in affectedUsers)
        {
            await securityStampStore.InvalidateAsync(affectedUser.Id, ct);
        }

        return new RoleDto(role.Id, role.RoleCode, role.NameAr, role.NameEn, nextPermissions);
    }
}

public sealed class CustomersModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider? clock = null) : ICustomersModuleService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private bool IsScopedSalesRep() =>
        currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SalesRep)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SuperAdmin)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.ExecutiveManager)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SalesManager)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.Accountant);

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(GetCustomersQuery query, CancellationToken ct)
    {
        var q = db.Customers.AsNoTracking().AsQueryable();

        if (IsScopedSalesRep())
        {
            q = q.Where(c => c.AssignedRepId == currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(c => c.CustomerCode.Contains(term) || c.NameAr.Contains(term) || (c.NameEn != null && c.NameEn.Contains(term)) || c.Phone.Contains(term));
        }

        if (query.GradeId.HasValue) q = q.Where(c => c.GradeId == query.GradeId.Value);
        if (!string.IsNullOrWhiteSpace(query.City)) q = q.Where(c => c.City == query.City.Trim());
        if (query.IsActive.HasValue) q = q.Where(c => c.IsActive == query.IsActive.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(c => c.CustomerCode)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CustomerDto(
                c.Id, c.CustomerCode, c.NameAr, c.NameEn, c.Phone, c.Address, c.City, c.Category,
                c.VatNumber, c.GradeId, c.Grade.GradeCode, c.PriceListId, c.CustomPaymentDays,
                c.CreditLimit, c.CurrentBalance, c.AssignedRepId, c.Latitude, c.Longitude, c.IsActive,
                c.AssignedRep.EmployeeCode, c.AssignedRep.FullNameAr, c.AssignedRep.FullNameEn))
            .ToListAsync(ct);

        var nextCursor = (query.Page * query.PageSize) < total ? (query.Page + 1).ToString() : null;
        return new PagedResult<CustomerDto>(items, total, nextCursor);
    }

    public async Task<IReadOnlyList<SalesRepOptionDto>> GetSalesRepsAsync(CancellationToken ct)
    {
        var countsByRep = await db.Customers
            .AsNoTracking()
            .GroupBy(c => c.AssignedRepId)
            .Select(g => new { RepId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RepId, x => x.Count, ct);

        var repUsers = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Roles.Any(ur =>
                ur.Role.RoleCode == CanonicalRolesAndPermissions.Roles.SalesRep
                || ur.Role.Permissions.Any(p => p.PermissionClaim == CanonicalRolesAndPermissions.Permissions.OrdersCreate)))
            .Select(u => new
            {
                u.Id,
                u.EmployeeCode,
                u.FullNameAr,
                u.FullNameEn,
                u.Phone,
                u.RepType,
                IsSalesRep = u.Roles.Any(ur => ur.Role.RoleCode == CanonicalRolesAndPermissions.Roles.SalesRep)
            })
            .OrderByDescending(u => u.IsSalesRep)
            .ThenBy(u => u.FullNameAr)
            .ToListAsync(ct);

        return repUsers
            .Select(u => new SalesRepOptionDto(
                u.Id,
                u.EmployeeCode,
                u.FullNameAr,
                u.FullNameEn,
                u.Phone,
                u.RepType,
                countsByRep.TryGetValue(u.Id, out var count) ? count : 0))
            .ToList();
    }

    public async Task<CustomerDto> AssignCustomerRepAsync(AssignCustomerRepCommand command, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Grade)
            .FirstOrDefaultAsync(c => c.Id == command.CustomerId, ct)
            ?? throw new NotFoundException("Customer", command.CustomerId);

        var assignedRep = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == command.AssignedRepId, ct)
            ?? throw new NotFoundException("User (AssignedRep)", command.AssignedRepId);

        if (!assignedRep.IsActive)
            throw new BusinessRuleException($"Assigned representative '{assignedRep.EmployeeCode}' is inactive.");

        var previousRepId = customer.AssignedRepId;
        customer.AssignRepresentative(assignedRep.Id);

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(
            db,
            currentUser,
            "AssignCustomerRep",
            "Customer",
            customer.Id.ToString("D"),
            new { AssignedRepId = previousRepId },
            new { AssignedRepId = assignedRep.Id, assignedRep.EmployeeCode },
            now);

        await db.SaveChangesAsync(ct);

        return new CustomerDto(
            customer.Id, customer.CustomerCode, customer.NameAr, customer.NameEn, customer.Phone,
            customer.Address, customer.City, customer.Category, customer.VatNumber, customer.GradeId,
            customer.Grade.GradeCode, customer.PriceListId, customer.CustomPaymentDays, customer.CreditLimit,
            customer.CurrentBalance, customer.AssignedRepId, customer.Latitude, customer.Longitude, customer.IsActive,
            assignedRep.EmployeeCode, assignedRep.FullNameAr, assignedRep.FullNameEn);
    }

    public async Task<BulkAssignCustomersRepResponse> BulkAssignCustomersRepAsync(BulkAssignCustomersRepCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var assignedRep = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == req.AssignedRepId, ct)
            ?? throw new NotFoundException("User (AssignedRep)", req.AssignedRepId);

        if (!assignedRep.IsActive)
            throw new BusinessRuleException($"Assigned representative '{assignedRep.EmployeeCode}' is inactive.");

        var distinctCustomerIds = req.CustomerIds.Distinct().ToArray();
        var customers = await db.Customers
            .Where(c => distinctCustomerIds.Contains(c.Id))
            .ToListAsync(ct);

        if (customers.Count != distinctCustomerIds.Length)
        {
            var foundSet = customers.Select(c => c.Id).ToHashSet();
            var missingId = distinctCustomerIds.First(id => !foundSet.Contains(id));
            throw new NotFoundException("Customer", missingId);
        }

        foreach (var customer in customers)
        {
            customer.AssignRepresentative(assignedRep.Id);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        AuditAndOutboxHelper.AppendAudit(
            db,
            currentUser,
            "BulkAssignCustomersRep",
            "Customer",
            "BULK",
            null,
            new { AssignedRepId = assignedRep.Id, assignedRep.EmployeeCode, CustomerIds = distinctCustomerIds },
            now);

        await db.SaveChangesAsync(ct);

        return new BulkAssignCustomersRepResponse(
            assignedRep.Id,
            assignedRep.EmployeeCode,
            assignedRep.FullNameAr,
            customers.Count,
            distinctCustomerIds);
    }

    public async Task<CustomerCreditStatusDto> GetCustomerCreditStatusAsync(Guid customerId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var customer = await db.Customers
            .AsNoTracking()
            .Include(c => c.Grade)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw new NotFoundException("Customer", customerId);

        if (IsScopedSalesRep() && customer.AssignedRepId != currentUser.UserId)
            throw new ForbiddenDomainException("Sales Representatives can only inspect credit status for their own assigned customers.");

        var overdueDebt = await db.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.PaymentStatus != "Paid" && i.DueDateUtc < now)
            .SumAsync(i => (decimal?)(i.TotalAmount - i.PaidAmount), ct) ?? 0.00m;

        var recentOrders = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(10)
            .Select(o => new RecentCustomerOrderDto(o.Id, o.OrderNumber, o.Status, o.TotalAmount, o.CreatedAtUtc))
            .ToListAsync(ct);

        var effectivePaymentDays = customer.CustomPaymentDays ?? customer.Grade.DefaultPaymentDays;
        return new CustomerCreditStatusDto(
            customer.Id,
            customer.CustomerCode,
            customer.NameAr,
            customer.Grade.GradeCode,
            customer.Grade.AllowCredit,
            effectivePaymentDays,
            customer.PriceListId,
            customer.CreditLimit,
            customer.CurrentBalance,
            overdueDebt,
            recentOrders);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var normalizedCode = req.CustomerCode.Trim().ToUpperInvariant();
        if (await db.Customers.AsNoTracking().AnyAsync(c => c.CustomerCode == normalizedCode, ct))
            throw new ConflictException($"CustomerCode '{normalizedCode}' already exists.");

        var grade = await db.CustomerGrades.AsNoTracking().FirstOrDefaultAsync(g => g.Id == req.GradeId, ct)
            ?? throw new NotFoundException("CustomerGrade", req.GradeId);

        if (req.PriceListId.HasValue && !await db.PriceLists.AsNoTracking().AnyAsync(p => p.Id == req.PriceListId.Value, ct))
            throw new NotFoundException("PriceList", req.PriceListId.Value);

        var assignedRep = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == req.AssignedRepId, ct)
            ?? throw new NotFoundException("User (AssignedRep)", req.AssignedRepId);

        if (!assignedRep.IsActive)
            throw new BusinessRuleException($"Assigned representative '{assignedRep.EmployeeCode}' is inactive.");

        var customer = Customer.Create(
            normalizedCode, req.NameAr, req.NameEn, req.Phone, req.Address, req.City ?? "Riyadh",
            req.Category, req.VatNumber, req.GradeId, req.PriceListId, req.CustomPaymentDays,
            req.CreditLimit, req.AssignedRepId, req.Latitude, req.Longitude);

        db.Customers.Add(customer);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateCustomer", "Customer", customer.Id.ToString("D"), null, new { customer.CustomerCode, customer.GradeId, customer.CreditLimit, customer.AssignedRepId }, _clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);

        return new CustomerDto(
            customer.Id, customer.CustomerCode, customer.NameAr, customer.NameEn, customer.Phone,
            customer.Address, customer.City, customer.Category, customer.VatNumber, customer.GradeId,
            grade.GradeCode, customer.PriceListId, customer.CustomPaymentDays, customer.CreditLimit,
            customer.CurrentBalance, customer.AssignedRepId, customer.Latitude, customer.Longitude, customer.IsActive,
            assignedRep.EmployeeCode, assignedRep.FullNameAr, assignedRep.FullNameEn);
    }

    public async Task<IReadOnlyList<CustomerGradeDto>> GetGradesAsync(CancellationToken ct)
    {
        return await db.CustomerGrades
            .AsNoTracking()
            .OrderBy(g => g.GradeCode)
            .Select(g => new CustomerGradeDto(g.Id, g.GradeCode, g.DefaultPaymentDays, g.AllowCredit, g.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CustomerGradeDto>> UpdateGradesAsync(UpdateCustomerGradesCommand command, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var grades = await db.CustomerGrades.ToListAsync(ct);

        foreach (var item in command.Request.Grades)
        {
            var code = item.GradeCode.Trim().ToUpperInvariant();
            var grade = grades.FirstOrDefault(g => g.GradeCode == code)
                ?? throw new NotFoundException("CustomerGrade", code);
            grade.UpdatePolicy(item.DefaultPaymentDays, item.AllowCredit, now);
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateCustomerGrades", "CustomerGrade", "ALL", null, command.Request.Grades, now);
        await db.SaveChangesAsync(ct);

        return grades.OrderBy(g => g.GradeCode)
            .Select(g => new CustomerGradeDto(g.Id, g.GradeCode, g.DefaultPaymentDays, g.AllowCredit, g.UpdatedAtUtc))
            .ToList();
    }
}
