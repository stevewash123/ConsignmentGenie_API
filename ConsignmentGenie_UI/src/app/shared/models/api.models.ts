// Core API Response Models
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
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

// Store Configuration
export interface PublicStoreInfo {
  organizationId: string;
  storeName: string;
  storeUrl?: string;
  logoUrl?: string;
  theme: string;
  contactInfo?: any;
  storeDescription?: string;
  storeTagline?: string;
  allowWishlist: boolean;
  allowCustomerReviews: boolean;
  requireCustomerRegistration: boolean;
  currency: string;
  isPublicStoreEnabled: boolean;
}

// Product Models
export interface PublicItem {
  id: string;
  title: string;
  description?: string;
  price: number;
  originalPrice?: number;
  condition?: string;
  category?: string;
  subcategory?: string;
  tags: string[];
  photos: ItemPhoto[];
  providerName: string;
  providerId: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
  organizationId: string;
  isAvailable: boolean;
  dimensions?: string;
  materials?: string;
  brand?: string;
}

export interface PublicItemDetail extends PublicItem {
  providerInfo: {
    id: string;
    name: string;
    bio?: string;
    specialties?: string[];
  };
  relatedItems?: PublicItem[];
  specifications?: { [key: string]: any };
}

export interface ItemPhoto {
  id: string;
  url: string;
  altText?: string;
  isPrimary: boolean;
  order: number;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  itemCount?: number;
  subcategories?: Category[];
}

// Search Models
export interface PublicItemSearchRequest {
  orgSlug: string;
  page: number;
  pageSize: number;
  search?: string;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy: string;
  sortOrder: string;
}

export interface PublicSearchRequest {
  orgSlug: string;
  query?: string;
  categories?: string[];
  priceRange?: {
    min: number;
    max: number;
  };
  conditions?: string[];
  providers?: string[];
  tags?: string[];
  sortBy?: string;
  sortOrder?: string;
  page: number;
  pageSize: number;
}

export interface PublicSearchResult {
  items: PublicItem[];
  facets: {
    categories: { name: string; count: number; }[];
    priceRanges: { label: string; min: number; max: number; count: number; }[];
    conditions: { name: string; count: number; }[];
    providers: { name: string; count: number; }[];
  };
  totalCount: number;
  organizationId: string;
}