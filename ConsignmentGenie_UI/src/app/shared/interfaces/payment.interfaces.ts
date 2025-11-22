export interface PaymentTransaction {
  id?: string;
  amount: number;
  currency: string;
  customerId?: string;
  orderId?: string;
  method: PaymentMethod;
  status: PaymentTransactionStatus;
  gateway: PaymentGateway;
  gatewayTransactionId?: string;
  fees?: PaymentFees;
  metadata?: Record<string, any>;
  createdAt: Date;
  processedAt?: Date;
  failureReason?: string;
}

export enum PaymentMethod {
  CreditCard = 'credit_card',
  DebitCard = 'debit_card',
  ACH = 'ach',
  PayPal = 'paypal',
  ApplePay = 'apple_pay',
  GooglePay = 'google_pay',
  Cash = 'cash',
  Check = 'check'
}

export enum PaymentTransactionStatus {
  Pending = 'pending',
  Processing = 'processing',
  Completed = 'completed',
  Failed = 'failed',
  Cancelled = 'cancelled',
  Refunded = 'refunded',
  PartiallyRefunded = 'partially_refunded'
}

export enum PaymentGateway {
  Stripe = 'stripe',
  Square = 'square',
  PayPal = 'paypal',
  Internal = 'internal' // For cash/check transactions
}

export interface PaymentFees {
  processingFee: number;
  gatewayFee: number;
  totalFees: number;
  netAmount: number;
}

export interface PaymentRefund {
  id?: string;
  originalTransactionId: string;
  amount: number;
  reason: RefundReason;
  status: RefundStatus;
  gatewayRefundId?: string;
  processedAt?: Date;
  notes?: string;
}

export enum RefundReason {
  CustomerRequest = 'customer_request',
  Defective = 'defective',
  Fraud = 'fraud',
  Duplicate = 'duplicate',
  Other = 'other'
}

export enum RefundStatus {
  Pending = 'pending',
  Processing = 'processing',
  Completed = 'completed',
  Failed = 'failed'
}

export interface PaymentMethod_Config {
  gateway: PaymentGateway;
  type: PaymentMethod;
  enabled: boolean;
  displayName: string;
  configuration: GatewayConfiguration;
}

export interface GatewayConfiguration {
  // Stripe-specific config
  publishableKey?: string;
  webhookEndpoint?: string;

  // Square-specific config
  applicationId?: string;
  locationId?: string;

  // PayPal-specific config
  clientId?: string;

  // Common config
  testMode: boolean;
  currency: string;
}

export interface PaymentSummary {
  period: DateRange;
  totalTransactions: number;
  totalAmount: number;
  totalFees: number;
  netAmount: number;
  successRate: number;
  averageTransactionValue: number;
  byMethod: PaymentMethodSummary[];
  byGateway: PaymentGatewaySummary[];
}

export interface PaymentMethodSummary {
  method: PaymentMethod;
  transactionCount: number;
  totalAmount: number;
  percentage: number;
}

export interface PaymentGatewaySummary {
  gateway: PaymentGateway;
  transactionCount: number;
  totalAmount: number;
  totalFees: number;
  successRate: number;
}

export interface DateRange {
  startDate: Date;
  endDate: Date;
}

// Payment Service Interface (Stripe is primary, limited fallbacks due to uniqueness)

/**
 * Interface for payment processing operations
 * Primary Implementation: StripePaymentService
 * Limited fallbacks: InternalPaymentService (for cash/check only)
 * Note: Stripe is unique - most operations don't have meaningful fallbacks
 */
export interface IPaymentProcessor {
  // Core payment processing
  processPayment(transaction: Omit<PaymentTransaction, 'id' | 'createdAt'>): Promise<PaymentTransaction>;
  getTransaction(transactionId: string): Promise<PaymentTransaction | null>;
  getTransactions(dateRange?: DateRange): Promise<PaymentTransaction[]>;

  // Refund operations
  processRefund(refund: Omit<PaymentRefund, 'id' | 'processedAt'>): Promise<PaymentRefund>;
  getRefund(refundId: string): Promise<PaymentRefund | null>;
  getRefunds(transactionId: string): Promise<PaymentRefund[]>;

  // Configuration and capabilities
  getSupportedPaymentMethods(): PaymentMethod[];
  isPaymentMethodSupported(method: PaymentMethod): boolean;
  getGatewayInfo(): PaymentGateway;
}

/**
 * Interface for payment analytics and reporting
 * Implementations: StripeAnalyticsService, InternalAnalyticsService
 */
export interface IPaymentAnalytics {
  getPaymentSummary(period: DateRange): Promise<PaymentSummary>;
  getFailedTransactions(period: DateRange): Promise<PaymentTransaction[]>;
  getChargebacksAndDisputes(period: DateRange): Promise<any[]>; // TBD based on gateway
  exportTransactions(period: DateRange): Promise<Blob>;
  generateRevenueReport(period: DateRange): Promise<any>; // TBD based on needs
}

/**
 * Interface for payment method and gateway configuration
 * Implementations: StripeConfigService, InternalConfigService
 */
export interface IPaymentConfiguration {
  getPaymentMethodConfigs(): Promise<PaymentMethod_Config[]>;
  updatePaymentMethodConfig(method: PaymentMethod, config: PaymentMethod_Config): Promise<void>;
  enablePaymentMethod(method: PaymentMethod): Promise<void>;
  disablePaymentMethod(method: PaymentMethod): Promise<void>;
  testGatewayConnection(gateway: PaymentGateway): Promise<boolean>;
  getGatewayConfiguration(gateway: PaymentGateway): Promise<GatewayConfiguration>;
  updateGatewayConfiguration(gateway: PaymentGateway, config: GatewayConfiguration): Promise<void>;
}