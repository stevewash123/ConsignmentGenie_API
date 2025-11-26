import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Transaction, CreateTransactionRequest } from '../models/transaction.model';
import { PagedResult } from '../shared/models/api.models';

export type { PagedResult };
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private readonly apiUrl = `${environment.apiUrl}/api/transactions`;

  constructor(private http: HttpClient) {}

  getTransactions(queryParams?: TransactionQueryParams): Observable<PagedResult<Transaction>> {
    let params = new HttpParams();
    if (queryParams?.startDate) params = params.set('startDate', queryParams.startDate.toISOString());
    if (queryParams?.endDate) params = params.set('endDate', queryParams.endDate.toISOString());
    if (queryParams?.providerId) params = params.set('providerId', queryParams.providerId);
    if (queryParams?.paymentMethod) params = params.set('paymentMethod', queryParams.paymentMethod);
    if (queryParams?.page) params = params.set('page', queryParams.page.toString());
    if (queryParams?.pageSize) params = params.set('pageSize', queryParams.pageSize.toString());
    if (queryParams?.sortBy) params = params.set('sortBy', queryParams.sortBy);
    if (queryParams?.sortDirection) params = params.set('sortDirection', queryParams.sortDirection);

    return this.http.get<PagedResult<Transaction>>(this.apiUrl, { params });
  }

  getTransaction(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.apiUrl}/${id}`);
  }

  createTransaction(request: CreateTransactionRequest): Observable<Transaction> {
    return this.http.post<Transaction>(this.apiUrl, request);
  }

  updateTransaction(id: string, request: UpdateTransactionRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.apiUrl}/${id}`, request);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getSalesMetrics(queryParams?: MetricsQueryParams): Observable<SalesMetrics> {
    let params = new HttpParams();
    if (queryParams?.startDate) params = params.set('startDate', queryParams.startDate.toISOString());
    if (queryParams?.endDate) params = params.set('endDate', queryParams.endDate.toISOString());
    if (queryParams?.providerId) params = params.set('providerId', queryParams.providerId);

    return this.http.get<SalesMetrics>(`${this.apiUrl}/metrics`, { params });
  }
}

export interface TransactionQueryParams {
  startDate?: Date;
  endDate?: Date;
  providerId?: string;
  paymentMethod?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface MetricsQueryParams {
  startDate?: Date;
  endDate?: Date;
  providerId?: string;
}

export interface UpdateTransactionRequest {
  salePrice?: number;
  salesTaxAmount?: number;
  paymentMethod?: string;
  notes?: string;
}

export interface SalesMetrics {
  totalSales: number;
  totalShopAmount: number;
  totalProviderAmount: number;
  totalTax: number;
  transactionCount: number;
  averageTransactionValue: number;
  topProviders: ProviderSales[];
  paymentMethodBreakdown: PaymentMethodBreakdown[];
  periodStart?: Date;
  periodEnd?: Date;
}

export interface ProviderSales {
  providerId: string;
  providerName: string;
  transactionCount: number;
  totalSales: number;
  totalProviderAmount: number;
}

export interface PaymentMethodBreakdown {
  paymentMethod: string;
  count: number;
  total: number;
}