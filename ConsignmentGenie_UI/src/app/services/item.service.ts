import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Item, CreateItemRequest, UpdateItemRequest, ItemStatus } from '../models/item.model';

@Injectable({
  providedIn: 'root'
})
export class ItemService {
  private readonly apiUrl = 'http://localhost:5000/api/items';

  constructor(private http: HttpClient) {}

  getItems(filters?: ItemFilters): Observable<Item[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.providerId) params = params.set('providerId', filters.providerId.toString());
    if (filters?.search) params = params.set('search', filters.search);

    return this.http.get<Item[]>(this.apiUrl, { params });
  }

  getItem(id: number): Observable<Item> {
    return this.http.get<Item>(`${this.apiUrl}/${id}`);
  }

  createItem(request: CreateItemRequest): Observable<Item> {
    return this.http.post<Item>(this.apiUrl, request);
  }

  updateItem(id: number, request: UpdateItemRequest): Observable<Item> {
    return this.http.put<Item>(`${this.apiUrl}/${id}`, request);
  }

  deleteItem(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  updateItemStatus(id: number, status: ItemStatus): Observable<Item> {
    return this.http.patch<Item>(`${this.apiUrl}/${id}/status`, { status });
  }

  getItemsByProvider(providerId: number): Observable<Item[]> {
    return this.http.get<Item[]>(`${this.apiUrl}/provider/${providerId}`);
  }

  searchItems(searchTerm: string): Observable<Item[]> {
    const params = new HttpParams().set('search', searchTerm);
    return this.http.get<Item[]>(`${this.apiUrl}/search`, { params });
  }
}

export interface ItemFilters {
  status?: ItemStatus;
  providerId?: number;
  search?: string;
}