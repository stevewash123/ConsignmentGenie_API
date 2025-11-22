import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PayoutReport, GeneratePayoutRequest, PayoutStatus } from '../models/payout.model';

export { PayoutStatus };
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PayoutService {
  private readonly apiUrl = `${environment.apiUrl}/api/payouts`;

  constructor(private http: HttpClient) {}

  getPayoutReports(providerId?: number, status?: PayoutStatus): Observable<PayoutReport[]> {
    let params = new HttpParams();
    if (providerId) params = params.set('providerId', providerId.toString());
    if (status) params = params.set('status', status);

    return this.http.get<PayoutReport[]>(this.apiUrl, { params });
  }

  getPayoutReport(id: number): Observable<PayoutReport> {
    return this.http.get<PayoutReport>(`${this.apiUrl}/${id}`);
  }

  generatePayoutReport(request: GeneratePayoutRequest): Observable<PayoutReport> {
    return this.http.post<PayoutReport>(`${this.apiUrl}/generate`, request);
  }

  updatePayoutStatus(id: number, status: PayoutStatus): Observable<PayoutReport> {
    return this.http.patch<PayoutReport>(`${this.apiUrl}/${id}/status`, { status });
  }

  exportPayoutToCsv(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/export`, {
      responseType: 'blob'
    });
  }

  getPayoutsByProvider(providerId: number): Observable<PayoutReport[]> {
    return this.http.get<PayoutReport[]>(`${this.apiUrl}/provider/${providerId}`);
  }

  deletePayoutReport(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  markPayoutAsPaid(id: number): Observable<PayoutReport> {
    return this.updatePayoutStatus(id, PayoutStatus.Paid);
  }

  markPayoutAsProcessed(id: number): Observable<PayoutReport> {
    return this.updatePayoutStatus(id, PayoutStatus.Processed);
  }
}