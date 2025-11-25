import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Provider, CreateProviderRequest, UpdateProviderRequest } from '../models/provider.model';

export interface ProviderInvitationRequest {
  name: string;
  email: string;
}

export interface ProviderInvitationResponse {
  success: boolean;
  message: string;
  invitation?: any;
}

export interface InvitationValidationResponse {
  isValid: boolean;
  message?: string;
  shopName?: string;
  invitedName?: string;
  invitedEmail?: string;
  expirationDate?: string;
}

export interface ProviderRegistrationRequest {
  invitationToken: string;
  fullName: string;
  email: string;
  password: string;
  phone?: string;
  address?: string;
}

export interface RegistrationResult {
  success: boolean;
  message: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ProviderService {
  private readonly apiUrl = 'http://localhost:5000/api/providers';
  private readonly authUrl = 'http://localhost:5000/api/auth';

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

  inviteProvider(invitation: ProviderInvitationRequest): Observable<ProviderInvitationResponse> {
    return this.http.post<ProviderInvitationResponse>(`${this.apiUrl}/invite`, invitation);
  }

  validateInvitation(token: string): Observable<InvitationValidationResponse> {
    return this.http.get<InvitationValidationResponse>(`${this.authUrl}/validate-invitation/${token}`);
  }

  registerFromInvitation(request: ProviderRegistrationRequest): Observable<RegistrationResult> {
    return this.http.post<RegistrationResult>(`${this.authUrl}/register/provider/invitation`, request);
  }
}