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
            <h1>Create Your Shop</h1>

            <!-- Progress Indicator -->
            <div class="progress-bar">
              <div class="progress-step" [class.active]="currentStep() === 1" [class.completed]="currentStep() > 1">
                <div class="step-number">1</div>
                <div class="step-label">Account Info</div>
              </div>
              <div class="progress-line" [class.completed]="currentStep() > 1"></div>
              <div class="progress-step" [class.active]="currentStep() === 2" [class.completed]="currentStep() > 2">
                <div class="step-number">2</div>
                <div class="step-label">Shop Setup</div>
              </div>
              <div class="progress-line" [class.completed]="currentStep() > 2"></div>
              <div class="progress-step" [class.active]="currentStep() === 3">
                <div class="step-number">3</div>
                <div class="step-label">Business Details</div>
              </div>
            </div>
          </div>

          <form [formGroup]="wizardForm" (ngSubmit)="onSubmit()">
            <!-- Step 1: Basic Account Info -->
            <div class="wizard-step" *ngIf="currentStep() === 1">
              <h3>Your Information</h3>
              <p>Let's start with your basic account information</p>

              <div class="form-group">
                <label for="fullName">Full Name *</label>
                <input
                  id="fullName"
                  type="text"
                  formControlName="fullName"
                  [class.error]="wizardForm.get('fullName')?.invalid && wizardForm.get('fullName')?.touched"
                  placeholder="John Smith">
                <div class="error-message"
                     *ngIf="wizardForm.get('fullName')?.invalid && wizardForm.get('fullName')?.touched">
                  Full name is required
                </div>
              </div>

              <div class="form-group">
                <label for="email">Email *</label>
                <input
                  id="email"
                  type="email"
                  formControlName="email"
                  [class.error]="wizardForm.get('email')?.invalid && wizardForm.get('email')?.touched"
                  placeholder="john@myshop.com">
                <div class="error-message"
                     *ngIf="wizardForm.get('email')?.invalid && wizardForm.get('email')?.touched">
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
                  [class.error]="wizardForm.get('password')?.invalid && wizardForm.get('password')?.touched"
                  placeholder="••••••••••••">
                <div class="form-hint">At least 8 characters</div>
                <div class="error-message"
                     *ngIf="wizardForm.get('password')?.invalid && wizardForm.get('password')?.touched">
                  Password must be at least 8 characters
                </div>
              </div>

              <div class="form-group">
                <label for="confirmPassword">Confirm Password *</label>
                <input
                  id="confirmPassword"
                  type="password"
                  formControlName="confirmPassword"
                  [class.error]="wizardForm.get('confirmPassword')?.invalid && wizardForm.get('confirmPassword')?.touched"
                  placeholder="••••••••••••">
                <div class="error-message"
                     *ngIf="wizardForm.get('confirmPassword')?.invalid && wizardForm.get('confirmPassword')?.touched">
                  Passwords must match
                </div>
              </div>
            </div>

            <!-- Step 2: Shop Setup -->
            <div class="wizard-step" *ngIf="currentStep() === 2">
              <h3>Shop Information</h3>
              <p>Tell us about your consignment shop</p>

              <div class="form-group">
                <label for="shopName">Shop/Business Name *</label>
                <input
                  id="shopName"
                  type="text"
                  formControlName="shopName"
                  [class.error]="wizardForm.get('shopName')?.invalid && wizardForm.get('shopName')?.touched"
                  placeholder="Main Street Consignment">
                <div class="error-message"
                     *ngIf="wizardForm.get('shopName')?.invalid && wizardForm.get('shopName')?.touched">
                  Shop name is required
                </div>
              </div>

              <div class="form-group">
                <label for="address">Address *</label>
                <textarea
                  id="address"
                  formControlName="address"
                  rows="3"
                  [class.error]="wizardForm.get('address')?.invalid && wizardForm.get('address')?.touched"
                  placeholder="123 Main Street&#10;Anytown, State 12345"></textarea>
                <div class="error-message"
                     *ngIf="wizardForm.get('address')?.invalid && wizardForm.get('address')?.touched">
                  Address is required
                </div>
              </div>

              <div class="form-group">
                <label for="shopType">Shop Type *</label>
                <select
                  id="shopType"
                  formControlName="shopType"
                  [class.error]="wizardForm.get('shopType')?.invalid && wizardForm.get('shopType')?.touched">
                  <option value="">Select shop type...</option>
                  <option value="consignment">Consignment Shop</option>
                  <option value="thrift">Thrift Store</option>
                  <option value="boutique">Boutique Consignment</option>
                  <option value="specialty">Specialty Items</option>
                  <option value="other">Other</option>
                </select>
                <div class="error-message"
                     *ngIf="wizardForm.get('shopType')?.invalid && wizardForm.get('shopType')?.touched">
                  Please select a shop type
                </div>
              </div>
            </div>

            <!-- Step 3: Business Details -->
            <div class="wizard-step" *ngIf="currentStep() === 3">
              <h3>Business Details</h3>
              <p>Configure your shop's commission and policies</p>

              <div class="form-group">
                <label for="defaultCommissionRate">Default Commission Rate (%) *</label>
                <input
                  id="defaultCommissionRate"
                  type="number"
                  min="10"
                  max="80"
                  formControlName="defaultCommissionRate"
                  [class.error]="wizardForm.get('defaultCommissionRate')?.invalid && wizardForm.get('defaultCommissionRate')?.touched"
                  placeholder="50">
                <div class="form-hint">Typical range is 40-60% for consignment shops</div>
                <div class="error-message"
                     *ngIf="wizardForm.get('defaultCommissionRate')?.invalid && wizardForm.get('defaultCommissionRate')?.touched">
                  Commission rate is required and must be between 10-80%
                </div>
              </div>

              <div class="form-group">
                <label for="taxId">Tax ID (optional)</label>
                <input
                  id="taxId"
                  type="text"
                  formControlName="taxId"
                  placeholder="XX-XXXXXXX">
                <div class="form-hint">Federal Tax ID for business reporting</div>
              </div>

              <div class="form-group">
                <label for="returnPolicy">Return Policy</label>
                <textarea
                  id="returnPolicy"
                  formControlName="returnPolicy"
                  rows="3"
                  placeholder="All sales are final. Items may be exchanged within 7 days with receipt..."></textarea>
              </div>

              <div class="form-group">
                <label for="consignmentTerms">Consignment Terms</label>
                <textarea
                  id="consignmentTerms"
                  formControlName="consignmentTerms"
                  rows="3"
                  placeholder="Items are consigned for 90 days. Commission is 50/50 split..."></textarea>
              </div>
            </div>

            <!-- Navigation -->
            <div class="navigation">
              <button
                type="button"
                class="nav-btn secondary"
                *ngIf="currentStep() > 1"
                (click)="previousStep()">
                Previous
              </button>

              <button
                type="button"
                class="nav-btn primary"
                *ngIf="currentStep() < 3"
                (click)="nextStep()"
                [disabled]="!isStepValid()">
                Next
              </button>

              <button
                type="submit"
                class="nav-btn primary submit"
                *ngIf="currentStep() === 3"
                [disabled]="wizardForm.invalid || isSubmitting()">
                {{ isSubmitting() ? 'Creating Shop...' : 'Create Shop' }}
              </button>
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
      max-width: 700px;
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
      margin: 0 0 2rem 0;
      font-weight: 700;
    }

    /* Progress Bar */
    .progress-bar {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 2rem;
      padding: 1rem 0;
    }

    .progress-step {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
    }

    .step-number {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: #e5e7eb;
      color: #6b7280;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      margin-bottom: 0.5rem;
      transition: all 0.3s;
    }

    .progress-step.active .step-number {
      background: #047857;
      color: white;
    }

    .progress-step.completed .step-number {
      background: #10b981;
      color: white;
    }

    .step-label {
      font-size: 0.875rem;
      color: #6b7280;
      font-weight: 500;
    }

    .progress-step.active .step-label {
      color: #047857;
      font-weight: 600;
    }

    .progress-line {
      height: 2px;
      width: 80px;
      background: #e5e7eb;
      margin: 0 1rem;
      transition: all 0.3s;
    }

    .progress-line.completed {
      background: #10b981;
    }

    /* Wizard Steps */
    .wizard-step {
      min-height: 400px;
    }

    .wizard-step h3 {
      color: #1f2937;
      font-size: 1.5rem;
      margin-bottom: 0.5rem;
    }

    .wizard-step p {
      color: #6b7280;
      margin-bottom: 2rem;
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

    input, select, textarea {
      width: 100%;
      padding: 0.75rem;
      border: 2px solid #e5e7eb;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s;
      box-sizing: border-box;
      font-family: inherit;
    }

    input:focus, select:focus, textarea:focus {
      outline: none;
      border-color: #047857;
    }

    input.error, select.error, textarea.error {
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

    /* Navigation */
    .navigation {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
      padding-top: 2rem;
      border-top: 1px solid #e5e7eb;
    }

    .nav-btn {
      padding: 0.75rem 2rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
      font-size: 1rem;
    }

    .nav-btn.secondary {
      background: #f3f4f6;
      color: #374151;
    }

    .nav-btn.secondary:hover:not(:disabled) {
      background: #e5e7eb;
    }

    .nav-btn.primary {
      background: #047857;
      color: white;
    }

    .nav-btn.primary:hover:not(:disabled) {
      background: #059669;
    }

    .nav-btn.submit {
      background: #0d9488;
    }

    .nav-btn.submit:hover:not(:disabled) {
      background: #0f766e;
    }

    .nav-btn:disabled {
      background: #d1d5db;
      color: #9ca3af;
      cursor: not-allowed;
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

      .progress-bar {
        margin-bottom: 1.5rem;
      }

      .step-number {
        width: 35px;
        height: 35px;
      }

      .progress-line {
        width: 60px;
      }

      .step-label {
        font-size: 0.75rem;
      }

      .navigation {
        flex-direction: column;
      }

      .nav-btn {
        width: 100%;
      }
    }
  `]
})
export class RegisterOwnerComponent {
  wizardForm: FormGroup;
  currentStep = signal(1);
  isSubmitting = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.wizardForm = this.fb.group({
      // Step 1: Account Info
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],

      // Step 2: Shop Setup
      shopName: ['', [Validators.required]],
      address: ['', [Validators.required]],
      shopType: ['', [Validators.required]],

      // Step 3: Business Details
      defaultCommissionRate: [50, [Validators.required, Validators.min(10), Validators.max(80)]],
      taxId: [''],
      returnPolicy: ['All sales are final. Items may be exchanged within 7 days with receipt for store credit only.'],
      consignmentTerms: ['Items are consigned for 90 days. Commission split is as agreed. Items not sold after 90 days may be returned to consigner or donated.']
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

  isStepValid(): boolean {
    const step = this.currentStep();
    if (step === 1) {
      return this.wizardForm.get('fullName')?.valid &&
             this.wizardForm.get('email')?.valid &&
             this.wizardForm.get('password')?.valid &&
             this.wizardForm.get('confirmPassword')?.valid;
    }
    if (step === 2) {
      return this.wizardForm.get('shopName')?.valid &&
             this.wizardForm.get('address')?.valid &&
             this.wizardForm.get('shopType')?.valid;
    }
    if (step === 3) {
      return this.wizardForm.get('defaultCommissionRate')?.valid;
    }
    return false;
  }

  nextStep() {
    if (this.isStepValid() && this.currentStep() < 3) {
      this.currentStep.set(this.currentStep() + 1);
    }
  }

  previousStep() {
    if (this.currentStep() > 1) {
      this.currentStep.set(this.currentStep() - 1);
    }
  }

  onSubmit() {
    if (this.wizardForm.invalid) {
      this.markAllFieldsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const formValue = this.wizardForm.value;
    const request = {
      fullName: formValue.fullName,
      email: formValue.email,
      phone: formValue.phone,
      password: formValue.password,
      shopName: formValue.shopName,
      address: formValue.address,
      shopType: formValue.shopType,
      defaultCommissionRate: formValue.defaultCommissionRate,
      taxId: formValue.taxId,
      returnPolicy: formValue.returnPolicy,
      consignmentTerms: formValue.consignmentTerms
    };

    this.authService.registerOwner(request).subscribe({
      next: (result) => {
        if (result.success) {
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
        this.isSubmitting.set(false);
      },
      error: (error: any) => {
        this.errorMessage.set(error.message || 'An unexpected error occurred');
        this.isSubmitting.set(false);
      }
    });
  }

  private markAllFieldsTouched() {
    Object.keys(this.wizardForm.controls).forEach(key => {
      this.wizardForm.get(key)?.markAsTouched();
    });
  }
}