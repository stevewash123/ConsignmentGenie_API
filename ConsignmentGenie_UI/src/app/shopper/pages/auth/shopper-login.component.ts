import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { ShopperAuthService, ShopperLoginRequest } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

@Component({
  selector: 'app-shopper-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <div class="store-branding" *ngIf="storeInfo">
            <img *ngIf="storeInfo.logoUrl" [src]="storeInfo.logoUrl" [alt]="storeInfo.name" class="store-logo">
            <h1 class="store-name">{{ storeInfo.name }}</h1>
          </div>
          <h2 class="login-title">Welcome Back</h2>
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
          <div class="form-group">
            <label for="email">Email</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('email')"
              placeholder="Enter your email"
              autocomplete="email">
            <div class="invalid-feedback" *ngIf="isFieldInvalid('email')">
              <span *ngIf="loginForm.get('email')?.errors?.['required']">Email is required</span>
              <span *ngIf="loginForm.get('email')?.errors?.['email']">Please enter a valid email</span>
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <div class="password-input">
              <input
                id="password"
                [type]="showPassword ? 'text' : 'password'"
                formControlName="password"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('password')"
                placeholder="Enter your password"
                autocomplete="current-password">
              <button
                type="button"
                class="password-toggle"
                (click)="togglePasswordVisibility()"
                [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'">
                {{ showPassword ? '👁️' : '👁️‍🗨️' }}
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="isFieldInvalid('password')">
              <span *ngIf="loginForm.get('password')?.errors?.['required']">Password is required</span>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group-checkbox">
              <input
                id="rememberMe"
                type="checkbox"
                formControlName="rememberMe"
                class="form-check-input">
              <label for="rememberMe" class="form-check-label">Remember me</label>
            </div>

            <a href="#" class="forgot-password" (click)="onForgotPassword($event)">
              Forgot password?
            </a>
          </div>

          <div class="error-message" *ngIf="errorMessage">
            <div class="alert alert-danger">
              {{ errorMessage }}
            </div>
          </div>

          <button
            type="submit"
            class="btn btn-primary btn-block"
            [disabled]="loginForm.invalid || isLoading">
            <span *ngIf="isLoading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            {{ isLoading ? 'Signing In...' : 'Sign In' }}
          </button>
        </form>

        <div class="login-footer">
          <div class="divider">
            <span>or</span>
          </div>

          <p class="signup-prompt">
            Don't have an account?
            <a [routerLink]="['/shop', storeSlug, 'register']" class="signup-link">Create Account</a>
          </p>

          <div class="guest-option">
            <p class="guest-text">
              <a [routerLink]="['/shop', storeSlug, 'checkout']" class="guest-link">
                Continue as Guest →
              </a>
            </p>
            <small class="guest-note">(You can checkout without an account)</small>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 80vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
      background-color: #f8f9fa;
    }

    .login-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      width: 100%;
      max-width: 400px;
      padding: 2rem;
    }

    .login-header {
      text-align: center;
      margin-bottom: 2rem;
    }

    .store-branding {
      margin-bottom: 1rem;
    }

    .store-logo {
      height: 60px;
      width: auto;
      margin-bottom: 0.5rem;
    }

    .store-name {
      font-size: 1.5rem;
      font-weight: bold;
      color: #343a40;
      margin: 0;
    }

    .login-title {
      font-size: 1.25rem;
      color: #6c757d;
      margin: 0;
      font-weight: normal;
    }

    .login-form {
      margin-bottom: 2rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 500;
      color: #343a40;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid #ced4da;
      border-radius: 0.375rem;
      font-size: 1rem;
      transition: border-color 0.2s, box-shadow 0.2s;
    }

    .form-control:focus {
      outline: none;
      border-color: #007bff;
      box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
    }

    .form-control.is-invalid {
      border-color: #dc3545;
    }

    .password-input {
      position: relative;
    }

    .password-toggle {
      position: absolute;
      right: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.25rem;
      color: #6c757d;
    }

    .invalid-feedback {
      display: block;
      color: #dc3545;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .form-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }

    .form-group-checkbox {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .form-check-input {
      margin: 0;
    }

    .form-check-label {
      font-size: 0.875rem;
      color: #6c757d;
      margin: 0;
    }

    .forgot-password {
      color: #007bff;
      text-decoration: none;
      font-size: 0.875rem;
    }

    .forgot-password:hover {
      text-decoration: underline;
    }

    .error-message {
      margin-bottom: 1.5rem;
    }

    .alert {
      padding: 0.75rem 1rem;
      border-radius: 0.375rem;
      margin: 0;
    }

    .alert-danger {
      background-color: #f8d7da;
      border-color: #f5c6cb;
      color: #721c24;
    }

    .btn {
      display: inline-block;
      font-weight: 400;
      text-align: center;
      text-decoration: none;
      vertical-align: middle;
      cursor: pointer;
      border: 1px solid transparent;
      padding: 0.75rem 1rem;
      font-size: 1rem;
      border-radius: 0.375rem;
      transition: all 0.2s;
    }

    .btn-primary {
      background-color: #007bff;
      border-color: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
      border-color: #004085;
    }

    .btn-primary:disabled {
      background-color: #6c757d;
      border-color: #6c757d;
      cursor: not-allowed;
    }

    .btn-block {
      width: 100%;
    }

    .spinner-border {
      display: inline-block;
      width: 1rem;
      height: 1rem;
      vertical-align: text-bottom;
      border: 0.125em solid currentColor;
      border-right-color: transparent;
      border-radius: 50%;
      animation: spinner-border 0.75s linear infinite;
    }

    .spinner-border-sm {
      width: 0.875rem;
      height: 0.875rem;
      border-width: 0.1em;
    }

    @keyframes spinner-border {
      to {
        transform: rotate(360deg);
      }
    }

    .login-footer {
      text-align: center;
    }

    .divider {
      position: relative;
      margin: 2rem 0 1.5rem;
      text-align: center;
    }

    .divider::before {
      content: '';
      position: absolute;
      top: 50%;
      left: 0;
      right: 0;
      height: 1px;
      background-color: #dee2e6;
    }

    .divider span {
      background-color: white;
      color: #6c757d;
      padding: 0 1rem;
      font-size: 0.875rem;
    }

    .signup-prompt {
      margin-bottom: 1.5rem;
      color: #6c757d;
    }

    .signup-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .signup-link:hover {
      text-decoration: underline;
    }

    .guest-option {
      border-top: 1px solid #dee2e6;
      padding-top: 1.5rem;
    }

    .guest-text {
      margin-bottom: 0.5rem;
    }

    .guest-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .guest-link:hover {
      text-decoration: underline;
    }

    .guest-note {
      color: #6c757d;
      font-size: 0.75rem;
    }

    @media (max-width: 480px) {
      .login-container {
        padding: 1rem;
      }

      .login-card {
        padding: 1.5rem;
      }

      .form-row {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }
    }
  `]
})
export class ShopperLoginComponent implements OnInit, OnDestroy {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  storeSlug = '';
  storeInfo: StoreInfoDto | null = null;
  returnUrl = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private shopperAuthService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });
  }

  ngOnInit(): void {
    // Get store slug and return URL from route
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';

      // Check if already authenticated for this store
      if (this.shopperAuthService.isAuthenticated(this.storeSlug)) {
        this.redirectAfterLogin();
        return;
      }
    });

    this.route.queryParams.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.returnUrl = params['returnUrl'] || `/shop/${this.storeSlug}`;
    });

    // Get store info for branding
    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const loginRequest: ShopperLoginRequest = {
      email: this.loginForm.value.email,
      password: this.loginForm.value.password,
      rememberMe: this.loginForm.value.rememberMe
    };

    this.shopperAuthService.login(this.storeSlug, loginRequest).pipe(
      takeUntil(this.destroy$),
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (result) => {
        if (result.success) {
          this.redirectAfterLogin();
        } else {
          this.errorMessage = result.errorMessage || 'Login failed. Please try again.';
        }
      },
      error: (error) => {
        this.errorMessage = error.message || 'An error occurred during login. Please try again.';
      }
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    // TODO: Implement forgot password flow in future
    alert('Forgot password functionality will be implemented in a future update.');
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markAllFieldsAsTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
  }

  private redirectAfterLogin(): void {
    this.router.navigateByUrl(this.returnUrl);
  }
}