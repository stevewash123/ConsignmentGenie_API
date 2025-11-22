import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register-owner',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  template: `
    <div class="registration-page">
      <div class="container">
        <div class="registration-card">
          <div class="header">
            <a routerLink="/register" class="back-link">← Back</a>
            <h1>Register Your Shop</h1>
          </div>

          <form [formGroup]="registrationForm" (ngSubmit)="onSubmit()">
            <!-- Your Information -->
            <div class="form-section">
              <h3>Your Information</h3>

              <div class="form-group">
                <label for="fullName">Full Name *</label>
                <input
                  id="fullName"
                  type="text"
                  formControlName="fullName"
                  [class.error]="registrationForm.get('fullName')?.invalid && registrationForm.get('fullName')?.touched"
                  placeholder="John Smith">
                <div class="error-message"
                     *ngIf="registrationForm.get('fullName')?.invalid && registrationForm.get('fullName')?.touched">
                  Full name is required
                </div>
              </div>

              <div class="form-group">
                <label for="email">Email *</label>
                <input
                  id="email"
                  type="email"
                  formControlName="email"
                  [class.error]="registrationForm.get('email')?.invalid && registrationForm.get('email')?.touched"
                  placeholder="john@myshop.com">
                <div class="error-message"
                     *ngIf="registrationForm.get('email')?.invalid && registrationForm.get('email')?.touched">
                  Valid email is required
                </div>
              </div>

              <div class="form-group">
                <label for="phone">Phone</label>
                <input
                  id="phone"
                  type="tel"
                  formControlName="phone"
                  placeholder="(555) 123-4567">
              </div>

              <div class="form-group">
                <label for="password">Password *</label>
                <input
                  id="password"
                  type="password"
                  formControlName="password"
                  [class.error]="registrationForm.get('password')?.invalid && registrationForm.get('password')?.touched"
                  placeholder="••••••••••••">
                <div class="form-hint">At least 8 characters</div>
                <div class="error-message"
                     *ngIf="registrationForm.get('password')?.invalid && registrationForm.get('password')?.touched">
                  Password must be at least 8 characters
                </div>
              </div>

              <div class="form-group">
                <label for="confirmPassword">Confirm Password *</label>
                <input
                  id="confirmPassword"
                  type="password"
                  formControlName="confirmPassword"
                  [class.error]="registrationForm.get('confirmPassword')?.invalid && registrationForm.get('confirmPassword')?.touched"
                  placeholder="••••••••••••">
                <div class="error-message"
                     *ngIf="registrationForm.get('confirmPassword')?.invalid && registrationForm.get('confirmPassword')?.touched">
                  Passwords must match
                </div>
              </div>
            </div>

            <!-- Shop Information -->
            <div class="form-section">
              <h3>Shop Information</h3>

              <div class="form-group">
                <label for="shopName">Shop Name *</label>
                <input
                  id="shopName"
                  type="text"
                  formControlName="shopName"
                  [class.error]="registrationForm.get('shopName')?.invalid && registrationForm.get('shopName')?.touched"
                  placeholder="Main Street Consignment">
                <div class="error-message"
                     *ngIf="registrationForm.get('shopName')?.invalid && registrationForm.get('shopName')?.touched">
                  Shop name is required
                </div>
              </div>
            </div>

            <!-- Submit -->
            <div class="submit-section">
              <button type="submit"
                      class="submit-btn"
                      [disabled]="registrationForm.invalid || isSubmitting()">
                {{ isSubmitting() ? 'Creating Account...' : 'Create Account' }}
              </button>

              <div class="info-note">
                <span class="info-icon">ℹ️</span>
                Your account will be reviewed by our team. You'll receive an email when approved.
              </div>
            </div>
          </form>

          <!-- Error Display -->
          <div class="alert error" *ngIf="errorMessage()">
            {{ errorMessage() }}
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .registration-page {
      min-height: 100vh;
      background: linear-gradient(135deg, #047857 0%, #059669 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
    }

    .container {
      width: 100%;
      max-width: 600px;
    }

    .registration-card {
      background: white;
      border-radius: 16px;
      padding: 3rem;
      box-shadow: 0 20px 40px rgba(0,0,0,0.1);
    }

    .header {
      margin-bottom: 2rem;
    }

    .back-link {
      color: #047857;
      text-decoration: none;
      font-weight: 600;
      margin-bottom: 1rem;
      display: inline-block;
    }

    .back-link:hover {
      text-decoration: underline;
    }

    h1 {
      color: #047857;
      font-size: 2rem;
      margin: 0;
      font-weight: 700;
    }

    .form-section {
      margin-bottom: 2rem;
    }

    .form-section h3 {
      color: #1f2937;
      font-size: 1.25rem;
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid #e5e7eb;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    label {
      display: block;
      color: #374151;
      font-weight: 600;
      margin-bottom: 0.5rem;
    }

    input {
      width: 100%;
      padding: 0.75rem;
      border: 2px solid #e5e7eb;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s;
      box-sizing: border-box;
    }

    input:focus {
      outline: none;
      border-color: #047857;
    }

    input.error {
      border-color: #ef4444;
    }

    .form-hint {
      color: #6b7280;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .error-message {
      color: #ef4444;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .submit-section {
      text-align: center;
      margin-top: 2rem;
    }

    .submit-btn {
      background: #047857;
      color: white;
      border: none;
      padding: 1rem 3rem;
      border-radius: 8px;
      font-size: 1.1rem;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
      width: 100%;
      margin-bottom: 1rem;
    }

    .submit-btn:hover:not(:disabled) {
      background: #059669;
    }

    .submit-btn:disabled {
      background: #d1d5db;
      cursor: not-allowed;
    }

    .info-note {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      color: #6b7280;
      font-size: 0.875rem;
      text-align: center;
      padding: 1rem;
      background: #f9fafb;
      border-radius: 8px;
    }

    .info-icon {
      font-size: 1rem;
    }

    .alert {
      margin-top: 1rem;
      padding: 1rem;
      border-radius: 8px;
    }

    .alert.error {
      background: #fef2f2;
      color: #dc2626;
      border: 1px solid #fecaca;
    }

    @media (max-width: 768px) {
      .registration-card {
        padding: 2rem;
      }

      h1 {
        font-size: 1.75rem;
      }
    }
  `]
})
export class RegisterOwnerComponent {
  registrationForm: FormGroup;
  isSubmitting = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registrationForm = this.fb.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      shopName: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
    } else if (confirmPassword && confirmPassword.hasError('passwordMismatch')) {
      confirmPassword.setErrors(null);
    }

    return null;
  }

  async onSubmit() {
    if (this.registrationForm.invalid) {
      this.markAllFieldsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    try {
      const formValue = this.registrationForm.value;
      const request = {
        fullName: formValue.fullName,
        email: formValue.email,
        phone: formValue.phone,
        password: formValue.password,
        shopName: formValue.shopName
      };

      const result = await this.authService.registerOwner(request);

      if (result.success) {
        // Navigate to success page
        this.router.navigate(['/register/success'], {
          queryParams: {
            type: 'owner',
            shopName: formValue.shopName,
            email: formValue.email
          }
        });
      } else {
        this.errorMessage.set(result.message || 'Registration failed');
      }
    } catch (error: any) {
      this.errorMessage.set(error.message || 'An unexpected error occurred');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private markAllFieldsTouched() {
    Object.keys(this.registrationForm.controls).forEach(key => {
      this.registrationForm.get(key)?.markAsTouched();
    });
  }
}