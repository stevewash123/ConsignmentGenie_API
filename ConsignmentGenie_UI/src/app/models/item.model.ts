export interface Item {
  id: number;
  name: string;
  description?: string;
  sku?: string;
  price: number;
  cost?: number;
  providerId: number;
  categoryId?: number;
  status: ItemStatus;
  organizationId: number;
  createdAt: Date;
  updatedAt: Date;
  provider?: { name: string };
  category?: { name: string };
}

export enum ItemStatus {
  Available = 'Available',
  Sold = 'Sold',
  Reserved = 'Reserved',
  Returned = 'Returned'
}

export interface CreateItemRequest {
  name: string;
  description?: string;
  sku?: string;
  price: number;
  cost?: number;
  providerId: number;
  categoryId?: number;
}

export interface UpdateItemRequest extends CreateItemRequest {
  status: ItemStatus;
}