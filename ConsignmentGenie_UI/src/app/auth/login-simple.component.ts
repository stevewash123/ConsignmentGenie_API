import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login-simple',
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
            {{ errorMessage() }}
          </div>
        }

        <form (ngSubmit)="onSubmit()" #loginForm="ngForm" class="login-form">
          <div class="form-group">
            <label for="email">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              [(ngModel)]="email"
              required
              [disabled]="isLoading()"
              placeholder="Enter your email"
            >
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input
              type="password"
              id="password"
              name="password"
              [(ngModel)]="password"
              required
              [disabled]="isLoading()"
              placeholder="Enter your password"
            >
          </div>

          <button
            type="submit"
            class="login-btn"
            [disabled]="loginForm.invalid || isLoading()"
          >
            {{ isLoading() ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>

        <!-- Quick Login Buttons -->
        <div class="quick-login">
          <h3>Quick Login</h3>
          <div class="login-buttons">
            <button
              class="quick-btn admin"
              (click)="quickLogin('admin@microsaasbuilders.com')"
              [disabled]="isLoading()"
            >
              <span class="role">Admin</span>
              <span class="email">admin@microsaasbuilders.com</span>
            </button>

            <button
              class="quick-btn owner"
              (click)="quickLogin('owner1@microsaasbuilders.com')"
              [disabled]="isLoading()"
            >
              <span class="role">Shop Owner</span>
              <span class="email">owner1@microsaasbuilders.com</span>
            </button>

            <button
              class="quick-btn provider"
              (click)="quickLogin('provider1@microsaasbuilders.com')"
              [disabled]="isLoading()"
            >
              <span class="role">Provider</span>
              <span class="email">provider1@microsaasbuilders.com</span>
            </button>
          </div>
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
      padding: 1rem;
    }

    .login-card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
      padding: 2rem;
      max-width: 400px;
      width: 100%;
    }

    .login-header {
      text-align: center;
      margin-bottom: 2rem;
    }

    .login-header h1 {
      font-size: 2rem;
      font-weight: bold;
      color: #1f2937;
      margin: 0 0 0.5rem 0;
    }

    .login-header p {
      color: #6b7280;
      margin: 0;
    }

    .error-message {
      background: #fef2f2;
      border: 1px solid #fca5a5;
      color: #dc2626;
      padding: 0.75rem;
      border-radius: 6px;
      margin-bottom: 1rem;
      font-size: 0.875rem;
    }

    .login-form {
      margin-bottom: 2rem;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      font-weight: 500;
      color: #374151;
      margin-bottom: 0.25rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 1rem;
      box-sizing: border-box;
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

    .login-btn {
      width: 100%;
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.75rem;
      border-radius: 6px;
      font-size: 1rem;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .login-btn:hover:not(:disabled) {
      background: #2563eb;
    }

    .login-btn:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .quick-login {
      border-top: 1px solid #e5e7eb;
      padding-top: 1.5rem;
    }

    .quick-login h3 {
      text-align: center;
      color: #6b7280;
      font-size: 0.875rem;
      font-weight: 500;
      margin: 0 0 1rem 0;
      text-transform: uppercase;
      letter-spacing: 0.025em;
    }

    .login-buttons {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .quick-btn {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      padding: 0.75rem;
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      background: white;
      cursor: pointer;
      transition: all 0.2s;
      text-align: left;
    }

    .quick-btn:hover:not(:disabled) {
      border-color: #3b82f6;
      box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
    }

    .quick-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .quick-btn.admin { border-left: 3px solid #dc2626; }
    .quick-btn.owner { border-left: 3px solid #059669; }
    .quick-btn.provider { border-left: 3px solid #d97706; }

    .role {
      font-weight: 500;
      font-size: 0.875rem;
      color: #374151;
    }

    .email {
      font-size: 0.75rem;
      color: #6b7280;
      font-family: 'Monaco', 'Consolas', monospace;
    }

    @media (max-width: 480px) {
      .login-container {
        padding: 0.5rem;
      }

      .login-card {
        padding: 1.5rem;
      }

      .login-header h1 {
        font-size: 1.75rem;
      }
    }
  `]
})
export class LoginSimpleComponent {
  email = '';
  password = '';
  isLoading = signal(false);
  errorMessage = signal('');

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  quickLogin(email: string) {
    this.email = email;
    this.password = 'password123';
    this.errorMessage.set('');
  }

  onSubmit() {
    if (!this.email || !this.password) {
      this.errorMessage.set('Please enter both email and password');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const loginRequest = { email: this.email, password: this.password };

    this.authService.login(loginRequest).subscribe({
      next: (response) => {
        console.log('Login successful:', response);
        // Handle different API response formats
        const userData = this.extractUserData(response);
        this.redirectBasedOnUser(userData);
      },
      error: (error) => {
        console.error('Login error:', error);

        if (error.status === 401) {
          this.errorMessage.set('Invalid email or password');
        } else if (error.status === 0) {
          this.errorMessage.set('Cannot connect to server');
        } else {
          this.errorMessage.set('Login failed. Please try again.');
        }
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  private extractUserData(response: any): any {
    // Type guard for wrapped response with nested user
    if (response.data?.user) {
      return response.data.user;
    }
    // Type guard for wrapped response with direct user data
    if (response.data && !response.user) {
      return response.data;
    }
    // Direct user response
    if (response.user) {
      return response.user;
    }
    // Fallback to response itself
    return response;
  }

  private redirectBasedOnUser(userData: any) {
    const email = userData?.email || '';
    const role = this.normalizeRole(userData?.role);

    console.log('Redirecting user:', { email, role, rawRole: userData?.role });

    // Admin always goes to admin dashboard
    if (email === 'admin@microsaasbuilders.com' || role === 'Admin') {
      this.router.navigate(['/admin/dashboard']);
      return;
    }

    // Role-based routing
    switch (role) {
      case 'Admin':
        this.router.navigate(['/admin/dashboard']);
        break;
      case 'Owner':
        this.router.navigate(['/owner/dashboard']);
        break;
      // case 'Manager':
      //   // Managers get owner dashboard access (planned feature)
      //   this.router.navigate(['/owner/dashboard']);
      //   break;
      case 'Provider':
        this.router.navigate(['/customer/dashboard']); // For now, redirect to customer area
        break;
      case 'Customer':
        this.router.navigate(['/customer/dashboard']);
        break;
      default:
        // Default to owner dashboard
        console.warn('Unknown role:', role);
        this.router.navigate(['/owner/dashboard']);
    }
  }

  private normalizeRole(role: string | number): string {
    // Validate input - null and undefined should throw
    if (role === null) {
      throw new Error('Role cannot be null');
    }
    if (role === undefined) {
      throw new Error('Role cannot be undefined');
    }

    // Handle numeric role values (enum numbers from API)
    if (typeof role === 'number' || !isNaN(Number(role))) {
      const roleMap: { [key: number]: string } = {
        0: 'Admin',
        1: 'Owner',
        // 2: 'Manager', // Planned feature - commented out for now
        2: 'Provider',
        3: 'Customer'
      };
      return roleMap[Number(role)] || 'Owner';
    }

    // Return string role as-is
    return String(role);
  }
}