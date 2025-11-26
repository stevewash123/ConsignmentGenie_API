import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminMetrics {
  activeOrganizations: number;
  pendingApprovals: number;
  pendingInvitations: number;
}

export interface OwnerInvitation {
  id: string;
  name: string;
  email: string;
  sentAt: string;
  expiresAt: string;
  status: 'pending' | 'accepted' | 'expired' | 'cancelled';
}

export interface InviteOwnerRequest {
  name: string;
  email: string;
}

export interface PendingApproval {
  id: string;
  organization: string;
  owner: string;
  email: string;
  submittedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/api/admin`;

  constructor(private http: HttpClient) {}

  getMetrics(): Observable<AdminMetrics> {
    return this.http.get<AdminMetrics>(`${this.apiUrl}/invitations/owner/metrics`);
  }

  // Owner Invitation Management
  getOwnerInvitations(): Observable<OwnerInvitation[]> {
    return this.http.get<OwnerInvitation[]>(`${this.apiUrl}/invitations/owner`);
  }

  inviteOwner(request: InviteOwnerRequest): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/invitations/owner`, request);
  }

  resendOwnerInvitation(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/invitations/owner/${id}/resend`, {});
  }

  cancelOwnerInvitation(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.patch<{ success: boolean; message: string }>(`${this.apiUrl}/invitations/owner/${id}/cancel`, {});
  }

  // Approval Management
  getPendingApprovals(): Observable<PendingApproval[]> {
    return this.http.get<PendingApproval[]>(`${this.apiUrl}/pending-owners`);
  }

  approveOrganization(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/approvals/${id}/approve`, {});
  }

  rejectOrganization(id: string, reason?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/approvals/${id}/reject`, { reason });
  }
}