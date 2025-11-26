export interface OwnerInvitationListDto {
  id: string;
  name: string;
  email: string;
  status: string;
  invitedByName: string;
  createdAt: string;
  expiresAt: string;
  isExpired: boolean;
}

export interface OwnerInvitationDetailDto {
  id: string;
  name: string;
  email: string;
  token: string;
  status: string;
  invitedByName: string;
  invitedByEmail: string;
  createdAt: string;
  expiresAt: string;
  isExpired: boolean;
  invitationUrl: string;
}

export interface OwnerInvitationMetricsDto {
  totalInvitations: number;
  pendingInvitations: number;
  acceptedInvitations: number;
  expiredInvitations: number;
  cancelledInvitations: number;
  invitationsThisMonth: number;
  acceptanceRate: number;
}

export interface CreateOwnerInvitationRequest {
  name: string;
  email: string;
}

export interface OwnerInvitationQueryParams {
  search?: string;
  status?: string;
  sortBy?: string;
  sortDirection?: string;
  page: number;
  pageSize: number;
}

export interface ValidateInvitationResponse {
  isValid: boolean;
  name: string;
  email: string;
  expiresAt: string;
  errorMessage?: string;
}

export interface OwnerRegistrationRequest {
  token: string;
  name: string;
  email: string;
  password: string;
  shopName: string;
  subdomain: string;
}

export interface OwnerRegistrationResponse {
  success: boolean;
  userId?: string;
  organizationId?: string;
  token?: string;
  redirectUrl?: string;
  errorMessage?: string;
}

export type InvitationStatus = 'Pending' | 'Accepted' | 'Expired' | 'Cancelled';