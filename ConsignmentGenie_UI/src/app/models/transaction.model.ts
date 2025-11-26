export interface Transaction {
  id: string;
  saleDate: Date;
  salePrice: number;
  salesTaxAmount?: number;
  paymentMethod: string;

  // Commission split
  providerSplitPercentage: number;
  providerAmount: number;
  shopAmount: number;

  // Navigation data
  item: ItemSummary;
  provider: ProviderSummary;
  notes?: string;

  // Audit
  createdAt: Date;
  updatedAt: Date;
}

export interface ItemSummary {
  id: string;
  name: string;
  description?: string;
  originalPrice: number;
}

export interface ProviderSummary {
  id: string;
  name: string;
  email?: string;
}

export interface CreateTransactionRequest {
  itemId: string;
  salePrice: number;
  salesTaxAmount?: number;
  paymentMethod: string;
  notes?: string;
  saleDate?: Date;
}