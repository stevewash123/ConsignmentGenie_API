export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  businessName: string;
  ownerName: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface WrappedAuthResponse {
  success: boolean;
  data: AuthResponse;
  message?: string;
  errors?: string[] | null;
}

export type LoginResponse = AuthResponse | WrappedAuthResponse;

export interface User {
  id: number;
  email: string;
  businessName: string;
  ownerName: string;
  organizationId: number;
  role: string | number;
}

export interface TokenInfo {
  token: string;
  expiresAt: Date;
}