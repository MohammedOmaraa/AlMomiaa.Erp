using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Customers;
using AlMomiaa.Application.Finance;
using AlMomiaa.Application.Inventory;
using AlMomiaa.Application.Operations;
using AlMomiaa.Application.Procurement;
using AlMomiaa.Application.Sales;
using AlMomiaa.Application.Users;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class ErpApiEndToEndTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static async Task<(Guid UserId, string AccessToken)> SeedAndLoginAsync(
        AuthWebApplicationFactory factory,
        HttpClient client,
        string employeeCode,
        string phone,
        string password,
        string roleCode,
        string? repType = null,
        int? defaultWarehouseId = null,
        int[]? assignedWarehouseIds = null)
    {
        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var role = await db.Roles.FirstAsync(r => r.RoleCode == roleCode);
            var user = User.Create(
                employeeCode,
                $"موظف {employeeCode}",
                $"Employee {employeeCode}",
                phone,
                $"{employeeCode.ToLowerInvariant()}@almomiaa.sa",
                hasher.HashPassword(password),
                repType,
                defaultWarehouseId,
                isActive: true);
            user.AssignRole(role.Id);
            if (assignedWarehouseIds is not null)
            {
                foreach (var whId in assignedWarehouseIds)
                {
                    user.AssignWarehouse(whId);
                }
            }

            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(employeeCode, password));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(auth);
        return (userId, auth!.AccessToken);
    }

    private static HttpClient CreateAuthedClient(AuthWebApplicationFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task Full_8_Module_Workflow_EndToEnd_Succeeds_And_Maintains_Accounting_And_Zatca_Invariants()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var anonClient = factory.CreateClient();

        var (adminId, adminToken) = await SeedAndLoginAsync(
            factory, anonClient, "ADM-E2E-01", "+966500009001", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.SuperAdmin);
        using var client = CreateAuthedClient(factory, adminToken);

        // 1. IAM: List roles & create a CashVan rep with auto-provisioned mobile van
        var roles = await client.GetFromJsonAsync<List<RoleDto>>("/api/v1/roles", JsonOptions);
        Assert.NotNull(roles);
        Assert.Equal(7, roles!.Count);

        var createUserResp = await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "REP-VAN-01", "مندوب فان", "Van Rep", "+966501119001", "vanrep@almomiaa.sa",
            "VanRep!2026#Pass", "CashVan", null, [4], [], CreateMobileVan: true, VehiclePlateNumber: "VAN-999"));
        Assert.Equal(HttpStatusCode.Created, createUserResp.StatusCode);
        var repUser = await createUserResp.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions);
        Assert.NotNull(repUser);
        Assert.NotNull(repUser!.DefaultWarehouseId);

        // 2. Catalog: Category -> Brand -> Multi-Unit Master Product -> Tiered Price List
        var catResp = await client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest(null, "مشروبات", "Beverages", 1));
        Assert.Equal(HttpStatusCode.Created, catResp.StatusCode);
        var category = (await catResp.Content.ReadFromJsonAsync<CategoryNodeDto>(JsonOptions))!;

        var brandResp = await client.PostAsJsonAsync("/api/v1/catalog/brands", new CreateBrandRequest("المومياء", "AlMomiaa"));
        Assert.Equal(HttpStatusCode.Created, brandResp.StatusCode);
        var brand = (await brandResp.Content.ReadFromJsonAsync<BrandDto>(JsonOptions))!;

        var prodResp = await client.PostAsJsonAsync("/api/v1/catalog/products", new CreateMasterProductRequest(
            "SKU-E2E-100", "6281000099001", "مياه المومياء 330 مل", "AlMomiaa Water 330ml",
            category.Id, brand.Id, "Piece", 10.00m, 100.00m, 120.00m, 90.00m, 15.00m,
            [new CreateProductUnitRequest("حبة", "Piece", 1.0m, "6281000099001", 100.00m, 120.00m, 90.00m, true)]));
        Assert.Equal(HttpStatusCode.Created, prodResp.StatusCode);
        var product = (await prodResp.Content.ReadFromJsonAsync<MasterProductDto>(JsonOptions))!;
        var unitId = product.Units[0].Id;

        var plResp = await client.PostAsJsonAsync("/api/v1/catalog/price-lists", new CreatePriceListRequest(
            "PL-STD-2026", "قائمة الجملة", "Wholesale List", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null,
            [new CreatePriceListItemRequest(unitId, 1m, 100.00m)]));
        Assert.Equal(HttpStatusCode.Created, plResp.StatusCode);
        var priceList = (await plResp.Content.ReadFromJsonAsync<PriceListDto>(JsonOptions))!;

        // 3. Procurement: Supplier -> SupplierProduct -> PO -> Container -> GRN (WAC recalculation)
        var supResp = await client.PostAsJsonAsync("/api/v1/procurement/suppliers", new CreateSupplierRequest(
            "SUP-E2E-01", "مورد عالمي", "Global Supplier", "China", "+8613800000001", null));
        Assert.Equal(HttpStatusCode.Created, supResp.StatusCode);
        var supplier = (await supResp.Content.ReadFromJsonAsync<SupplierDto>(JsonOptions))!;

        var linkResp = await client.PostAsJsonAsync($"/api/v1/catalog/products/{product.Id}/suppliers", new LinkSupplierProductRequest(
            supplier.Id, "GS-100", "690000000100", 12.00m, 20, true));
        Assert.Equal(HttpStatusCode.Created, linkResp.StatusCode);

        var poResp = await client.PostAsJsonAsync("/api/v1/procurement/purchase-orders", new CreatePurchaseOrderRequest(
            supplier.Id, 1, "ContainerImport", [new CreatePurchaseOrderItemRequest(product.Id, 50m, 12.00m)]));
        Assert.Equal(HttpStatusCode.Created, poResp.StatusCode);
        var po = (await poResp.Content.ReadFromJsonAsync<PurchaseOrderDto>(JsonOptions))!;

        var contResp = await client.PostAsJsonAsync("/api/v1/procurement/containers", new CreateContainerRequest(
            po.Id, "MSCU-E2E-01", "BOL-001", "MSC", "Jeddah Port", new DateOnly(2026, 11, 25), null, "InTransitRiyadh", 600m, 150m));
        Assert.Equal(HttpStatusCode.Created, contResp.StatusCode);
        var container = (await contResp.Content.ReadFromJsonAsync<ContainerDto>(JsonOptions))!;

        var grnResp = await client.PostAsJsonAsync("/api/v1/procurement/goods-receipts", new CreateGoodsReceiptRequest(
            po.Id, container.Id, 1, [new CreateGoodsReceiptItemRequest(product.Id, 50m, 15.00m, "LOT-1", new DateOnly(2028, 1, 1))]));
        Assert.Equal(HttpStatusCode.Created, grnResp.StatusCode);

        // Verify WAC updated to 15.0000 and WH-01 physical stock = 50
        var productsPage = await client.GetFromJsonAsync<PagedResult<MasterProductDto>>("/api/v1/catalog/products", JsonOptions);
        Assert.Equal(15.0000m, productsPage!.Items.Single(p => p.Id == product.Id).WeightedAvgCost);

        // 4. Inventory: Transfer 10 units WH-01 -> WH-02, receive, and verify stocks
        var transferResp = await client.PostAsJsonAsync("/api/v1/warehouses/transfers", new CreateStockTransferRequest(
            null, 1, 2, [new CreateStockTransferItemRequest(product.Id, 10m, 10m)]));
        Assert.Equal(HttpStatusCode.Created, transferResp.StatusCode);
        var transfer = (await transferResp.Content.ReadFromJsonAsync<StockTransferDto>(JsonOptions))!;

        var receiveResp = await client.PutAsJsonAsync($"/api/v1/warehouses/transfers/{transfer.Id}/receive", new ReceiveStockTransferRequest(
            [new ReceiveStockTransferItemRequest(transfer.Items[0].Id, 10m)]));
        Assert.Equal(HttpStatusCode.OK, receiveResp.StatusCode);

        var wh1Stocks = await client.GetFromJsonAsync<PagedResult<WarehouseStockDto>>("/api/v1/warehouses/1/stocks", JsonOptions);
        Assert.Equal(40m, wh1Stocks!.Items.Single(s => s.MasterProductId == product.Id).PhysicalQty);

        // 5. Customers: Create customer with CreditLimit = 500 SAR (to trigger MR-03 credit co-approval)
        var custResp = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "CUST-E2E-01", "عميل الاختبار", "Test Customer", "+966550000011", "الرياض", "Riyadh",
            "Supermarket", "310123456700003", 2, priceList.Id, 30, 500.00m, adminId, 24.71m, 46.67m));
        Assert.Equal(HttpStatusCode.Created, custResp.StatusCode);
        var customer = (await custResp.Content.ReadFromJsonAsync<CustomerDto>(JsonOptions))!;

        // 6. Sales Order: Submit 6 units at 95 SAR (below tier 100 => MR-02 override, total 655.50 > 500 => MR-03 co-approval)
        var orderIdemKey = Guid.CreateVersion7().ToString();
        using var orderReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(new CreateSalesOrderRequest(
                "PreSales", customer.Id, 1, [new CreateSalesOrderItemRequest(product.Id, unitId, 6m, 95.00m)]))
        };
        orderReq1.Headers.Add("X-Idempotency-Key", orderIdemKey);
        var orderResp = await client.SendAsync(orderReq1);
        Assert.Equal(HttpStatusCode.Created, orderResp.StatusCode);
        var order = (await orderResp.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions))!;
        Assert.True(order.HasPriceOverridePending);
        Assert.True(order.RequiresFinanceCoApproval);
        Assert.Equal(655.50m, order.TotalAmount); // 570 net + 85.50 VAT (15%)

        // Idempotency replay returns identical order ID
        using var orderReqReplay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(new CreateSalesOrderRequest(
                "PreSales", customer.Id, 1, [new CreateSalesOrderItemRequest(product.Id, unitId, 6m, 95.00m)]))
        };
        orderReqReplay.Headers.Add("X-Idempotency-Key", orderIdemKey);
        var replayResp = await client.SendAsync(orderReqReplay);
        var replayedOrder = (await replayResp.Content.ReadFromJsonAsync<SalesOrderDto>(JsonOptions))!;
        Assert.Equal(order.Id, replayedOrder.Id);

        // Approving before co-approval & price override MUST fail with 422
        var prematureApprove = await client.PostAsync($"/api/v1/orders/{order.Id}/approve", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, prematureApprove.StatusCode);

        // Manager Edit -> Override Price -> Co-Approve Credit -> Approve
        var editResp = await client.PutAsJsonAsync($"/api/v1/orders/{order.Id}/manager-edit", new ManagerEditOrderRequest(
            [new ManagerEditOrderItemRequest(order.Items[0].Id, 5m, 95.00m)]));
        Assert.Equal(HttpStatusCode.OK, editResp.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/orders/{order.Id}/override-price", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/orders/{order.Id}/co-approve-credit", null)).StatusCode);
        var approveResp = await client.PostAsync($"/api/v1/orders/{order.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        // 7. Register ZATCA EGS Device -> Confirm Shipment -> Invoice Print -> Upload Copy -> Delivery POD -> Sales Return
        var egsResp = await client.PostAsJsonAsync("/api/v1/invoices/egs-devices", new RegisterEgsDeviceRequest("EGS-E2E-01", 1, "CERT-E2E-01"));
        Assert.Equal(HttpStatusCode.Created, egsResp.StatusCode);

        using var shipReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/{order.Id}/confirm-shipment")
        {
            Content = JsonContent.Create(new ConfirmShipmentRequest("EGS-E2E-01", "StandardB2B", null))
        };
        shipReq.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        var shipResp = await client.SendAsync(shipReq);
        Assert.Equal(HttpStatusCode.OK, shipResp.StatusCode);
        var invoice = (await shipResp.Content.ReadFromJsonAsync<InvoiceDto>(JsonOptions))!;
        Assert.Equal(1L, invoice.ZatcaIcv);
        Assert.False(string.IsNullOrWhiteSpace(invoice.ZatcaXmlHash));
        Assert.False(string.IsNullOrWhiteSpace(invoice.ZatcaQrCodeBase64));
        Assert.Equal(546.25m, invoice.TotalAmount); // 475 net + 71.25 VAT

        var printResp = await client.GetFromJsonAsync<InvoicePrintDto>($"/api/v1/invoices/{invoice.Id}/print", JsonOptions);
        Assert.Equal(invoice.InvoiceNumber, printResp!.Invoice.InvoiceNumber);

        var copyResp = await client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/upload-copy", new UploadInvoiceCopyRequest("https://storage.almomiaa.sa/signed.pdf"));
        Assert.Equal(HttpStatusCode.OK, copyResp.StatusCode);

        var dispResp = await client.PostAsJsonAsync("/api/v1/delivery/dispatches", new CreateDeliveryDispatchRequest(1, adminId, "RYD-100", [invoice.Id]));
        Assert.Equal(HttpStatusCode.Created, dispResp.StatusCode);
        var dispatch = (await dispResp.Content.ReadFromJsonAsync<DeliveryDispatchDto>(JsonOptions))!;

        var podResp = await client.PutAsJsonAsync($"/api/v1/delivery/dispatches/{dispatch.Id}/items/{invoice.Id}/pod",
            new UpdateDeliveryPodRequest("Delivered", "https://storage.almomiaa.sa/pod.jpg", "Signed"));
        Assert.Equal(HttpStatusCode.OK, podResp.StatusCode);
        Assert.Equal("Completed", (await podResp.Content.ReadFromJsonAsync<DeliveryDispatchDto>(JsonOptions))!.Status);

        var returnResp = await client.PostAsJsonAsync("/api/v1/returns", new CreateSalesReturnRequest(
            invoice.Id, 1, "Damaged box", "EGS-E2E-01",
            [new CreateSalesReturnItemRequest(product.Id, unitId, 1m, true, 95.00m)]));
        Assert.Equal(HttpStatusCode.Created, returnResp.StatusCode);
        var salesReturn = (await returnResp.Content.ReadFromJsonAsync<SalesReturnDto>(JsonOptions))!;
        Assert.Equal(2L, salesReturn.ZatcaIcv); // Monotonic ZATCA ICV incremented

        // 8. Finance: Accounts, Open Fiscal Period, Cash Collection, Handover, Bank Transfer + Duplicate Proof 409, Balanced Ledger, Closed Period 422
        var safeResp = await client.PostAsJsonAsync("/api/v1/finance/accounts", new CreateFinancialAccountRequest("SAFE-01", "الخزينة", "Main Safe", "MainTreasurySafe", null, null, null));
        Assert.Equal(HttpStatusCode.Created, safeResp.StatusCode);
        var safeAccount = (await safeResp.Content.ReadFromJsonAsync<FinancialAccountDto>(JsonOptions))!;

        var bankAccResp = await client.PostAsJsonAsync("/api/v1/finance/accounts", new CreateFinancialAccountRequest("BANK-01", "الأهلي", "SNB", "BankAccount", null, "SNB", "SA001"));
        Assert.Equal(HttpStatusCode.Created, bankAccResp.StatusCode);
        var bankAccount = (await bankAccResp.Content.ReadFromJsonAsync<FinancialAccountDto>(JsonOptions))!;

        var nowUtc = factory.Clock.GetUtcNow().UtcDateTime;
        var fpResp = await client.PostAsJsonAsync("/api/v1/finance/fiscal-periods", new CreateFiscalPeriodRequest(nowUtc.Year, nowUtc.Month));
        Assert.Equal(HttpStatusCode.Created, fpResp.StatusCode);
        var fiscalPeriod = (await fpResp.Content.ReadFromJsonAsync<FiscalPeriodDto>(JsonOptions))!;

        using var cashReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/collections/cash")
        {
            Content = JsonContent.Create(new RecordCashCollectionRequest(customer.Id, 100.00m, [new CollectionAllocationRequest(invoice.Id, 100.00m)]))
        };
        cashReq.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        var cashResp = await client.SendAsync(cashReq);
        Assert.Equal(HttpStatusCode.Created, cashResp.StatusCode);
        var cashCollection = (await cashResp.Content.ReadFromJsonAsync<CollectionDto>(JsonOptions))!;

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/collections/{cashCollection.Id}/confirm", null)).StatusCode);
        var handoverResp = await client.PostAsJsonAsync("/api/v1/collections/handover", new RecordTreasuryHandoverRequest(cashCollection.Id, safeAccount.Id));
        Assert.Equal(HttpStatusCode.OK, handoverResp.StatusCode);

        var proofHash = new string('b', 64);
        using var bankReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/collections/bank-transfer")
        {
            Content = JsonContent.Create(new RecordBankTransferRequest(
                customer.Id, 200.00m, "REF-SNB-01", "https://storage.almomiaa.sa/slip.jpg", proofHash,
                new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc), [new CollectionAllocationRequest(invoice.Id, 200.00m)]))
        };
        bankReq1.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        var bankResp1 = await client.SendAsync(bankReq1);
        Assert.Equal(HttpStatusCode.Created, bankResp1.StatusCode);
        var bankCollection = (await bankResp1.Content.ReadFromJsonAsync<CollectionDto>(JsonOptions))!;

        // Duplicate proof hash MUST return 409 Conflict
        using var bankReqDup = new HttpRequestMessage(HttpMethod.Post, "/api/v1/collections/bank-transfer")
        {
            Content = JsonContent.Create(new RecordBankTransferRequest(
                customer.Id, 200.00m, "REF-SNB-02", "https://storage.almomiaa.sa/slip2.jpg", proofHash, null, null))
        };
        bankReqDup.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        var dupResp = await client.SendAsync(bankReqDup);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        var verifyResp = await client.PostAsJsonAsync($"/api/v1/collections/{bankCollection.Id}/verify-bank", new VerifyBankTransferRequest(bankAccount.Id, true));
        Assert.Equal(HttpStatusCode.OK, verifyResp.StatusCode);

        // Verify ALL journal entries are strictly balanced (SUM(Debit) == SUM(Credit))
        var journals = await client.GetFromJsonAsync<PagedResult<JournalEntryDto>>("/api/v1/finance/journal-entries", JsonOptions);
        Assert.NotNull(journals);
        Assert.True(journals!.Items.Count >= 3);
        foreach (var entry in journals.Items)
        {
            Assert.Equal(entry.Lines.Sum(l => l.DebitAmount), entry.Lines.Sum(l => l.CreditAmount));
        }

        // Close Fiscal Period (MR-05) -> further financial postings in closed period MUST return 422
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/finance/fiscal-periods/{fiscalPeriod.Id}/close", null)).StatusCode);
        using var closedCashReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/collections/cash")
        {
            Content = JsonContent.Create(new RecordCashCollectionRequest(customer.Id, 10.00m, null))
        };
        closedCashReq.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        var closedCashResp = await client.SendAsync(closedCashReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, closedCashResp.StatusCode);

        // 9. Dashboards & Audit Logs
        var execDash = await client.GetFromJsonAsync<ExecutiveDashboardDto>("/api/v1/dashboards/executive", JsonOptions);
        Assert.NotNull(execDash);
        Assert.Equal(100.00m, execDash!.TotalMainSafeCash);
        Assert.Equal(200.00m, execDash.TotalBankBalance);

        var salesDash = await client.GetFromJsonAsync<RoleDashboardDto>("/api/v1/dashboards/role/sales", JsonOptions);
        Assert.Equal("sales", salesDash!.RoleView);

        var auditLogs = await client.GetFromJsonAsync<PagedResult<AuditLogDto>>("/api/v1/audit-logs?pageSize=25", JsonOptions);
        Assert.NotNull(auditLogs);
        Assert.True(auditLogs!.Items.Count >= 10);
    }

    [Fact]
    public async Task Rbac_And_Abac_Security_Boundaries_Are_Enforced()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var anonClient = factory.CreateClient();

        var (adminId, adminToken) = await SeedAndLoginAsync(
            factory, anonClient, "ADM-SEC-01", "+966500009101", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.SuperAdmin);
        var (repId, repToken) = await SeedAndLoginAsync(
            factory, anonClient, "REP-SEC-01", "+966500009102", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.SalesRep, repType: "Wholesale");
        var (_, whToken) = await SeedAndLoginAsync(
            factory, anonClient, "WHK-SEC-01", "+966500009103", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.WarehouseKeeper, defaultWarehouseId: 1, assignedWarehouseIds: [1]);

        using var adminClient = CreateAuthedClient(factory, adminToken);
        using var repClient = CreateAuthedClient(factory, repToken);
        using var whClient = CreateAuthedClient(factory, whToken);

        // Seed a product & a customer owned by Admin (not repId)
        var cat = (await (await adminClient.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest(null, "فئة", "Cat", 1))).Content.ReadFromJsonAsync<CategoryNodeDto>(JsonOptions))!;
        var brand = (await (await adminClient.PostAsJsonAsync("/api/v1/catalog/brands", new CreateBrandRequest("علامة", "Brand"))).Content.ReadFromJsonAsync<BrandDto>(JsonOptions))!;
        await adminClient.PostAsJsonAsync("/api/v1/catalog/products", new CreateMasterProductRequest(
            "SKU-SEC-1", "6281000088001", "منتج", "Prod", cat.Id, brand.Id, "Piece", 42.00m, 80.00m, 100.00m, 70.00m, 15.00m,
            [new CreateProductUnitRequest("حبة", "Piece", 1.0m, "6281000088001", 80.00m, 100.00m, 70.00m, true)]));

        var otherCust = (await (await adminClient.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "CUST-OTHER-1", "عميل آخر", "Other Cust", "+966559999001", "الرياض", "Riyadh", "Market", null, 2, null, 30, 5000m, adminId, null, null))).Content.ReadFromJsonAsync<CustomerDto>(JsonOptions))!;

        // 1. SALES_REP cannot call manager-edit (403 Forbidden)
        var repEditResp = await repClient.PutAsJsonAsync($"/api/v1/orders/{Guid.CreateVersion7()}/manager-edit",
            new ManagerEditOrderRequest([new ManagerEditOrderItemRequest(Guid.CreateVersion7(), 1m, 80m)]));
        Assert.Equal(HttpStatusCode.Forbidden, repEditResp.StatusCode);

        // 2. SALES_REP sees masked WeightedAvgCost == null
        var repProducts = await repClient.GetFromJsonAsync<PagedResult<MasterProductDto>>("/api/v1/catalog/products", JsonOptions);
        Assert.All(repProducts!.Items, p => Assert.Null(p.WeightedAvgCost));

        // 3. SALES_REP cannot view another rep's customer credit status (ABAC 403)
        var abacCustResp = await repClient.GetAsync($"/api/v1/customers/{otherCust.Id}/credit-status");
        Assert.Equal(HttpStatusCode.Forbidden, abacCustResp.StatusCode);

        // 4. SALES_REP can stream GPS pings (202 Accepted)
        var gpsResp = await repClient.PostAsJsonAsync("/api/v1/gps/pings", new RecordGpsPingsRequest(
            [new GpsPingItemRequest(24.7136m, 46.6753m, 5.0m, null, DateTime.UtcNow)]));
        Assert.Equal(HttpStatusCode.Accepted, gpsResp.StatusCode);

        // 5. WH_KEEPER cannot approve orders (403) and cannot view unassigned warehouse #4 stocks (ABAC 403)
        Assert.Equal(HttpStatusCode.Forbidden, (await whClient.PostAsync($"/api/v1/orders/{Guid.CreateVersion7()}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await whClient.GetAsync("/api/v1/warehouses/4/stocks")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await whClient.GetAsync("/api/v1/warehouses/1/stocks")).StatusCode);
        Assert.True(repId != Guid.Empty);
    }

    [Fact]
    public async Task Per_Column_Validation_And_Missing_Idempotency_Return_400_ProblemDetails()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var anonClient = factory.CreateClient();

        var (adminId, adminToken) = await SeedAndLoginAsync(
            factory, anonClient, "ADM-VAL-01", "+966500009201", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.SuperAdmin);
        using var client = CreateAuthedClient(factory, adminToken);

        // Invalid Saudi VAT number regex -> 400
        var badVatResp = await client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "CUST-BAD-VAT", "عميل", "Cust", "+966550001122", "الرياض", "Riyadh", "Market", "12345", 1, null, null, 1000m, adminId, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, badVatResp.StatusCode);

        // MinAllowedSellingPrice > WholesaleSellingPrice -> 400
        var badPriceResp = await client.PostAsJsonAsync("/api/v1/catalog/products", new CreateMasterProductRequest(
            "SKU-BAD-1", "6281000077001", "منتج", "Prod", 1, 1, "Piece", 10m, 50m, 60m, 55m, 15m,
            [new CreateProductUnitRequest("حبة", "Piece", 1m, "6281000077001", 50m, 60m, 45m, true)]));
        Assert.Equal(HttpStatusCode.BadRequest, badPriceResp.StatusCode);

        // Invalid SHA-256 hash on bank transfer -> 400
        using var badHashReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/collections/bank-transfer")
        {
            Content = JsonContent.Create(new RecordBankTransferRequest(Guid.CreateVersion7(), 50m, "REF", "https://a.com/b.jpg", "not-a-sha256", null, null))
        };
        badHashReq.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(badHashReq)).StatusCode);

        // Missing X-Idempotency-Key header on POST /orders -> 400
        var missingKeyResp = await client.PostAsJsonAsync("/api/v1/orders", new CreateSalesOrderRequest(
            "PreSales", Guid.CreateVersion7(), 1, [new CreateSalesOrderItemRequest(Guid.CreateVersion7(), Guid.CreateVersion7(), 1m, 10m)]));
        Assert.Equal(HttpStatusCode.BadRequest, missingKeyResp.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Document_Publishes_All_8_Module_Route_Groups()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var openApiJson = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("/api/v1/auth/login", openApiJson);
        Assert.Contains("/api/v1/users", openApiJson);
        Assert.Contains("/api/v1/customers", openApiJson);
        Assert.Contains("/api/v1/catalog/products", openApiJson);
        Assert.Contains("/api/v1/warehouses/transfers", openApiJson);
        Assert.Contains("/api/v1/procurement/goods-receipts", openApiJson);
        Assert.Contains("/api/v1/orders/{id}/manager-edit", openApiJson);
        Assert.Contains("/api/v1/invoices/{id}/print", openApiJson);
        Assert.Contains("/api/v1/collections/bank-transfer", openApiJson);
        Assert.Contains("/api/v1/dashboards/executive", openApiJson);
        Assert.Contains("/api/v1/audit-logs", openApiJson);
        Assert.Contains("/api/v1/gps/pings", openApiJson);
        Assert.Contains("/api/v1/users/{id}/roles", openApiJson);
        Assert.Contains("/api/v1/users/{id}/warehouses", openApiJson);
        Assert.Contains("/api/v1/roles", openApiJson);
    }

    [Fact]
    public async Task Admin_Can_Create_Custom_Role_Assign_Permissions_To_User_And_Enforce_Immediately()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var anonClient = factory.CreateClient();

        var (_, adminToken) = await SeedAndLoginAsync(
            factory, anonClient, "ADM-PERM-01", "+966500009301", "Strong!Pass2026#", CanonicalRolesAndPermissions.Roles.SuperAdmin);
        using var adminClient = CreateAuthedClient(factory, adminToken);

        // 1. Create custom role with only catalog:view + customers:view
        var roleResp = await adminClient.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(
            "CUSTOM_VIEWER", "مراجع مخصص", "Custom Viewer", ["catalog:view", "customers:view"]));
        Assert.Equal(HttpStatusCode.Created, roleResp.StatusCode);
        var customRole = (await roleResp.Content.ReadFromJsonAsync<RoleDto>(JsonOptions))!;
        Assert.Equal("CUSTOM_VIEWER", customRole.RoleCode);

        // Unknown permission must fail with 400
        var invalidRoleResp = await adminClient.PostAsJsonAsync("/api/v1/roles", new CreateRoleRequest(
            "BAD_ROLE", "خطأ", "Bad", ["nonexistent:perm"]));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRoleResp.StatusCode);

        // 2. Create user initially with SalesRep role (4), then reassign to customRole + warehouse 1
        var userResp = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest(
            "EMP-CUSTOM-01", "موظف مخصص", "Custom User", "+966500009302", "custom@almomiaa.sa",
            "Custom!Pass2026#", null, null, [4], []));
        Assert.Equal(HttpStatusCode.Created, userResp.StatusCode);
        var createdUser = (await userResp.Content.ReadFromJsonAsync<UserSummaryDto>(JsonOptions))!;

        var assignRolesResp = await adminClient.PutAsJsonAsync($"/api/v1/users/{createdUser.Id}/roles", new UpdateUserRolesRequest([customRole.Id]));
        Assert.Equal(HttpStatusCode.OK, assignRolesResp.StatusCode);

        var assignWhResp = await adminClient.PutAsJsonAsync($"/api/v1/users/{createdUser.Id}/warehouses", new UpdateUserWarehousesRequest(1, [1, 2]));
        Assert.Equal(HttpStatusCode.OK, assignWhResp.StatusCode);

        // 3. Inspect GET /api/v1/users/{id} effective permissions
        var detail = await adminClient.GetFromJsonAsync<UserDetailDto>($"/api/v1/users/{createdUser.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(["CUSTOM_VIEWER"], detail!.RoleCodes);
        Assert.Equal(["catalog:view", "customers:view"], detail.EffectivePermissions);
        Assert.Equal([1, 2], detail.AssignedWarehouseIds);
        Assert.Equal(1, detail.DefaultWarehouseId);

        // 4. New user logs in: allowed on catalog:view (200), forbidden on orders:create (403)
        var loginResp = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("+966500009302", "Custom!Pass2026#"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var userAuth = (await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions))!;
        using var userClient = CreateAuthedClient(factory, userAuth.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await userClient.GetAsync("/api/v1/catalog/products")).StatusCode);
        using var blockedOrder = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(new CreateSalesOrderRequest("PreSales", Guid.CreateVersion7(), 1, [new CreateSalesOrderItemRequest(Guid.CreateVersion7(), Guid.CreateVersion7(), 1m, 10m)]))
        };
        blockedOrder.Headers.Add("X-Idempotency-Key", Guid.CreateVersion7().ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.SendAsync(blockedOrder)).StatusCode);

        // 5. Admin grants orders:create to CUSTOM_VIEWER -> user's existing JWT is immediately revoked (401)
        var grantResp = await adminClient.PutAsJsonAsync($"/api/v1/roles/{customRole.Id}/permissions",
            new UpdateRolePermissionsRequest(["catalog:view", "customers:view", "orders:create"]));
        Assert.Equal(HttpStatusCode.OK, grantResp.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await userClient.GetAsync("/api/v1/catalog/products")).StatusCode);

        // 6. After re-login, user JWT includes orders:create
        var reloginResp = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("+966500009302", "Custom!Pass2026#"));
        Assert.Equal(HttpStatusCode.OK, reloginResp.StatusCode);
        var refreshedAuth = (await reloginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions))!;
        Assert.Contains("orders:create", refreshedAuth.User.Permissions);
    }
}
