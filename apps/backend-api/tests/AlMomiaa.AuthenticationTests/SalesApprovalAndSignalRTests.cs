using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Customers;
using AlMomiaa.Application.Inventory;
using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;
using AlMomiaa.Infrastructure.Persistence;
using AlMomiaa.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class RecordingOperationsRealtimeNotifier : IOperationsRealtimeNotifier
{
    public ConcurrentQueue<OrderSubmittedNotification> Submitted { get; } = new();
    public ConcurrentQueue<CreditCoApprovalRequiredNotification> CreditCoApprovalRequired { get; } = new();
    public ConcurrentQueue<OrderApprovedNotification> Approved { get; } = new();
    public ConcurrentQueue<OrderRejectedNotification> Rejected { get; } = new();
    public ConcurrentQueue<OrderUpdatedNotification> Updated { get; } = new();

    public Task NotifyOrderSubmittedAsync(OrderSubmittedNotification notification, CancellationToken ct = default)
    {
        Submitted.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task NotifyCreditCoApprovalRequiredAsync(CreditCoApprovalRequiredNotification notification, CancellationToken ct = default)
    {
        CreditCoApprovalRequired.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task NotifyOrderApprovedAsync(OrderApprovedNotification notification, CancellationToken ct = default)
    {
        Approved.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task NotifyOrderRejectedAsync(OrderRejectedNotification notification, CancellationToken ct = default)
    {
        Rejected.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task NotifyOrderUpdatedAsync(OrderUpdatedNotification notification, CancellationToken ct = default)
    {
        Updated.Enqueue(notification);
        return Task.CompletedTask;
    }
}

public sealed class SalesApprovalAndSignalRTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task OperationsHub_Negotiate_RejectsAnonymous_AndAcceptsQueryAccessToken()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var repId = await SeedUserAsync(factory, "REP-HUB-1", "+966511000001", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);
        using var client = factory.CreateClient();

        // 1. Unauthenticated negotiate must fail closed with 401
        var anonResp = await client.PostAsync("/hubs/operations/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonResp.StatusCode);

        // 2. Authenticated negotiate via ?access_token= (SignalR browser WebSocket path) must succeed
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("REP-HUB-1", "RepPass#123"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(auth);
        Assert.Equal(repId, auth.User.Id);

        var authedNegotiate = await client.PostAsync(
            $"/hubs/operations/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(auth.AccessToken)}",
            null);
        Assert.Equal(HttpStatusCode.OK, authedNegotiate.StatusCode);

        var body = await authedNegotiate.Content.ReadAsStringAsync();
        Assert.Contains("connectionId", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("availableTransports", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OperationsHub_OnConnectedAsync_JoinsCanonicalUserRoleAndWarehouseGroups()
    {
        var userId = Guid.CreateVersion7();
        var identity = new ClaimsIdentity(
        [
            new Claim("UserId", userId.ToString("D")),
            new Claim(ClaimTypes.Role, CanonicalRolesAndPermissions.Roles.SalesRep),
            new Claim("RoleCodes", CanonicalRolesAndPermissions.Roles.SalesRep),
            new Claim("DefaultWarehouseId", "1"),
            new Claim("AssignedWarehouseIds", "1,2")
        ], "Bearer");

        var groups = new RecordingGroupManager();
        var hub = new OperationsHub
        {
            Groups = groups,
            Context = new FakeHubCallerContext("conn-123", new ClaimsPrincipal(identity))
        };

        await hub.OnConnectedAsync();

        Assert.Contains(OperationsHub.UserGroup(userId), groups.JoinedGroups);
        Assert.Contains(OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SalesRep), groups.JoinedGroups);
        Assert.Contains(OperationsHub.WarehouseGroup(1), groups.JoinedGroups);
        Assert.Contains(OperationsHub.WarehouseGroup(2), groups.JoinedGroups);
    }

    [Fact]
    public async Task SalesRep_SubmitPendingApproval_And_SalesMgr_ApproveAndReject_DispatchesRealtimeNotifications()
    {
        var recorder = new RecordingOperationsRealtimeNotifier();
        await using var baseFactory = new AuthWebApplicationFactory();
        await baseFactory.InitializeDatabaseAsync();

        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOperationsRealtimeNotifier>();
                services.AddSingleton<IOperationsRealtimeNotifier>(recorder);
            });
        });

        var repId = await SeedUserAsync(baseFactory, "REP-PWA-1", "+966511000010", "RepPass#123", CanonicalRolesAndPermissions.Roles.SalesRep, "PreSales", 1);
        _ = await SeedUserAsync(baseFactory, "MGR-ERP-1", "+966511000011", "MgrPass#123", CanonicalRolesAndPermissions.Roles.SalesManager, null, null);
        var (customerId, productId, unitId) = await SeedCatalogAndCustomerAsync(baseFactory, repId);

        using var repClient = factory.CreateClient();
        var repAuth = await LoginAsync(repClient, "REP-PWA-1", "RepPass#123");
        repClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", repAuth.AccessToken);

        using var mgrClient = factory.CreateClient();
        var mgrAuth = await LoginAsync(mgrClient, "MGR-ERP-1", "MgrPass#123");
        mgrClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mgrAuth.AccessToken);

        // 1. SALES_REP can read warehouses via WarehousesRead policy (scoped to assigned warehouse #1)
        var whResp = await repClient.GetAsync("/api/v1/warehouses");
        Assert.Equal(HttpStatusCode.OK, whResp.StatusCode);
        var warehouses = await whResp.Content.ReadFromJsonAsync<List<WarehouseDto>>(JsonOptions);
        Assert.NotNull(warehouses);
        Assert.Single(warehouses);
        Assert.Equal(1, warehouses[0].Id);

        // 2. SALES_REP cannot see WeightedAvgCost on catalog products
        var prodResp = await repClient.GetAsync("/api/v1/catalog/products");
        Assert.Equal(HttpStatusCode.OK, prodResp.StatusCode);
        var prodPage = await prodResp.Content.ReadFromJsonAsync<PagedResult<MasterProductDto>>(JsonOptions);
        Assert.NotNull(prodPage);
        Assert.All(prodPage.Items, p => Assert.Null(p.WeightedAvgCost));

        // 3. SALES_REP submits order #1 -> PendingApproval + OrderSubmitted realtime notification
        var createReq1 = new CreateSalesOrderRequest(
            "PreSales",
            customerId,
            1,
            [new CreateSalesOrderItemRequest(productId, unitId, 2m, 100m)]);

        using var postMsg1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(createReq1)
        };
        postMsg1.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString("D"));
        var createResp1 = await repClient.SendAsync(postMsg1);
        Assert.Equal(HttpStatusCode.Created, createResp1.StatusCode);
        var order1 = await createResp1.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions);
        Assert.NotNull(order1);
        Assert.Equal("PendingApproval", order1.Status);
        Assert.Equal(repId, order1.RepId);

        Assert.True(recorder.Submitted.TryDequeue(out var submittedEvt1));
        Assert.Equal(order1.Id, submittedEvt1.OrderId);
        Assert.Equal(repId, submittedEvt1.RepId);

        // 4. SALES_REP is strictly forbidden (403) from editing or approving submitted orders
        var repEditResp = await repClient.PutAsJsonAsync(
            $"/api/v1/orders/{order1.Id}/manager-edit",
            new ManagerEditOrderRequest([new ManagerEditOrderItemRequest(order1.Items[0].Id, 1m, 100m)]));
        Assert.Equal(HttpStatusCode.Forbidden, repEditResp.StatusCode);

        var repApproveResp = await repClient.PostAsync($"/api/v1/orders/{order1.Id}/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, repApproveResp.StatusCode);

        // 5. SALES_MGR sees pending order and approves it -> OrderApproved realtime notification to rep
        var pendingResp = await mgrClient.GetAsync("/api/v1/orders?status=PendingApproval");
        Assert.Equal(HttpStatusCode.OK, pendingResp.StatusCode);
        var pendingPage = await pendingResp.Content.ReadFromJsonAsync<PagedResult<SalesOrderDto>>(JsonOptions);
        Assert.NotNull(pendingPage);
        Assert.Contains(pendingPage.Items, o => o.Id == order1.Id);

        var approveResp = await mgrClient.PostAsync($"/api/v1/orders/{order1.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
        var approvedOrder = await approveResp.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions);
        Assert.NotNull(approvedOrder);
        Assert.Equal("Approved", approvedOrder.Status);

        Assert.True(recorder.Approved.TryDequeue(out var approvedEvt));
        Assert.Equal(order1.Id, approvedEvt.OrderId);
        Assert.Equal(repId, approvedEvt.RepId);

        // 6. SALES_REP submits order #2 -> SALES_MGR rejects it -> OrderRejected notification with reason
        using var postMsg2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(createReq1)
        };
        postMsg2.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString("D"));
        var createResp2 = await repClient.SendAsync(postMsg2);
        Assert.Equal(HttpStatusCode.Created, createResp2.StatusCode);
        var order2 = await createResp2.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions);
        Assert.NotNull(order2);

        var rejectResp = await mgrClient.PostAsJsonAsync(
            $"/api/v1/orders/{order2.Id}/reject",
            new RejectSalesOrderRequest("Customer requested postponement"));
        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejectedOrder = await rejectResp.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions);
        Assert.NotNull(rejectedOrder);
        Assert.Equal("Rejected", rejectedOrder.Status);
        Assert.Equal("Customer requested postponement", rejectedOrder.RejectionReason);

        Assert.True(recorder.Rejected.TryDequeue(out var rejectedEvt));
        Assert.Equal(order2.Id, rejectedEvt.OrderId);
        Assert.Equal(repId, rejectedEvt.RepId);
        Assert.Equal("Customer requested postponement", rejectedEvt.RejectionReason);
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
        int? defaultWarehouseId)
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

        var category = Category.Create(null, "الزيوت والمشروبات", "Beverages", 1);
        var brand = Brand.Create("المومياء", "AlMomiaa");
        db.Categories.Add(category);
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var product = MasterProduct.Create("SKU-PWA-001", "عصير طبيعي 24 عبوة", "Natural Juice 24pk", null, category.Id, brand.Id, "Carton", 15m, 100m, 110m, 80m, 50m);
        var unit = ProductUnit.Create(product.Id, "كرتون", "Carton", 1m, "6281000000011", 100m, 110m, 85m, isDefaultUnit: true);
        product.Units.Add(unit);
        db.MasterProducts.Add(product);

        var stock = WarehouseStock.Create(1, product.Id, 10m);
        stock.ReceiveGoods(500m, 0m);
        db.WarehouseStocks.Add(stock);

        var grade = await db.CustomerGrades.FirstAsync(g => g.GradeCode == "A");
        var customer = Customer.Create(
            "CUST-PWA-001",
            "سوبرماركت الرياض",
            "Riyadh Supermarket",
            "Supermarket",
            "300000000000003",
            "+966500000111",
            "Riyadh",
            "Al Olaya",
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

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<string> JoinedGroups { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            JoinedGroups.Add(groupName);
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;

        public FakeHubCallerContext(string connectionId, ClaimsPrincipal user)
        {
            ConnectionId = connectionId;
            _user = user;
        }

        public override string ConnectionId { get; }
        public override string? UserIdentifier => _user.FindFirstValue("UserId");
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; } = new Microsoft.AspNetCore.Http.Features.FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }
}
