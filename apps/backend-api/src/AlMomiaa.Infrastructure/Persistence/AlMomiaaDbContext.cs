using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Finance;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Domain.Procurement;
using AlMomiaa.Domain.Sales;
using AlMomiaa.Domain.SystemOps;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence;

public sealed class AlMomiaaDbContext(DbContextOptions<AlMomiaaDbContext> options) : DbContext(options)
{
    // [id] Identity & Access
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();
    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();

    // [cat] Catalog, Price Lists & Multi-Supplier
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<MasterProduct> MasterProducts => Set<MasterProduct>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<SupplierProduct> SupplierProducts => Set<SupplierProduct>();

    // [cust] Customers & Grades
    public DbSet<CustomerGrade> CustomerGrades => Set<CustomerGrade>();
    public DbSet<Customer> Customers => Set<Customer>();

    // [inv] Inventory & Warehouses
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

    // [proc] Procurement & Containers
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();

    // [sales] Sales Orders, ZATCA Invoices, Returns & Delivery
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<ZatcaDeviceSequence> ZatcaDeviceSequences => Set<ZatcaDeviceSequence>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<DeliveryDispatch> DeliveryDispatches => Set<DeliveryDispatch>();
    public DbSet<DeliveryDispatchItem> DeliveryDispatchItems => Set<DeliveryDispatchItem>();

    // [fin] Finance, Custody, Fiscal Periods & Ledger
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionAllocation> CollectionAllocations => Set<CollectionAllocation>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<DailyFinancialSnapshot> DailyFinancialSnapshots => Set<DailyFinancialSnapshot>();

    // [sys] Audit, Outbox & GPS
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<GpsRouteHistory> GpsRouteHistories => Set<GpsRouteHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("id");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlMomiaaDbContext).Assembly);

        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersion = entityType.FindProperty("RowVersion");
                if (rowVersion is not null)
                {
                    rowVersion.SetDefaultValueSql("randomblob(8)");
                }
            }
        }
    }
}
