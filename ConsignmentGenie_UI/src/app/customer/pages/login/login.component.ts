import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { CustomerAuthService } from '../../services/customer-auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-page">
      <div class="login-container">
        <div class="login-card">
          <div class="login-header">
            <h1>Welcome Back</h1>
            <p>Sign in to your account</p>
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
            <!-- Email Field -->
            <div class="form-group">
              <label for="email">Email Address</label>
              <input
                id="email"
                type="email"
                formControlName="email"
                class="form-control"
                [class.error]="loginForm.get('email')?.invalid && loginForm.get('email')?.touched"
                placeholder="Enter your email"
              >
              @if (loginForm.get('email')?.invalid && loginForm.get('email')?.touched) {
                <div class="error-message">
                  @if (loginForm.get('email')?.errors?.['required']) {
                    <span>Email is required</span>
                  }
                  @if (loginForm.get('email')?.errors?.['email']) {
                    <span>Please enter a valid email address</span>
                  }
                </div>
              }
            </div>

            <!-- Password Field -->
            <div class="form-group">
              <label for="password">Password</label>
              <div class="password-input">
                <input
                  id="password"
                  [type]="showPassword() ? 'text' : 'password'"
                  formControlName="password"
                  class="form-control"
                  [class.error]="loginForm.get('password')?.invalid && loginForm.get('password')?.touched"
                  placeholder="Enter your password"
                >
                <button
                  type="button"
                  (click)="togglePassword()"
                  class="password-toggle"
                >
                  {{ showPassword() ? 'Hide' : 'Show' }}
                </button>
              </div>
              @if (loginForm.get('password')?.invalid && loginForm.get('password')?.touched) {
                <div class="error-message">
                  <span>Password is required</span>
                </div>
              }
            </div>

            <!-- Store Slug Field -->
            <div class="form-group">
              <label for="orgSlug">Store</label>
              <input
                id="orgSlug"
                type="text"
                formControlName="orgSlug"
                class="form-control"
                [class.error]="loginForm.get('orgSlug')?.invalid && loginForm.get('orgSlug')?.touched"
                placeholder="Enter store name"
              >
              @if (loginForm.get('orgSlug')?.invalid && loginForm.get('orgSlug')?.touched) {
                <div class="error-message">
                  <span>Store name is required</span>
                </div>
              }
            </div>

            <!-- Remember Me -->
            <div class="form-group">
              <label class="checkbox-label">
                <input type="checkbox" formControlName="rememberMe" class="checkbox">
                <span class="checkmark"></span>
                Remember me
              </label>
            </div>

            <!-- Error Message -->
            @if (errorMessage()) {
              <div class="alert alert-error">
                {{ errorMessage() }}
              </div>
            }

            <!-- Submit Button -->
            <button
              type="submit"
              [disabled]="loginForm.invalid || loading()"
              class="submit-button"
            >
              @if (loading()) {
                <span class="loading-spinner"></span>
                <span>Signing in...</span>
              } @else {
                <span>Sign In</span>
              }
            </button>
          </form>

          <div class="login-footer">
            <div class="forgot-password">
              <a [routerLink]="['/customer/forgot-password']">Forgot your password?</a>
            </div>

            <div class="signup-link">
              <span>Don't have an account?</span>
              <a [routerLink]="['/customer/register']">Sign up</a>
            </div>
          </div>
        </div>

        <!-- Store Branding -->
        <div class="branding-section">
          <h2>Shop with Confidence</h2>
          <div class="features">
            <div class="feature">
              <div class="feature-icon">🛒</div>
              <div class="feature-text">
                <h3>Easy Shopping</h3>
                <p>Browse and purchase unique items</p>
              </div>
            </div>
            <div class="feature">
              <div class="feature-icon">📦</div>
              <div class="feature-text">
                <h3>Order Tracking</h3>
                <p>Track your orders from purchase to delivery</p>
              </div>
            </div>
            <div class="feature">
              <div class="feature-icon">❤️</div>
              <div class="feature-text">
                <h3>Wishlist</h3>
                <p>Save items for later purchase</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(CustomerAuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Signals
  protected loading = signal(false);
  protected errorMessage = signal('');
  protected showPassword = signal(false);

  // Form
  protected loginForm: FormGroup;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      orgSlug: ['', Validators.required],
      rememberMe: [false]
    });

    // Pre-populate store slug if available from URL
    this.route.queryParams.subscribe(params => {
      if (params['store']) {
        this.loginForm.patchValue({ orgSlug: params['store'] });
      }
    });
  }

  protected togglePassword(): void {
    this.showPassword.set(!this.showPassword());
  }

  protected onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    const loginData = this.loginForm.value;

    this.authService.login(loginData).subscribe({
      next: (response) => {
        this.loading.set(false);

        // Redirect to intended page or customer dashboard
        const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/customer/dashboard';
        this.router.navigate([returnUrl]);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(
          error.message || 'Invalid email, password, or store name. Please try again.'
        );
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control) {
        control.markAsTouched();
        if (control instanceof FormGroup) {
          this.markFormGroupTouched(control);
        }
      }
    });
  }
}