namespace AlMomiaa.Domain.Identity;

public sealed record CanonicalRoleDefinition(int Id, string RoleCode, string NameAr, string NameEn, IReadOnlyList<string> Permissions);

public static class CanonicalRolesAndPermissions
{
    public static class Roles
    {
        public const string SuperAdmin = "SUPER_ADMIN";
        public const string ExecutiveManager = "EXEC_MGR";
        public const string SalesManager = "SALES_MGR";
        public const string SalesRep = "SALES_REP";
        public const string Accountant = "ACCOUNTANT";
        public const string WarehouseKeeper = "WH_KEEPER";
        public const string PurchasingManager = "PURCH_MGR";
    }

    public static class Permissions
    {
        public const string PermissionsManage = "permissions:manage";
        public const string UsersManage = "users:manage";
        public const string SettingsManage = "settings:manage";
        public const string DashboardsExecutive = "dashboards:executive";
        public const string DashboardsProfit = "dashboards:profit";
        public const string DashboardsSales = "dashboards:sales";
        public const string DashboardsFinance = "dashboards:finance";
        public const string DashboardsInventory = "dashboards:inventory";
        public const string CustomersView = "customers:view";
        public const string CustomersManage = "customers:manage";
        public const string CatalogView = "catalog:view";
        public const string CatalogManage = "catalog:manage";
        public const string OrdersCreate = "orders:create";
        public const string OrdersEditSubmitted = "orders:edit_submitted";
        public const string OrdersOverridePrice = "orders:override_price";
        public const string OrdersCoapproveCredit = "orders:coapprove_credit";
        public const string OrdersApprove = "orders:approve";
        public const string WarehouseShip = "warehouse:ship";
        public const string DeliveryDispatch = "delivery:dispatch";
        public const string DeliveryUpdatePod = "delivery:update_pod";
        public const string InvoicesView = "invoices:view";
        public const string InvoicesUploadCopy = "invoices:upload_copy";
        public const string SalesReturns = "sales:returns";
        public const string CollectionsCreate = "collections:create";
        public const string FinanceConfirmTreasury = "finance:confirm_treasury";
        public const string FinanceVerifyBank = "finance:verify_bank";
        public const string FinanceClosePeriod = "finance:close_period";
        public const string ProcurementManage = "procurement:manage";
        public const string ProcurementReceive = "procurement:receive";
        public const string AuditView = "audit:view";
        public const string GpsStream = "gps:stream";
    }

    public static readonly IReadOnlyList<CanonicalRoleDefinition> All =
    [
        new(
            1,
            Roles.SuperAdmin,
            "مدير النظام",
            "Super Admin",
            [
                Permissions.PermissionsManage,
                Permissions.UsersManage,
                Permissions.SettingsManage,
                Permissions.DashboardsExecutive,
                Permissions.DashboardsProfit,
                Permissions.DashboardsSales,
                Permissions.DashboardsFinance,
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.CatalogView,
                Permissions.CatalogManage,
                Permissions.OrdersCreate,
                Permissions.OrdersEditSubmitted,
                Permissions.OrdersOverridePrice,
                Permissions.OrdersCoapproveCredit,
                Permissions.OrdersApprove,
                Permissions.WarehouseShip,
                Permissions.DeliveryDispatch,
                Permissions.DeliveryUpdatePod,
                Permissions.InvoicesView,
                Permissions.InvoicesUploadCopy,
                Permissions.SalesReturns,
                Permissions.CollectionsCreate,
                Permissions.FinanceConfirmTreasury,
                Permissions.FinanceVerifyBank,
                Permissions.FinanceClosePeriod,
                Permissions.ProcurementManage,
                Permissions.ProcurementReceive,
                Permissions.AuditView
            ]),
        new(
            2,
            Roles.ExecutiveManager,
            "المدير التنفيذي",
            "Executive Manager",
            [
                Permissions.SettingsManage,
                Permissions.DashboardsExecutive,
                Permissions.DashboardsProfit,
                Permissions.DashboardsSales,
                Permissions.DashboardsFinance,
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CatalogView,
                Permissions.OrdersOverridePrice,
                Permissions.OrdersCoapproveCredit,
                Permissions.OrdersApprove,
                Permissions.InvoicesView,
                Permissions.SalesReturns,
                Permissions.FinanceClosePeriod,
                Permissions.ProcurementManage,
                Permissions.AuditView
            ]),
        new(
            3,
            Roles.SalesManager,
            "مدير المبيعات",
            "Sales Manager",
            [
                Permissions.UsersManage,
                Permissions.DashboardsSales,
                Permissions.DashboardsFinance,
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.CatalogView,
                Permissions.CatalogManage,
                Permissions.OrdersCreate,
                Permissions.OrdersEditSubmitted,
                Permissions.OrdersOverridePrice,
                Permissions.OrdersApprove,
                Permissions.InvoicesView,
                Permissions.InvoicesUploadCopy,
                Permissions.SalesReturns
            ]),
        new(
            4,
            Roles.SalesRep,
            "مندوب مبيعات",
            "Sales Representative",
            [
                Permissions.DashboardsSales,
                Permissions.DashboardsFinance,
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CatalogView,
                Permissions.OrdersCreate,
                Permissions.DeliveryUpdatePod,
                Permissions.InvoicesView,
                Permissions.InvoicesUploadCopy,
                Permissions.CollectionsCreate,
                Permissions.GpsStream
            ]),
        new(
            5,
            Roles.Accountant,
            "محاسب",
            "Accountant",
            [
                Permissions.DashboardsProfit,
                Permissions.DashboardsFinance,
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CustomersManage,
                Permissions.CatalogView,
                Permissions.OrdersCoapproveCredit,
                Permissions.InvoicesView,
                Permissions.InvoicesUploadCopy,
                Permissions.SalesReturns,
                Permissions.FinanceConfirmTreasury,
                Permissions.FinanceVerifyBank,
                Permissions.FinanceClosePeriod
            ]),
        new(
            6,
            Roles.WarehouseKeeper,
            "أمين مستودع",
            "Warehouse Keeper",
            [
                Permissions.DashboardsInventory,
                Permissions.CustomersView,
                Permissions.CatalogView,
                Permissions.WarehouseShip,
                Permissions.DeliveryDispatch,
                Permissions.DeliveryUpdatePod,
                Permissions.InvoicesView,
                Permissions.InvoicesUploadCopy,
                Permissions.SalesReturns,
                Permissions.ProcurementReceive
            ]),
        new(
            7,
            Roles.PurchasingManager,
            "مدير المشتريات",
            "Purchasing Manager",
            [
                Permissions.DashboardsInventory,
                Permissions.CatalogView,
                Permissions.CatalogManage,
                Permissions.ProcurementManage,
                Permissions.ProcurementReceive
            ])
    ];
}
