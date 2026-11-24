using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Procurement;
using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Domain.Procurement;
using AlMomiaa.Domain.Sales;
using AlMomiaa.Infrastructure.Zatca;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence.Services;

public sealed class ProcurementModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IProcurementModuleService
{
    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(GetSuppliersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(s => s.SupplierCode.Contains(term) || s.NameAr.Contains(term) || (s.NameEn != null && s.NameEn.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(s => s.SupplierCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto(s.Id, s.SupplierCode, s.NameAr, s.NameEn, s.Country, s.Phone, s.VatNumber, s.CurrentPayableBalance, s.IsActive))
            .ToListAsync(ct);

        var nextCursor = (page * pageSize) < total ? (page + 1).ToString() : null;
        return new PagedResult<SupplierDto>(items, total, nextCursor);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var code = req.SupplierCode.Trim().ToUpperInvariant();
        if (await db.Suppliers.AnyAsync(s => s.SupplierCode == code, ct))
        {
            throw new ConflictException($"Supplier code '{code}' already exists.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var supplier = Supplier.Create(code, req.NameAr, req.NameEn, req.Country ?? "Saudi Arabia", req.Phone, req.VatNumber);
        db.Suppliers.Add(supplier);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateSupplier", "Supplier", supplier.Id.ToString(), null, new { supplier.SupplierCode, supplier.NameAr }, utcNow);
        await db.SaveChangesAsync(ct);

        return new SupplierDto(supplier.Id, supplier.SupplierCode, supplier.NameAr, supplier.NameEn, supplier.Country, supplier.Phone, supplier.VatNumber, supplier.CurrentPayableBalance, supplier.IsActive);
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(GetPurchaseOrdersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.PurchaseOrders.AsNoTracking().Include(p => p.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            q = q.Where(p => p.Status == status);
        }

        var total = await q.CountAsync(ct);
        var orders = await q
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var nextCursor = (page * pageSize) < total ? (page + 1).ToString() : null;
        return new PagedResult<PurchaseOrderDto>(orders.Select(MapPo).ToList(), total, nextCursor);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == req.SupplierId, ct)
            ?? throw new NotFoundException("Supplier", req.SupplierId);
        if (!await db.Warehouses.AnyAsync(w => w.Id == req.TargetWarehouseId, ct))
        {
            throw new NotFoundException("Warehouse", req.TargetWarehouseId);
        }

        var productIds = req.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var existingCount = await db.MasterProducts.CountAsync(p => productIds.Contains(p.Id), ct);
        if (existingCount != productIds.Count)
        {
            throw new NotFoundException("MasterProduct", string.Join(',', productIds));
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var poNumber = $"PO-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var totalAmount = req.Items.Sum(i => Math.Round(i.OrderedQty * i.UnitCostSAR, 2, MidpointRounding.AwayFromZero));

        var po = PurchaseOrder.Create(poNumber, supplier.Id, req.TargetWarehouseId, req.OrderType, totalAmount, currentUser.UserId, utcNow);
        db.PurchaseOrders.Add(po);

        foreach (var itemReq in req.Items)
        {
            var item = PurchaseOrderItem.Create(po.Id, itemReq.MasterProductId, itemReq.OrderedQty, itemReq.UnitCostSAR);
            po.Items.Add(item);

            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == req.TargetWarehouseId && s.MasterProductId == itemReq.MasterProductId, ct);
            if (stock is null)
            {
                stock = WarehouseStock.Create(req.TargetWarehouseId, itemReq.MasterProductId);
                db.WarehouseStocks.Add(stock);
            }

            stock.AddIncoming(itemReq.OrderedQty);
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreatePurchaseOrder", "PurchaseOrder", po.Id.ToString(), null, new { po.PoNumber, po.TotalAmountSAR, po.OrderType }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapPo(po);
    }

    public async Task<ContainerDto> CreateContainerAsync(CreateContainerCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (!await db.PurchaseOrders.AnyAsync(p => p.Id == req.PurchaseOrderId, ct))
        {
            throw new NotFoundException("PurchaseOrder", req.PurchaseOrderId);
        }

        var containerNumber = req.ContainerNumber.Trim().ToUpperInvariant();
        if (await db.Containers.AnyAsync(c => c.ContainerNumber == containerNumber, ct))
        {
            throw new ConflictException($"Container number '{containerNumber}' already exists.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var container = Container.Create(
            req.PurchaseOrderId,
            containerNumber,
            req.BillOfLading,
            req.ShippingLine,
            req.PortOfArrival,
            req.EstimatedArrivalDate,
            req.ActualArrivalDate,
            req.Status,
            req.SupplierGoodsCostSAR,
            req.FreightAndCustomsCostSAR);

        db.Containers.Add(container);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateContainer", "Container", container.Id.ToString(), null, new { container.ContainerNumber, container.Status }, utcNow);
        await db.SaveChangesAsync(ct);

        return new ContainerDto(
            container.Id,
            container.PurchaseOrderId,
            container.ContainerNumber,
            container.BillOfLading,
            container.ShippingLine,
            container.PortOfArrival,
            container.EstimatedArrivalDate,
            container.ActualArrivalDate,
            container.Status,
            container.SupplierGoodsCostSAR,
            container.FreightAndCustomsCostSAR,
            Math.Round(container.SupplierGoodsCostSAR + container.FreightAndCustomsCostSAR, 2, MidpointRounding.AwayFromZero));
    }

    public async Task<GoodsReceiptDto> CreateGoodsReceiptAsync(CreateGoodsReceiptCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var po = await db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == req.PurchaseOrderId, ct)
            ?? throw new NotFoundException("PurchaseOrder", req.PurchaseOrderId);

        if (!await db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId, ct))
        {
            throw new NotFoundException("Warehouse", req.WarehouseId);
        }

        Container? container = null;
        if (req.ContainerId.HasValue)
        {
            container = await db.Containers.FirstOrDefaultAsync(c => c.Id == req.ContainerId.Value, ct)
                ?? throw new NotFoundException("Container", req.ContainerId.Value);
            if (container.PurchaseOrderId != po.Id)
            {
                throw new BusinessRuleException("Specified container does not belong to the referenced purchase order.");
            }
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var grnNumber = $"GRN-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var grn = GoodsReceipt.Create(grnNumber, po.Id, req.ContainerId, req.WarehouseId, currentUser.UserId, utcNow);
        db.GoodsReceipts.Add(grn);

        decimal totalReceiptCost = 0m;

        foreach (var itemReq in req.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.MasterProductId == itemReq.MasterProductId)
                ?? throw new BusinessRuleException($"Product '{itemReq.MasterProductId}' is not on purchase order '{po.PoNumber}'.");

            var product = await db.MasterProducts.FirstOrDefaultAsync(p => p.Id == itemReq.MasterProductId, ct)
                ?? throw new NotFoundException("MasterProduct", itemReq.MasterProductId);

            var existingGlobalPhysical = await db.WarehouseStocks
                .Where(s => s.MasterProductId == product.Id)
                .SumAsync(s => s.PhysicalQty, ct);

            product.RecalculateWeightedAvgCost(existingGlobalPhysical, itemReq.ReceivedQty, itemReq.AllocatedLandedUnitCostSAR);

            var stock = await db.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == req.WarehouseId && s.MasterProductId == product.Id, ct);
            if (stock is null)
            {
                stock = WarehouseStock.Create(req.WarehouseId, product.Id);
                db.WarehouseStocks.Add(stock);
            }

            stock.ReceiveGoods(itemReq.ReceivedQty, itemReq.ReceivedQty);
            poItem.AddReceived(itemReq.ReceivedQty);

            var grnItem = GoodsReceiptItem.Create(
                grn.Id,
                product.Id,
                itemReq.ReceivedQty,
                itemReq.AllocatedLandedUnitCostSAR,
                itemReq.LotNumber,
                itemReq.ExpiryDate);
            grn.Items.Add(grnItem);

            db.StockMovements.Add(StockMovement.Append(
                req.WarehouseId,
                product.Id,
                "GoodsReceipt",
                itemReq.ReceivedQty,
                stock.PhysicalQty,
                itemReq.AllocatedLandedUnitCostSAR,
                "GoodsReceipt",
                grn.Id,
                currentUser.UserId,
                utcNow,
                itemReq.LotNumber,
                itemReq.ExpiryDate));

            totalReceiptCost += Math.Round(itemReq.ReceivedQty * itemReq.AllocatedLandedUnitCostSAR, 2, MidpointRounding.AwayFromZero);
        }

        po.RefreshReceiptStatus();
        container?.MarkReceivedAtWarehouse(DateOnly.FromDateTime(utcNow));
        po.Supplier.AddPayable(totalReceiptCost);

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateGoodsReceipt", "GoodsReceipt", grn.Id.ToString(), null, new { grn.GrnNumber, po.PoNumber, TotalReceiptCost = totalReceiptCost }, utcNow);
        await db.SaveChangesAsync(ct);

        return new GoodsReceiptDto(
            grn.Id,
            grn.GrnNumber,
            grn.PurchaseOrderId,
            grn.ContainerId,
            grn.WarehouseId,
            grn.ReceivedByUserId,
            grn.ReceivedAtUtc,
            grn.Items.Select(i => new GoodsReceiptItemDto(i.Id, i.MasterProductId, i.ReceivedQty, i.AllocatedLandedUnitCostSAR, i.LotNumber, i.ExpiryDate)).ToList());
    }

    private static PurchaseOrderDto MapPo(PurchaseOrder po) =>
        new(
            po.Id,
            po.PoNumber,
            po.SupplierId,
            po.TargetWarehouseId,
            po.OrderType,
            po.Status,
            po.TotalAmountSAR,
            po.ApprovedByUserId,
            po.CreatedAtUtc,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id,
                i.MasterProductId,
                i.OrderedQty,
                i.ReceivedQty,
                i.UnitCostSAR,
                Math.Round(i.OrderedQty * i.UnitCostSAR, 2, MidpointRounding.AwayFromZero))).ToList());
}

public sealed class SalesModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IOperationsRealtimeNotifier realtimeNotifier) : ISalesModuleService
{
    public async Task<PagedResult<SalesOrderDto>> GetSalesOrdersAsync(GetSalesOrdersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = db.SalesOrders.AsNoTracking().Include(o => o.Items).AsQueryable();

        var isElevated = currentUser.HasAnyRole(
            CanonicalRolesAndPermissions.Roles.SuperAdmin,
            CanonicalRolesAndPermissions.Roles.ExecutiveManager,
            CanonicalRolesAndPermissions.Roles.SalesManager,
            CanonicalRolesAndPermissions.Roles.Accountant);

        if (!isElevated)
        {
            if (currentUser.HasRole(CanonicalRolesAndPermissions.Roles.SalesRep))
            {
                q = q.Where(o => o.RepId == currentUser.UserId);
            }
            else if (currentUser.HasRole(CanonicalRolesAndPermissions.Roles.WarehouseKeeper))
            {
                var allowed = currentUser.AssignedWarehouseIds.ToList();
                if (currentUser.DefaultWarehouseId.HasValue && !allowed.Contains(currentUser.DefaultWarehouseId.Value))
                {
                    allowed.Add(currentUser.DefaultWarehouseId.Value);
                }

                q = q.Where(o => allowed.Contains(o.WarehouseId));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            q = q.Where(o => o.Status == status);
        }

        var total = await q.CountAsync(ct);
        var orders = await q
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var nextCursor = (page * pageSize) < total ? (page + 1).ToString() : null;
        return new PagedResult<SalesOrderDto>(orders.Select(MapOrder).ToList(), total, nextCursor);
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderCommand command, CancellationToken ct)
    {
        var existing = await db.SalesOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == command.IdempotencyKey, ct);
        if (existing is not null)
        {
            return MapOrder(existing);
        }

        var req = command.Request;
        var customer = await db.Customers
            .Include(c => c.Grade)
            .FirstOrDefaultAsync(c => c.Id == req.CustomerId, ct)
            ?? throw new NotFoundException("Customer", req.CustomerId);

        if (!customer.IsActive)
        {
            throw new BusinessRuleException("Cannot create sales order for an inactive customer.");
        }

        var isRepOnly = currentUser.HasRole(CanonicalRolesAndPermissions.Roles.SalesRep) &&
            !currentUser.HasAnyRole(CanonicalRolesAndPermissions.Roles.SuperAdmin, CanonicalRolesAndPermissions.Roles.SalesManager, CanonicalRolesAndPermissions.Roles.ExecutiveManager);
        if (isRepOnly && customer.AssignedRepId != currentUser.UserId)
        {
            throw new ForbiddenDomainException("Sales Representatives can only submit orders for their own assigned customers.");
        }

        var warehouse = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == req.WarehouseId, ct)
            ?? throw new NotFoundException("Warehouse", req.WarehouseId);

