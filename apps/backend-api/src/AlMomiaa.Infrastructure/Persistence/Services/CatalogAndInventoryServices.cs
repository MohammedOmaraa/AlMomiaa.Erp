using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Inventory;
using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence.Services;

public sealed class CatalogModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider? clock = null) : ICatalogModuleService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private bool CanViewCost() =>
        currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SuperAdmin)
        || currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.ExecutiveManager)
        || currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.Accountant)
        || currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.PurchasingManager);

    public async Task<IReadOnlyList<CategoryNodeDto>> GetCategoryTreeAsync(CancellationToken ct)
    {
        var all = await db.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).ToListAsync(ct);
        var lookup = all.ToLookup(c => c.ParentCategoryId);

        List<CategoryNodeDto> Build(int? parentId) =>
            lookup[parentId]
                .Select(c => new CategoryNodeDto(c.Id, c.ParentCategoryId, c.NameAr, c.NameEn, c.DisplayOrder, c.IsActive, Build(c.Id)))
                .ToList();

        return Build(null);
    }

    public async Task<CategoryNodeDto> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (req.ParentCategoryId.HasValue && !await db.Categories.AsNoTracking().AnyAsync(c => c.Id == req.ParentCategoryId.Value, ct))
            throw new NotFoundException("ParentCategory", req.ParentCategoryId.Value);

        var cat = Category.Create(req.ParentCategoryId, req.NameAr, req.NameEn, req.DisplayOrder);
        db.Categories.Add(cat);
        await db.SaveChangesAsync(ct);

        return new CategoryNodeDto(cat.Id, cat.ParentCategoryId, cat.NameAr, cat.NameEn, cat.DisplayOrder, cat.IsActive, []);
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken ct) =>
        await db.Brands.AsNoTracking().OrderBy(b => b.Id).Select(b => new BrandDto(b.Id, b.NameAr, b.NameEn, b.IsActive)).ToListAsync(ct);

    public async Task<BrandDto> CreateBrandAsync(CreateBrandCommand command, CancellationToken ct)
    {
        var brand = Brand.Create(command.Request.NameAr, command.Request.NameEn);
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return new BrandDto(brand.Id, brand.NameAr, brand.NameEn, brand.IsActive);
    }

    public async Task<PagedResult<MasterProductDto>> GetProductsAsync(GetProductsQuery query, CancellationToken ct)
    {
        var includeCost = CanViewCost();
        var q = db.MasterProducts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(p => p.SKU.Contains(term) || p.Barcode.Contains(term) || p.NameAr.Contains(term) || (p.NameEn != null && p.NameEn.Contains(term)));
        }

        if (query.CategoryId.HasValue) q = q.Where(p => p.CategoryId == query.CategoryId.Value);
        if (query.BrandId.HasValue) q = q.Where(p => p.BrandId == query.BrandId.Value);
        if (query.IsActive.HasValue) q = q.Where(p => p.IsActive == query.IsActive.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(p => p.SKU)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new MasterProductDto(
                p.Id, p.SKU, p.Barcode, p.NameAr, p.NameEn, p.CategoryId, p.BrandId, p.BaseUnit,
                includeCost ? p.WeightedAvgCost : null,
                p.WholesaleSellingPrice, p.RetailSellingPrice, p.MinAllowedSellingPrice, p.VatRate, p.IsActive,
                p.Units.Select(u => new ProductUnitDto(u.Id, u.UnitNameAr, u.UnitNameEn, u.ConversionFactor, u.Barcode, u.WholesalePrice, u.RetailPrice, u.MinAllowedPrice, u.IsDefaultUnit)).ToList()))
            .ToListAsync(ct);

        var nextCursor = (query.Page * query.PageSize) < total ? (query.Page + 1).ToString() : null;
        return new PagedResult<MasterProductDto>(items, total, nextCursor);
    }

    public async Task<PagedResult<CategoryProductPriceDto>> GetCategoryProductsAsync(GetCategoryProductsQuery query, CancellationToken ct)
    {
        int? priceListId = null;
        if (query.CustomerId.HasValue)
        {
            priceListId = await db.Customers.AsNoTracking()
                .Where(c => c.Id == query.CustomerId.Value)
                .Select(c => c.PriceListId)
                .FirstOrDefaultAsync(ct);
        }

        var baseQuery = db.MasterProducts.AsNoTracking().Where(p => p.CategoryId == query.CategoryId && p.IsActive);
        var total = await baseQuery.CountAsync(ct);
        var products = await baseQuery
            .Include(p => p.Units)
            .OrderBy(p => p.SKU)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToArray();
        var allStockRows = productIds.Length > 0
            ? await db.WarehouseStocks.AsNoTracking()
                .Where(s => productIds.Contains(s.MasterProductId))
                .OrderBy(s => s.WarehouseId)
                .Select(s => new
                {
                    s.MasterProductId,
                    Breakdown = new ProductWarehouseStockBreakdownDto(
                        s.WarehouseId,
                        s.Warehouse.WarehouseCode,
                        s.Warehouse.NameAr,
                        s.Warehouse.NameEn,
                        s.Warehouse.WarehouseType,
                        s.PhysicalQty,
                        s.ReservedQty,
                        s.IncomingQty,
                        s.PhysicalQty - s.ReservedQty,
                        s.MinReorderQty)
                })
                .ToListAsync(ct)
            : [];

        var stocksByProduct = allStockRows
            .GroupBy(x => x.MasterProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ProductWarehouseStockBreakdownDto>)g.Select(x => x.Breakdown).ToList());

        var unitIds = products.SelectMany(p => p.Units).Select(u => u.Id).ToArray();
        var tierPrices = priceListId.HasValue
            ? await db.PriceListItems.AsNoTracking()
                .Where(i => i.PriceListId == priceListId.Value && unitIds.Contains(i.ProductUnitId))
                .OrderBy(i => i.MinQuantity)
                .ToListAsync(ct)
            : [];

        var items = products.Select(p =>
        {
            var defaultUnit = p.Units.FirstOrDefault(u => u.IsDefaultUnit) ?? p.Units.First();
            var tier = tierPrices.FirstOrDefault(t => t.ProductUnitId == defaultUnit.Id);
            var resolvedPrice = tier?.TierUnitPrice ?? defaultUnit.WholesalePrice;
            var breakdown = stocksByProduct.TryGetValue(p.Id, out var list) ? list : [];
            var totalPhysical = breakdown.Sum(s => s.PhysicalQty);
            var totalAvailable = breakdown.Sum(s => s.AvailableQty);
            var available = query.WarehouseId.HasValue
                ? breakdown.FirstOrDefault(s => s.WarehouseId == query.WarehouseId.Value)?.AvailableQty ?? 0m
                : totalAvailable;

            return new CategoryProductPriceDto(
                p.Id,
                p.SKU,
                p.NameAr,
                p.NameEn,
                defaultUnit.Id,
                resolvedPrice,
                defaultUnit.MinAllowedPrice,
                available,
                p.BaseUnit,
                defaultUnit.UnitNameAr,
                defaultUnit.UnitNameEn,
                totalPhysical,
                totalAvailable,
                breakdown);
        }).ToList();

        var nextCursor = (query.Page * query.PageSize) < total ? (query.Page + 1).ToString() : null;
        return new PagedResult<CategoryProductPriceDto>(items, total, nextCursor);
    }

    public async Task<ProductAvailabilityDto> GetProductAvailabilityAsync(Guid masterProductId, CancellationToken ct)
    {
        var product = await db.MasterProducts
            .AsNoTracking()
            .Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == masterProductId, ct)
            ?? throw new NotFoundException("MasterProduct", masterProductId);

        var warehouseStocks = await db.WarehouseStocks
            .AsNoTracking()
            .Where(s => s.MasterProductId == masterProductId)
            .OrderBy(s => s.WarehouseId)
            .Select(s => new ProductWarehouseStockBreakdownDto(
                s.WarehouseId,
                s.Warehouse.WarehouseCode,
                s.Warehouse.NameAr,
                s.Warehouse.NameEn,
                s.Warehouse.WarehouseType,
                s.PhysicalQty,
                s.ReservedQty,
                s.IncomingQty,
                s.PhysicalQty - s.ReservedQty,
                s.MinReorderQty))
            .ToListAsync(ct);

        var units = product.Units
            .OrderByDescending(u => u.IsDefaultUnit)
            .ThenBy(u => u.ConversionFactor)
            .Select(u => new ProductUnitDto(u.Id, u.UnitNameAr, u.UnitNameEn, u.ConversionFactor, u.Barcode, u.WholesalePrice, u.RetailPrice, u.MinAllowedPrice, u.IsDefaultUnit))
            .ToList();

        return new ProductAvailabilityDto(
            product.Id,
            product.SKU,
            product.NameAr,
            product.NameEn,
            product.BaseUnit,
            product.WholesaleSellingPrice,
            product.RetailSellingPrice,
            product.MinAllowedSellingPrice,
            product.VatRate,
            warehouseStocks.Sum(w => w.PhysicalQty),
            warehouseStocks.Sum(w => w.ReservedQty),
            warehouseStocks.Sum(w => w.IncomingQty),
            warehouseStocks.Sum(w => w.AvailableQty),
            units,
            warehouseStocks);
    }

    public async Task<MasterProductDto> CreateMasterProductAsync(CreateMasterProductCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var sku = req.SKU.Trim().ToUpperInvariant();
        var barcode = req.Barcode.Trim();

        if (await db.MasterProducts.AsNoTracking().AnyAsync(p => p.SKU == sku, ct))
            throw new ConflictException($"SKU '{sku}' already exists.");
        if (await db.MasterProducts.AsNoTracking().AnyAsync(p => p.Barcode == barcode, ct))
            throw new ConflictException($"Barcode '{barcode}' already exists.");
        if (!await db.Categories.AsNoTracking().AnyAsync(c => c.Id == req.CategoryId, ct))
            throw new NotFoundException("Category", req.CategoryId);
        if (!await db.Brands.AsNoTracking().AnyAsync(b => b.Id == req.BrandId, ct))
            throw new NotFoundException("Brand", req.BrandId);

        var unitBarcodes = req.Units.Select(u => u.Barcode.Trim()).ToArray();
        if (unitBarcodes.Length != unitBarcodes.Distinct(StringComparer.Ordinal).Count())
            throw new ConflictException("Duplicate ProductUnit barcode inside request.");
        if (await db.ProductUnits.AsNoTracking().AnyAsync(u => unitBarcodes.Contains(u.Barcode), ct))
            throw new ConflictException("One or more ProductUnit barcodes already exist.");

        if (req.InitialWarehouseStocks is { Length: > 0 })
        {
            var requestedWarehouseIds = req.InitialWarehouseStocks.Select(w => w.WarehouseId).Distinct().ToArray();
            var existingWarehouseIds = await db.Warehouses.AsNoTracking()
                .Where(w => requestedWarehouseIds.Contains(w.Id))
                .Select(w => w.Id)
                .ToListAsync(ct);

            var missingWarehouseId = requestedWarehouseIds.FirstOrDefault(id => !existingWarehouseIds.Contains(id));
            if (missingWarehouseId != 0)
                throw new NotFoundException("Warehouse", missingWarehouseId);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var product = MasterProduct.Create(
            sku, barcode, req.NameAr, req.NameEn, req.CategoryId, req.BrandId, req.BaseUnit,
            req.WeightedAvgCost, req.WholesaleSellingPrice, req.RetailSellingPrice, req.MinAllowedSellingPrice, req.VatRate);

        foreach (var u in req.Units)
        {
            product.Units.Add(ProductUnit.Create(
                product.Id, u.UnitNameAr, u.UnitNameEn, u.ConversionFactor, u.Barcode,
                u.WholesalePrice, u.RetailPrice, u.MinAllowedPrice, u.IsDefaultUnit));
        }

        db.MasterProducts.Add(product);

        if (req.InitialWarehouseStocks is { Length: > 0 })
        {
            foreach (var entry in req.InitialWarehouseStocks)
            {
                var stock = WarehouseStock.Create(entry.WarehouseId, product.Id, entry.MinReorderQty);
                if (entry.InitialQty > 0m)
                {
                    stock.ReceiveGoods(entry.InitialQty, 0m);
                }

                db.WarehouseStocks.Add(stock);

                if (entry.InitialQty > 0m)
                {
                    db.StockMovements.Add(StockMovement.Append(
                        entry.WarehouseId,
                        product.Id,
                        "OpeningBalance",
                        entry.InitialQty,
                        stock.PhysicalQty,
                        product.WeightedAvgCost,
                        "MasterProduct",
                        product.Id,
                        currentUser.UserId,
                        now));
                }
            }
        }

        AuditAndOutboxHelper.AppendAudit(
            db,
            currentUser,
            "CreateMasterProduct",
            "MasterProduct",
            product.Id.ToString("D"),
            null,
            new
            {
                product.SKU,
                product.BaseUnit,
                product.MinAllowedSellingPrice,
                InitialWarehousesCount = req.InitialWarehouseStocks?.Length ?? 0,
                TotalInitialQty = req.InitialWarehouseStocks?.Sum(s => s.InitialQty) ?? 0m
            },
            now);
        await db.SaveChangesAsync(ct);

        return new MasterProductDto(
            product.Id, product.SKU, product.Barcode, product.NameAr, product.NameEn, product.CategoryId,
            product.BrandId, product.BaseUnit, CanViewCost() ? product.WeightedAvgCost : null,
            product.WholesaleSellingPrice, product.RetailSellingPrice, product.MinAllowedSellingPrice,
            product.VatRate, product.IsActive,
            product.Units.Select(u => new ProductUnitDto(u.Id, u.UnitNameAr, u.UnitNameEn, u.ConversionFactor, u.Barcode, u.WholesalePrice, u.RetailPrice, u.MinAllowedPrice, u.IsDefaultUnit)).ToList());
    }

    public async Task<SupplierProductDto> LinkSupplierProductAsync(LinkSupplierProductCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (!await db.MasterProducts.AsNoTracking().AnyAsync(p => p.Id == command.MasterProductId, ct))
            throw new NotFoundException("MasterProduct", command.MasterProductId);
        if (!await db.Suppliers.AsNoTracking().AnyAsync(s => s.Id == req.SupplierId, ct))
            throw new NotFoundException("Supplier", req.SupplierId);
        if (await db.SupplierProducts.AsNoTracking().AnyAsync(sp => sp.MasterProductId == command.MasterProductId && sp.SupplierId == req.SupplierId, ct))
            throw new ConflictException("Supplier is already linked to this MasterProduct.");

        var now = _clock.GetUtcNow().UtcDateTime;
        var sp = SupplierProduct.Create(command.MasterProductId, req.SupplierId, req.SupplierSku, req.SupplierBarcode, req.LastPurchasePrice, req.LeadTimeDays, req.IsPreferredSupplier, now);
        db.SupplierProducts.Add(sp);
        await db.SaveChangesAsync(ct);

        return new SupplierProductDto(sp.Id, sp.MasterProductId, sp.SupplierId, sp.SupplierSku, sp.SupplierBarcode, sp.LastPurchasePrice, sp.LeadTimeDays, sp.IsPreferredSupplier, sp.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<PriceListDto>> GetPriceListsAsync(CancellationToken ct)
    {
        return await db.PriceLists
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PriceListDto(
                p.Id, p.PriceListCode, p.NameAr, p.NameEn, p.IsDefault, p.IsActive, p.EffectiveFromUtc, p.EffectiveToUtc,
                p.Items.Select(i => new PriceListItemDto(i.Id, i.ProductUnitId, i.MinQuantity, i.TierUnitPrice)).ToList()))
            .ToListAsync(ct);
    }

    public async Task<PriceListDto> CreatePriceListAsync(CreatePriceListCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var code = req.PriceListCode.Trim().ToUpperInvariant();
        if (await db.PriceLists.AsNoTracking().AnyAsync(p => p.PriceListCode == code, ct))
            throw new ConflictException($"PriceListCode '{code}' already exists.");

        var now = _clock.GetUtcNow().UtcDateTime;
        var priceList = PriceList.Create(code, req.NameAr, req.NameEn, req.IsDefault, req.EffectiveFromUtc ?? now, req.EffectiveToUtc);
        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync(ct);

        if (req.Items is { Length: > 0 })
        {
            var unitIds = req.Items.Select(i => i.ProductUnitId).Distinct().ToArray();
            var foundUnits = await db.ProductUnits.AsNoTracking().CountAsync(u => unitIds.Contains(u.Id), ct);
            if (foundUnits != unitIds.Length)
                throw new NotFoundException("ProductUnit", string.Join(',', unitIds));

            foreach (var item in req.Items)
            {
                db.PriceListItems.Add(PriceListItem.Create(priceList.Id, item.ProductUnitId, item.MinQuantity, item.TierUnitPrice));
            }
            await db.SaveChangesAsync(ct);
        }

        var savedItems = await db.PriceListItems.AsNoTracking()
            .Where(i => i.PriceListId == priceList.Id)
            .Select(i => new PriceListItemDto(i.Id, i.ProductUnitId, i.MinQuantity, i.TierUnitPrice))
            .ToListAsync(ct);

        return new PriceListDto(priceList.Id, priceList.PriceListCode, priceList.NameAr, priceList.NameEn, priceList.IsDefault, priceList.IsActive, priceList.EffectiveFromUtc, priceList.EffectiveToUtc, savedItems);
    }
}

public sealed class InventoryModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider? clock = null) : IInventoryModuleService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private bool IsScopedWarehouseKeeper() =>
        currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.WarehouseKeeper)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SuperAdmin)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.ExecutiveManager)
        && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.PurchasingManager);

    private void EnsureWarehouseAccess(int warehouseId)
    {
        if (IsScopedWarehouseKeeper() && !currentUser.AssignedWarehouseIds.Contains(warehouseId))
            throw new ForbiddenDomainException($"Warehouse Keeper is not assigned to warehouse #{warehouseId}.");
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(CancellationToken ct)
    {
        var q = db.Warehouses.AsNoTracking().AsQueryable();
        if (IsScopedWarehouseKeeper())
        {
            var assigned = currentUser.AssignedWarehouseIds;
            q = q.Where(w => assigned.Contains(w.Id));
        }
        else if (currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SalesRep)
            && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SuperAdmin)
            && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.ExecutiveManager)
            && !currentUser.IsInRole(CanonicalRolesAndPermissions.Roles.SalesManager))
        {
            var allowed = currentUser.AssignedWarehouseIds.ToList();
            if (currentUser.DefaultWarehouseId.HasValue && !allowed.Contains(currentUser.DefaultWarehouseId.Value))
            {
                allowed.Add(currentUser.DefaultWarehouseId.Value);
            }

            q = q.Where(w => w.IsActive);
            if (allowed.Count > 0)
            {
                q = q.Where(w => allowed.Contains(w.Id) || w.AssignedRepId == currentUser.UserId);
            }
        }

        return await q.OrderBy(w => w.Id)
            .Select(w => new WarehouseDto(w.Id, w.WarehouseCode, w.NameAr, w.NameEn, w.WarehouseType, w.AssignedRepId, w.VehiclePlateNumber, w.City, w.IsActive))
            .ToListAsync(ct);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var code = req.WarehouseCode.Trim().ToUpperInvariant();
        if (await db.Warehouses.AsNoTracking().AnyAsync(w => w.WarehouseCode == code, ct))
            throw new ConflictException($"WarehouseCode '{code}' already exists.");
        if (req.AssignedRepId.HasValue && !await db.Users.AsNoTracking().AnyAsync(u => u.Id == req.AssignedRepId.Value, ct))
            throw new NotFoundException("User (AssignedRep)", req.AssignedRepId.Value);

        var wh = Warehouse.Create(code, req.NameAr, req.NameEn, req.WarehouseType, req.AssignedRepId, req.VehiclePlateNumber, req.City ?? "Riyadh");
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync(ct);

        return new WarehouseDto(wh.Id, wh.WarehouseCode, wh.NameAr, wh.NameEn, wh.WarehouseType, wh.AssignedRepId, wh.VehiclePlateNumber, wh.City, wh.IsActive);
    }

    public async Task<PagedResult<WarehouseStockDto>> GetWarehouseStocksAsync(GetWarehouseStocksQuery query, CancellationToken ct)
    {
        EnsureWarehouseAccess(query.WarehouseId);
        var q = db.WarehouseStocks.AsNoTracking().Where(s => s.WarehouseId == query.WarehouseId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(s => s.MasterProduct.SKU.Contains(term) || s.MasterProduct.NameAr.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(s => s.MasterProduct.SKU)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new WarehouseStockDto(
                s.Id, s.WarehouseId, s.MasterProductId, s.MasterProduct.SKU, s.MasterProduct.NameAr,
                s.PhysicalQty, s.ReservedQty, s.IncomingQty, s.PhysicalQty - s.ReservedQty, s.MinReorderQty))
            .ToListAsync(ct);

        var nextCursor = (query.Page * query.PageSize) < total ? (query.Page + 1).ToString() : null;
        return new PagedResult<WarehouseStockDto>(items, total, nextCursor);
    }

    public async Task<StockTransferDto> CreateStockTransferAsync(CreateStockTransferCommand command, CancellationToken ct)
    {
        var req = command.Request;
        EnsureWarehouseAccess(req.SourceWarehouseId);

        if (!await db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == req.SourceWarehouseId, ct))
            throw new NotFoundException("SourceWarehouse", req.SourceWarehouseId);
        if (!await db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == req.DestinationWarehouseId, ct))
            throw new NotFoundException("DestinationWarehouse", req.DestinationWarehouseId);

        var now = _clock.GetUtcNow().UtcDateTime;
        var transferNo = string.IsNullOrWhiteSpace(req.TransferNumber)
            ? $"TRF-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : req.TransferNumber.Trim().ToUpperInvariant();

        if (await db.StockTransfers.AsNoTracking().AnyAsync(t => t.TransferNumber == transferNo, ct))
            throw new ConflictException($"TransferNumber '{transferNo}' already exists.");

        var transfer = StockTransfer.CreateInTransit(transferNo, req.SourceWarehouseId, req.DestinationWarehouseId, currentUser.UserId, now);

        foreach (var line in req.Items)
        {
            var product = await db.MasterProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == line.MasterProductId, ct)
                ?? throw new NotFoundException("MasterProduct", line.MasterProductId);

            var sourceStock = await db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == req.SourceWarehouseId && s.MasterProductId == line.MasterProductId, ct)
                ?? throw new BusinessRuleException($"No stock record for product '{product.SKU}' in source warehouse #{req.SourceWarehouseId}.");

            sourceStock.DeductForTransfer(line.DispatchedQty);

            var destStock = await db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == req.DestinationWarehouseId && s.MasterProductId == line.MasterProductId, ct);
            if (destStock is null)
            {
                destStock = WarehouseStock.Create(req.DestinationWarehouseId, line.MasterProductId);
                db.WarehouseStocks.Add(destStock);
            }
            destStock.AddIncoming(line.DispatchedQty);

            transfer.Items.Add(StockTransferItem.Create(transfer.Id, line.MasterProductId, line.RequestedQty, line.DispatchedQty));

            db.StockMovements.Add(StockMovement.Append(
                req.SourceWarehouseId, line.MasterProductId, "TransferOut", -line.DispatchedQty,
                sourceStock.PhysicalQty, product.WeightedAvgCost, "StockTransfer", transfer.Id, currentUser.UserId, now));
        }

        db.StockTransfers.Add(transfer);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateStockTransfer", "StockTransfer", transfer.Id.ToString("D"), null, new { transfer.TransferNumber, transfer.SourceWarehouseId, transfer.DestinationWarehouseId }, now);
        await db.SaveChangesAsync(ct);

        return MapTransfer(transfer);
    }

    public async Task<StockTransferDto> ReceiveStockTransferAsync(ReceiveStockTransferCommand command, CancellationToken ct)
    {
        var transfer = await db.StockTransfers.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == command.StockTransferId, ct)
            ?? throw new NotFoundException("StockTransfer", command.StockTransferId);

        EnsureWarehouseAccess(transfer.DestinationWarehouseId);
        var now = _clock.GetUtcNow().UtcDateTime;
        transfer.MarkReceived(currentUser.UserId, now);

        foreach (var lineReq in command.Request.Items)
        {
            var item = transfer.Items.FirstOrDefault(i => i.Id == lineReq.StockTransferItemId)
                ?? throw new NotFoundException("StockTransferItem", lineReq.StockTransferItemId);

            if (lineReq.ReceivedQty > item.DispatchedQty)
                throw new BusinessRuleException($"ReceivedQty ({lineReq.ReceivedQty}) cannot exceed DispatchedQty ({item.DispatchedQty}).");

            item.SetReceivedQty(lineReq.ReceivedQty);

            var product = await db.MasterProducts.AsNoTracking().FirstAsync(p => p.Id == item.MasterProductId, ct);
            var destStock = await db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == transfer.DestinationWarehouseId && s.MasterProductId == item.MasterProductId, ct);
            if (destStock is null)
            {
                destStock = WarehouseStock.Create(transfer.DestinationWarehouseId, item.MasterProductId);
                db.WarehouseStocks.Add(destStock);
            }

            destStock.ReceiveGoods(lineReq.ReceivedQty, item.DispatchedQty);

            db.StockMovements.Add(StockMovement.Append(
                transfer.DestinationWarehouseId, item.MasterProductId, "TransferIn", lineReq.ReceivedQty,
                destStock.PhysicalQty, product.WeightedAvgCost, "StockTransfer", transfer.Id, currentUser.UserId, now));
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "ReceiveStockTransfer", "StockTransfer", transfer.Id.ToString("D"), null, new { transfer.Status }, now);
        await db.SaveChangesAsync(ct);
        return MapTransfer(transfer);
    }

    public async Task<IReadOnlyList<WarehouseStockDto>> SubmitInventoryCountAsync(SubmitInventoryCountCommand command, CancellationToken ct)
    {
        EnsureWarehouseAccess(command.WarehouseId);
        if (!await db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == command.WarehouseId, ct))
            throw new NotFoundException("Warehouse", command.WarehouseId);

        var now = _clock.GetUtcNow().UtcDateTime;
        var updated = new List<WarehouseStockDto>();

        foreach (var line in command.Request.Items)
        {
            var product = await db.MasterProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == line.MasterProductId, ct)
                ?? throw new NotFoundException("MasterProduct", line.MasterProductId);

            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == command.WarehouseId && s.MasterProductId == line.MasterProductId, ct);
            if (stock is null)
            {
                stock = WarehouseStock.Create(command.WarehouseId, line.MasterProductId);
                db.WarehouseStocks.Add(stock);
            }

            var delta = line.CountedPhysicalQty - stock.PhysicalQty;
            stock.SetPhysicalCount(line.CountedPhysicalQty);

            db.StockMovements.Add(StockMovement.Append(
                command.WarehouseId, line.MasterProductId, "CountAdjustment", delta, stock.PhysicalQty,
                product.WeightedAvgCost, "InventoryCount", stock.Id, currentUser.UserId, now, line.LotNumber, line.ExpiryDate));

            updated.Add(new WarehouseStockDto(
                stock.Id, stock.WarehouseId, stock.MasterProductId, product.SKU, product.NameAr,
                stock.PhysicalQty, stock.ReservedQty, stock.IncomingQty, stock.ComputedAvailableQty, stock.MinReorderQty));
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "SubmitInventoryCount", "WarehouseStock", command.WarehouseId.ToString(), null, updated, now);
        await db.SaveChangesAsync(ct);
        return updated;
    }

    private static StockTransferDto MapTransfer(StockTransfer t) =>
        new(
            t.Id, t.TransferNumber, t.SourceWarehouseId, t.DestinationWarehouseId, t.Status,
            t.DispatchedByUserId, t.DispatchedAtUtc, t.ReceivedByUserId, t.ReceivedAtUtc,
            t.Items.Select(i => new StockTransferItemDto(i.Id, i.MasterProductId, i.RequestedQty, i.DispatchedQty, i.ReceivedQty)).ToList());
}
