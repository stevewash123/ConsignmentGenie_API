namespace ConsignmentGenie.Core.Enums;

public enum Permission
{
    // General Permissions
    ViewDashboard = 1,
    ViewReports = 2,

    // Item Management
    ViewItems = 10,
    CreateItems = 11,
    EditItems = 12,
    DeleteItems = 13,
    ApproveItems = 14,

    // Transaction Management
    ViewTransactions = 20,
    CreateTransactions = 21,
    EditTransactions = 22,
    DeleteTransactions = 23,
    ProcessRefunds = 24,

    // Consignor Management
    ViewProviders = 30,
    CreateProviders = 31,
    EditProviders = 32,
    DeleteProviders = 33,
    ProcessPayouts = 34,

    // Customer Management
    ViewCustomers = 40,
    EditCustomers = 41,
    DeleteCustomers = 42,
    ViewCustomerOrders = 43,

    // User Management
    ViewUsers = 50,
    CreateUsers = 51,
    EditUsers = 52,
    DeleteUsers = 53,
    ManageRoles = 54,

    // Financial Management
    ViewFinancials = 60,
    ManagePayouts = 61,
    ViewAnalytics = 62,
    ExportReports = 63,
    ManageSubscription = 64,

    // System Configuration
    ViewSettings = 70,
    EditSettings = 71,
    ManageIntegrations = 72,
    ViewAuditLogs = 73,

    // Store Management
    ManageStorefront = 80,
    EditStoreConfiguration = 81,
    ManageCategories = 82
}