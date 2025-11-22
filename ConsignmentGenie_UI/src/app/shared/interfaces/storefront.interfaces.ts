export interface Product {
  id: string;
  sku: string;
  title: string;
  description: string;
  price: number;
  category: string;
  providerId: string;
  images: ProductImage[];
  inventory: InventoryInfo;
  status: ProductStatus;
  createdAt: Date;
  updatedAt: Date;
}

export interface ProductImage {
  id: string;
  url: string;
  altText: string;
  isPrimary: boolean;
  order: number;
}

export interface InventoryInfo {
  quantity: number;
  lowStockThreshold: number;
  trackInventory: boolean;
}

export enum ProductStatus {
  Draft = 'draft',
  Active = 'active',
  Inactive = 'inactive',
  OutOfStock = 'out_of_stock',
  Sold = 'sold'
}

export interface InventoryUpdate {
  productId: string;
  sku: string;
  quantityChange: number;
  reason: InventoryChangeReason;
  notes?: string;
}

export enum InventoryChangeReason {
  Sale = 'sale',
  Adjustment = 'adjustment',
  Damage = 'damage',
  Return = 'return',
  Restock = 'restock'
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId?: string;
  customerInfo: CustomerInfo;
  items: OrderItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  shippingAddress?: Address;
  billingAddress?: Address;
  createdAt: Date;
  updatedAt: Date;
}

export interface CustomerInfo {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
}

export interface OrderItem {
  productId: string;
  sku: string;
  title: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export enum OrderStatus {
  Pending = 'pending',
  Processing = 'processing',
  Shipped = 'shipped',
  Delivered = 'delivered',
  Cancelled = 'cancelled',
  Returned = 'returned'
}

export enum PaymentStatus {
  Pending = 'pending',
  Completed = 'completed',
  Failed = 'failed',
  Refunded = 'refunded',
  PartiallyRefunded = 'partially_refunded'
}

export interface TrafficStats {
  period: DateRange;
  uniqueVisitors: number;
  pageViews: number;
  bounceRate: number;
  averageSessionDuration: number;
  topPages: PageView[];
}

export interface PageView {
  page: string;
  views: number;
  uniqueViews: number;
}

export interface ConversionMetrics {
  period: DateRange;
  conversionRate: number;
  averageOrderValue: number;
  totalOrders: number;
  totalRevenue: number;
  cartAbandonmentRate: number;
}

export interface DateRange {
  startDate: Date;
  endDate: Date;
}

// Granular Storefront Service Interfaces

/**
 * Interface for product catalog management operations
 * Implementations: SquareCatalogService, ShopifyCatalogService, InternalCatalogService
 */
export interface IStorefrontCatalog {
  publishProducts(products: Product[]): Promise<void>;
  updateProduct(productId: string, updates: Partial<Product>): Promise<void>;
  removeProduct(productId: string): Promise<void>;
  updateInventory(updates: InventoryUpdate[]): Promise<void>;
  syncInventory(): Promise<void>;
  getStorefrontUrl(): string;
  getPublishedProducts(): Promise<Product[]>;
}

/**
 * Interface for order management operations
 * Implementations: SquareOrderService, ShopifyOrderService, InternalOrderService
 */
export interface IStorefrontOrders {
  getOrders(dateRange?: DateRange): Promise<Order[]>;
  getOrder(orderId: string): Promise<Order | null>;
  updateOrderStatus(orderId: string, status: OrderStatus): Promise<void>;
  updatePaymentStatus(orderId: string, status: PaymentStatus): Promise<void>;
  processRefund(orderId: string, amount: number): Promise<string>;
  cancelOrder(orderId: string, reason: string): Promise<void>;
  exportOrders(dateRange: DateRange): Promise<Blob>;
}

/**
 * Interface for storefront analytics and reporting operations
 * Implementations: SquareAnalyticsService, ShopifyAnalyticsService, InternalAnalyticsService
 */
export interface IStorefrontAnalytics {
  getTrafficStats(period: DateRange): Promise<TrafficStats>;
  getConversionMetrics(period: DateRange): Promise<ConversionMetrics>;
  getTopSellingProducts(period: DateRange, limit: number): Promise<Product[]>;
  getCustomerAnalytics(period: DateRange): Promise<any>; // TBD based on needs
  generatePerformanceReport(period: DateRange): Promise<any>; // TBD based on dashboard
}

/**
 * Interface for storefront configuration and customization
 * Implementations: SquareConfigService, ShopifyConfigService, InternalConfigService
 */
export interface IStorefrontConfiguration {
  updateTheme(themeConfig: any): Promise<void>;
  setBusinessHours(hours: BusinessHours[]): Promise<void>;
  configurePaymentMethods(methods: PaymentMethodConfig[]): Promise<void>;
  updateStoreSettings(settings: StoreSettings): Promise<void>;
  getStoreConfiguration(): Promise<StoreConfiguration>;
}

export interface BusinessHours {
  dayOfWeek: number; // 0 = Sunday
  openTime: string; // "09:00"
  closeTime: string; // "17:00"
  isClosed: boolean;
}

export interface PaymentMethodConfig {
  type: string; // 'credit_card', 'paypal', 'apple_pay', etc.
  enabled: boolean;
  configuration: any; // Provider-specific config
}

export interface StoreSettings {
  storeName: string;
  description: string;
  logoUrl?: string;
  bannerUrl?: string;
  contactInfo: ContactInfo;
  socialMedia: SocialMediaLinks;
}

export interface ContactInfo {
  email: string;
  phone?: string;
  address?: Address;
}

export interface SocialMediaLinks {
  facebook?: string;
  instagram?: string;
  twitter?: string;
  website?: string;
}

export interface StoreConfiguration {
  settings: StoreSettings;
  theme: any;
  businessHours: BusinessHours[];
  paymentMethods: PaymentMethodConfig[];
}