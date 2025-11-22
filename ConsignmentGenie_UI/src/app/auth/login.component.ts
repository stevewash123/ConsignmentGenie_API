import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { environment } from '../../environments/environment';

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginData {
  token: string;
  userId: string;
  email: string;
  role: number;
  organizationId: string;
  organizationName: string;
  expiresAt: string;
  approvalStatus?: number;
}

interface LoginResponse {
  success: boolean;
  data: LoginData;
  message: string;
  errors: string[] | null;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <h1>ConsignmentGenie</h1>
          <p>Sign in to your account</p>
        </div>

        @if (errorMessage()) {
          <div class="error-message">
            <span class="error-icon">⚠️</span>
            {{ errorMessage() }}
          </div>
        }

        <form (ngSubmit)="onSubmit()" #loginForm="ngForm" class="login-form">
          <div class="form-group">
            <label for="email">Email Address</label>
            <input
              type="email"
              id="email"
              name="email"
              [(ngModel)]="credentials.email"
              required
              email
              placeholder="Enter your email"
              [disabled]="isLoading()"
              #emailInput="ngModel"
            >
            @if (emailInput.invalid && emailInput.touched) {
              <div class="field-error">
                @if (emailInput.errors?.['required']) {
                  Email is required
                }
                @if (emailInput.errors?.['email']) {
                  Please enter a valid email address
                }
              </div>
            }
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <div class="password-input">
              <input
                [type]="showPassword() ? 'text' : 'password'"
                id="password"
                name="password"
                [(ngModel)]="credentials.password"
                required
                placeholder="Enter your password"
                [disabled]="isLoading()"
                #passwordInput="ngModel"
              >
              <button
                type="button"
                class="password-toggle"
                (click)="togglePassword()"
                [disabled]="isLoading()"
              >
                {{ showPassword() ? '🙈' : '👁️' }}
              </button>
            </div>
            @if (passwordInput.invalid && passwordInput.touched) {
              <div class="field-error">
                Password is required
              </div>
            }
          </div>

          <button
            type="submit"
            class="login-btn"
            [disabled]="loginForm.invalid || isLoading()"
          >
            @if (isLoading()) {
              <span class="spinner"></span>
              Signing in...
            } @else {
              Sign In
            }
          </button>
        </form>

        <div class="test-accounts">
          <h3>Test Accounts</h3>
          <div class="test-account-grid">
            <button
              class="test-account-btn admin"
              (click)="useTestAccount('admin@demoshop.com')"
              [disabled]="isLoading()"
            >
              <div class="account-role">System Admin</div>
              <div class="account-email">admin@demoshop.com</div>
            </button>
            <button
              class="test-account-btn owner"
              (click)="useTestAccount('owner@demoshop.com')"
              [disabled]="isLoading()"
            >
              <div class="account-role">Shop Owner</div>
              <div class="account-email">owner@demoshop.com</div>
            </button>
            <button
              class="test-account-btn provider"
              (click)="useTestAccount('provider@demoshop.com')"
              [disabled]="isLoading()"
            >
              <div class="account-role">Provider</div>
              <div class="account-email">provider@demoshop.com</div>
            </button>
            <button
              class="test-account-btn customer"
              (click)="useTestAccount('customer@demoshop.com')"
              [disabled]="isLoading()"
            >
              <div class="account-role">Customer</div>
              <div class="account-email">customer@demoshop.com</div>
            </button>
          </div>
        </div>