        var channel = string.IsNullOrWhiteSpace(req.OrderChannel)
            ? (string.Equals(currentUser.RepType, "CashVan", StringComparison.OrdinalIgnoreCase) ? "CashVan" : "PreSales")
            : req.OrderChannel.Trim();

        if (channel == "CashVan" && warehouse.WarehouseType != "MobileVan" && isRepOnly)
        {
            throw new BusinessRuleException("CashVan orders submitted by a rep must reference a MobileVan warehouse (MR-01).");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = req.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var products = await db.MasterProducts
            .Include(p => p.Units)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("MasterProduct", string.Join(',', productIds));
        }

        var unitIds = req.Items.Select(i => i.ProductUnitId).Distinct().ToList();
        var priceListItems = customer.PriceListId.HasValue
            ? await db.PriceListItems.AsNoTracking()
                .Where(pli => pli.PriceListId == customer.PriceListId.Value && unitIds.Contains(pli.ProductUnitId))
                .ToListAsync(ct)
            : new List<Domain.Catalog.PriceListItem>();

        var stocks = await db.WarehouseStocks
            .Where(s => s.WarehouseId == req.WarehouseId && productIds.Contains(s.MasterProductId))
            .ToDictionaryAsync(s => s.MasterProductId, ct);

        decimal subTotal = 0m;
        decimal vatTotal = 0m;
        bool hasStockShortage = false;
        bool hasPriceOverridePending = false;

