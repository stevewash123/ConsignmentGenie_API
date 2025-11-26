import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../shared/models/api.models';
import {
  OwnerInvitationListDto,
  OwnerInvitationDetailDto,
  OwnerInvitationMetricsDto,
  CreateOwnerInvitationRequest,
  OwnerInvitationQueryParams,
  ValidateInvitationResponse,
  OwnerRegistrationRequest,
  OwnerRegistrationResponse
} from '../models/owner-invitation.model';

@Injectable({
  providedIn: 'root'
})
export class OwnerInvitationService {
  private readonly adminApiUrl = `${environment.apiUrl}/api/admin/invitations/owner`;
  private readonly publicApiUrl = `${environment.apiUrl}/api/owner-registration`;

  constructor(private http: HttpClient) {}

  // Admin endpoints
  createInvitation(request: CreateOwnerInvitationRequest): Observable<ApiResponse<OwnerInvitationDetailDto>> {
    return this.http.post<ApiResponse<OwnerInvitationDetailDto>>(this.adminApiUrl, request);
  }

  getInvitations(queryParams: OwnerInvitationQueryParams): Observable<ApiResponse<PagedResult<OwnerInvitationListDto>>> {
    let params = new HttpParams();

    if (queryParams.search) {
      params = params.set('search', queryParams.search);
    }
    if (queryParams.status) {
      params = params.set('status', queryParams.status);
    }
    if (queryParams.sortBy) {
      params = params.set('sortBy', queryParams.sortBy);
    }
    if (queryParams.sortDirection) {
      params = params.set('sortDirection', queryParams.sortDirection);
    }
    params = params.set('page', queryParams.page.toString());
    params = params.set('pageSize', queryParams.pageSize.toString());

    return this.http.get<ApiResponse<PagedResult<OwnerInvitationListDto>>>(this.adminApiUrl, { params });
  }

  getInvitationById(invitationId: string): Observable<ApiResponse<OwnerInvitationDetailDto>> {
    return this.http.get<ApiResponse<OwnerInvitationDetailDto>>(`${this.adminApiUrl}/${invitationId}`);
  }

  getMetrics(): Observable<ApiResponse<OwnerInvitationMetricsDto>> {
    return this.http.get<ApiResponse<OwnerInvitationMetricsDto>>(`${this.adminApiUrl}/metrics`);
  }

  cancelInvitation(invitationId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.adminApiUrl}/${invitationId}/cancel`, {});
  }

  resendInvitation(invitationId: string): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.adminApiUrl}/${invitationId}/resend`, {});
  }

  // Public endpoints (for owner registration)
  validateToken(token: string): Observable<ApiResponse<ValidateInvitationResponse>> {
    const params = new HttpParams().set('token', token);
    return this.http.get<ApiResponse<ValidateInvitationResponse>>(`${this.publicApiUrl}/validate`, { params });
  }

  registerOwner(request: OwnerRegistrationRequest): Observable<ApiResponse<OwnerRegistrationResponse>> {
    return this.http.post<ApiResponse<OwnerRegistrationResponse>>(`${this.publicApiUrl}/register`, request);
  }
}