export interface PayoutReport {
  id: number;
  providerId: number;
  providerName: string;
  startDate: Date;
  endDate: Date;
  totalSales: number;
  totalCommission: number;
  payoutAmount: number;
  status: PayoutStatus;
  transactionCount: number;
  organizationId: number;
  createdAt: Date;
  transactions: PayoutTransaction[];
}

export enum PayoutStatus {
  Pending = 'Pending',
  Processed = 'Processed',
  Paid = 'Paid'
}

export interface PayoutTransaction {
  transactionDate: Date;
  itemName: string;
  quantity: number;
  unitPrice: number;
  commission: number;
  providerAmount: number;
}

export interface GeneratePayoutRequest {
  providerId: number;
  startDate: Date;
  endDate: Date;
}