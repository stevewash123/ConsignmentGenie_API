export interface Provider {
  id: number;
  name: string;
  email: string;
  phone?: string;
  address?: string;
  commissionRate: number;
  preferredPaymentMethod?: string;
  paymentDetails?: string;
  notes?: string;
  isActive: boolean;
  organizationId: number;
  providerNumber?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateProviderRequest {
  name: string;
  email: string;
  phone?: string;
  address?: string;
  commissionRate: number;
  preferredPaymentMethod?: string;
  paymentDetails?: string;
  notes?: string;
}

export interface UpdateProviderRequest extends CreateProviderRequest {
  isActive: boolean;
}