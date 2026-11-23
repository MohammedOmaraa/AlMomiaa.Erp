using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Finance;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Domain.Procurement;
using AlMomiaa.Domain.Sales;
using AlMomiaa.Domain.SystemOps;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class DatabaseSchemaModelTests
{
    private static AlMomiaaDbContext CreateSqlServerModelContext()
    {
        var options = new DbContextOptionsBuilder<AlMomiaaDbContext>()
            .UseSqlServer("Server=localhost;Database=AlMomiaa;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new AlMomiaaDbContext(options);
    }

    [Fact]
    public void Model_maps_all_8_canonical_schemas_and_44_tables_from_05_Database()
    {
        using var db = CreateSqlServerModelContext();
        var mappedTables = db.Model.GetEntityTypes()
            .Select(e => $"{e.GetSchema()}.{e.GetTableName()}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var schemas = db.Model.GetEntityTypes()
            .Select(e => e.GetSchema() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["cat", "cust", "fin", "id", "inv", "ops", "proc", "sales"], schemas);
        Assert.Equal(44, mappedTables.Length);

        // Spot-check key tables across every schema
        Assert.Contains("id.Users", mappedTables);
        Assert.Contains("id.RefreshTokenSessions", mappedTables);
        Assert.Contains("cat.MasterProducts", mappedTables);
        Assert.Contains("cat.SupplierProducts", mappedTables);
        Assert.Contains("cust.CustomerGrades", mappedTables);
        Assert.Contains("cust.Customers", mappedTables);
        Assert.Contains("inv.Warehouses", mappedTables);
        Assert.Contains("inv.WarehouseStocks", mappedTables);
        Assert.Contains("inv.StockMovements", mappedTables);
        Assert.Contains("proc.Suppliers", mappedTables);
        Assert.Contains("proc.Containers", mappedTables);
        Assert.Contains("sales.SalesOrders", mappedTables);
        Assert.Contains("sales.ZatcaDeviceSequences", mappedTables);
        Assert.Contains("sales.Invoices", mappedTables);
        Assert.Contains("sales.DeliveryDispatches", mappedTables);
        Assert.Contains("fin.FiscalPeriods", mappedTables);
        Assert.Contains("fin.JournalLines", mappedTables);
        Assert.Contains("fin.DailyFinancialSnapshots", mappedTables);
        Assert.Contains("ops.AuditLogs", mappedTables);
        Assert.Contains("ops.OutboxMessages", mappedTables);
        Assert.Contains("ops.GpsRouteHistory", mappedTables);
    }

    [Fact]
    public void Mutable_aggregates_have_rowversion_and_append_only_tables_have_partition_ready_composite_keys()
    {
        using var db = CreateSqlServerModelContext();

        Type[] rowVersionTypes =
        [
            typeof(RefreshTokenSession),
            typeof(MasterProduct),
            typeof(Customer),
            typeof(WarehouseStock),
            typeof(StockTransfer),
            typeof(Supplier),
            typeof(PurchaseOrder),
            typeof(Container),
            typeof(SalesOrder),
            typeof(Invoice),
            typeof(DeliveryDispatch),
            typeof(FinancialAccount),
            typeof(Collection)
        ];

        foreach (var type in rowVersionTypes)
        {
            var entity = db.Model.FindEntityType(type);
            Assert.NotNull(entity);
            var rv = entity.FindProperty("RowVersion");
            Assert.NotNull(rv);
            Assert.True(rv.IsConcurrencyToken, $"{type.Name}.RowVersion must be a concurrency token.");
        }

        // Partition-ready clustered composite primary keys
        Assert.Equal(["CreatedAtUtc", "Id"], db.Model.FindEntityType(typeof(StockMovement))!.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(["CreatedAtUtc", "Id"], db.Model.FindEntityType(typeof(JournalLine))!.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(["TimestampUtc", "Id"], db.Model.FindEntityType(typeof(AuditLog))!.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(["RecordedAtUtc", "Id"], db.Model.FindEntityType(typeof(GpsRouteHistory))!.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Deferred_fks_computed_columns_check_constraints_filtered_indexes_and_reference_seeds_are_configured()
    {
        using var db = CreateSqlServerModelContext();
        var designModel = db.GetService<IDesignTimeModel>().Model;

        // Deferred FKs
        var userWhFks = designModel.FindEntityType(typeof(UserWarehouse))!.GetForeignKeys().Select(fk => fk.GetConstraintName()).ToArray();
        Assert.Contains("FK_UserWarehouses_Warehouse", userWhFks);

        var supplierProdFks = designModel.FindEntityType(typeof(SupplierProduct))!.GetForeignKeys().Select(fk => fk.GetConstraintName()).ToArray();
        Assert.Contains("FK_SupplierProducts_Supplier", supplierProdFks);

        // Persisted computed columns
        Assert.True(designModel.FindEntityType(typeof(WarehouseStock))!.FindProperty(nameof(WarehouseStock.AvailableQty))!.GetIsStored());
        Assert.True(designModel.FindEntityType(typeof(PurchaseOrderItem))!.FindProperty(nameof(PurchaseOrderItem.LineTotalSAR))!.GetIsStored());
        Assert.True(designModel.FindEntityType(typeof(Container))!.FindProperty(nameof(Container.TotalLandedCostSAR))!.GetIsStored());
        Assert.True(designModel.FindEntityType(typeof(Invoice))!.FindProperty(nameof(Invoice.RemainingBalance))!.GetIsStored());

        // Check constraints
        Assert.Contains(designModel.FindEntityType(typeof(WarehouseStock))!.GetCheckConstraints(), c => c.Name == "CK_WarehouseStocks_NonNegative");
        Assert.Contains(designModel.FindEntityType(typeof(FiscalPeriod))!.GetCheckConstraints(), c => c.Name == "CK_FiscalPeriods_Month");
        Assert.Contains(designModel.FindEntityType(typeof(JournalLine))!.GetCheckConstraints(), c => c.Name == "CK_JournalLines_NonNegative");

        // Filtered indexes
        Assert.Contains(designModel.FindEntityType(typeof(FinancialAccount))!.GetIndexes(), i => i.GetDatabaseName() == "UX_FinancialAccounts_OwnerRep" && i.GetFilter() is not null);
        Assert.Contains(designModel.FindEntityType(typeof(Collection))!.GetIndexes(), i => i.GetDatabaseName() == "UX_Collections_ProofHash" && i.GetFilter() is not null);
        Assert.Contains(designModel.FindEntityType(typeof(OutboxMessage))!.GetIndexes(), i => i.GetDatabaseName() == "IX_Outbox_Unprocessed" && i.GetFilter() is not null);

        // Canonical reference seeds: 7 roles, 4 customer grades, 0 users
        Assert.Equal(7, designModel.FindEntityType(typeof(Role))!.GetSeedData().Count());
        Assert.Equal(4, designModel.FindEntityType(typeof(CustomerGrade))!.GetSeedData().Count());
        Assert.Empty(designModel.FindEntityType(typeof(User))!.GetSeedData());
    }
}
