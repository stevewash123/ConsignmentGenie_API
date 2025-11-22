import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  PayoutDto,
  PayoutListDto,
  PayoutStatus,
  CreatePayoutRequest,
  UpdatePayoutRequest,
  PayoutSearchRequest,
  PayoutSearchResponse,
  PendingPayoutsRequest,
  PendingPayoutData
} from '../models/payout.model';
import { environment } from '../../environments/environment';

export { PayoutStatus };

@Injectable({
  providedIn: 'root'
})
export class PayoutService {
  private readonly apiUrl = `${environment.apiUrl}/api/payouts`;

  constructor(private http: HttpClient) {}

  getPayouts(request?: PayoutSearchRequest): Observable<PayoutSearchResponse> {
    let params = new HttpParams();

    if (request?.providerId) params = params.set('providerId', request.providerId);
    if (request?.payoutDateFrom) params = params.set('payoutDateFrom', request.payoutDateFrom.toISOString());
    if (request?.payoutDateTo) params = params.set('payoutDateTo', request.payoutDateTo.toISOString());
    if (request?.status) params = params.set('status', request.status);
    if (request?.periodStart) params = params.set('periodStart', request.periodStart.toISOString());
    if (request?.periodEnd) params = params.set('periodEnd', request.periodEnd.toISOString());
    if (request?.page) params = params.set('page', request.page.toString());
    if (request?.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request?.sortBy) params = params.set('sortBy', request.sortBy);
    if (request?.sortDirection) params = params.set('sortDirection', request.sortDirection);

    return this.http.get<PayoutSearchResponse>(this.apiUrl, { params });
  }

  getPayoutById(id: string): Observable<PayoutDto> {
    return this.http.get<{success: boolean, data: PayoutDto}>(`${this.apiUrl}/${id}`)
      .pipe(map(response => response.data));
  }

  getPendingPayouts(request?: PendingPayoutsRequest): Observable<PendingPayoutData[]> {
    let params = new HttpParams();

    if (request?.providerId) params = params.set('providerId', request.providerId);
    if (request?.periodEndBefore) params = params.set('periodEndBefore', request.periodEndBefore.toISOString());
    if (request?.minimumAmount) params = params.set('minimumAmount', request.minimumAmount.toString());

    return this.http.get<{success: boolean, data: PendingPayoutData[]}>(`${this.apiUrl}/pending`, { params })
      .pipe(map(response => response.data));
  }

  createPayout(request: CreatePayoutRequest): Observable<PayoutDto> {
    return this.http.post<{success: boolean, data: PayoutDto}>(this.apiUrl, request)
      .pipe(map(response => response.data));
  }

  updatePayout(id: string, request: UpdatePayoutRequest): Observable<void> {
    return this.http.put<{success: boolean, message: string}>(`${this.apiUrl}/${id}`, request)
      .pipe(map(() => {}));
  }

  deletePayout(id: string): Observable<void> {
    return this.http.delete<{success: boolean, message: string}>(`${this.apiUrl}/${id}`)
      .pipe(map(() => {}));
  }

  exportPayoutToCsv(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/export`, {
      responseType: 'blob'
    });
  }

  exportPayoutToPdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, {
      responseType: 'blob'
    });
  }
}