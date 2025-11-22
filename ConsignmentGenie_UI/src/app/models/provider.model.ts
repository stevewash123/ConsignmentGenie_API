export interface Provider {
  id: number;
  name: string;
  email?: string;
  phone?: string;
  address?: string;
  commissionRate: number;
  notes?: string;
  isActive: boolean;
  organizationId: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateProviderRequest {
  name: string;
  email?: string;
  phone?: string;
  address?: string;
  commissionRate: number;
  notes?: string;
}

export interface UpdateProviderRequest extends CreateProviderRequest {
  isActive: boolean;
}