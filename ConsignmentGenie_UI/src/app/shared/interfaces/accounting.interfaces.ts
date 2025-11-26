export interface Invoice {
  id?: string;
  number: string;
  customerId: string;
  customerName: string;
  items: InvoiceItem[];
  subtotal: number;
  tax: number;
  total: number;
  dueDate: Date;
  status: InvoiceStatus;
  createdAt: Date;
}

export interface InvoiceItem {
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export enum InvoiceStatus {
  Draft = 'draft',
  Sent = 'sent',
  Paid = 'paid',
  Overdue = 'overdue',
  Void = 'void'
}

export interface Payment {
  id?: string;
  invoiceId?: string;
  amount: number;
  method: PaymentMethod;
  reference?: string;
  processedAt: Date;
  status: PaymentStatus;
}

export enum PaymentMethod {
  Cash = 'cash',
  Check = 'check',
  CreditCard = 'credit_card',
  ACH = 'ach',
  Other = 'other'
}

export enum PaymentStatus {
  Pending = 'pending',
  Completed = 'completed',
  Failed = 'failed',
  Refunded = 'refunded'
}

export interface DateRange {
  startDate: Date;
  endDate: Date;
}

export interface Period {
  year: number;
  quarter?: number;
  month?: number;
}

export interface SalesReport {
  period: Period;
  totalSales: number;
  totalTransactions: number;
  averageTransactionValue: number;
  topItems: ReportItem[];
  salesByProvider: ProviderSales[];
}

export interface ReportItem {
  itemId: string;
  itemName: string;
  quantitySold: number;
  totalRevenue: number;
}

export interface ProviderSales {
  providerId: string;
  providerName: string;
  totalSales: number;
  commission: number;
}

export interface ReconciliationReport {
  period: DateRange;
  totalPayments: number;
  reconciledPayments: number;
  discrepancies: PaymentDiscrepancy[];
}

export interface PaymentDiscrepancy {
  paymentId: string;
  expectedAmount: number;
  actualAmount: number;
  difference: number;
  reason?: string;
}

// Granular Accounting Service Interfaces

/**
 * Interface for invoice management operations
 * Implementations: QuickBooksInvoiceService, SpreadsheetInvoiceService
 */
export interface IAccountingInvoices {
  createInvoice(invoice: Invoice): Promise<string>;
  getInvoice(id: string): Promise<Invoice | null>;
  updateInvoice(id: string, updates: Partial<Invoice>): Promise<void>;
  updateInvoiceStatus(id: string, status: InvoiceStatus): Promise<void>;
  getInvoices(dateRange?: DateRange): Promise<Invoice[]>;
  deleteInvoice(id: string): Promise<void>;
}

/**
 * Interface for payment processing and tracking operations
 * Implementations: QuickBooksPaymentService, SpreadsheetPaymentService
 */
export interface IAccountingPayments {
  recordPayment(payment: Payment): Promise<string>;
  getPayment(id: string): Promise<Payment | null>;
  getPaymentHistory(dateRange: DateRange): Promise<Payment[]>;
  getPaymentsByInvoice(invoiceId: string): Promise<Payment[]>;
  processRefund(paymentId: string, amount: number): Promise<string>;
  reconcilePayments(period: DateRange): Promise<ReconciliationReport>;
}

/**
 * Interface for financial reporting and analytics operations
 * Implementations: QuickBooksReportService, SpreadsheetReportService
 */
export interface IAccountingReports {
  generateSalesReport(period: Period): Promise<SalesReport>;
  generateTaxReport(period: Period): Promise<any>; // TBD based on tax requirements
  generateProviderPayoutReport(period: DateRange): Promise<ProviderSales[]>;
  exportToSpreadsheet(reportType: string, period: Period): Promise<Blob>;
  getFinancialSummary(period: Period): Promise<any>; // TBD based on dashboard needs
}