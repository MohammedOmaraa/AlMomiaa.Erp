using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Customers;
using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class CatalogWarehouseAndRepCustomerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateProduct_WithMultiWarehouseInitialStocks_PersistsOpeningMovements_AndReturnsTotalAndPerWarehouseBreakdown()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        await SeedUserAsync(factory, "ADM-CAT-1", "+966533000001", "AdminPass#123", CanonicalRolesAndPermissions.Roles.SuperAdmin, null, null);
        await SeedUserAsync(factory, "REP-CAT-1", "+966533000002", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);

        int categoryId;
        int brandId;
        int[] warehouseIds;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            var category = Category.Create(null, "مواد غذائية", "Foodstuffs", 1);
            var brand = Brand.Create("المومياء الذهبية", "AlMomiaa Gold");
            db.Categories.Add(category);
            db.Brands.Add(brand);
            await db.SaveChangesAsync();

            categoryId = category.Id;
            brandId = brand.Id;
            warehouseIds = await db.Warehouses.AsNoTracking().OrderBy(w => w.Id).Select(w => w.Id).Take(2).ToArrayAsync();
        }

        Assert.True(warehouseIds.Length >= 2, "Expected at least 2 seeded warehouses.");

        using var adminClient = factory.CreateClient();
        var adminAuth = await LoginAsync(adminClient, "ADM-CAT-1", "AdminPass#123");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        var createReq = new CreateMasterProductRequest(
            SKU: "SKU-MW-001",
            Barcode: "6281000500001",
            NameAr: "زيت زيتون بكر 1 لتر",
            NameEn: "Extra Virgin Olive Oil 1L",
            CategoryId: categoryId,
            BrandId: brandId,
            BaseUnit: "Piece",
            WeightedAvgCost: 10m,
            WholesaleSellingPrice: 20m,
            RetailSellingPrice: 25m,
            MinAllowedSellingPrice: 15m,
            VatRate: 15m,
            Units:
            [
                new CreateProductUnitRequest("حبة", "Piece", 1m, "6281000500001", 20m, 25m, 15m, true),
                new CreateProductUnitRequest("كرتون (12 حبة)", "Carton (12 pcs)", 12m, "6281000500012", 228m, 285m, 180m, false)
            ],
            InitialWarehouseStocks:
            [
                new InitialWarehouseStockRequest(warehouseIds[0], 100m, 15m),
                new InitialWarehouseStockRequest(warehouseIds[1], 40m, 10m)
            ]);

        var createResp = await adminClient.PostAsJsonAsync("/api/v1/catalog/products", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<MasterProductDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(2, created.Units.Count);
        Assert.Equal(10m, created.WeightedAvgCost);

        // Verify immutable OpeningBalance StockMovements were recorded
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            var movements = await db.StockMovements
                .Where(m => m.MasterProductId == created.Id && m.MovementType == "OpeningBalance")
                .OrderBy(m => m.WarehouseId)
                .ToListAsync();

            Assert.Equal(2, movements.Count);
            Assert.Equal(100m, movements[0].QuantityDelta);
            Assert.Equal(40m, movements[1].QuantityDelta);
        }

        // Query as SALES_REP: cost must be masked, availability endpoint must show total=140 & per-warehouse breakdown
        using var repClient = factory.CreateClient();
        var repAuth = await LoginAsync(repClient, "REP-CAT-1", "RepPass#123");
        repClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repAuth.AccessToken);

        var repProductsResp = await repClient.GetAsync("/api/v1/catalog/products?search=SKU-MW-001");
        Assert.Equal(HttpStatusCode.OK, repProductsResp.StatusCode);
        var repProducts = await repProductsResp.Content.ReadFromJsonAsync<PagedResult<MasterProductDto>>(JsonOptions);
        Assert.NotNull(repProducts);
        Assert.Single(repProducts.Items);
        Assert.Null(repProducts.Items[0].WeightedAvgCost);

        var availabilityResp = await repClient.GetAsync($"/api/v1/catalog/products/{created.Id}/availability");
        Assert.Equal(HttpStatusCode.OK, availabilityResp.StatusCode);
        var availabilityJson = await availabilityResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("weightedAvgCost", availabilityJson, StringComparison.OrdinalIgnoreCase);

        var availability = JsonSerializer.Deserialize<ProductAvailabilityDto>(availabilityJson, JsonOptions);
        Assert.NotNull(availability);
        Assert.Equal(140m, availability.TotalPhysicalQty);
        Assert.Equal(140m, availability.TotalAvailableQty);
        Assert.Equal(2, availability.WarehouseStocks.Count);
        Assert.Contains(availability.WarehouseStocks, w => w.WarehouseId == warehouseIds[0] && w.AvailableQty == 100m);
        Assert.Contains(availability.WarehouseStocks, w => w.WarehouseId == warehouseIds[1] && w.AvailableQty == 40m);

        // Category products lookup without warehouseId returns TotalAvailableQty (140) and breakdown
        var catAllResp = await repClient.GetAsync($"/api/v1/catalog/categories/{categoryId}/products");
        Assert.Equal(HttpStatusCode.OK, catAllResp.StatusCode);
        var catAll = await catAllResp.Content.ReadFromJsonAsync<PagedResult<CategoryProductPriceDto>>(JsonOptions);
        Assert.NotNull(catAll);
        var rowAll = Assert.Single(catAll.Items);
        Assert.Equal(140m, rowAll.AvailableQty);
        Assert.Equal(140m, rowAll.TotalAvailableQty);
        Assert.NotNull(rowAll.WarehouseStocks);
        Assert.Equal(2, rowAll.WarehouseStocks.Count);

        // Category products lookup filtered to WH-2 returns warehouse-specific AvailableQty=40 AND TotalAvailableQty=140
        var catWh2Resp = await repClient.GetAsync($"/api/v1/catalog/categories/{categoryId}/products?warehouseId={warehouseIds[1]}");
        Assert.Equal(HttpStatusCode.OK, catWh2Resp.StatusCode);
        var catWh2 = await catWh2Resp.Content.ReadFromJsonAsync<PagedResult<CategoryProductPriceDto>>(JsonOptions);
        Assert.NotNull(catWh2);
        var rowWh2 = Assert.Single(catWh2.Items);
        Assert.Equal(40m, rowWh2.AvailableQty);
        Assert.Equal(140m, rowWh2.TotalAvailableQty);
    }

    [Fact]
    public async Task CustomersRepLinking_SupportsSingleAndBulkAssignment_AndEnforcesRowScopedSalesRepVisibility()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        await SeedUserAsync(factory, "ADM-REP-1", "+966533000010", "AdminPass#123", CanonicalRolesAndPermissions.Roles.SuperAdmin, null, null);
        var repAId = await SeedUserAsync(factory, "REP-LINK-A", "+966533000011", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);
        var repBId = await SeedUserAsync(factory, "REP-LINK-B", "+966533000012", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "VanSales", 1);

        Guid cust1Id;
        Guid cust2Id;
        Guid cust3Id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            var grade = await db.CustomerGrades.FirstAsync(g => g.GradeCode == "A");

            var c1 = Customer.Create("CUST-LNK-1", "متجر الأمل 1", "Al Amal 1", "+966500111001", "Riyadh North", "Riyadh", "Market", null, grade.Id, null, null, 20000m, repAId, null, null);
            var c2 = Customer.Create("CUST-LNK-2", "متجر الأمل 2", "Al Amal 2", "+966500111002", "Riyadh South", "Riyadh", "Store", null, grade.Id, null, null, 25000m, repAId, null, null);
            var c3 = Customer.Create("CUST-LNK-3", "متجر الأمل 3", "Al Amal 3", "+966500111003", "Riyadh East", "Riyadh", "Supermarket", null, grade.Id, null, null, 30000m, repAId, null, null);

            db.Customers.AddRange(c1, c2, c3);
            await db.SaveChangesAsync();
            cust1Id = c1.Id;
            cust2Id = c2.Id;
            cust3Id = c3.Id;
        }

        using var adminClient = factory.CreateClient();
        var adminAuth = await LoginAsync(adminClient, "ADM-REP-1", "AdminPass#123");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        // 1. List sales reps and verify customer counts
        var repsResp = await adminClient.GetAsync("/api/v1/customers/sales-reps");
        Assert.Equal(HttpStatusCode.OK, repsResp.StatusCode);
        var reps = await repsResp.Content.ReadFromJsonAsync<List<SalesRepOptionDto>>(JsonOptions);
        Assert.NotNull(reps);
        var repAOption = Assert.Single(reps, r => r.Id == repAId);
        var repBOption = Assert.Single(reps, r => r.Id == repBId);
        Assert.Equal(3, repAOption.AssignedCustomersCount);
        Assert.Equal(0, repBOption.AssignedCustomersCount);

        // 2. Single reassign cust1 -> Rep B
        var singleResp = await adminClient.PutAsJsonAsync($"/api/v1/customers/{cust1Id}/assign-rep", new AssignCustomerRepRequest(repBId));
        Assert.Equal(HttpStatusCode.OK, singleResp.StatusCode);
        var updatedCust1 = await singleResp.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions);
        Assert.NotNull(updatedCust1);
        Assert.Equal(repBId, updatedCust1.AssignedRepId);
        Assert.Equal("REP-LINK-B", updatedCust1.AssignedRepCode);

        // 3. Bulk assign cust2 & cust3 -> Rep B (1 Rep -> Many Customers)
        var bulkResp = await adminClient.PostAsJsonAsync(
            "/api/v1/customers/assign-rep/bulk",
            new BulkAssignCustomersRepRequest(repBId, [cust2Id, cust3Id]));
        Assert.Equal(HttpStatusCode.OK, bulkResp.StatusCode);
        var bulkResult = await bulkResp.Content.ReadFromJsonAsync<BulkAssignCustomersRepResponse>(JsonOptions);
        Assert.NotNull(bulkResult);
        Assert.Equal(2, bulkResult.UpdatedCount);
        Assert.Equal("REP-LINK-B", bulkResult.AssignedRepCode);

        // 4. Verify row-scoped GET /api/v1/customers for Rep B (sees all 3) and Rep A (sees 0)
        using var repBClient = factory.CreateClient();
        var repBAuth = await LoginAsync(repBClient, "REP-LINK-B", "RepPass#123");
        repBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repBAuth.AccessToken);

        var repBCustomers = await repBClient.GetFromJsonAsync<PagedResult<CustomerDto>>("/api/v1/customers", JsonOptions);
        Assert.NotNull(repBCustomers);
        Assert.Equal(3, repBCustomers.TotalCount);
        Assert.All(repBCustomers.Items, c => Assert.Equal("REP-LINK-B", c.AssignedRepCode));

        using var repAClient = factory.CreateClient();
        var repAAuth = await LoginAsync(repAClient, "REP-LINK-A", "RepPass#123");
        repAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repAAuth.AccessToken);

        var repACustomers = await repAClient.GetFromJsonAsync<PagedResult<CustomerDto>>("/api/v1/customers", JsonOptions);
        Assert.NotNull(repACustomers);
        Assert.Equal(0, repACustomers.TotalCount);
    }

    [Fact]
    public async Task Validation_RejectsDuplicateWarehouseStocks_AndEmptyBulkCustomerSelection()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var adminId = await SeedUserAsync(factory, "ADM-VAL-1", "+966533000020", "AdminPass#123", CanonicalRolesAndPermissions.Roles.SuperAdmin, null, null);

        using var adminClient = factory.CreateClient();
        var adminAuth = await LoginAsync(adminClient, "ADM-VAL-1", "AdminPass#123");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        // Duplicate WarehouseId in InitialWarehouseStocks -> 400
        var dupWhResp = await adminClient.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CreateMasterProductRequest(
                "SKU-DUP-1", "6281000600001", "منتج اختبار", "Test", 1, 1, "Piece",
                10m, 20m, 25m, 15m, 15m,
                [new CreateProductUnitRequest("حبة", "Piece", 1m, "6281000600001", 20m, 25m, 15m, true)],
                [new InitialWarehouseStockRequest(1, 10m, 5m), new InitialWarehouseStockRequest(1, 20m, 5m)]));
        Assert.Equal(HttpStatusCode.BadRequest, dupWhResp.StatusCode);

        // Empty CustomerIds in BulkAssign -> 400
        var emptyBulkResp = await adminClient.PostAsJsonAsync(
            "/api/v1/customers/assign-rep/bulk",
            new BulkAssignCustomersRepRequest(adminId, []));
        Assert.Equal(HttpStatusCode.BadRequest, emptyBulkResp.StatusCode);
    }

    private static async Task<AuthenticationResponse> LoginAsync(HttpClient client, string identifier, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(identifier, password));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var auth = await resp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(auth);
        return auth;
    }

    private static async Task<Guid> SeedUserAsync(
        AuthWebApplicationFactory factory,
        string employeeCode,
        string phone,
        string password,
        string roleCode,
        string? repType,
        int? defaultWarehouseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var role = await db.Roles.FirstAsync(r => r.RoleCode == roleCode);
        var user = User.Create(employeeCode, $"مستخدم {employeeCode}", $"User {employeeCode}", phone, $"{employeeCode.ToLowerInvariant()}@almomiaa.sa", hasher.HashPassword(password), repType, defaultWarehouseId, isActive: true);
        user.AssignRole(role.Id);
        if (defaultWarehouseId.HasValue)
        {
            user.AssignWarehouse(defaultWarehouseId.Value);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}
