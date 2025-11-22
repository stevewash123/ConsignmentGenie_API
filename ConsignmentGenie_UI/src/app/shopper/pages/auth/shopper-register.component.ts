import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { ShopperAuthService, ShopperRegisterRequest } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

@Component({
  selector: 'app-shopper-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="register-container">
      <div class="register-card">
        <div class="register-header">
          <div class="store-branding" *ngIf="storeInfo">
            <img *ngIf="storeInfo.logoUrl" [src]="storeInfo.logoUrl" [alt]="storeInfo.name" class="store-logo">
            <h1 class="store-name">{{ storeInfo.name }}</h1>
          </div>
          <h2 class="register-title">Create Your Account</h2>
          <p class="register-subtitle">Start shopping with us today!</p>
        </div>

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="register-form">
          <div class="form-group">
            <label for="fullName">Full Name *</label>
            <input
              id="fullName"
              type="text"
              formControlName="fullName"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('fullName')"
              placeholder="Enter your full name"
              autocomplete="name">
            <div class="invalid-feedback" *ngIf="isFieldInvalid('fullName')">
              <span *ngIf="registerForm.get('fullName')?.errors?.['required']">Full name is required</span>
              <span *ngIf="registerForm.get('fullName')?.errors?.['maxlength']">Full name must be less than 200 characters</span>
            </div>
          </div>

          <div class="form-group">
            <label for="email">Email *</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('email')"
              placeholder="Enter your email"
              autocomplete="email">
            <div class="invalid-feedback" *ngIf="isFieldInvalid('email')">
              <span *ngIf="registerForm.get('email')?.errors?.['required']">Email is required</span>
              <span *ngIf="registerForm.get('email')?.errors?.['email']">Please enter a valid email</span>
              <span *ngIf="registerForm.get('email')?.errors?.['maxlength']">Email must be less than 255 characters</span>
            </div>
          </div>

          <div class="form-group">
            <label for="phone">Phone</label>
            <input
              id="phone"
              type="tel"
              formControlName="phone"
              class="form-control"
              [class.is-invalid]="isFieldInvalid('phone')"
              placeholder="(555) 123-4567"
              autocomplete="tel">
            <div class="invalid-feedback" *ngIf="isFieldInvalid('phone')">
              <span *ngIf="registerForm.get('phone')?.errors?.['maxlength']">Phone must be less than 20 characters</span>
              <span *ngIf="registerForm.get('phone')?.errors?.['pattern']">Please enter a valid phone number</span>
            </div>
          </div>

          <div class="form-group">
            <label for="password">Password *</label>
            <div class="password-input">
              <input
                id="password"
                [type]="showPassword ? 'text' : 'password'"
                formControlName="password"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('password')"
                placeholder="Enter your password"
                autocomplete="new-password">
              <button
                type="button"
                class="password-toggle"
                (click)="togglePasswordVisibility()"
                [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'">
                {{ showPassword ? '👁️' : '👁️‍🗨️' }}
              </button>
            </div>
            <div class="password-requirements">
              <small class="form-text">At least 8 characters</small>
            </div>
            <div class="invalid-feedback" *ngIf="isFieldInvalid('password')">
              <span *ngIf="registerForm.get('password')?.errors?.['required']">Password is required</span>
              <span *ngIf="registerForm.get('password')?.errors?.['minlength']">Password must be at least 8 characters</span>
            </div>
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm Password *</label>
            <div class="password-input">
              <input
                id="confirmPassword"
                [type]="showConfirmPassword ? 'text' : 'password'"
                formControlName="confirmPassword"
                class="form-control"
                [class.is-invalid]="isFieldInvalid('confirmPassword')"
                placeholder="Confirm your password"
                autocomplete="new-password">
              <button
                type="button"
                class="password-toggle"
                (click)="toggleConfirmPasswordVisibility()"
                [attr.aria-label]="showConfirmPassword ? 'Hide password' : 'Show password'">
                {{ showConfirmPassword ? '👁️' : '👁️‍🗨️' }}
              </button>
            </div>
            <div class="invalid-feedback" *ngIf="isFieldInvalid('confirmPassword')">
              <span *ngIf="registerForm.get('confirmPassword')?.errors?.['required']">Please confirm your password</span>
              <span *ngIf="registerForm.get('confirmPassword')?.errors?.['mismatch']">Passwords do not match</span>
            </div>
          </div>

          <div class="form-group-checkbox">
            <input
              id="emailNotifications"
              type="checkbox"
              formControlName="emailNotifications"
              class="form-check-input">
            <label for="emailNotifications" class="form-check-label">
              Email me about new arrivals and sales
            </label>
          </div>

          <div class="error-message" *ngIf="errorMessage">
            <div class="alert alert-danger">
              {{ errorMessage }}
            </div>
          </div>

          <button
            type="submit"
            class="btn btn-primary btn-block"
            [disabled]="registerForm.invalid || isLoading">
            <span *ngIf="isLoading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
            {{ isLoading ? 'Creating Account...' : 'Create Account' }}
          </button>
        </form>

        <div class="register-footer">
          <div class="divider">
            <span>or</span>
          </div>

          <p class="login-prompt">
            Already have an account?
            <a [routerLink]="['/shop', storeSlug, 'login']" class="login-link">Sign In</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .register-container {
      min-height: 80vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
      background-color: #f8f9fa;
    }

    .register-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      width: 100%;
      max-width: 450px;
      padding: 2rem;
    }

    .register-header {
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

    .register-title {
      font-size: 1.5rem;
      color: #343a40;
      margin: 0;
      font-weight: bold;
    }

    .register-subtitle {
      color: #6c757d;
      margin: 0.5rem 0 0 0;
      font-size: 1rem;
    }

    .register-form {
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

    .password-requirements {
      margin-top: 0.25rem;
    }

    .form-text {
      color: #6c757d;
      font-size: 0.875rem;
    }

    .invalid-feedback {
      display: block;
      color: #dc3545;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .form-group-checkbox {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
    }

    .form-check-input {
      margin-top: 0.125rem;
    }

    .form-check-label {
      font-size: 0.875rem;
      color: #6c757d;
      line-height: 1.4;
      cursor: pointer;
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

    .register-footer {
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

    .login-prompt {
      color: #6c757d;
    }

    .login-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .login-link:hover {
      text-decoration: underline;
    }

    @media (max-width: 480px) {
      .register-container {
        padding: 1rem;
      }

      .register-card {
        padding: 1.5rem;
      }
    }
  `]
})
export class ShopperRegisterComponent implements OnInit, OnDestroy {
  registerForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  showConfirmPassword = false;
  storeSlug = '';
  storeInfo: StoreInfoDto | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private shopperAuthService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      phone: ['', [Validators.maxLength(20), this.phoneValidator]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      emailNotifications: [true]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  ngOnInit(): void {
    // Get store slug from route
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';

      // Check if already authenticated for this store
      if (this.shopperAuthService.isAuthenticated(this.storeSlug)) {
        this.router.navigate(['/shop', this.storeSlug]);
        return;
      }
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
    if (this.registerForm.invalid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const registerRequest: ShopperRegisterRequest = {
      fullName: this.registerForm.value.fullName,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      phone: this.registerForm.value.phone || undefined,
      emailNotifications: this.registerForm.value.emailNotifications
    };

    this.shopperAuthService.register(this.storeSlug, registerRequest).pipe(
      takeUntil(this.destroy$),
      finalize(() => {
        this.isLoading = false;
      })
    ).subscribe({
      next: (result) => {
        if (result.success) {
          // Registration successful, redirect to catalog
          this.router.navigate(['/shop', this.storeSlug]);
        } else {
          this.errorMessage = result.errorMessage || 'Registration failed. Please try again.';
        }
      },
      error: (error) => {
        this.errorMessage = error.message || 'An error occurred during registration. Please try again.';
      }
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private markAllFieldsAsTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      this.registerForm.get(key)?.markAsTouched();
    });
  }

  private passwordMatchValidator(control: AbstractControl): { [key: string]: boolean } | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    if (password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ mismatch: true });
      return { mismatch: true };
    }

    // Clear mismatch error if passwords match
    if (confirmPassword.errors?.['mismatch']) {
      delete confirmPassword.errors['mismatch'];
      if (Object.keys(confirmPassword.errors).length === 0) {
        confirmPassword.setErrors(null);
      }
    }

    return null;
  }

  private phoneValidator(control: AbstractControl): { [key: string]: boolean } | null {
    if (!control.value) {
      return null; // Phone is optional
    }

    // Simple phone validation - allows various formats
    const phonePattern = /^[\+]?[\d\s\(\)\-\.]{7,20}$/;
    if (!phonePattern.test(control.value)) {
      return { pattern: true };
    }

    return null;
  }
}