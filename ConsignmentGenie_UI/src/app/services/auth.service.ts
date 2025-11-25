import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, BehaviorSubject, catchError, of } from 'rxjs';
import { LoginRequest, RegisterRequest, AuthResponse, User, TokenInfo, LoginResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api';
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  private tokenInfo = signal<TokenInfo | null>(null);

  public currentUser$ = this.currentUserSubject.asObservable();
  public isLoggedIn = signal(false);

  constructor(private http: HttpClient) {
    this.loadStoredAuth();
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, request)
      .pipe(
        tap(response => {
          // Handle wrapped API response format
          const authData = this.extractAuthData(response);
          this.setAuthData(authData);
        })
      );
  }

  private extractAuthData(response: LoginResponse): AuthResponse {
    // Type guard to check if it's a wrapped response
    if ('success' in response && 'data' in response) {
      return response.data;
    }
    // Otherwise it's a direct AuthResponse
    return response;
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, request)
      .pipe(
        tap(response => this.setAuthData(response))
      );
  }

  registerOwner(request: {
    fullName: string;
    email: string;
    phone?: string;
    password: string;
    shopName: string;
  }): Observable<{ success: boolean; message?: string; errors?: string[] }> {
    return this.http.post<{ success: boolean; message?: string; errors?: string[] }>(
      `${this.apiUrl}/auth/register/owner`,
      request
    ).pipe(
      catchError((error: any) => {
        return of({
          success: false,
          message: error.error?.message || 'Registration failed',
          errors: error.error?.errors || [error.message]
        });
      })
    );
  }

  registerProvider(request: {
    storeCode: string;
    fullName: string;
    email: string;
    password: string;
    phone?: string;
    preferredPaymentMethod?: string;
    paymentDetails?: string;
  }): Observable<{ success: boolean; message?: string; errors?: string[] }> {
    return this.http.post<{ success: boolean; message?: string; errors?: string[] }>(
      `${this.apiUrl}/auth/register/provider`,
      request
    ).pipe(
      catchError((error: any) => {
        return of({
          success: false,
          message: error.error?.message || 'Registration failed',
          errors: error.error?.errors || [error.message]
        });
      })
    );
  }

  validateStoreCode(code: string): Observable<{
    isValid: boolean;
    shopName?: string;
    errorMessage?: string;
  }> {
    return this.http.get<{
      isValid: boolean;
      shopName?: string;
      errorMessage?: string;
    }>(`${this.apiUrl}/auth/validate-store-code/${code}`).pipe(
      catchError((error: any) => {
        return of({
          isValid: false,
          errorMessage: 'Unable to validate store code'
        });
      })
    );
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user_data');
    localStorage.removeItem('tokenExpiry');

    this.currentUserSubject.next(null);
    this.tokenInfo.set(null);
    this.isLoggedIn.set(false);
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(
        tap(response => this.setAuthData(response))
      );
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isTokenExpired(): boolean {
    const tokenInfo = this.tokenInfo();
    if (!tokenInfo) return true;
    return new Date() >= tokenInfo.expiresAt;
  }

  private setAuthData(response: AuthResponse): void {
    localStorage.setItem('auth_token', response.token);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('user_data', JSON.stringify(response.user));
    localStorage.setItem('tokenExpiry', response.expiresAt);

    this.currentUserSubject.next(response.user);
    this.tokenInfo.set({
      token: response.token,
      expiresAt: new Date(response.expiresAt)
    });
    this.isLoggedIn.set(true);
  }

  public loadStoredAuth(): void {
    try {
      const token = localStorage.getItem('auth_token');
      const userJson = localStorage.getItem('user_data');
      const expiryStr = localStorage.getItem('tokenExpiry');

      // Validate that we have valid values (not null, undefined, or the string "undefined")
      if (token && userJson && expiryStr &&
          token !== 'undefined' && userJson !== 'undefined' && expiryStr !== 'undefined') {
        const user = JSON.parse(userJson);
        const expiry = new Date(expiryStr);

        if (expiry > new Date()) {
          this.currentUserSubject.next(user);
          this.tokenInfo.set({ token, expiresAt: expiry });
          this.isLoggedIn.set(true);
        } else {
          this.logout();
        }
      }
    } catch (error) {
      console.error('Error loading stored auth data:', error);
      // Clear potentially corrupted localStorage data
      this.logout();
    }
  }
}