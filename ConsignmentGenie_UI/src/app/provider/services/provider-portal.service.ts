import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ProviderDashboard,
  ProviderItem,
  ProviderItemDetail,
  ProviderSale,
  ProviderPayout,
  ProviderPayoutDetail,
  ProviderProfile,
  UpdateProviderProfile,
  PagedResult,
  ProviderItemQuery,
  ProviderSaleQuery
} from '../models/provider.models';

@Injectable({
  providedIn: 'root'
})
export class ProviderPortalService {
  private apiUrl = `${environment.apiUrl}/api/provider`;

  constructor(private http: HttpClient) {}

  // Dashboard
  getDashboard(): Observable<ProviderDashboard> {
    return this.http.get<ProviderDashboard>(`${this.apiUrl}/dashboard`);
  }

  // Items
  getMyItems(query?: ProviderItemQuery): Observable<PagedResult<ProviderItem>> {
    let params = new HttpParams();

    if (query?.status) params = params.set('status', query.status);
    if (query?.category) params = params.set('category', query.category);
    if (query?.dateFrom) params = params.set('dateFrom', query.dateFrom.toISOString());
    if (query?.dateTo) params = params.set('dateTo', query.dateTo.toISOString());
    if (query?.search) params = params.set('search', query.search);
    if (query?.page) params = params.set('page', query.page.toString());
    if (query?.pageSize) params = params.set('pageSize', query.pageSize.toString());

    return this.http.get<PagedResult<ProviderItem>>(`${this.apiUrl}/items`, { params });
  }

  getMyItem(id: string): Observable<ProviderItemDetail> {
    return this.http.get<ProviderItemDetail>(`${this.apiUrl}/items/${id}`);
  }

  // Sales
  getMySales(query?: ProviderSaleQuery): Observable<PagedResult<ProviderSale>> {
    let params = new HttpParams();

    if (query?.dateFrom) params = params.set('dateFrom', query.dateFrom.toISOString());
    if (query?.dateTo) params = params.set('dateTo', query.dateTo.toISOString());
    if (query?.payoutStatus) params = params.set('payoutStatus', query.payoutStatus);
    if (query?.page) params = params.set('page', query.page.toString());
    if (query?.pageSize) params = params.set('pageSize', query.pageSize.toString());

    return this.http.get<PagedResult<ProviderSale>>(`${this.apiUrl}/sales`, { params });
  }

  // Payouts
  getMyPayouts(): Observable<PagedResult<ProviderPayout>> {
    return this.http.get<PagedResult<ProviderPayout>>(`${this.apiUrl}/payouts`);
  }

  getMyPayout(id: string): Observable<ProviderPayoutDetail> {
    return this.http.get<ProviderPayoutDetail>(`${this.apiUrl}/payouts/${id}`);
  }

  // Profile
  getProfile(): Observable<ProviderProfile> {
    return this.http.get<ProviderProfile>(`${this.apiUrl}/profile`);
  }

  updateProfile(profile: UpdateProviderProfile): Observable<ProviderProfile> {
    return this.http.put<ProviderProfile>(`${this.apiUrl}/profile`, profile);
  }
}