import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiService } from '../../shared/services/api.service';
import {
  PublicStoreInfo,
  PublicItem,
  PublicItemDetail,
  Category,
  PublicItemSearchRequest,
  PublicSearchRequest,
  PublicSearchResult,
  PagedResult
} from '../../shared/models/api.models';

@Injectable({
  providedIn: 'root'
})
export class PublicStoreService {
  private readonly apiService = inject(ApiService);

  // Store info cache
  private storeInfoSubject = new BehaviorSubject<PublicStoreInfo | null>(null);
  public storeInfo$ = this.storeInfoSubject.asObservable();

  getStoreInfo(orgSlug: string): Observable<PublicStoreInfo | null> {
    return this.apiService.get<PublicStoreInfo>(`/api/public/${orgSlug}/store-info`)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.storeInfoSubject.next(response.data);
            return response.data;
          }
          return null;
        }),
        catchError(() => {
          this.storeInfoSubject.next(null);
          throw new Error('Store not found');
        })
      );
  }

  searchItems(request: PublicItemSearchRequest): Observable<PagedResult<PublicItem> | null> {
    const params = this.apiService.buildParams({
      page: request.page,
      pageSize: request.pageSize,
      search: request.search,
      category: request.category,
      minPrice: request.minPrice,
      maxPrice: request.maxPrice,
      sortBy: request.sortBy,
      sortOrder: request.sortOrder
    });

    return this.apiService.get<PagedResult<PublicItem>>(`/api/public/${request.orgSlug}/items`, params)
      .pipe(
        map(response => response.success ? response.data || null : null),
        catchError(() => {
          console.error('Error searching items');
          return [null];
        })
      );
  }

  getItemDetail(orgSlug: string, itemId: string): Observable<PublicItemDetail | null> {
    return this.apiService.get<PublicItemDetail>(`/api/public/${orgSlug}/items/${itemId}`)
      .pipe(
        map(response => response.success ? response.data || null : null),
        catchError(() => {
          console.error('Error getting item detail');
          return [null];
        })
      );
  }

  getCategories(orgSlug: string): Observable<Category[]> {
    return this.apiService.get<Category[]>(`/api/public/${orgSlug}/categories`)
      .pipe(
        map(response => response.success ? response.data || [] : []),
        catchError(() => {
          console.error('Error getting categories');
          return [[]];
        })
      );
  }

  advancedSearch(request: PublicSearchRequest): Observable<PublicSearchResult | null> {
    return this.apiService.post<PublicSearchResult>(`/api/public/${request.orgSlug}/search`, request)
      .pipe(
        map(response => response.success ? response.data || null : null),
        catchError(() => {
          console.error('Error performing advanced search');
          return [null];
        })
      );
  }

  // Helper methods for common search scenarios
  getLatestItems(orgSlug: string, pageSize: number = 12): Observable<PagedResult<PublicItem> | null> {
    return this.searchItems({
      orgSlug,
      page: 1,
      pageSize,
      sortBy: 'created',
      sortOrder: 'desc'
    });
  }

  getFeaturedItems(orgSlug: string, pageSize: number = 8): Observable<PagedResult<PublicItem> | null> {
    return this.searchItems({
      orgSlug,
      page: 1,
      pageSize,
      sortBy: 'featured',
      sortOrder: 'desc'
    });
  }

  getItemsByCategory(orgSlug: string, category: string, page: number = 1, pageSize: number = 20): Observable<PagedResult<PublicItem> | null> {
    return this.searchItems({
      orgSlug,
      page,
      pageSize,
      category,
      sortBy: 'created',
      sortOrder: 'desc'
    });
  }

  searchItemsByQuery(orgSlug: string, query: string, page: number = 1, pageSize: number = 20): Observable<PagedResult<PublicItem> | null> {
    return this.searchItems({
      orgSlug,
      page,
      pageSize,
      search: query,
      sortBy: 'relevance',
      sortOrder: 'desc'
    });
  }
}