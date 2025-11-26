import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface StoreInfoDto {
  organizationId: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  address?: string;
  phone?: string;
  email?: string;
  hours?: any;
  isOpen: boolean;
}

export interface PublicItemDto {
  itemId: string;
  title: string;
  description?: string;
  price: number;
  category?: string;
  condition?: string;
  size?: string;
  primaryImageUrl?: string;
  listedDate: Date;
}

export interface PublicItemDetailDto extends PublicItemDto {
  imageUrls: string[];
  brand?: string;
  color?: string;
  materials?: string;
  measurements?: string;
  isAvailable: boolean;
}

export interface CategoryDto {
  id: string;
  name: string;
  itemCount: number;
}

export interface ShopItemQueryParams {
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

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ShopperStoreService {
  private readonly API_URL = environment.apiUrl;

  private currentStoreSubject = new BehaviorSubject<StoreInfoDto | null>(null);
  public currentStore$ = this.currentStoreSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Get store information
   */
  getStoreInfo(storeSlug: string): Observable<StoreInfoDto> {
    return this.http.get<ApiResponse<StoreInfoDto>>(
      `${this.API_URL}/api/shop/${storeSlug}`
    ).pipe(
      map(response => response.data!),
      tap(store => this.currentStoreSubject.next(store)),
      catchError(this.handleError)
    );
  }

  /**
   * Get catalog items with pagination and filters
   */
  getCatalogItems(storeSlug: string, queryParams?: ShopItemQueryParams): Observable<PagedResult<PublicItemDto>> {
    let params = new HttpParams();

    if (queryParams) {
      if (queryParams.category) params = params.set('category', queryParams.category);
      if (queryParams.minPrice !== undefined) params = params.set('minPrice', queryParams.minPrice.toString());
      if (queryParams.maxPrice !== undefined) params = params.set('maxPrice', queryParams.maxPrice.toString());
      if (queryParams.condition) params = params.set('condition', queryParams.condition);
      if (queryParams.size) params = params.set('size', queryParams.size);
      if (queryParams.sortBy) params = params.set('sortBy', queryParams.sortBy);
      if (queryParams.sortDirection) params = params.set('sortDirection', queryParams.sortDirection);
      if (queryParams.page !== undefined) params = params.set('page', queryParams.page.toString());
      if (queryParams.pageSize !== undefined) params = params.set('pageSize', queryParams.pageSize.toString());
    }

    return this.http.get<ApiResponse<PagedResult<PublicItemDto>>>(
      `${this.API_URL}/api/shop/${storeSlug}/items`,
      { params }
    ).pipe(
      map(response => response.data!),
      catchError(this.handleError)
    );
  }

  /**
   * Get item detail
   */
  getItemDetail(storeSlug: string, itemId: string): Observable<PublicItemDetailDto> {
    return this.http.get<ApiResponse<PublicItemDetailDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/items/${itemId}`
    ).pipe(
      map(response => response.data!),
      catchError(this.handleError)
    );
  }

  /**
   * Get store categories
   */
  getCategories(storeSlug: string): Observable<CategoryDto[]> {
    return this.http.get<ApiResponse<CategoryDto[]>>(
      `${this.API_URL}/api/shop/${storeSlug}/categories`
    ).pipe(
      map(response => response.data || []),
      catchError(this.handleError)
    );
  }

  /**
   * Search items
   */
  searchItems(storeSlug: string, searchQuery: string, queryParams?: ShopItemQueryParams): Observable<PagedResult<PublicItemDto>> {
    let params = new HttpParams().set('q', searchQuery);

    if (queryParams) {
      if (queryParams.category) params = params.set('category', queryParams.category);
      if (queryParams.minPrice !== undefined) params = params.set('minPrice', queryParams.minPrice.toString());
      if (queryParams.maxPrice !== undefined) params = params.set('maxPrice', queryParams.maxPrice.toString());
      if (queryParams.condition) params = params.set('condition', queryParams.condition);
      if (queryParams.size) params = params.set('size', queryParams.size);
      if (queryParams.sortBy) params = params.set('sortBy', queryParams.sortBy);
      if (queryParams.sortDirection) params = params.set('sortDirection', queryParams.sortDirection);
      if (queryParams.page !== undefined) params = params.set('page', queryParams.page.toString());
      if (queryParams.pageSize !== undefined) params = params.set('pageSize', queryParams.pageSize.toString());
    }

    return this.http.get<ApiResponse<PagedResult<PublicItemDto>>>(
      `${this.API_URL}/api/shop/${storeSlug}/search`,
      { params }
    ).pipe(
      map(response => response.data!),
      catchError(this.handleError)
    );
  }

  /**
   * Get current store
   */
  getCurrentStore(): StoreInfoDto | null {
    return this.currentStoreSubject.value;
  }

  /**
   * Clear current store
   */
  clearCurrentStore(): void {
    this.currentStoreSubject.next(null);
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unexpected error occurred';

    // Check for structured API error responses first
    if (error.error?.error?.message) {
      // Handle test mock format: { error: { message: '...' } }
      errorMessage = error.error.error.message;
    } else if (error.error?.error?.errors?.length > 0) {
      // Handle test mock format: { error: { errors: ['...'] } }
      errorMessage = error.error.error.errors[0];
    } else if (error.error?.message) {
      // Handle direct API error format
      errorMessage = error.error.message;
    } else if (error.error?.errors?.length > 0) {
      // Handle direct API errors array format
      errorMessage = error.error.errors[0];
    } else if (error.message) {
      // Fallback to HTTP error message
      errorMessage = error.message;
    }

    console.error('ShopperStoreService error:', error);
    return throwError(() => new Error(errorMessage));
  }
}