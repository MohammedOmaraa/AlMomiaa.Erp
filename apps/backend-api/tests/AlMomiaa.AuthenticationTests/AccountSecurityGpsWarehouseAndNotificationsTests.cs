using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Commands.ChangePassword;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Notifications;
using AlMomiaa.Application.Operations;
using AlMomiaa.Application.Sales;
using AlMomiaa.Application.Users;
using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class AccountSecurityGpsWarehouseAndNotificationsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ChangePassword_ValidatesPolicy_RotatesSecurityStamp_RevokesSessions_AndAllowsNewLogin()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, "USR-PWD-1", "+966522000001", "OldPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("USR-PWD-1", "OldPass#123"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Wrong current password -> 422
        var wrongResp = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest("WrongPass#1", "NewStrong#999", "NewStrong#999"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongResp.StatusCode);

        // Weak / mismatched password -> 400
        var mismatchResp = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest("OldPass#123", "short", "different"));
        Assert.Equal(HttpStatusCode.BadRequest, mismatchResp.StatusCode);

        // Valid change password -> 204
        var okResp = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new ChangePasswordRequest("OldPass#123", "NewStrong#999", "NewStrong#999"));
        Assert.Equal(HttpStatusCode.NoContent, okResp.StatusCode);

        // Old access JWT must immediately fail closed with 401 (SecurityStamp rotated)
        var staleJwtResp = await client.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, staleJwtResp.StatusCode);

        // Clear revoked Authorization header before testing re-login
        client.DefaultRequestHeaders.Authorization = null;

        // Old password must fail login
        var oldLoginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("USR-PWD-1", "OldPass#123"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResp.StatusCode);

        // New password must succeed
        var newLoginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("USR-PWD-1", "NewStrong#999"));
        Assert.Equal(HttpStatusCode.OK, newLoginResp.StatusCode);
    }

    [Fact]
    public async Task WarehouseKeeper_StrictlyEnforcesExactlyOneWarehouse_OnCreateRoleAndWarehouseUpdates()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, "ADM-WH-1", "+966522000010", "AdminPass#123", CanonicalRolesAndPermissions.Roles.SuperAdmin, null, null);

        int whKeeperRoleId;
        int salesRepRoleId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            whKeeperRoleId = (await db.Roles.FirstAsync(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.WarehouseKeeper)).Id;
            salesRepRoleId = (await db.Roles.FirstAsync(r => r.RoleCode == CanonicalRolesAndPermissions.Roles.SalesRep)).Id;
        }

        using var adminClient = factory.CreateClient();
        var adminAuth = await LoginAsync(adminClient, "ADM-WH-1", "AdminPass#123");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

        // 0 warehouses for WH_KEEPER -> 422
        var zeroWhResp = await adminClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest("WHK-000", "أمين 0", "Keeper 0", "+966522000011", null, "Strong#123", null, null, [whKeeperRoleId], []));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, zeroWhResp.StatusCode);

        // 2 warehouses for WH_KEEPER -> 422
        var twoWhResp = await adminClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest("WHK-002", "أمين 2", "Keeper 2", "+966522000012", null, "Strong#123", null, 1, [whKeeperRoleId], [1, 2]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, twoWhResp.StatusCode);

        // Exactly 1 warehouse -> 201 Created & DefaultWarehouseId aligned
        var oneWhResp = await adminClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest("WHK-001", "أمين 1", "Keeper 1", "+966522000013", null, "Strong#123", null, null, [whKeeperRoleId], [2]));
        Assert.Equal(HttpStatusCode.Created, oneWhResp.StatusCode);
        var keeper = await oneWhResp.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions);
        Assert.NotNull(keeper);
        Assert.Equal(2, keeper.DefaultWarehouseId);
        Assert.Single(keeper.AssignedWarehouseIds);
        Assert.Equal(2, keeper.AssignedWarehouseIds[0]);

        // Updating WH_KEEPER to 2 warehouses -> 422
        var updateTwoResp = await adminClient.PutAsJsonAsync(
            $"/api/v1/users/{keeper.Id}/warehouses",
            new UpdateUserWarehousesRequest(1, [1, 2]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, updateTwoResp.StatusCode);

        // Updating WH_KEEPER to 1 different warehouse -> 200 OK
        var updateOneResp = await adminClient.PutAsJsonAsync(
            $"/api/v1/users/{keeper.Id}/warehouses",
            new UpdateUserWarehousesRequest(4, [4]));
        Assert.Equal(HttpStatusCode.OK, updateOneResp.StatusCode);

        // Assigning WH_KEEPER role to a multi-warehouse user -> 422
        var multiUserId = await SeedUserAsync(factory, "REP-MULTI", "+966522000014", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1, [1, 2]);
        var assignWhRoleResp = await adminClient.PutAsJsonAsync(
            $"/api/v1/users/{multiUserId}/roles",
            new UpdateUserRolesRequest([salesRepRoleId, whKeeperRoleId]));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, assignWhRoleResp.StatusCode);
    }

    [Fact]
    public async Task LiveGpsEndpoints_AllowOnlySuperAdminExecMgrSalesMgr_AndForbidOtherRoles()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var repId = await SeedUserAsync(factory, "REP-GPS-1", "+966522000020", "Pass#1234", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);
        await SeedUserAsync(factory, "ADM-GPS-1", "+966522000021", "Pass#1234", CanonicalRolesAndPermissions.Roles.SuperAdmin, null, null);
        await SeedUserAsync(factory, "EXE-GPS-1", "+966522000022", "Pass#1234", CanonicalRolesAndPermissions.Roles.ExecutiveManager, null, null);
        await SeedUserAsync(factory, "SMG-GPS-1", "+966522000023", "Pass#1234", CanonicalRolesAndPermissions.Roles.SalesManager, null, null);
        await SeedUserAsync(factory, "WHK-GPS-1", "+966522000024", "Pass#1234", CanonicalRolesAndPermissions.Roles.WarehouseKeeper, null, 1);
        await SeedUserAsync(factory, "ACC-GPS-1", "+966522000025", "Pass#1234", CanonicalRolesAndPermissions.Roles.Accountant, null, null);

        // Rep submits a GPS ping
        using var repClient = factory.CreateClient();
        var repAuth = await LoginAsync(repClient, "REP-GPS-1", "Pass#1234");
        repClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repAuth.AccessToken);

        var pingResp = await repClient.PostAsJsonAsync(
            "/api/v1/gps/pings",
            new RecordGpsPingsRequest([new GpsPingItemRequest(24.7136m, 46.6753m, 8.5m, null, DateTime.UtcNow)]));
        Assert.Equal(HttpStatusCode.Accepted, pingResp.StatusCode);

        // Forbidden roles: SALES_REP, WH_KEEPER, ACCOUNTANT -> 403
        Assert.Equal(HttpStatusCode.Forbidden, (await repClient.GetAsync("/api/v1/gps/live")).StatusCode);
        foreach (var forbiddenCode in new[] { "WHK-GPS-1", "ACC-GPS-1" })
        {
            using var c = factory.CreateClient();
            var a = await LoginAsync(c, forbiddenCode, "Pass#1234");
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/gps/live")).StatusCode);
        }

        // Allowed roles: SUPER_ADMIN, EXEC_MGR, SALES_MGR -> 200 with rep's live coordinates
        foreach (var allowedCode in new[] { "ADM-GPS-1", "EXE-GPS-1", "SMG-GPS-1" })
        {
            using var c = factory.CreateClient();
            var a = await LoginAsync(c, allowedCode, "Pass#1234");
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);
            var liveResp = await c.GetAsync("/api/v1/gps/live");
            Assert.Equal(HttpStatusCode.OK, liveResp.StatusCode);
            var locations = await liveResp.Content.ReadFromJsonAsync<List<LiveRepGpsDto>>(JsonOptions);
            Assert.NotNull(locations);
            var match = Assert.Single(locations, x => x.RepId == repId);
            Assert.Equal(24.7136m, match.Latitude);
            Assert.Equal(46.6753m, match.Longitude);
        }
    }

    [Fact]
    public async Task Notifications_PersistOnOrderSubmitAndApprove_AndMarkAllReadClearsUnreadCount()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var repId = await SeedUserAsync(factory, "REP-NOT-1", "+966522000030", "Pass#1234", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);
        await SeedUserAsync(factory, "MGR-NOT-1", "+966522000031", "Pass#1234", CanonicalRolesAndPermissions.Roles.SalesManager, null, null);
        var (customerId, productId, unitId) = await SeedCatalogAndCustomerAsync(factory, repId);

        using var repClient = factory.CreateClient();
        var repAuth = await LoginAsync(repClient, "REP-NOT-1", "Pass#1234");
        repClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repAuth.AccessToken);

        using var mgrClient = factory.CreateClient();
        var mgrAuth = await LoginAsync(mgrClient, "MGR-NOT-1", "Pass#1234");
        mgrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mgrAuth.AccessToken);

        // Rep submits an order -> SALES_MGR gets a persistent unread notification
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(new CreateSalesOrderRequest(
                "PreSales",
                customerId,
                1,
                [new CreateSalesOrderItemRequest(productId, unitId, 2m, 100m)]))
        };
        reqMsg.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString("D"));
        var createResp = await repClient.SendAsync(reqMsg);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var order = await createResp.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions);
        Assert.NotNull(order);

        var mgrNotifResp = await mgrClient.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.OK, mgrNotifResp.StatusCode);
        var mgrPage = await mgrNotifResp.Content.ReadFromJsonAsync<NotificationsPageDto>(JsonOptions);
        Assert.NotNull(mgrPage);
        Assert.True(mgrPage.UnreadCount >= 1);
        Assert.Contains(mgrPage.Items, n => n.EventType == "OrderSubmitted" && !n.IsRead);

        // SALES_MGR marks all read -> unreadCount becomes 0 and unreadOnly returns empty
        var markAllResp = await mgrClient.PostAsync("/api/v1/notifications/mark-all-read", null);
        Assert.Equal(HttpStatusCode.OK, markAllResp.StatusCode);
        var marked = await markAllResp.Content.ReadFromJsonAsync<MarkAllNotificationsReadResponse>(JsonOptions);
        Assert.NotNull(marked);
        Assert.True(marked.MarkedCount >= 1);
        Assert.Equal(0, marked.UnreadCount);

        var mgrUnreadAfter = await mgrClient.GetFromJsonAsync<NotificationsPageDto>("/api/v1/notifications?unreadOnly=true", JsonOptions);
        Assert.NotNull(mgrUnreadAfter);
        Assert.Equal(0, mgrUnreadAfter.UnreadCount);
        Assert.Empty(mgrUnreadAfter.Items);

        // SALES_MGR approves order -> SALES_REP receives persistent OrderApproved notification
        var approveResp = await mgrClient.PostAsync($"/api/v1/orders/{order.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        var repNotifPage = await repClient.GetFromJsonAsync<NotificationsPageDto>("/api/v1/notifications", JsonOptions);
        Assert.NotNull(repNotifPage);
        Assert.Equal(1, repNotifPage.UnreadCount);
        Assert.Single(repNotifPage.Items, n => n.EventType == "OrderApproved" && !n.IsRead);
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
        string code,
        string phone,
        string password,
        string roleCode,
        string? repType,
        int? defaultWarehouseId,
        int[]? extraWarehouses = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var role = await db.Roles.FirstAsync(r => r.RoleCode == roleCode);

        var user = User.Create(
            code,
            $"مستخدم {code}",
            $"User {code}",
            phone,
            $"{code.ToLowerInvariant()}@almomiaa.sa",
            hasher.HashPassword(password),
            repType,
            defaultWarehouseId,
            isActive: true);
        user.AssignRole(role.Id);
        if (defaultWarehouseId.HasValue)
        {
            user.AssignWarehouse(defaultWarehouseId.Value);
        }

        if (extraWarehouses is not null)
        {
            foreach (var wh in extraWarehouses)
            {
                user.AssignWarehouse(wh);
            }
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<(Guid CustomerId, Guid ProductId, Guid UnitId)> SeedCatalogAndCustomerAsync(
        AuthWebApplicationFactory factory,
        Guid repId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();

        var category = Category.Create(null, "مشروبات", "Beverages", 1);
        var brand = Brand.Create("المومياء", "AlMomiaa");
        db.Categories.Add(category);
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var product = MasterProduct.Create("SKU-NOT-001", "مياه 24 عبوة", "Water 24pk", null, category.Id, brand.Id, "Carton", 15m, 100m, 110m, 80m, 50m);
        var unit = ProductUnit.Create(product.Id, "كرتون", "Carton", 1m, "6281000000099", 100m, 110m, 85m, isDefaultUnit: true);
        product.Units.Add(unit);
        db.MasterProducts.Add(product);

        var stock = WarehouseStock.Create(1, product.Id, 10m);
        stock.ReceiveGoods(200m, 0m);
        db.WarehouseStocks.Add(stock);

        var grade = await db.CustomerGrades.FirstAsync(g => g.GradeCode == "A");
        var customer = Customer.Create(
            "CUST-NOT-001",
            "أسواق النخيل",
            "Nakheel Markets",
            "Supermarket",
            "300000000000003",
            "+966500000222",
            "Riyadh",
            "Al Malqa",
            grade.Id,
            null,
            null,
            50000m,
            repId,
            null,
            null);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return (customer.Id, product.Id, unit.Id);
    }
}
