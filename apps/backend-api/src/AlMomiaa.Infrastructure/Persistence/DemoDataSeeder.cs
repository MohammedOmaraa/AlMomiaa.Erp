using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Finance;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Domain.Sales;
using AlMomiaa.Domain.SystemOps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMomiaa.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        AlMomiaaDbContext db,
        IPasswordHasher passwordHasher,
        string? defaultPassword,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(defaultPassword) || defaultPassword.Trim().Length < 8)
        {
            throw new InvalidOperationException(
                "Demo:DefaultPassword must be configured via environment variable (at least 8 characters) when Demo:SeedEnabled=true.");
        }

        var now = DateTime.UtcNow;
        var hashedPassword = passwordHasher.HashPassword(defaultPassword.Trim());

        // 1. Ensure Demo Warehouses
        var mainWh = await EnsureWarehouseAsync(db, "WH-RYD-MAIN", "المستودع الرئيسي — الرياض", "Riyadh Main Warehouse", "MainWarehouse", "Riyadh", ct);
        var jedWh = await EnsureWarehouseAsync(db, "WH-JED-BR", "مستودع فرع جدة", "Jeddah Branch Warehouse", "BranchWarehouse", "Jeddah", ct);
        await EnsureWarehouseAsync(db, "VAN-RYD-01", "سيارة توزيع الرياض 01", "Riyadh Delivery Van 01", "MobileVan", "Riyadh", ct);

        // 2. Load Canonical Roles
        var roles = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.RoleCode, r => r.Id, StringComparer.OrdinalIgnoreCase, ct);
        if (!roles.TryGetValue(CanonicalRolesAndPermissions.Roles.SuperAdmin, out var superAdminRoleId)
            || !roles.TryGetValue(CanonicalRolesAndPermissions.Roles.SalesManager, out var salesMgrRoleId)
            || !roles.TryGetValue(CanonicalRolesAndPermissions.Roles.SalesRep, out var salesRepRoleId)
            || !roles.TryGetValue(CanonicalRolesAndPermissions.Roles.WarehouseKeeper, out var whKeeperRoleId))
        {
            throw new InvalidOperationException("Canonical roles are missing from the database. Run migrations before demo seeding.");
        }

        // 3. Ensure 4 Demo Role Accounts (WHK-DEMO strictly assigned to 1 warehouse)
        var adminUser = await EnsureUserAsync(
            db, "ADMIN-DEMO", "مدير النظام (تجريبي)", "Demo Super Admin", "+966500000001", "admin.demo@almomiaa.sa",
            hashedPassword, null, null, superAdminRoleId, [], ct);

        await EnsureUserAsync(
            db, "MGR-DEMO", "مدير المبيعات (تجريبي)", "Demo Sales Manager", "+966500000002", "manager.demo@almomiaa.sa",
            hashedPassword, null, mainWh.Id, salesMgrRoleId, [mainWh.Id, jedWh.Id], ct);

        var repUser = await EnsureUserAsync(
            db, "REP-DEMO", "مندوب المبيعات (تجريبي)", "Demo Sales Rep", "+966500000003", "rep.demo@almomiaa.sa",
            hashedPassword, "PreSales", mainWh.Id, salesRepRoleId, [mainWh.Id], ct);

        await EnsureUserAsync(
            db, "WHK-DEMO", "أمين المستودع (تجريبي)", "Demo Warehouse Keeper", "+966500000004", "keeper.demo@almomiaa.sa",
            hashedPassword, null, mainWh.Id, whKeeperRoleId, [mainWh.Id], ct);

        // 4. Categories & Brands
        var bevCat = await EnsureCategoryAsync(db, "مشروبات وعصائر", "Beverages & Juices", 1, ct);
        var foodCat = await EnsureCategoryAsync(db, "مواد غذائية أساسية", "Essential Foodstuffs", 2, ct);

        var goldBrand = await EnsureBrandAsync(db, "المومياء الذهبية", "AlMomiaa Gold", ct);
        var oasisBrand = await EnsureBrandAsync(db, "واحة الرياض", "Riyadh Oasis", ct);

        // 5. Default Price List
        var priceList = await db.PriceLists.FirstOrDefaultAsync(p => p.PriceListCode == "PL-WHOLESALE-DEMO", ct);
        if (priceList is null)
        {
            priceList = PriceList.Create("PL-WHOLESALE-DEMO", "قائمة أسعار الجملة التجريبية", "Demo Wholesale Price List", true, now, null);
            db.PriceLists.Add(priceList);
            await db.SaveChangesAsync(ct);
        }

        // 6. Demo Master Products with Multi-Unit Definitions & Multi-Warehouse Opening Stocks
        await EnsureProductWithMultiWarehouseStockAsync(
            db, adminUser.Id, now,
            sku: "ALM-WTR-24", barcode: "6281001100001",
            nameAr: "مياه شرب نقية 330 مل", nameEn: "Pure Drinking Water 330ml",
            categoryId: bevCat.Id, brandId: oasisBrand.Id, baseUnit: "Carton",
            wac: 10m, minFloor: 13m, wholesale: 16m, retail: 19m,
            baseUnitAr: "كرتون (40 عبوة)", baseUnitEn: "Carton (40 bottles)",
            extraUnit: ("طبلية (60 كرتون)", "Pallet (60 cartons)", 60m, "6281001100060", 920m, 1100m, 780m),
            warehouseStocks: [(mainWh.Id, 250m, 30m), (jedWh.Id, 110m, 20m)],
            ct: ct);

        await EnsureProductWithMultiWarehouseStockAsync(
            db, adminUser.Id, now,
            sku: "ALM-JCE-12", barcode: "6281001100002",
            nameAr: "عصير برتقال طبيعي 1 لتر", nameEn: "Natural Orange Juice 1L",
            categoryId: bevCat.Id, brandId: oasisBrand.Id, baseUnit: "Piece",
            wac: 6m, minFloor: 8m, wholesale: 10m, retail: 13m,
            baseUnitAr: "حبة", baseUnitEn: "Piece",
            extraUnit: ("كرتون (12 حبة)", "Carton (12 pcs)", 12m, "6281001100012", 115m, 150m, 96m),
            warehouseStocks: [(mainWh.Id, 180m, 25m), (jedWh.Id, 75m, 15m)],
            ct: ct);

        await EnsureProductWithMultiWarehouseStockAsync(
            db, adminUser.Id, now,
            sku: "ALM-OIL-01", barcode: "6281001100003",
            nameAr: "زيت زيتون بكر ممتاز 1 لتر", nameEn: "Extra Virgin Olive Oil 1L",
            categoryId: foodCat.Id, brandId: goldBrand.Id, baseUnit: "Piece",
            wac: 24m, minFloor: 30m, wholesale: 36m, retail: 44m,
            baseUnitAr: "حبة", baseUnitEn: "Piece",
            extraUnit: ("كرتون (12 حبة)", "Carton (12 pcs)", 12m, "6281001100032", 420m, 510m, 360m),
            warehouseStocks: [(mainWh.Id, 120m, 15m), (jedWh.Id, 60m, 10m)],
            ct: ct);

        await EnsureProductWithMultiWarehouseStockAsync(
            db, adminUser.Id, now,
            sku: "ALM-RCE-10", barcode: "6281001100004",
            nameAr: "أرز بسمتي عنبر فاخر 10 كجم", nameEn: "Premium Basmati Rice 10kg",
            categoryId: foodCat.Id, brandId: goldBrand.Id, baseUnit: "Box",
            wac: 52m, minFloor: 62m, wholesale: 72m, retail: 85m,
            baseUnitAr: "كيس / عبوة 10 كجم", baseUnitEn: "Bag / Pack 10kg",
            extraUnit: ("طبلية (40 كيس)", "Pallet (40 bags)", 40m, "6281001100040", 2800m, 3300m, 2480m),
            warehouseStocks: [(mainWh.Id, 95m, 20m), (jedWh.Id, 45m, 10m)],
            ct: ct);

        // 7. Demo Customers Linked to REP-DEMO
        var grades = await db.CustomerGrades.AsNoTracking().ToDictionaryAsync(g => g.GradeCode, g => g.Id, StringComparer.OrdinalIgnoreCase, ct);
        if (grades.TryGetValue("A", out var gradeA) && grades.TryGetValue("B", out var gradeB) && grades.TryGetValue("C", out var gradeC))
        {
            await EnsureCustomerAsync(db, "CUST-DEMO-001", "أسواق النخيل المركزية", "Al Nakheel Central Markets", "+966551000001", "طريق الملك فهد، الرياض", "Riyadh", "Supermarket", "310111111100003", gradeA, priceList.Id, 100000m, repUser.Id, 24.7136m, 46.6753m, ct);
            await EnsureCustomerAsync(db, "CUST-DEMO-002", "تموينات الواحة الحديثة", "Modern Oasis Grocery", "+966551000002", "حي العليا، الرياض", "Riyadh", "Store", null, gradeB, priceList.Id, 45000m, repUser.Id, 24.6949m, 46.6852m, ct);
            await EnsureCustomerAsync(db, "CUST-DEMO-003", "مطاعم الديرة الذهبية", "Golden Deerah Restaurants", "+966551000003", "طريق الأمير محمد بن عبدالعزيز، جدة", "Jeddah", "Restaurant", "310222222200003", gradeC, priceList.Id, 25000m, repUser.Id, 21.5433m, 39.1728m, ct);
        }

        // 8. Financial Accounts, Open Fiscal Period, ZATCA EGS Device, and Initial GPS Ping
        await EnsureFinancialAccountAsync(db, "SAFE-MAIN", "الخزينة الرئيسية — الرياض", "Main Treasury Safe", "MainTreasurySafe", null, null, null, ct);
        await EnsureFinancialAccountAsync(db, "BANK-SNB", "الحساب البنكي — البنك الأهلي السعودي", "Saudi National Bank Account", "BankAccount", null, "SNB", "SA0380000000608010167519", ct);
        await EnsureFinancialAccountAsync(db, "WALLET-REP-DEMO", "عهدة المندوب التجريبي", "Demo Rep Custody Wallet", "RepCustodyWallet", repUser.Id, null, null, ct);

        if (!await db.FiscalPeriods.AnyAsync(p => p.FiscalYear == now.Year && p.FiscalMonth == now.Month, ct))
        {
            db.FiscalPeriods.Add(FiscalPeriod.Create(now.Year, now.Month));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ZatcaDeviceSequences.AnyAsync(z => z.EgsDeviceId == "EGS-DEMO-01", ct))
        {
            db.ZatcaDeviceSequences.Add(ZatcaDeviceSequence.Create(
                "EGS-DEMO-01",
                mainWh.Id,
                "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==",
                "CERT-ALMOMIAA-DEMO-2026",
                now));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.GpsRouteHistories.AnyAsync(g => g.RepId == repUser.Id, ct))
        {
            db.GpsRouteHistories.Add(GpsRouteHistory.Record(repUser.Id, 24.7136m, 46.6753m, 12m, null, now));
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("DemoDataSeeder completed idempotently (warehouses, 4 demo users, 4 multi-warehouse products, 3 customers, finance & ZATCA EGS ready).");
    }

    private static async Task<Warehouse> EnsureWarehouseAsync(
        AlMomiaaDbContext db, string code, string nameAr, string nameEn, string type, string city, CancellationToken ct)
    {
        var existing = await db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == code, ct);
        if (existing is not null) return existing;

        var wh = Warehouse.Create(code, nameAr, nameEn, type, null, type == "MobileVan" ? "RYD-4021" : null, city);
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync(ct);
        return wh;
    }

    private static async Task<User> EnsureUserAsync(
        AlMomiaaDbContext db,
        string code,
        string nameAr,
        string nameEn,
        string phone,
        string email,
        string passwordHash,
        string? repType,
        int? defaultWarehouseId,
        int roleId,
        int[] warehouseIds,
        CancellationToken ct)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.EmployeeCode == code, ct);
        if (existing is not null) return existing;

        var user = User.Create(code, nameAr, nameEn, phone, email, passwordHash, repType, defaultWarehouseId, isActive: true);
        user.AssignRole(roleId);
        foreach (var whId in warehouseIds.Distinct())
        {
            user.AssignWarehouse(whId);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Category> EnsureCategoryAsync(AlMomiaaDbContext db, string nameAr, string nameEn, int order, CancellationToken ct)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.NameAr == nameAr, ct);
        if (existing is not null) return existing;

        var cat = Category.Create(null, nameAr, nameEn, order);
        db.Categories.Add(cat);
        await db.SaveChangesAsync(ct);
        return cat;
    }

    private static async Task<Brand> EnsureBrandAsync(AlMomiaaDbContext db, string nameAr, string nameEn, CancellationToken ct)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(b => b.NameAr == nameAr, ct);
        if (existing is not null) return existing;

        var brand = Brand.Create(nameAr, nameEn);
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return brand;
    }

    private static async Task EnsureProductWithMultiWarehouseStockAsync(
        AlMomiaaDbContext db,
        Guid actorUserId,
        DateTime now,
        string sku,
        string barcode,
        string nameAr,
        string nameEn,
        int categoryId,
        int brandId,
        string baseUnit,
        decimal wac,
        decimal minFloor,
        decimal wholesale,
        decimal retail,
        string baseUnitAr,
        string baseUnitEn,
        (string NameAr, string NameEn, decimal Factor, string Barcode, decimal Wholesale, decimal Retail, decimal MinFloor)? extraUnit,
        (int WarehouseId, decimal Qty, decimal MinReorder)[] warehouseStocks,
        CancellationToken ct)
    {
        if (await db.MasterProducts.AsNoTracking().AnyAsync(p => p.SKU == sku, ct))
            return;

        var product = MasterProduct.Create(sku, barcode, nameAr, nameEn, categoryId, brandId, baseUnit, wac, wholesale, retail, minFloor, 15m);
        product.Units.Add(ProductUnit.Create(product.Id, baseUnitAr, baseUnitEn, 1m, barcode, wholesale, retail, minFloor, isDefaultUnit: true));

        if (extraUnit is { } eu)
        {
            product.Units.Add(ProductUnit.Create(product.Id, eu.NameAr, eu.NameEn, eu.Factor, eu.Barcode, eu.Wholesale, eu.Retail, eu.MinFloor, isDefaultUnit: false));
        }

        db.MasterProducts.Add(product);

        foreach (var (whId, qty, minReorder) in warehouseStocks)
        {
            var stock = WarehouseStock.Create(whId, product.Id, minReorder);
            if (qty > 0m)
            {
                stock.ReceiveGoods(qty, 0m);
                db.StockMovements.Add(StockMovement.Append(whId, product.Id, "OpeningBalance", qty, stock.PhysicalQty, wac, "MasterProduct", product.Id, actorUserId, now));
            }
            db.WarehouseStocks.Add(stock);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureCustomerAsync(
        AlMomiaaDbContext db,
        string code,
        string nameAr,
        string nameEn,
        string phone,
        string address,
        string city,
        string category,
        string? vatNumber,
        int gradeId,
        int? priceListId,
        decimal creditLimit,
        Guid assignedRepId,
        decimal? lat,
        decimal? lng,
        CancellationToken ct)
    {
        if (await db.Customers.AsNoTracking().AnyAsync(c => c.CustomerCode == code, ct))
            return;

        db.Customers.Add(Customer.Create(code, nameAr, nameEn, phone, address, city, category, vatNumber, gradeId, priceListId, null, creditLimit, assignedRepId, lat, lng));
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureFinancialAccountAsync(
        AlMomiaaDbContext db,
        string code,
        string nameAr,
        string nameEn,
        string accountType,
        Guid? ownerRepId,
        string? bankName,
        string? iban,
        CancellationToken ct)
    {
        if (await db.FinancialAccounts.AsNoTracking().AnyAsync(a => a.AccountCode == code, ct))
            return;

        db.FinancialAccounts.Add(FinancialAccount.Create(code, nameAr, nameEn, accountType, ownerRepId, bankName, iban));
        await db.SaveChangesAsync(ct);
    }
}