        var pendingItems = new List<(Domain.Catalog.MasterProduct Product, Domain.Catalog.ProductUnit Unit, CreateSalesOrderItemRequest Req, decimal StdPrice, bool OverridePending, decimal Vat, decimal LineTotal)>();

        foreach (var itemReq in req.Items)
        {
            var product = products[itemReq.MasterProductId];
            var unit = product.Units.FirstOrDefault(u => u.Id == itemReq.ProductUnitId)
                ?? throw new NotFoundException("ProductUnit", itemReq.ProductUnitId);

            var tierItem = priceListItems
                .Where(pli => pli.ProductUnitId == unit.Id && pli.MinQuantity <= itemReq.OrderedQty)
                .OrderByDescending(pli => pli.MinQuantity)
                .FirstOrDefault();
            var standardTierPrice = tierItem?.TierUnitPrice ?? unit.WholesalePrice;
            var floorPrice = unit.MinAllowedPrice;

            var overridePending = itemReq.UnitPrice < standardTierPrice || itemReq.UnitPrice < floorPrice;
            if (overridePending)
            {
                hasPriceOverridePending = true;
            }

            var lineNet = Math.Round(itemReq.OrderedQty * itemReq.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var lineVat = ZatcaInvoiceHelper.CalculateVat(lineNet, product.VatRate);
            var lineTotal = lineNet + lineVat;

            subTotal += lineNet;
            vatTotal += lineVat;

            var baseQty = Math.Round(itemReq.OrderedQty * unit.ConversionFactor, 3, MidpointRounding.AwayFromZero);
            if (!stocks.TryGetValue(product.Id, out var stock))
            {
                stock = WarehouseStock.Create(req.WarehouseId, product.Id);
                db.WarehouseStocks.Add(stock);
                stocks[product.Id] = stock;
            }

            var available = stock.ComputedAvailableQty;
            if (available < baseQty)
            {
                hasStockShortage = true;
                if (available > 0)
                {
                    stock.Reserve(available);
                }
            }
            else
            {
                stock.Reserve(baseQty);
            }

            pendingItems.Add((product, unit, itemReq, standardTierPrice, overridePending, lineVat, lineTotal));
        }

        var grandTotal = subTotal + vatTotal;
        var hasOverdueInvoices = await db.Invoices.AsNoTracking().AnyAsync(
            i => i.CustomerId == customer.Id && i.PaymentStatus != "Paid" && i.DueDateUtc < utcNow, ct);
        var projectedBalance = customer.CurrentBalance + grandTotal;
        var exceededCreditWarning = !customer.Grade.AllowCredit || projectedBalance > customer.CreditLimit || hasOverdueInvoices;
        var requiresFinanceCoApproval = exceededCreditWarning;

        var orderNumber = $"SO-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var order = SalesOrder.Submit(
            orderNumber,
            channel,
            customer.Id,
            currentUser.UserId,
            req.WarehouseId,
            customer.CurrentBalance,
            customer.CreditLimit,
            exceededCreditWarning,
            requiresFinanceCoApproval,
            hasStockShortage,
            hasPriceOverridePending,
            subTotal,
            vatTotal,
            grandTotal,
            command.IdempotencyKey,
            utcNow);

