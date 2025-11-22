// Updated inventory models to match the new specification

export interface ItemListDto {
  id: string;
  sku: string;
  title: string;
  description?: string;
  category: string;
  condition: ItemCondition;
  price: number;
  status: ItemStatus;
  primaryImageUrl?: string;
  providerId: string;
  providerName: string;
  commissionRate: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface ItemDetailDto {
  id: string;
  sku: string;
  title: string;
  description?: string;
  category: string;
  condition: ItemCondition;
  price: number;
  originalPrice?: number;
  status: ItemStatus;
  primaryImageUrl?: string;
  materials?: string;
  measurements?: string;
  brand?: string;
  size?: string;
  color?: string;
  season?: string;
  year?: number;
  providerId: string;
  providerName: string;
  commissionRate: number;
  location?: string;
  notes?: string;
  internalNotes?: string;
  createdAt: Date;
  updatedAt: Date;
  transactionId?: string;
  salePrice?: number;
  images: ItemImageDto[];
}

export interface ItemImageDto {
  id: string;
  imageUrl: string;
  displayOrder: number;
  isPrimary: boolean;
  createdAt: Date;
}

export interface CreateItemRequest {
  providerId: string;
  title: string;
  description?: string;
  category: string;
  condition: ItemCondition;
  price: number;
  originalPrice?: number;
  materials?: string;
  measurements?: string;
  brand?: string;
  size?: string;
  color?: string;
  season?: string;
  year?: number;
  location?: string;
  notes?: string;
  internalNotes?: string;
}

export interface UpdateItemRequest {
  providerId: string;
  title: string;
  description?: string;
  category: string;
  condition: ItemCondition;
  price: number;
  originalPrice?: number;
  materials?: string;
  measurements?: string;
  brand?: string;
  size?: string;
  color?: string;
  season?: string;
  year?: number;
  location?: string;
  notes?: string;
  internalNotes?: string;
}

export interface UpdateItemStatusRequest {
  status: string;
  reason?: string;
}

export interface ItemQueryParams {
  page: number;
  pageSize: number;
  search?: string;
  category?: string;
  status?: string;
  condition?: string;
  providerId?: string;
  priceMin?: number;
  priceMax?: number;
  createdAfter?: Date;
  createdBefore?: Date;
  sortBy?: string;
  sortDirection?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  organizationId: string;
}

export enum ItemCondition {
  New = 'New',
  LikeNew = 'LikeNew',
  Good = 'Good',
  Fair = 'Fair',
  Poor = 'Poor'
}

export enum ItemStatus {
  Available = 'Available',
  Sold = 'Sold',
  Removed = 'Removed'
}

export interface CategoryDto {
  id: string;
  name: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: Date;
}

export interface CreateCategoryRequest {
  name: string;
  displayOrder?: number;
}

export interface UpdateCategoryRequest {
  name: string;
  displayOrder: number;
}

export interface CategoryUsageDto {
  categoryId: string;
  categoryName: string;
  itemCount: number;
  availableItemCount: number;
  soldItemCount: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}