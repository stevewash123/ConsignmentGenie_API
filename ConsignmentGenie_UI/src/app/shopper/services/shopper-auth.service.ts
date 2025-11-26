import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface ShopperRegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
  emailNotifications: boolean;
}

export interface ShopperLoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface GuestSessionRequest {
  email: string;
  fullName: string;
  phone?: string;
}

export interface AuthResultDto {
  success: boolean;
  token?: string;
  expiresAt?: Date;
  profile?: ShopperProfileDto;
  errorMessage?: string;
}

export interface ShopperProfileDto {
  shopperId: string;
  fullName: string;
  email: string;
  phone?: string;
  shippingAddress?: AddressDto;
  emailNotifications: boolean;
  memberSince: Date;
}

export interface AddressDto {
  address1?: string;
  address2?: string;
  city?: string;
  state?: string;
  zip?: string;
}

export interface GuestSessionDto {
  sessionToken: string;
  expiresAt: Date;
}

export interface StoreInfoDto {
  organizationId: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  address?: string;
  phone?: string;
  email?: string;
  hours?: any;
  isOpen: boolean;
}

export interface UpdateShopperProfileRequest {
  fullName: string;
  phone?: string;
  shippingAddress?: AddressDto;
  emailNotifications: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ShopperAuthService {
  private readonly API_URL = environment.apiUrl;
  private readonly TOKEN_KEY = 'shopper_token_';
  private readonly PROFILE_KEY = 'shopper_profile_';

  private currentProfileSubject = new BehaviorSubject<ShopperProfileDto | null>(null);
  public currentProfile$ = this.currentProfileSubject.asObservable();

  private authStatusSubject = new BehaviorSubject<boolean>(false);
  public authStatus$ = this.authStatusSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initializeAuthStatus();
  }

  /**
   * Register a new shopper for a store
   */
  register(storeSlug: string, request: ShopperRegisterRequest): Observable<AuthResultDto> {
    return this.http.post<ApiResponse<AuthResultDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/auth/register`,
      request
    ).pipe(
      map(response => response.data!),
      tap(result => {
        if (result.success && result.token && result.profile) {
          this.setAuthData(storeSlug, result.token, result.profile);
        }
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Login a shopper for a store
   */
  login(storeSlug: string, request: ShopperLoginRequest): Observable<AuthResultDto> {
    return this.http.post<ApiResponse<AuthResultDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/auth/login`,
      request
    ).pipe(
      map(response => response.data!),
      tap(result => {
        if (result.success && result.token && result.profile) {
          this.setAuthData(storeSlug, result.token, result.profile);
        }
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Create a guest session for checkout
   */
  createGuestSession(storeSlug: string, request: GuestSessionRequest): Observable<GuestSessionDto> {
    return this.http.post<ApiResponse<GuestSessionDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/auth/guest`,
      request
    ).pipe(
      map(response => response.data!),
      catchError(this.handleError)
    );
  }

  /**
   * Get shopper profile
   */
  getProfile(storeSlug: string): Observable<ShopperProfileDto> {
    return this.http.get<ApiResponse<ShopperProfileDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/account`
    ).pipe(
      map(response => response.data!),
      tap(profile => this.currentProfileSubject.next(profile)),
      catchError(this.handleError)
    );
  }

  /**
   * Update shopper profile
   */
  updateProfile(storeSlug: string, request: UpdateShopperProfileRequest): Observable<ShopperProfileDto> {
    return this.http.put<ApiResponse<ShopperProfileDto>>(
      `${this.API_URL}/api/shop/${storeSlug}/account`,
      request
    ).pipe(
      map(response => response.data!),
      tap(profile => {
        this.currentProfileSubject.next(profile);
        this.setProfile(storeSlug, profile);
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Change password
   */
  changePassword(storeSlug: string, request: ChangePasswordRequest): Observable<any> {
    return this.http.post<ApiResponse<any>>(
      `${this.API_URL}/api/shop/${storeSlug}/account/change-password`,
      request
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * Logout from current store
   */
  logout(storeSlug: string): void {
    localStorage.removeItem(this.TOKEN_KEY + storeSlug);
    localStorage.removeItem(this.PROFILE_KEY + storeSlug);
    this.currentProfileSubject.next(null);
    this.authStatusSubject.next(false);
  }

  /**
   * Check if shopper is authenticated for a specific store
   */
  isAuthenticated(storeSlug: string): boolean {
    const token = this.getToken(storeSlug);
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const now = Math.floor(Date.now() / 1000);
      return payload.exp > now && payload.StoreSlug === storeSlug;
    } catch {
      return false;
    }
  }

  /**
   * Get stored token for a store
   */
  getToken(storeSlug: string): string | null {
    return localStorage.getItem(this.TOKEN_KEY + storeSlug);
  }

  /**
   * Get stored profile for a store
   */
  getStoredProfile(storeSlug: string): ShopperProfileDto | null {
    const profileJson = localStorage.getItem(this.PROFILE_KEY + storeSlug);
    return profileJson ? JSON.parse(profileJson) : null;
  }

  /**
   * Get current profile
   */
  getCurrentProfile(): ShopperProfileDto | null {
    return this.currentProfileSubject.value;
  }

  /**
   * Initialize authentication status on service creation
   */
  private initializeAuthStatus(): void {
    // Check if any store authentication exists
    const hasAnyAuth = Object.keys(localStorage).some(key =>
      key.startsWith(this.TOKEN_KEY) && this.isTokenValid(localStorage.getItem(key))
    );
    this.authStatusSubject.next(hasAnyAuth);
  }

  /**
   * Check if a token is valid
   */
  private isTokenValid(token: string | null): boolean {
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const now = Math.floor(Date.now() / 1000);
      return payload.exp > now;
    } catch {
      return false;
    }
  }

  /**
   * Set authentication data in local storage
   */
  private setAuthData(storeSlug: string, token: string, profile: ShopperProfileDto): void {
    localStorage.setItem(this.TOKEN_KEY + storeSlug, token);
    localStorage.setItem(this.PROFILE_KEY + storeSlug, JSON.stringify(profile));
    this.currentProfileSubject.next(profile);
    this.authStatusSubject.next(true);
  }

  /**
   * Set profile data in local storage
   */
  private setProfile(storeSlug: string, profile: ShopperProfileDto): void {
    localStorage.setItem(this.PROFILE_KEY + storeSlug, JSON.stringify(profile));
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unexpected error occurred';

    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.error?.errors?.length > 0) {
      errorMessage = error.error.errors[0];
    } else if (error.message) {
      errorMessage = error.message;
    }

    console.error('ShopperAuthService error:', error);
    return throwError(() => new Error(errorMessage));
  }
}