        db.SalesOrders.Add(order);

        foreach (var entry in pendingItems)
        {
            order.Items.Add(SalesOrderItem.Create(
                order.Id,
                entry.Product.Id,
                entry.Unit.Id,
                entry.Unit.ConversionFactor,
                entry.Req.OrderedQty,
                entry.StdPrice,
                entry.Req.UnitPrice,
                entry.OverridePending,
                entry.Product.WeightedAvgCost,
                entry.Vat,
                entry.LineTotal));
        }

        AuditAndOutboxHelper.EnqueueOutbox(db, "OrderSubmitted", new { order.Id, order.OrderNumber, order.CustomerId, order.TotalAmount, order.RequiresFinanceCoApproval, order.HasStockShortage, order.HasPriceOverridePending }, utcNow);
        if (requiresFinanceCoApproval)
        {
            AuditAndOutboxHelper.EnqueueOutbox(db, "CreditCoApprovalRequired", new { order.Id, order.OrderNumber, order.CustomerId, ProjectedBalance = projectedBalance, customer.CreditLimit, HasOverdueInvoices = hasOverdueInvoices }, utcNow);
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateSalesOrder", "SalesOrder", order.Id.ToString(), null, new { order.OrderNumber, order.TotalAmount, order.RequiresFinanceCoApproval, order.HasStockShortage, order.HasPriceOverridePending }, utcNow);
        await db.SaveChangesAsync(ct);

        var repUser = await db.Users.AsNoTracking()
            .Where(u => u.Id == order.RepId)
            .Select(u => new { u.FullNameAr, u.FullNameEn, u.EmployeeCode })
            .FirstOrDefaultAsync(ct);
        var repDisplayName = repUser?.FullNameAr ?? repUser?.FullNameEn ?? repUser?.EmployeeCode ?? order.RepId.ToString("D");

        await realtimeNotifier.NotifyOrderSubmittedAsync(
            new OrderSubmittedNotification(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.RepId,
                repDisplayName,
                order.WarehouseId,
                order.TotalAmount,
                order.ExceededCreditWarning,
                order.RequiresFinanceCoApproval,
                order.HasStockShortage,
                order.HasPriceOverridePending,
                order.CreatedAtUtc),
            ct);

        if (order.RequiresFinanceCoApproval)
        {
            await realtimeNotifier.NotifyCreditCoApprovalRequiredAsync(
                new CreditCoApprovalRequiredNotification(
                    order.Id,
                    order.OrderNumber,
                    order.CustomerId,
                    order.CustomerBalanceAtOrder,
                    order.CustomerCreditLimitAtOrder,
                    order.TotalAmount),
                ct);
        }

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> EditSubmittedOrderByManagerAsync(EditSubmittedOrderByManagerCommand command, CancellationToken ct)
    {
        var order = await db.SalesOrders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        order.EnsureEditableByManager(currentUser.HasPermission(CanonicalRolesAndPermissions.Permissions.OrdersEditSubmitted));

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var oldSnapshot = new
        {
            order.SubTotal,
            order.VatAmount,
            order.TotalAmount,
            Items = order.Items.Select(i => new { i.Id, i.ApprovedQty, i.UnitPrice }).ToList()
        };

        var productIds = order.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var products = await db.MasterProducts.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var stocks = await db.WarehouseStocks
            .Where(s => s.WarehouseId == order.WarehouseId && productIds.Contains(s.MasterProductId))
            .ToDictionaryAsync(s => s.MasterProductId, ct);

        foreach (var edit in command.Request.Items)
        {
            var line = order.Items.FirstOrDefault(i => i.Id == edit.SalesOrderItemId)
                ?? throw new NotFoundException("SalesOrderItem", edit.SalesOrderItemId);

            var product = products[line.MasterProductId];
            var oldBaseQty = Math.Round(line.ApprovedQty * line.ConversionFactor, 3, MidpointRounding.AwayFromZero);
            var newBaseQty = Math.Round(edit.ApprovedQty * line.ConversionFactor, 3, MidpointRounding.AwayFromZero);
            var deltaBaseQty = newBaseQty - oldBaseQty;

            if (stocks.TryGetValue(line.MasterProductId, out var stock))
            {
                stock.AdjustReservation(deltaBaseQty);
            }

            var floorPrice = Math.Round(product.MinAllowedSellingPrice * line.ConversionFactor, 2, MidpointRounding.AwayFromZero);
            var overridePending = edit.UnitPrice < line.StandardTierPrice || edit.UnitPrice < floorPrice;
            var lineNet = Math.Round(edit.ApprovedQty * edit.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var lineVat = ZatcaInvoiceHelper.CalculateVat(lineNet, product.VatRate);
            line.UpdateByManager(edit.ApprovedQty, edit.UnitPrice, overridePending, lineVat, lineNet + lineVat);
        }

        var subTotal = order.Items.Sum(i => Math.Round(i.ApprovedQty * i.UnitPrice, 2, MidpointRounding.AwayFromZero));
        var vatAmount = order.Items.Sum(i => i.VatAmount);
        var totalAmount = subTotal + vatAmount;
        var hasPriceOverridePending = order.Items.Any(i => i.PriceOverridePending);

        var hasStockShortage = false;
        foreach (var line in order.Items)
        {
            var requiredBase = Math.Round(line.ApprovedQty * line.ConversionFactor, 3, MidpointRounding.AwayFromZero);
            if (!stocks.TryGetValue(line.MasterProductId, out var stock) || stock.PhysicalQty < requiredBase)
            {
                hasStockShortage = true;
            }
        }

        order.RecordManagerEdit(currentUser.UserId, utcNow, subTotal, vatAmount, totalAmount, hasStockShortage, hasPriceOverridePending);

        var newSnapshot = new
        {
            order.SubTotal,
            order.VatAmount,
            order.TotalAmount,
            Items = order.Items.Select(i => new { i.Id, i.ApprovedQty, i.UnitPrice }).ToList()
        };

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "ManagerEditSalesOrder", "SalesOrder", order.Id.ToString(), oldSnapshot, newSnapshot, utcNow);
        await db.SaveChangesAsync(ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(
            new OrderUpdatedNotification(order.Id, order.OrderNumber, order.RepId, order.Status, order.TotalAmount, "ManagerEdit"),
            ct);

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> OverrideOrderPriceAsync(OverrideOrderPriceCommand command, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        order.ApprovePriceOverrides(currentUser.UserId);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "OverrideOrderPrice", "SalesOrder", order.Id.ToString(), null, new { order.OrderNumber, order.HasPriceOverridePending }, utcNow);
        await db.SaveChangesAsync(ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(
            new OrderUpdatedNotification(order.Id, order.OrderNumber, order.RepId, order.Status, order.TotalAmount, "PriceOverrideApproved"),
            ct);

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> CoApproveOrderCreditAsync(CoApproveOrderCreditCommand command, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        order.CoApproveCredit(currentUser.UserId, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CoApproveOrderCredit", "SalesOrder", order.Id.ToString(), null, new { order.OrderNumber, order.FinanceCoApprovedByUserId }, utcNow);
        await db.SaveChangesAsync(ct);

        await realtimeNotifier.NotifyOrderUpdatedAsync(
            new OrderUpdatedNotification(order.Id, order.OrderNumber, order.RepId, order.Status, order.TotalAmount, "CreditCoApproved"),
            ct);

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> ApproveSalesOrderAsync(ApproveSalesOrderCommand command, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        order.Approve(currentUser.UserId, utcNow);

        AuditAndOutboxHelper.EnqueueOutbox(db, "OrderApproved", new { order.Id, order.OrderNumber, order.WarehouseId, order.TotalAmount }, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "ApproveSalesOrder", "SalesOrder", order.Id.ToString(), null, new { order.OrderNumber, order.Status }, utcNow);
        await db.SaveChangesAsync(ct);

        await realtimeNotifier.NotifyOrderApprovedAsync(
            new OrderApprovedNotification(order.Id, order.OrderNumber, order.RepId, order.WarehouseId, order.TotalAmount, order.ApprovedAtUtc ?? utcNow),
            ct);

        return MapOrder(order);
    }

    public async Task<SalesOrderDto> RejectSalesOrderAsync(RejectSalesOrderCommand command, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = order.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var stocks = await db.WarehouseStocks
            .Where(s => s.WarehouseId == order.WarehouseId && productIds.Contains(s.MasterProductId))
            .ToDictionaryAsync(s => s.MasterProductId, ct);

        foreach (var item in order.Items)
        {
            if (stocks.TryGetValue(item.MasterProductId, out var stock))
            {
                var releaseQty = Math.Round(item.ApprovedQty * item.ConversionFactor, 3, MidpointRounding.AwayFromZero);
                stock.AdjustReservation(-releaseQty);
            }
        }

        order.Reject(currentUser.UserId, command.RejectionReason, utcNow);
        AuditAndOutboxHelper.EnqueueOutbox(db, "OrderRejected", new { order.Id, order.OrderNumber, order.RejectionReason }, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "RejectSalesOrder", "SalesOrder", order.Id.ToString(), null, new { order.OrderNumber, order.RejectionReason }, utcNow);
        await db.SaveChangesAsync(ct);

        await realtimeNotifier.NotifyOrderRejectedAsync(
            new OrderRejectedNotification(order.Id, order.OrderNumber, order.RepId, order.RejectionReason ?? command.RejectionReason, utcNow),
            ct);

        return MapOrder(order);
    }

    public async Task<InvoiceDto> ConfirmOrderShipmentAsync(ConfirmOrderShipmentCommand command, CancellationToken ct)
    {
        var existingInvoice = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.SalesOrderId == command.SalesOrderId, ct);
        if (existingInvoice is not null)
        {
            return MapInvoice(existingInvoice);
        }

        var order = await db.SalesOrders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .ThenInclude(c => c.Grade)
            .FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
            ?? throw new NotFoundException("SalesOrder", command.SalesOrderId);

        if (currentUser.HasRole(CanonicalRolesAndPermissions.Roles.WarehouseKeeper) &&
            !currentUser.HasAnyRole(CanonicalRolesAndPermissions.Roles.SuperAdmin, CanonicalRolesAndPermissions.Roles.ExecutiveManager))
        {
            var hasAccess = currentUser.DefaultWarehouseId == order.WarehouseId ||
                currentUser.AssignedWarehouseIds.Contains(order.WarehouseId);
            if (!hasAccess)
            {
                throw new ForbiddenDomainException($"Warehouse Keeper is not assigned to warehouse '{order.WarehouseId}'.");
            }
        }

        if (order.Status is not ("Approved" or "PreparingShipment"))
        {
            throw new BusinessRuleException($"Order '{order.OrderNumber}' must be Approved before shipment confirmation (current status: {order.Status}).");
        }

        var req = command.Request;
        var egs = await db.ZatcaDeviceSequences.FirstOrDefaultAsync(d => d.EgsDeviceId == req.EgsDeviceId.Trim(), ct)
            ?? throw new NotFoundException("ZatcaDeviceSequence", req.EgsDeviceId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = order.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var products = await db.MasterProducts.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var stocks = await db.WarehouseStocks
            .Where(s => s.WarehouseId == order.WarehouseId && productIds.Contains(s.MasterProductId))
            .ToDictionaryAsync(s => s.MasterProductId, ct);

        decimal subTotal = 0m;
        decimal vatAmount = 0m;

        var invoiceNumber = $"INV-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var zatcaUuid = Guid.CreateVersion7();

        foreach (var item in order.Items)
        {
            var shippedOverride = req.Items?.FirstOrDefault(i => i.SalesOrderItemId == item.Id);
            var shippedQty = shippedOverride?.ShippedQty ?? item.ApprovedQty;
            if (shippedQty > item.ApprovedQty)
            {
                throw new BusinessRuleException($"Shipped quantity ({shippedQty}) cannot exceed approved quantity ({item.ApprovedQty}) for line '{item.Id}'.");
            }

            item.ConfirmShipped(shippedQty);
            var shippedBaseQty = Math.Round(shippedQty * item.ConversionFactor, 3, MidpointRounding.AwayFromZero);
            var reservedBaseQty = Math.Round(item.ApprovedQty * item.ConversionFactor, 3, MidpointRounding.AwayFromZero);

            if (!stocks.TryGetValue(item.MasterProductId, out var stock))
            {
                throw new BusinessRuleException($"Warehouse stock record not found for product '{item.MasterProductId}' in warehouse '{order.WarehouseId}'.");
            }

            stock.DeductForShipment(shippedBaseQty, reservedBaseQty);

            var product = products[item.MasterProductId];
            var lineNet = Math.Round(shippedQty * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var lineVat = ZatcaInvoiceHelper.CalculateVat(lineNet, product.VatRate);
            subTotal += lineNet;
            vatAmount += lineVat;

            db.StockMovements.Add(StockMovement.Append(
                order.WarehouseId,
                item.MasterProductId,
                "SalesShipment",
                -shippedBaseQty,
                stock.PhysicalQty,
                item.UnitCostSnapshot,
                "SalesOrder",
                order.Id,
                currentUser.UserId,
                utcNow));
        }

        var grandTotal = subTotal + vatAmount;
        var invoiceType = string.IsNullOrWhiteSpace(req.InvoiceType)
            ? (string.IsNullOrWhiteSpace(order.Customer.VatNumber) ? "SimplifiedB2C" : "StandardB2B")
            : req.InvoiceType.Trim();

        var nextIcv = egs.CurrentIcv + 1;
        var prevHash = egs.LastInvoiceHash;
        var xmlHash = ZatcaInvoiceHelper.ComputeXmlHashBase64(invoiceNumber, zatcaUuid, nextIcv, prevHash, utcNow, grandTotal, vatAmount);
        var qrCode = ZatcaInvoiceHelper.BuildTlvQrBase64(utcNow, grandTotal, vatAmount, xmlHash);
        egs.AllocateNext(xmlHash, utcNow);

        var effectiveDays = order.Customer.CustomPaymentDays ?? order.Customer.Grade.DefaultPaymentDays;
        var dueDateUtc = utcNow.AddDays(Math.Max(0, effectiveDays));
        var invoice = Invoice.Create(
            invoiceNumber,
            order.Id,
            order.CustomerId,
            invoiceType,
            utcNow,
            dueDateUtc,
            subTotal,
            vatAmount,
            grandTotal,
            egs.EgsDeviceId,
            nextIcv,
            zatcaUuid,
            prevHash,
            xmlHash,
            qrCode);

        db.Invoices.Add(invoice);
        order.Customer.Debit(grandTotal);
        order.MarkShippedAndInvoiced();

        AuditAndOutboxHelper.EnqueueOutbox(db, "InvoiceIssued", new { invoice.Id, invoice.InvoiceNumber, invoice.SalesOrderId, invoice.CustomerId, invoice.TotalAmount, invoice.ZatcaIcv }, utcNow);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "ConfirmOrderShipment", "Invoice", invoice.Id.ToString(), null, new { invoice.InvoiceNumber, order.OrderNumber, invoice.TotalAmount, invoice.ZatcaIcv }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapInvoice(invoice);
    }

    public async Task<ZatcaDeviceSequenceDto> RegisterEgsDeviceAsync(RegisterEgsDeviceCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var egsId = req.EgsDeviceId.Trim();
        if (await db.ZatcaDeviceSequences.AnyAsync(d => d.EgsDeviceId == egsId, ct))
        {
            throw new ConflictException($"ZATCA EGS device '{egsId}' is already registered.");
        }

        if (req.WarehouseId.HasValue && !await db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId.Value, ct))
        {
            throw new NotFoundException("Warehouse", req.WarehouseId.Value);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var device = ZatcaDeviceSequence.Create(egsId, req.WarehouseId, ZatcaInvoiceHelper.InitialPreviousInvoiceHash, req.CertificateSerial, utcNow);
        db.ZatcaDeviceSequences.Add(device);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "RegisterEgsDevice", "ZatcaDeviceSequence", device.EgsDeviceId, null, new { device.EgsDeviceId, device.WarehouseId }, utcNow);
        await db.SaveChangesAsync(ct);

        return new ZatcaDeviceSequenceDto(device.EgsDeviceId, device.WarehouseId, device.CurrentIcv, device.LastInvoiceHash, device.CertificateSerial, device.UpdatedAtUtc);
    }

    public async Task<InvoicePrintDto> GetInvoicePrintAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Invoice", invoiceId);

        return new InvoicePrintDto(
            MapInvoice(invoice),
            invoice.Customer.NameAr,
            invoice.Customer.NameEn,
            invoice.Customer.VatNumber,
            invoice.SalesOrder.Items.Select(MapOrderItem).ToList());
    }

    public async Task<InvoiceDto> UploadInvoiceCopyAsync(UploadInvoiceCopyCommand command, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new NotFoundException("Invoice", command.InvoiceId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        invoice.AttachCopyUrl(command.UploadedInvoiceCopyUrl);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UploadInvoiceCopy", "Invoice", invoice.Id.ToString(), null, new { invoice.UploadedInvoiceCopyUrl }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapInvoice(invoice);
    }

    public async Task<SalesReturnDto> CreateSalesReturnAsync(CreateSalesReturnCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var invoice = await db.Invoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == req.OriginalInvoiceId, ct)
            ?? throw new NotFoundException("Invoice", req.OriginalInvoiceId);

        if (!await db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId, ct))
        {
            throw new NotFoundException("Warehouse", req.WarehouseId);
        }

        var egs = await db.ZatcaDeviceSequences.FirstOrDefaultAsync(d => d.EgsDeviceId == req.EgsDeviceId.Trim(), ct)
            ?? throw new NotFoundException("ZatcaDeviceSequence", req.EgsDeviceId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = req.Items.Select(i => i.MasterProductId).Distinct().ToList();
        var products = await db.MasterProducts.Include(p => p.Units).Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("MasterProduct", string.Join(',', productIds));
        }

        decimal subTotal = 0m;
        decimal vatAmount = 0m;
        var calculated = new List<(CreateSalesReturnItemRequest Req, Domain.Catalog.MasterProduct Product, Domain.Catalog.ProductUnit Unit, decimal Vat, decimal LineTotal)>();

        foreach (var itemReq in req.Items)
        {
            var product = products[itemReq.MasterProductId];
            var unit = product.Units.FirstOrDefault(u => u.Id == itemReq.ProductUnitId)
                ?? throw new NotFoundException("ProductUnit", itemReq.ProductUnitId);

            var lineNet = Math.Round(itemReq.ReturnedQty * itemReq.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var lineVat = ZatcaInvoiceHelper.CalculateVat(lineNet, product.VatRate);
            subTotal += lineNet;
            vatAmount += lineVat;
            calculated.Add((itemReq, product, unit, lineVat, lineNet + lineVat));
        }

        var grandTotal = subTotal + vatAmount;
        var creditNoteNumber = $"CN-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var zatcaUuid = Guid.CreateVersion7();
        var nextIcv = egs.CurrentIcv + 1;
        var xmlHash = ZatcaInvoiceHelper.ComputeXmlHashBase64(creditNoteNumber, zatcaUuid, nextIcv, egs.LastInvoiceHash, utcNow, grandTotal, vatAmount);
        var qrCode = ZatcaInvoiceHelper.BuildTlvQrBase64(utcNow, grandTotal, vatAmount, xmlHash);
        egs.AllocateNext(xmlHash, utcNow);

        var salesReturn = SalesReturn.Create(
            creditNoteNumber,
            invoice.Id,
            invoice.CustomerId,
            req.WarehouseId,
            req.ReturnReason,
            subTotal,
            vatAmount,
            grandTotal,
            egs.EgsDeviceId,
            nextIcv,
            zatcaUuid,
            xmlHash,
            qrCode,
            currentUser.UserId,
            utcNow);
        db.SalesReturns.Add(salesReturn);

        foreach (var entry in calculated)
        {
            salesReturn.Items.Add(SalesReturnItem.Create(
                salesReturn.Id,
                entry.Product.Id,
                entry.Unit.Id,
                entry.Req.ReturnedQty,
                entry.Req.IsSellableRestock,
                entry.Req.UnitPrice,
                entry.Vat,
                entry.LineTotal));

            if (entry.Req.IsSellableRestock)
            {
                var baseQty = Math.Round(entry.Req.ReturnedQty * entry.Unit.ConversionFactor, 3, MidpointRounding.AwayFromZero);
                var stock = await db.WarehouseStocks.FirstOrDefaultAsync(
                    s => s.WarehouseId == req.WarehouseId && s.MasterProductId == entry.Product.Id, ct);
                if (stock is null)
                {
                    stock = WarehouseStock.Create(req.WarehouseId, entry.Product.Id);
                    db.WarehouseStocks.Add(stock);
                }

                stock.RestockReturn(baseQty);
                db.StockMovements.Add(StockMovement.Append(
                    req.WarehouseId,
                    entry.Product.Id,
                    "SalesReturnRestock",
                    baseQty,
                    stock.PhysicalQty,
                    entry.Product.WeightedAvgCost,
                    "SalesReturn",
                    salesReturn.Id,
                    currentUser.UserId,
                    utcNow));
            }
        }

        invoice.Customer.Credit(grandTotal);
        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateSalesReturn", "SalesReturn", salesReturn.Id.ToString(), null, new { salesReturn.CreditNoteNumber, salesReturn.TotalAmount, salesReturn.ZatcaIcv }, utcNow);
        await db.SaveChangesAsync(ct);

        return new SalesReturnDto(
            salesReturn.Id,
            salesReturn.CreditNoteNumber,
            salesReturn.OriginalInvoiceId,
            salesReturn.CustomerId,
            salesReturn.WarehouseId,
            salesReturn.ReturnReason,
            salesReturn.SubTotal,
            salesReturn.VatAmount,
            salesReturn.TotalAmount,
            salesReturn.EgsDeviceId,
            salesReturn.ZatcaIcv,
            salesReturn.ZatcaUuid,
            salesReturn.ZatcaXmlHash,
            salesReturn.ZatcaQrCodeBase64,
            salesReturn.ZatcaSubmissionStatus,
            salesReturn.CreatedAtUtc,
            salesReturn.Items.Select(i => new SalesReturnItemDto(i.Id, i.MasterProductId, i.ProductUnitId, i.ReturnedQty, i.IsSellableRestock, i.UnitPrice, i.VatAmount, i.LineTotal)).ToList());
    }

    public async Task<DeliveryDispatchDto> CreateDeliveryDispatchAsync(CreateDeliveryDispatchCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (!await db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId, ct))
        {
            throw new NotFoundException("Warehouse", req.WarehouseId);
        }

        if (!await db.Users.AnyAsync(u => u.Id == req.DriverOrRepId, ct))
        {
            throw new NotFoundException("User", req.DriverOrRepId);
        }

        var distinctInvoiceIds = req.InvoiceIds.Distinct().ToList();
        var existingInvoicesCount = await db.Invoices.CountAsync(i => distinctInvoiceIds.Contains(i.Id), ct);
        if (existingInvoicesCount != distinctInvoiceIds.Count)
        {
            throw new NotFoundException("Invoice", string.Join(',', distinctInvoiceIds));
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var dispatchNumber = $"DSP-{utcNow:yyyyMMdd}-{Guid.CreateVersion7().ToString("N")[^8..].ToUpperInvariant()}";
        var dispatch = DeliveryDispatch.Create(dispatchNumber, req.WarehouseId, req.DriverOrRepId, req.VehiclePlateNumber, currentUser.UserId, utcNow);
        db.DeliveryDispatches.Add(dispatch);

        foreach (var invoiceId in distinctInvoiceIds)
        {
            dispatch.Items.Add(DeliveryDispatchItem.Create(dispatch.Id, invoiceId));
        }

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "CreateDeliveryDispatch", "DeliveryDispatch", dispatch.Id.ToString(), null, new { dispatch.DispatchNumber, InvoiceCount = distinctInvoiceIds.Count }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapDispatch(dispatch);
    }

    public async Task<DeliveryDispatchDto> UpdateDeliveryPodAsync(UpdateDeliveryPodCommand command, CancellationToken ct)
    {
        var dispatch = await db.DeliveryDispatches
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == command.DispatchId, ct)
            ?? throw new NotFoundException("DeliveryDispatch", command.DispatchId);

        var item = dispatch.Items.FirstOrDefault(i => i.InvoiceId == command.InvoiceId)
            ?? throw new NotFoundException("DeliveryDispatchItem", command.InvoiceId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        item.UpdatePod(command.Request.DeliveryStatus, command.Request.SignedPodImageUrl, command.Request.DeliveryNotes, utcNow);
        dispatch.RefreshStatus(utcNow);

        AuditAndOutboxHelper.AppendAudit(db, currentUser, "UpdateDeliveryPod", "DeliveryDispatch", dispatch.Id.ToString(), null, new { command.InvoiceId, item.DeliveryStatus, dispatch.Status }, utcNow);
        await db.SaveChangesAsync(ct);

        return MapDispatch(dispatch);
    }

    private static SalesOrderItemDto MapOrderItem(SalesOrderItem i) =>
        new(i.Id, i.MasterProductId, i.ProductUnitId, i.ConversionFactor, i.OrderedQty, i.ApprovedQty, i.ShippedQty, i.StandardTierPrice, i.UnitPrice, i.PriceOverridePending, i.PriceOverrideApprovedByUserId, i.VatAmount, i.LineTotal);

    private static SalesOrderDto MapOrder(SalesOrder o) =>
        new(
            o.Id, o.OrderNumber, o.OrderChannel, o.CustomerId, o.RepId, o.WarehouseId, o.Status,
            o.CustomerBalanceAtOrder, o.CustomerCreditLimitAtOrder, o.ExceededCreditWarning,
            o.RequiresFinanceCoApproval, o.FinanceCoApprovedByUserId, o.HasStockShortage,
            o.HasPriceOverridePending, o.ReservationExpiresAtUtc, o.ReservationState,
            o.SubTotal, o.VatAmount, o.TotalAmount, o.ApprovedByManagerId, o.ApprovedAtUtc,
            o.RejectionReason, o.EditedByManagerId, o.CreatedAtUtc,
            o.Items.Select(MapOrderItem).ToList());

    private static InvoiceDto MapInvoice(Invoice i) =>
        new(
            i.Id, i.InvoiceNumber, i.SalesOrderId, i.CustomerId, i.InvoiceType,
            i.IssueDateUtc, i.DueDateUtc, i.SubTotal, i.VatAmount, i.TotalAmount,
            i.PaidAmount, Math.Round(i.TotalAmount - i.PaidAmount, 2, MidpointRounding.AwayFromZero),
            i.PaymentStatus, i.UploadedInvoiceCopyUrl, i.EgsDeviceId, i.ZatcaIcv, i.ZatcaUuid,
            i.ZatcaPreviousInvoiceHash, i.ZatcaXmlHash, i.ZatcaQrCodeBase64, i.ZatcaSubmissionStatus);

    private static DeliveryDispatchDto MapDispatch(DeliveryDispatch d) =>
        new(
            d.Id, d.DispatchNumber, d.WarehouseId, d.DriverOrRepId, d.VehiclePlateNumber,
            d.Status, d.DispatchedAtUtc, d.CompletedAtUtc, d.CreatedAtUtc,
            d.Items.Select(i => new DeliveryDispatchItemDto(i.Id, i.InvoiceId, i.DeliveryStatus, i.DeliveredAtUtc, i.SignedPodImageUrl, i.DeliveryNotes)).ToList());
}
