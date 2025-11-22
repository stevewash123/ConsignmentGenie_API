import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Provider, CreateProviderRequest, UpdateProviderRequest } from '../models/provider.model';

@Injectable({
  providedIn: 'root'
})
export class ProviderService {
  private readonly apiUrl = 'http://localhost:5000/api/providers';

  constructor(private http: HttpClient) {}

  getProviders(): Observable<Provider[]> {
    return this.http.get<Provider[]>(this.apiUrl);
  }

  getProvider(id: number): Observable<Provider> {
    return this.http.get<Provider>(`${this.apiUrl}/${id}`);
  }

  createProvider(request: CreateProviderRequest): Observable<Provider> {
    return this.http.post<Provider>(this.apiUrl, request);
  }

  updateProvider(id: number, request: UpdateProviderRequest): Observable<Provider> {
    return this.http.put<Provider>(`${this.apiUrl}/${id}`, request);
  }

  deleteProvider(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  deactivateProvider(id: number): Observable<Provider> {
    return this.http.patch<Provider>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  activateProvider(id: number): Observable<Provider> {
    return this.http.patch<Provider>(`${this.apiUrl}/${id}/activate`, {});
  }
}