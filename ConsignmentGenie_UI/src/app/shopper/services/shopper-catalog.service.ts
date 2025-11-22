import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ShopperItemImage {
  imageId: string;
  imageUrl: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ShopperItemList {
  itemId: string;
  title: string;
  description?: string;
  price: number;
  category?: string;
  brand?: string;
  size?: string;
  color?: string;
  condition: string;
  primaryImageUrl?: string;
  listedDate?: string;
  images: ShopperItemImage[];
}

export interface ShopperItemDetail {
  itemId: string;
  title: string;
  description?: string;
  price: number;
  category?: string;
  brand?: string;
  size?: string;
  color?: string;
  condition: string;
  materials?: string;
  measurements?: string;
  images: ShopperItemImage[];
  isAvailable: boolean;
  listedDate?: string;
}

export interface ShopperCatalogFilters {
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  condition?: string;
  size?: string;
  sortBy: string;
  sortDirection: string;
}

export interface ShopperCatalog {
  items: ShopperItemList[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  filters: ShopperCatalogFilters;
}

export interface ShopperCategory {
  name: string;
  itemCount: number;
}

export interface ShopperSearchResult {
  items: ShopperItemList[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  searchQuery: string;
  filters: ShopperCatalogFilters;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface CatalogRequest {
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  condition?: string;
  size?: string;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchRequest extends CatalogRequest {
  searchQuery: string;
}

@Injectable({
  providedIn: 'root'
})
export class ShopperCatalogService {
  private apiUrl = `${environment.apiUrl}/api/shop`;

  constructor(private http: HttpClient) {}

  /**
   * Get catalog items for a store with pagination and filtering
   */
  getCatalogItems(storeSlug: string, request: CatalogRequest = {}): Observable<ApiResponse<ShopperCatalog>> {
    let params = new HttpParams();

    if (request.category) params = params.set('category', request.category);
    if (request.minPrice !== undefined) params = params.set('minPrice', request.minPrice.toString());
    if (request.maxPrice !== undefined) params = params.set('maxPrice', request.maxPrice.toString());
    if (request.condition) params = params.set('condition', request.condition);
    if (request.size) params = params.set('size', request.size);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    if (request.page !== undefined) params = params.set('page', request.page.toString());
    if (request.pageSize !== undefined) params = params.set('pageSize', request.pageSize.toString());

    return this.http.get<ApiResponse<ShopperCatalog>>(`${this.apiUrl}/${storeSlug}/items`, { params });
  }

  /**
   * Get a single item detail
   */
  getItemDetail(storeSlug: string, itemId: string): Observable<ApiResponse<ShopperItemDetail>> {
    return this.http.get<ApiResponse<ShopperItemDetail>>(`${this.apiUrl}/${storeSlug}/items/${itemId}`);
  }

  /**
   * Get available categories for the store
   */
  getCategories(storeSlug: string): Observable<ApiResponse<ShopperCategory[]>> {
    return this.http.get<ApiResponse<ShopperCategory[]>>(`${this.apiUrl}/${storeSlug}/categories`);
  }

  /**
   * Search items in the store
   */
  searchItems(storeSlug: string, request: SearchRequest): Observable<ApiResponse<ShopperSearchResult>> {
    let params = new HttpParams();

    params = params.set('q', request.searchQuery);
    if (request.category) params = params.set('category', request.category);
    if (request.minPrice !== undefined) params = params.set('minPrice', request.minPrice.toString());
    if (request.maxPrice !== undefined) params = params.set('maxPrice', request.maxPrice.toString());
    if (request.condition) params = params.set('condition', request.condition);
    if (request.size) params = params.set('size', request.size);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    if (request.page !== undefined) params = params.set('page', request.page.toString());
    if (request.pageSize !== undefined) params = params.set('pageSize', request.pageSize.toString());

    return this.http.get<ApiResponse<ShopperSearchResult>>(`${this.apiUrl}/${storeSlug}/search`, { params });
  }
}