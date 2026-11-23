using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AlMomiaa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullErpSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "cat");

            migrationBuilder.EnsureSchema(
                name: "fin");

            migrationBuilder.EnsureSchema(
                name: "proc");

            migrationBuilder.EnsureSchema(
                name: "cust");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => new { x.TimestampUtc, x.Id })
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "cat",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerGrades",
                schema: "cust",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeCode = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    DefaultPaymentDays = table.Column<int>(type: "int", nullable: false),
                    AllowCredit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyFinancialSnapshots",
                schema: "fin",
                columns: table => new
                {
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalMainSafeCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalRepCustodyCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalBankBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalReceivables = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalPayables = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalInventoryValuation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    DailyGrossSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    DailyGrossProfit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    DailyCollectionsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyFinancialSnapshots", x => x.SnapshotDate);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OwnerRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Iban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Users_OwnerRepId",
                        column: x => x.OwnerRepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    FiscalMonth = table.Column<int>(type: "int", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                    table.CheckConstraint("CK_FiscalPeriods_Month", "[FiscalMonth] BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_FiscalPeriods_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GpsRouteHistory",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    AccuracyMeters = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    NearCustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsRouteHistory", x => new { x.RecordedAtUtc, x.Id })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_GpsRouteHistory_Users_RepId",
                        column: x => x.RepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Saudi Arabia"),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentPayableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    WarehouseType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "MainWarehouse"),
                    AssignedRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehiclePlateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Riyadh"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Users_AssignedRepId",
                        column: x => x.AssignedRepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasterProducts",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    BaseUnit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WeightedAvgCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0.0000m),
                    WholesaleSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetailSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinAllowedSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 15.00m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterProducts_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "cat",
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterProducts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "cat",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FiscalPeriodId = table.Column<int>(type: "int", nullable: false),
                    PostingDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    OriginalValueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReferenceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_FiscalPeriods_FiscalPeriodId",
                        column: x => x.FiscalPeriodId,
                        principalSchema: "fin",
                        principalTable: "FiscalPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "cust",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Riyadh"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VatNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    PriceListId = table.Column<int>(type: "int", nullable: true),
                    CustomPaymentDays = table.Column<int>(type: "int", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    AssignedRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_CustomerGrades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "cust",
                        principalTable: "CustomerGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "cat",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_Users_AssignedRepId",
                        column: x => x.AssignedRepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryDispatches",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    DriverOrRepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehiclePlateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PreparedAtWarehouse"),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryDispatches_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryDispatches_Users_DriverOrRepId",
                        column: x => x.DriverOrRepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryDispatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetWarehouseId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalAmountSAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "proc",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "int", nullable: false),
                    DestinationWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DispatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Users_DispatchedByUserId",
                        column: x => x.DispatchedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZatcaDeviceSequences",
                schema: "sales",
                columns: table => new
                {
                    EgsDeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    CurrentIcv = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastInvoiceHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CertificateSerial = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZatcaDeviceSequences", x => x.EgsDeviceId);
                    table.ForeignKey(
                        name: "FK_ZatcaDeviceSequences_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnits",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitNameAr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitNameEn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WholesalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetailPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinAllowedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDefaultUnit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUnits_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PhysicalBalanceAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => new { x.CreatedAtUtc, x.Id })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_StockMovements_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProducts",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupplierBarcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastPurchasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsPreferredSupplier = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierProducts_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProducts_Supplier",
                        column: x => x.SupplierId,
                        principalSchema: "proc",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhysicalQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0.000m),
                    ReservedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0.000m),
                    IncomingQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0.000m),
                    AvailableQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, computedColumnSql: "([PhysicalQty] - [ReservedQty])", stored: true),
                    MinReorderQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 10.000m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.CheckConstraint("CK_WarehouseStocks_NonNegative", "[PhysicalQty] >= 0 AND [ReservedQty] >= 0");
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Collections",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: false),
                    IsCustomerConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CustomerConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentProofImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentProofImageHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    BankReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetFinancialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedByAccountantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalValueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "cust",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Collections_FinancialAccounts_TargetFinancialAccountId",
                        column: x => x.TargetFinancialAccountId,
                        principalSchema: "fin",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Collections_Users_RepId",
                        column: x => x.RepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Collections_Users_VerifiedByAccountantId",
                        column: x => x.VerifiedByAccountantId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => new { x.CreatedAtUtc, x.Id })
                        .Annotation("SqlServer:Clustered", true);
                    table.CheckConstraint("CK_JournalLines_NonNegative", "[DebitAmount] >= 0 AND [CreditAmount] >= 0 AND ([DebitAmount] > 0 OR [CreditAmount] > 0)");
                    table.ForeignKey(
                        name: "FK_JournalLines_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "cust",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "fin",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "fin",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "proc",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderChannel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "PreSales"),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerBalanceAtOrder = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CustomerCreditLimitAtOrder = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExceededCreditWarning = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresFinanceCoApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FinanceCoApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinanceCoApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasStockShortage = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasPriceOverridePending = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReservationExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReservationState = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false, defaultValue: "Locked"),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedByManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EditedByManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "cust",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_ApprovedByManagerId",
                        column: x => x.ApprovedByManagerId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_EditedByManagerId",
                        column: x => x.EditedByManagerId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_FinanceCoApprovedByUserId",
                        column: x => x.FinanceCoApprovedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_RepId",
                        column: x => x.RepId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContainerNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillOfLading = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingLine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PortOfArrival = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstimatedArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualArrivalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SupplierGoodsCostSAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FreightAndCustomsCostSAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalLandedCostSAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "([SupplierGoodsCostSAR] + [FreightAndCustomsCostSAR])", stored: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "proc",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0.000m),
                    UnitCostSAR = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotalSAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "(CAST([OrderedQty] * [UnitCostSAR] AS DECIMAL(18,2)))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "proc",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferItems",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    DispatchedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferItems_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalSchema: "inv",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                schema: "cat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 1.000m),
                    TierUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "cat",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItems_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "cat",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "([TotalAmount] - [PaidAmount])", stored: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Unpaid"),
                    UploadedInvoiceCopyUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EgsDeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ZatcaIcv = table.Column<long>(type: "bigint", nullable: false),
                    ZatcaUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZatcaPreviousInvoiceHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ZatcaXmlHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ZatcaQrCodeBase64 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ZatcaSubmissionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    ZatcaXmlStorageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "cust",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_ZatcaDeviceSequences_EgsDeviceId",
                        column: x => x.EgsDeviceId,
                        principalSchema: "sales",
                        principalTable: "ZatcaDeviceSequences",
                        principalColumn: "EgsDeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 1.0000m),
                    OrderedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ApprovedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ShippedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    StandardTierPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceOverridePending = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PriceOverrideApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitCostSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "cat",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Users_PriceOverrideApprovedByUserId",
                        column: x => x.PriceOverrideApprovedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrnNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalSchema: "proc",
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "proc",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CollectionAllocations",
                schema: "fin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionAllocations_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalSchema: "fin",
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionAllocations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryDispatchItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryDispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PreparedAtWarehouse"),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedPodImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryDispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryDispatchItems_DeliveryDispatches_DeliveryDispatchId",
                        column: x => x.DeliveryDispatchId,
                        principalSchema: "sales",
                        principalTable: "DeliveryDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryDispatchItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "sales",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturns",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ReturnReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EgsDeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ZatcaIcv = table.Column<long>(type: "bigint", nullable: false),
                    ZatcaUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZatcaXmlHash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ZatcaQrCodeBase64 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ZatcaSubmissionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "cust",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Invoices_OriginalInvoiceId",
                        column: x => x.OriginalInvoiceId,
                        principalSchema: "sales",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "id",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_ZatcaDeviceSequences_EgsDeviceId",
                        column: x => x.EgsDeviceId,
                        principalSchema: "sales",
                        principalTable: "ZatcaDeviceSequences",
                        principalColumn: "EgsDeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItems",
                schema: "proc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    AllocatedLandedUnitCostSAR = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "proc",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnItems",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnedQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsSellableRestock = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnItems_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "cat",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnItems_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "cat",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnItems_SalesReturns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalSchema: "sales",
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "cust",
                table: "CustomerGrades",
                columns: new[] { "Id", "AllowCredit", "DefaultPaymentDays", "GradeCode", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, true, 60, "A", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, true, 45, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, true, 30, "C", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "cust",
                table: "CustomerGrades",
                columns: new[] { "Id", "DefaultPaymentDays", "GradeCode", "UpdatedAtUtc" },
                values: new object[] { 4, 0, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_UserWarehouses_WarehouseId",
                schema: "id",
                table: "UserWarehouses",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                schema: "cat",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionAllocations_CollectionId",
                schema: "fin",
                table: "CollectionAllocations",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionAllocations_InvoiceId",
                schema: "fin",
                table: "CollectionAllocations",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_CustomerId",
                schema: "fin",
                table: "Collections",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_RepId",
                schema: "fin",
                table: "Collections",
                column: "RepId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_TargetFinancialAccountId",
                schema: "fin",
                table: "Collections",
                column: "TargetFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_VerifiedByAccountantId",
                schema: "fin",
                table: "Collections",
                column: "VerifiedByAccountantId");

            migrationBuilder.CreateIndex(
                name: "UQ_Collections_IdempotencyKey",
                schema: "fin",
                table: "Collections",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Collections_ReceiptNumber",
                schema: "fin",
                table: "Collections",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Collections_ProofHash",
                schema: "fin",
                table: "Collections",
                column: "PaymentProofImageHash",
                unique: true,
                filter: "[PaymentProofImageHash] IS NOT NULL AND [Status] <> 'Rejected'");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_PurchaseOrderId",
                schema: "proc",
                table: "Containers",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerGrades_GradeCode",
                schema: "cust",
                table: "CustomerGrades",
                column: "GradeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AssignedRep",
                schema: "cust",
                table: "Customers",
                column: "AssignedRepId")
                .Annotation("SqlServer:Include", new[] { "NameAr", "GradeId", "PriceListId", "CreditLimit", "CurrentBalance" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GradeId",
                schema: "cust",
                table: "Customers",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PriceListId",
                schema: "cust",
                table: "Customers",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "UQ_Customers_CustomerCode",
                schema: "cust",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDispatches_CreatedByUserId",
                schema: "sales",
                table: "DeliveryDispatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDispatches_DriverOrRepId",
                schema: "sales",
                table: "DeliveryDispatches",
                column: "DriverOrRepId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDispatches_WarehouseId",
                schema: "sales",
                table: "DeliveryDispatches",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_DeliveryDispatches_DispatchNumber",
                schema: "sales",
                table: "DeliveryDispatches",
                column: "DispatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryDispatchItems_InvoiceId",
                schema: "sales",
                table: "DeliveryDispatchItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "UQ_Dispatch_Invoice",
                schema: "sales",
                table: "DeliveryDispatchItems",
                columns: new[] { "DeliveryDispatchId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_FinancialAccounts_AccountCode",
                schema: "fin",
                table: "FinancialAccounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FinancialAccounts_OwnerRep",
                schema: "fin",
                table: "FinancialAccounts",
                column: "OwnerRepId",
                unique: true,
                filter: "[OwnerRepId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_ClosedByUserId",
                schema: "fin",
                table: "FiscalPeriods",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_FiscalPeriod_YearMonth",
                schema: "fin",
                table: "FiscalPeriods",
                columns: new[] { "FiscalYear", "FiscalMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_GoodsReceiptId",
                schema: "proc",
                table: "GoodsReceiptItems",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_MasterProductId",
                schema: "proc",
                table: "GoodsReceiptItems",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ContainerId",
                schema: "proc",
                table: "GoodsReceipts",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                schema: "proc",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceivedByUserId",
                schema: "proc",
                table: "GoodsReceipts",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId",
                schema: "proc",
                table: "GoodsReceipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_GoodsReceipts_GrnNumber",
                schema: "proc",
                table: "GoodsReceipts",
                column: "GrnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GpsRouteHistory_RepId",
                schema: "ops",
                table: "GpsRouteHistory",
                column: "RepId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Customer_Status",
                schema: "sales",
                table: "Invoices",
                columns: new[] { "CustomerId", "PaymentStatus", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_Invoices_InvoiceNumber",
                schema: "sales",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Invoices_SalesOrderId",
                schema: "sales",
                table: "Invoices",
                column: "SalesOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Invoices_ZatcaUuid",
                schema: "sales",
                table: "Invoices",
                column: "ZatcaUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Zatca_Device_Icv",
                schema: "sales",
                table: "Invoices",
                columns: new[] { "EgsDeviceId", "ZatcaIcv" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreatedByUserId",
                schema: "fin",
                table: "JournalEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_FiscalPeriodId",
                schema: "fin",
                table: "JournalEntries",
                column: "FiscalPeriodId");

            migrationBuilder.CreateIndex(
                name: "UQ_JournalEntries_EntryNumber",
                schema: "fin",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_CustomerId",
                schema: "fin",
                table: "JournalLines",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_FinancialAccountId",
                schema: "fin",
                table: "JournalLines",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalEntryId",
                schema: "fin",
                table: "JournalLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_SupplierId",
                schema: "fin",
                table: "JournalLines",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_BrandId",
                schema: "cat",
                table: "MasterProducts",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_Category",
                schema: "cat",
                table: "MasterProducts",
                column: "CategoryId")
                .Annotation("SqlServer:Include", new[] { "SKU", "NameAr", "WholesaleSellingPrice", "RetailSellingPrice", "MinAllowedSellingPrice" });

            migrationBuilder.CreateIndex(
                name: "UQ_MasterProducts_Barcode",
                schema: "cat",
                table: "MasterProducts",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_MasterProducts_SKU",
                schema: "cat",
                table: "MasterProducts",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_Unprocessed",
                schema: "ops",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "CreatedAtUtc" },
                filter: "[ProcessedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_ProductUnitId",
                schema: "cat",
                table: "PriceListItems",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_PriceList_Unit_MinQty",
                schema: "cat",
                table: "PriceListItems",
                columns: new[] { "PriceListId", "ProductUnitId", "MinQuantity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PriceLists_PriceListCode",
                schema: "cat",
                table: "PriceLists",
                column: "PriceListCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_MasterProductId",
                schema: "cat",
                table: "ProductUnits",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductUnits_Barcode",
                schema: "cat",
                table: "ProductUnits",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_MasterProductId",
                schema: "proc",
                table: "PurchaseOrderItems",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                schema: "proc",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedByUserId",
                schema: "proc",
                table: "PurchaseOrders",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "proc",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TargetWarehouseId",
                schema: "proc",
                table: "PurchaseOrders",
                column: "TargetWarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_PurchaseOrders_PoNumber",
                schema: "proc",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_MasterProductId",
                schema: "sales",
                table: "SalesOrderItems",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_PriceOverrideApprovedByUserId",
                schema: "sales",
                table: "SalesOrderItems",
                column: "PriceOverrideApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_ProductUnitId",
                schema: "sales",
                table: "SalesOrderItems",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId",
                schema: "sales",
                table: "SalesOrderItems",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ApprovedByManagerId",
                schema: "sales",
                table: "SalesOrders",
                column: "ApprovedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId",
                schema: "sales",
                table: "SalesOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_EditedByManagerId",
                schema: "sales",
                table: "SalesOrders",
                column: "EditedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_FinanceCoApprovedByUserId",
                schema: "sales",
                table: "SalesOrders",
                column: "FinanceCoApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_RepId",
                schema: "sales",
                table: "SalesOrders",
                column: "RepId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Status_Created",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "Status", "CreatedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_WarehouseId",
                schema: "sales",
                table: "SalesOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrders_IdempotencyKey",
                schema: "sales",
                table: "SalesOrders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SalesOrders_OrderNumber",
                schema: "sales",
                table: "SalesOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnItems_MasterProductId",
                schema: "sales",
                table: "SalesReturnItems",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnItems_ProductUnitId",
                schema: "sales",
                table: "SalesReturnItems",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnItems_SalesReturnId",
                schema: "sales",
                table: "SalesReturnItems",
                column: "SalesReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_ApprovedByUserId",
                schema: "sales",
                table: "SalesReturns",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CustomerId",
                schema: "sales",
                table: "SalesReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_EgsDeviceId",
                schema: "sales",
                table: "SalesReturns",
                column: "EgsDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_OriginalInvoiceId",
                schema: "sales",
                table: "SalesReturns",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_WarehouseId",
                schema: "sales",
                table: "SalesReturns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_SalesReturns_CreditNoteNumber",
                schema: "sales",
                table: "SalesReturns",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SalesReturns_ZatcaUuid",
                schema: "sales",
                table: "SalesReturns",
                column: "ZatcaUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MasterProductId",
                schema: "inv",
                table: "StockMovements",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId",
                schema: "inv",
                table: "StockMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_MasterProductId",
                schema: "inv",
                table: "StockTransferItems",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferItems_StockTransferId",
                schema: "inv",
                table: "StockTransferItems",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationWarehouseId",
                schema: "inv",
                table: "StockTransfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DispatchedByUserId",
                schema: "inv",
                table: "StockTransfers",
                column: "DispatchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ReceivedByUserId",
                schema: "inv",
                table: "StockTransfers",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceWarehouseId",
                schema: "inv",
                table: "StockTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "UQ_StockTransfers_TransferNumber",
                schema: "inv",
                table: "StockTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProducts_MasterProductId",
                schema: "cat",
                table: "SupplierProducts",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "UQ_Supplier_MasterProduct",
                schema: "cat",
                table: "SupplierProducts",
                columns: new[] { "SupplierId", "MasterProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Suppliers_SupplierCode",
                schema: "proc",
                table: "Suppliers",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_AssignedRepId",
                schema: "inv",
                table: "Warehouses",
                column: "AssignedRepId");

            migrationBuilder.CreateIndex(
                name: "UQ_Warehouses_WarehouseCode",
                schema: "inv",
                table: "Warehouses",
                column: "WarehouseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_MasterProductId",
                schema: "inv",
                table: "WarehouseStocks",
                column: "MasterProductId");

            migrationBuilder.CreateIndex(
                name: "UQ_Warehouse_Product",
                schema: "inv",
                table: "WarehouseStocks",
                columns: new[] { "WarehouseId", "MasterProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZatcaDeviceSequences_WarehouseId",
                schema: "sales",
                table: "ZatcaDeviceSequences",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWarehouses_Warehouse",
                schema: "id",
                table: "UserWarehouses",
                column: "WarehouseId",
                principalSchema: "inv",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWarehouses_Warehouse",
                schema: "id",
                table: "UserWarehouses");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "CollectionAllocations",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "DailyFinancialSnapshots",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "DeliveryDispatchItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItems",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "GpsRouteHistory",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "JournalLines",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PriceListItems",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "SalesOrderItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturnItems",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockTransferItems",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "SupplierProducts",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "WarehouseStocks",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Collections",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "DeliveryDispatches",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "JournalEntries",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "ProductUnits",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "SalesReturns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "Containers",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "FiscalPeriods",
                schema: "fin");

            migrationBuilder.DropTable(
                name: "MasterProducts",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "Brands",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "cat");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "ZatcaDeviceSequences",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "proc");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "cust");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "CustomerGrades",
                schema: "cust");

            migrationBuilder.DropTable(
                name: "PriceLists",
                schema: "cat");

            migrationBuilder.DropIndex(
                name: "IX_UserWarehouses_WarehouseId",
                schema: "id",
                table: "UserWarehouses");
        }
    }
}
