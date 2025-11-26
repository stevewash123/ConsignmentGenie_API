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
  ProviderSaleQuery,
  NotificationDto,
  NotificationQueryParams,
  NotificationPreferencesDto,
  UpdateNotificationPreferencesRequest,
  StatementListDto,
  StatementDto
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

  // Notifications
  getNotifications(query?: NotificationQueryParams): Observable<PagedResult<NotificationDto>> {
    let params = new HttpParams();

    if (query?.unreadOnly) params = params.set('unreadOnly', query.unreadOnly.toString());
    if (query?.type) params = params.set('type', query.type);
    if (query?.page) params = params.set('page', query.page.toString());
    if (query?.pageSize) params = params.set('pageSize', query.pageSize.toString());

    return this.http.get<PagedResult<NotificationDto>>(`${this.apiUrl}/notifications`, { params });
  }

  getUnreadNotificationCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/notifications/unread-count`);
  }

  markNotificationAsRead(notificationId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/notifications/${notificationId}/read`, {});
  }

  markAllNotificationsAsRead(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/notifications/mark-all-read`, {});
  }

  deleteNotification(notificationId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/notifications/${notificationId}`);
  }

  getNotificationPreferences(): Observable<NotificationPreferencesDto> {
    return this.http.get<NotificationPreferencesDto>(`${this.apiUrl}/notifications/preferences`);
  }

  updateNotificationPreferences(preferences: UpdateNotificationPreferencesRequest): Observable<NotificationPreferencesDto> {
    return this.http.put<NotificationPreferencesDto>(`${this.apiUrl}/notifications/preferences`, preferences);
  }

  // Statements
  getStatements(): Observable<StatementListDto[]> {
    return this.http.get<StatementListDto[]>(`${this.apiUrl}/statements`);
  }

  getStatement(statementId: string): Observable<StatementDto> {
    return this.http.get<StatementDto>(`${this.apiUrl}/statements/${statementId}`);
  }

  getStatementByPeriod(year: number, month: number): Observable<StatementDto> {
    return this.http.get<StatementDto>(`${this.apiUrl}/statements/period/${year}/${month}`);
  }

  downloadStatementPdf(statementId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/statements/${statementId}/pdf`, {
      responseType: 'blob'
    });
  }

  downloadStatementPdfByPeriod(year: number, month: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/statements/period/${year}/${month}/pdf`, {
      responseType: 'blob'
    });
  }

  regenerateStatement(statementId: string): Observable<StatementDto> {
    return this.http.post<StatementDto>(`${this.apiUrl}/statements/${statementId}/regenerate`, {});
  }
}