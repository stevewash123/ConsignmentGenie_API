import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  ItemListDto,
  ItemDetailDto,
  CreateItemRequest,
  UpdateItemRequest,
  UpdateItemStatusRequest,
  ItemQueryParams,
  PagedResult,
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CategoryUsageDto,
  ApiResponse
} from '../models/inventory.model';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api`;

  // Items API
  getItems(params?: ItemQueryParams): Observable<PagedResult<ItemListDto>> {
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach(key => {
        const value = (params as any)[key];
        if (value !== undefined && value !== null && value !== '') {
          if (value instanceof Date) {
            httpParams = httpParams.set(key, value.toISOString());
          } else {
            httpParams = httpParams.set(key, value.toString());
          }
        }
      });
    }

    return this.http.get<PagedResult<ItemListDto>>(`${this.apiUrl}/items`, { params: httpParams });
  }

  getItem(id: string): Observable<ApiResponse<ItemDetailDto>> {
    return this.http.get<ApiResponse<ItemDetailDto>>(`${this.apiUrl}/items/${id}`);
  }

  createItem(item: CreateItemRequest): Observable<ApiResponse<ItemDetailDto>> {
    return this.http.post<ApiResponse<ItemDetailDto>>(`${this.apiUrl}/items`, item);
  }

  updateItem(id: string, item: UpdateItemRequest): Observable<ApiResponse<ItemDetailDto>> {
    return this.http.put<ApiResponse<ItemDetailDto>>(`${this.apiUrl}/items/${id}`, item);
  }

  updateItemStatus(id: string, request: UpdateItemStatusRequest): Observable<ApiResponse<ItemDetailDto>> {
    return this.http.put<ApiResponse<ItemDetailDto>>(`${this.apiUrl}/items/${id}/status`, request);
  }

  deleteItem(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/items/${id}`);
  }

  generateSku(prefix: string): Observable<ApiResponse<string>> {
    return this.http.get<ApiResponse<string>>(`${this.apiUrl}/items/generate-sku/${prefix}`);
  }

  uploadItemPhoto(itemId: string, file: File): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('photo', file);
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/items/${itemId}/photos`, formData);
  }

  deleteItemPhoto(itemId: string, photoUrl: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/items/${itemId}/photos`, {
      body: { photoUrl }
    });
  }

  // Categories API
  getCategories(): Observable<ApiResponse<CategoryDto[]>> {
    return this.http.get<ApiResponse<CategoryDto[]>>(`${this.apiUrl}/categories`);
  }

  getCategory(id: string): Observable<ApiResponse<CategoryDto>> {
    return this.http.get<ApiResponse<CategoryDto>>(`${this.apiUrl}/categories/${id}`);
  }

  createCategory(category: CreateCategoryRequest): Observable<ApiResponse<CategoryDto>> {
    return this.http.post<ApiResponse<CategoryDto>>(`${this.apiUrl}/categories`, category);
  }

  updateCategory(id: string, category: UpdateCategoryRequest): Observable<ApiResponse<CategoryDto>> {
    return this.http.put<ApiResponse<CategoryDto>>(`${this.apiUrl}/categories/${id}`, category);
  }

  deleteCategory(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/categories/${id}`);
  }

  reorderCategories(categoryOrders: { categoryId: string; displayOrder: number }[]): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/categories/reorder`, { categoryOrders });
  }

  getCategoryUsageStats(): Observable<ApiResponse<CategoryUsageDto[]>> {
    return this.http.get<ApiResponse<CategoryUsageDto[]>>(`${this.apiUrl}/categories/usage-stats`);
  }
}