        <div class="register-link">
          <p>Don't have an account? <a routerLink="/register">Sign up here</a></p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 2rem;
    }

    .login-card {
      background: white;
      border-radius: 16px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      padding: 3rem;
      max-width: 480px;
      width: 100%;
    }

    .login-header {
      text-align: center;
      margin-bottom: 2.5rem;
    }

    .login-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #1f2937;
      margin-bottom: 0.5rem;
    }

    .login-header p {
      color: #6b7280;
      font-size: 1.1rem;
    }

    .error-message {
      background: #fef2f2;
      border: 1px solid #fecaca;
      color: #dc2626;
      padding: 1rem;
      border-radius: 8px;
      margin-bottom: 1.5rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .error-icon {
      font-size: 1.2rem;
    }

    .login-form {
      margin-bottom: 2rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-group input {
      width: 100%;
      padding: 1rem;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s, box-shadow 0.2s;
    }

    .form-group input:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .form-group input:disabled {
      background: #f9fafb;
      color: #6b7280;
    }

    .password-input {
      position: relative;
      display: flex;
      align-items: center;
    }

    .password-toggle {
      position: absolute;
      right: 1rem;
      background: none;
      border: none;
      cursor: pointer;
      font-size: 1.2rem;
      padding: 0.25rem;
    }

    .field-error {
      color: #dc2626;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .login-btn {
      width: 100%;
      background: #3b82f6;
      color: white;
      border: none;
      padding: 1rem 1.5rem;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
    }

    .login-btn:hover:not(:disabled) {
      background: #2563eb;
    }

    .login-btn:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid transparent;
      border-top: 2px solid white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .test-accounts {
      border-top: 1px solid #e5e7eb;
      padding-top: 2rem;
    }

    .test-accounts h3 {
      text-align: center;
      color: #6b7280;
      font-size: 1rem;
      font-weight: 600;
      margin-bottom: 1rem;
    }

    .test-account-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;
    }

    .test-account-btn {
      padding: 1rem;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      background: white;
      cursor: pointer;
      transition: all 0.2s;
      text-align: left;
    }

    .test-account-btn:hover:not(:disabled) {
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .test-account-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .test-account-btn.admin { border-left: 4px solid #dc2626; }
    .test-account-btn.owner { border-left: 4px solid #059669; }
    .test-account-btn.provider { border-left: 4px solid #d97706; }
    .test-account-btn.customer { border-left: 4px solid #7c3aed; }

    .account-role {
      font-weight: 600;
      font-size: 0.875rem;
      color: #374151;
      margin-bottom: 0.25rem;
    }

    .account-email {
      font-size: 0.75rem;
      color: #6b7280;
      font-family: monospace;
    }

    .register-link {
      text-align: center;
      margin-top: 1.5rem;
      padding-top: 1rem;
      border-top: 1px solid #e5e7eb;
    }

    .register-link p {
      color: #6b7280;
      margin: 0;
    }

    .register-link a {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 600;
    }

    .register-link a:hover {
      text-decoration: underline;
    }

    @media (max-width: 640px) {
      .login-container {
        padding: 1rem;
      }

      .login-card {
        padding: 2rem;
      }

      .test-account-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class LoginComponent {
  credentials = {
    email: '',
    password: ''
  };

  isLoading = signal(false);
  showPassword = signal(false);
  errorMessage = signal('');

  constructor(
    private router: Router,
    private http: HttpClient,
    private authService: AuthService
  ) {}

  togglePassword() {
    this.showPassword.update(show => !show);
  }

  useTestAccount(email: string) {
    this.credentials.email = email;
    this.credentials.password = 'password123';
    this.errorMessage.set('');
  }

  async onSubmit() {
    if (!this.credentials.email || !this.credentials.password) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    try {
      const loginRequest: LoginRequest = {
        email: this.credentials.email,
        password: this.credentials.password
      };

      const response = await this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, loginRequest).toPromise();

      if (response && response.data) {
        console.log('Login response:', response);

        // Extract actual login data from API wrapper
        const loginData = response.data;

        // Check approval status for owners/managers before proceeding
        if (this.normalizeRole(loginData.role) === 'Owner' && loginData.approvalStatus !== undefined) {
          const approvalStatus = loginData.approvalStatus;

          // 0 = Pending, 1 = Approved, 2 = Rejected
          if (approvalStatus === 0) {
            this.errorMessage.set('Your account is pending admin approval. You will receive an email once approved.');
            return;
          } else if (approvalStatus === 2) {
            this.errorMessage.set('Your account has been rejected. Please contact support for more information.');
            return;
          }
        }

        // Clear any old auth data first
        localStorage.removeItem('auth_token');
        localStorage.removeItem('user_data');
        localStorage.removeItem('token');
        localStorage.removeItem('user');

        // Store authentication data using the correct keys the AuthService expects
        const expiryTime = loginData.expiresAt || new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(); // Default to 24 hours if not provided
        localStorage.setItem('auth_token', loginData.token);
        localStorage.setItem('tokenExpiry', expiryTime);
        localStorage.setItem('user_data', JSON.stringify({
          userId: loginData.userId,
          email: loginData.email,
          role: loginData.role,
          organizationId: loginData.organizationId,
          organizationName: loginData.organizationName,
          businessName: loginData.organizationName
        }));

        // Update AuthService state
        this.authService.loadStoredAuth();

        console.log('Stored auth data, calling redirect...');

        // Role-based redirection
        this.redirectBasedOnRole(loginData.role.toString(), loginData.email);
      }
    } catch (error: any) {
      console.error('Login error:', error);

      if (error.status === 401) {
        this.errorMessage.set('Invalid email or password. Please try again.');
      } else if (error.status === 0) {
        this.errorMessage.set('Unable to connect to server. Please check your connection.');
      } else {
        this.errorMessage.set('Login failed. Please try again later.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  private redirectBasedOnRole(role: string, email: string) {
    // Convert numeric role to string if needed
    const roleStr = this.normalizeRole(role);

    console.log('Redirect role:', roleStr, 'for email:', email);

    // Special case: System admin (you) gets admin dashboard
    if (email === 'admin@demoshop.com') {
      this.router.navigate(['/admin/dashboard']);
      return;
    }

    // Role-based routing for regular users
    switch (roleStr) {
      case 'Owner':
      case 'Manager':
      case 'Staff':
      case 'Cashier':
      case 'Accountant':
        this.router.navigate(['/owner/dashboard']);
        break;

      case 'Provider':
        // For now, redirect to customer area - could be separate provider portal later
        this.router.navigate(['/customer/dashboard']);
        break;

      case 'Customer':
        this.router.navigate(['/customer/dashboard']);
        break;

      default:
        console.warn('Unknown role:', roleStr);
        this.router.navigate(['/owner/dashboard']);
    }
  }

  private normalizeRole(role: string | number): string {
    // Handle numeric role values (enum numbers from API)
    if (typeof role === 'number' || !isNaN(Number(role))) {
      const roleMap: { [key: number]: string } = {
        1: 'Owner',
        2: 'Manager',
        3: 'Staff',
        4: 'Cashier',
        5: 'Accountant',
        6: 'Provider',
        7: 'Customer'
      };
      return roleMap[Number(role)] || 'Owner';
    }

    // Return string role as-is
    return String(role);
  }
}