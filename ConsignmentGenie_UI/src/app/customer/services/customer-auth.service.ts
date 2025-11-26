import { Injectable, inject, signal } from '@angular/core';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ApiService } from '../../shared/services/api.service';
import {
  CustomerRegistrationRequest,
  CustomerLoginRequest,
  CustomerLoginResponse,
  Customer,
  CustomerProfile
} from '../../shared/models/customer.models';

@Injectable({
  providedIn: 'root'
})
export class CustomerAuthService {
  private readonly apiService = inject(ApiService);
  private readonly router = inject(Router);

  // Authentication state
  private readonly currentCustomerSubject = new BehaviorSubject<Customer | null>(null);
  public currentCustomer$ = this.currentCustomerSubject.asObservable();

  // Signals for reactive UI
  public isAuthenticated = signal(false);
  public currentCustomer = signal<Customer | null>(null);

  private readonly TOKEN_KEY = 'cg_customer_token';
  private readonly CUSTOMER_KEY = 'cg_customer_data';

  constructor() {
    this.loadAuthenticationState();
  }

  register(request: CustomerRegistrationRequest): Observable<any> {
    return this.apiService.post('/api/customer/register', request)
      .pipe(
        map(response => {
          if (response.success) {
            return response.data;
          }
          throw new Error(response.message || 'Registration failed');
        }),
        catchError(error => {
          console.error('Registration error:', error);
          return throwError(() => error);
        })
      );
  }

  login(request: CustomerLoginRequest): Observable<CustomerLoginResponse> {
    return this.apiService.post<CustomerLoginResponse>('/api/customer/login', request)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.setAuthenticationData(response.data);
          }
        }),
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Login failed');
        }),
        catchError(error => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  logout(): void {
    this.clearAuthenticationData();
    this.router.navigate(['/']);
  }

  verifyEmail(token: string): Observable<any> {
    return this.apiService.post('/api/customer/verify-email', { token })
      .pipe(
        map(response => {
          if (response.success) {
            return response.data;
          }
          throw new Error(response.message || 'Email verification failed');
        }),
        catchError(error => {
          console.error('Email verification error:', error);
          return throwError(() => error);
        })
      );
  }

  forgotPassword(email: string, orgSlug: string): Observable<any> {
    return this.apiService.post('/api/customer/forgot-password', { email, orgSlug })
      .pipe(
        map(response => {
          if (response.success) {
            return response.data;
          }
          throw new Error(response.message || 'Password reset request failed');
        }),
        catchError(error => {
          console.error('Forgot password error:', error);
          return throwError(() => error);
        })
      );
  }

  resetPassword(token: string, newPassword: string): Observable<any> {
    return this.apiService.post('/api/customer/reset-password', { token, newPassword })
      .pipe(
        map(response => {
          if (response.success) {
            return response.data;
          }
          throw new Error(response.message || 'Password reset failed');
        }),
        catchError(error => {
          console.error('Reset password error:', error);
          return throwError(() => error);
        })
      );
  }

  getProfile(): Observable<CustomerProfile> {
    return this.apiService.get<CustomerProfile>('/api/customer/profile')
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Failed to get profile');
        }),
        catchError(error => {
          console.error('Get profile error:', error);
          return throwError(() => error);
        })
      );
  }

  updateProfile(profile: Partial<CustomerProfile>): Observable<CustomerProfile> {
    return this.apiService.put<CustomerProfile>('/api/customer/profile', profile)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            // Update local customer data
            const currentData = this.getCurrentCustomerData();
            if (currentData) {
              const updatedCustomer = { ...currentData.customer, ...response.data };
              const updatedData = { ...currentData, customer: updatedCustomer };
              this.setAuthenticationData(updatedData);
            }
          }
        }),
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Failed to update profile');
        }),
        catchError(error => {
          console.error('Update profile error:', error);
          return throwError(() => error);
        })
      );
  }

  // Helper methods
  private setAuthenticationData(data: CustomerLoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, data.token);
    localStorage.setItem(this.CUSTOMER_KEY, JSON.stringify(data));

    this.currentCustomerSubject.next(data.customer);
    this.currentCustomer.set(data.customer);
    this.isAuthenticated.set(true);
  }

  private clearAuthenticationData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.CUSTOMER_KEY);

    this.currentCustomerSubject.next(null);
    this.currentCustomer.set(null);
    this.isAuthenticated.set(false);
  }

  private loadAuthenticationState(): void {
    try {
      const token = localStorage.getItem(this.TOKEN_KEY);
      const customerData = localStorage.getItem(this.CUSTOMER_KEY);

      if (token && customerData) {
        const data: CustomerLoginResponse = JSON.parse(customerData);

        // Check if token is expired
        const expiresAt = new Date(data.expiresAt);
        if (expiresAt > new Date()) {
          this.currentCustomerSubject.next(data.customer);
          this.currentCustomer.set(data.customer);
          this.isAuthenticated.set(true);
        } else {
          this.clearAuthenticationData();
        }
      }
    } catch (error) {
      console.error('Error loading authentication state:', error);
      this.clearAuthenticationData();
    }
  }

  getCurrentToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getCurrentCustomerData(): CustomerLoginResponse | null {
    try {
      const customerData = localStorage.getItem(this.CUSTOMER_KEY);
      return customerData ? JSON.parse(customerData) : null;
    } catch {
      return null;
    }
  }

  isTokenValid(): boolean {
    try {
      const customerData = this.getCurrentCustomerData();
      if (!customerData) return false;

      const expiresAt = new Date(customerData.expiresAt);
      return expiresAt > new Date();
    } catch {
      return false;
    }
  }
}