export interface ProviderDashboard {
  shopName: string;
  providerName: string;
  totalItems: number;
  availableItems: number;
  soldItems: number;
  inventoryValue: number;
  pendingBalance: number;
  totalEarningsAllTime: number;
  earningsThisMonth: number;
  recentSales: ProviderSale[];
  lastPayout?: ProviderPayout;
}

export interface ProviderItem {
  itemId: string;
  sku: string;
  title: string;
  primaryImageUrl: string;
  price: number;
  myEarnings: number;
  category: string;
  status: string;
  receivedDate: Date;
  soldDate?: Date;
  salePrice?: number;
}

export interface ProviderItemDetail extends ProviderItem {
  description: string;
  imageUrls: string[];
  notes: string;
}

export interface ProviderSale {
  transactionId: string;
  saleDate: Date;
  itemTitle: string;
  itemSku: string;
  salePrice: number;
  myEarnings: number;
  payoutStatus: string;
}

export interface ProviderPayout {
  payoutId: string;
  payoutNumber: string;
  payoutDate: Date;
  amount: number;
  paymentMethod: string;
  itemCount: number;
}

export interface ProviderPayoutDetail extends ProviderPayout {
  paymentReference: string;
  periodStart: Date;
  periodEnd: Date;
  items: ProviderSale[];
}

export interface ProviderProfile {
  providerId: string;
  fullName: string;
  email: string;
  phone?: string;
  commissionRate: number;
  preferredPaymentMethod?: string;
  paymentDetails?: string;
  emailNotifications: boolean;
  memberSince: Date;
  organizationName: string;
}

export interface UpdateProviderProfile {
  fullName: string;
  phone?: string;
  preferredPaymentMethod?: string;
  paymentDetails?: string;
  emailNotifications: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface ProviderItemQuery {
  status?: string;
  category?: string;
  dateFrom?: Date;
  dateTo?: Date;
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface ProviderSaleQuery {
  dateFrom?: Date;
  dateTo?: Date;
  payoutStatus?: string;
  page?: number;
  pageSize?: number;
}