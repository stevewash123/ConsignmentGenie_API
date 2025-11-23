import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register-provider',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Join as a Provider
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600">
          Partner with a consignment shop to sell your items
        </p>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          <!-- Step 1: Store Code Validation -->
          <div *ngIf="!storeCodeValidated" class="space-y-6">
            <h3 class="text-lg font-medium text-gray-900">Step 1: Enter Store Code</h3>
            <p class="text-sm text-gray-600">
              Enter the store code provided by your consignment shop
            </p>

            <form [formGroup]="storeCodeForm" (ngSubmit)="validateStoreCode()" class="space-y-6">
              <div>
                <label for="storeCode" class="block text-sm font-medium text-gray-700">
                  Store Code
                </label>
                <div class="mt-1">
                  <input
                    id="storeCode"
                    name="storeCode"
                    type="text"
                    formControlName="storeCode"
                    required
                    placeholder="Enter 4-digit store code"
                    class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                    [class.border-red-300]="storeCodeForm.get('storeCode')?.invalid && storeCodeForm.get('storeCode')?.touched"
                  >
                </div>
                <div *ngIf="storeCodeForm.get('storeCode')?.invalid && storeCodeForm.get('storeCode')?.touched" class="mt-2 text-sm text-red-600">
                  <div *ngIf="storeCodeForm.get('storeCode')?.errors?.['required']">Store code is required</div>
                  <div *ngIf="storeCodeForm.get('storeCode')?.errors?.['pattern']">Store code must be 4 digits</div>
                </div>
                <div *ngIf="storeCodeError" class="mt-2 text-sm text-red-600">
                  {{ storeCodeError }}
                </div>
              </div>

              <div>
                <button
                  type="submit"
                  [disabled]="storeCodeForm.invalid || isValidatingStoreCode"
                  class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span *ngIf="isValidatingStoreCode" class="mr-2">
                    <svg class="animate-spin h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                  </span>
                  {{ isValidatingStoreCode ? 'Validating...' : 'Validate Store Code' }}
                </button>
              </div>
            </form>

            <div class="mt-6">
              <div class="relative">
                <div class="absolute inset-0 flex items-center">
                  <div class="w-full border-t border-gray-300"></div>
                </div>
                <div class="relative flex justify-center text-sm">
                  <span class="px-2 bg-white text-gray-500">Need help?</span>
                </div>
              </div>
              <div class="mt-3 text-center text-sm text-gray-600">
                Contact your consignment shop for their store code
              </div>
            </div>
          </div>

          <!-- Step 2: Registration Form -->
          <div *ngIf="storeCodeValidated" class="space-y-6" data-cy="registration-form">
            <div class="bg-green-50 border border-green-200 rounded-md p-4">
              <div class="flex">
                <div class="flex-shrink-0">
                  <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"></path>
                  </svg>
                </div>
                <div class="ml-3">
                  <h3 class="text-sm font-medium text-green-800">
                    Store Code Validated
                  </h3>
                  <p class="mt-1 text-sm text-green-700">
                    Ready to register with {{ shopName }}
                  </p>
                </div>
              </div>
            </div>

            <h3 class="text-lg font-medium text-gray-900">Step 2: Complete Registration</h3>

            <form [formGroup]="registrationForm" (ngSubmit)="onSubmit()" class="space-y-6">
              <!-- Personal Information Section -->
              <div class="bg-gray-50 p-4 rounded-lg">
                <h4 class="text-md font-medium text-gray-900 mb-4">Personal Information</h4>

                <div class="space-y-4">
                  <div>
                    <label for="fullName" class="block text-sm font-medium text-gray-700">
                      Full Name *
                    </label>
                    <div class="mt-1">
                      <input
                        id="fullName"
                        name="fullName"
                        type="text"
                        formControlName="fullName"
                        required
                        class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                        [class.border-red-300]="registrationForm.get('fullName')?.invalid && registrationForm.get('fullName')?.touched"
                      >
                    </div>
                    <div *ngIf="registrationForm.get('fullName')?.invalid && registrationForm.get('fullName')?.touched" class="mt-2 text-sm text-red-600">
                      <div *ngIf="registrationForm.get('fullName')?.errors?.['required']">Full name is required</div>
                      <div *ngIf="registrationForm.get('fullName')?.errors?.['minlength']">Full name must be at least 2 characters</div>
                    </div>
                  </div>

                  <div>
                    <label for="email" class="block text-sm font-medium text-gray-700">
                      Email Address *
                    </label>
                    <div class="mt-1">
                      <input
                        id="email"
                        name="email"
                        type="email"
                        formControlName="email"
                        required
                        class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                        [class.border-red-300]="registrationForm.get('email')?.invalid && registrationForm.get('email')?.touched"
                      >
                    </div>
                    <div *ngIf="registrationForm.get('email')?.invalid && registrationForm.get('email')?.touched" class="mt-2 text-sm text-red-600">
                      <div *ngIf="registrationForm.get('email')?.errors?.['required']">Email is required</div>
                      <div *ngIf="registrationForm.get('email')?.errors?.['email']">Please enter a valid email address</div>
                    </div>
                  </div>

                  <div>
                    <label for="phone" class="block text-sm font-medium text-gray-700">
                      Phone Number
                    </label>
                    <div class="mt-1">
                      <input
                        id="phone"
                        name="phone"
                        type="tel"
                        formControlName="phone"
                        placeholder="(555) 123-4567"
                        class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                        [class.border-red-300]="registrationForm.get('phone')?.invalid && registrationForm.get('phone')?.touched"
                      >
                    </div>
                    <div *ngIf="registrationForm.get('phone')?.invalid && registrationForm.get('phone')?.touched" class="mt-2 text-sm text-red-600">
                      <div *ngIf="registrationForm.get('phone')?.errors?.['pattern']">Please enter a valid phone number</div>
                    </div>
                  </div>

                  <div>
                    <label for="password" class="block text-sm font-medium text-gray-700">
                      Password *
                    </label>
                    <div class="mt-1">
                      <input
                        id="password"
                        name="password"
                        type="password"
                        formControlName="password"
                        required
                        class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                        [class.border-red-300]="registrationForm.get('password')?.invalid && registrationForm.get('password')?.touched"
                      >
                    </div>
                    <div *ngIf="registrationForm.get('password')?.invalid && registrationForm.get('password')?.touched" class="mt-2 text-sm text-red-600">
                      <div *ngIf="registrationForm.get('password')?.errors?.['required']">Password is required</div>
                      <div *ngIf="registrationForm.get('password')?.errors?.['minlength']">Password must be at least 8 characters</div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Payment Information Section -->
              <div class="bg-gray-50 p-4 rounded-lg">
                <h4 class="text-md font-medium text-gray-900 mb-4">Payment Preferences (Optional)</h4>

                <div class="space-y-4">
                  <div>
                    <label for="preferredPaymentMethod" class="block text-sm font-medium text-gray-700">
                      Preferred Payment Method
                    </label>
                    <div class="mt-1">
                      <select
                        id="preferredPaymentMethod"
                        name="preferredPaymentMethod"
                        formControlName="preferredPaymentMethod"
                        class="block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      >
                        <option value="">Select payment method...</option>
                        <option value="Check">Check</option>
                        <option value="Cash">Cash</option>
                        <option value="Venmo">Venmo</option>
                        <option value="PayPal">PayPal</option>
                        <option value="Zelle">Zelle</option>
                        <option value="Bank Transfer">Bank Transfer</option>
                      </select>
                    </div>
                  </div>

                  <div *ngIf="registrationForm.get('preferredPaymentMethod')?.value">
                    <label for="paymentDetails" class="block text-sm font-medium text-gray-700">
                      Payment Details
                    </label>
                    <div class="mt-1">
                      <input
                        id="paymentDetails"
                        name="paymentDetails"
                        type="text"
                        formControlName="paymentDetails"
                        [placeholder]="getPaymentPlaceholder()"
                        class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      >
                    </div>
                    <p class="mt-1 text-sm text-gray-500">
                      {{ getPaymentHelpText() }}
                    </p>
                  </div>
                </div>
              </div>

              <!-- Terms and Submit -->
              <div class="space-y-4">
                <div class="text-sm text-gray-600">
                  <p class="mb-2">
                    By registering, you agree to partner with {{ shopName }} under their consignment terms.
                  </p>
                  <p>
                    Your account will require approval from the shop owner before you can start consigning items.
                  </p>
                </div>

                <!-- Error Messages -->
                <div *ngIf="registrationError" class="rounded-md bg-red-50 p-4">
                  <div class="flex">
                    <div class="flex-shrink-0">
                      <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"></path>
                      </svg>
                    </div>
                    <div class="ml-3">
                      <h3 class="text-sm font-medium text-red-800">Registration Failed</h3>
                      <div class="mt-2 text-sm text-red-700">
                        {{ registrationError }}
                        <ul *ngIf="registrationErrors?.length" class="mt-1 list-disc pl-5">
                          <li *ngFor="let error of registrationErrors">{{ error }}</li>
                        </ul>
                      </div>
                    </div>
                  </div>
                </div>

                <div>
                  <button
                    type="submit"
                    [disabled]="registrationForm.invalid || isSubmitting"
                    class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <span *ngIf="isSubmitting" class="mr-2">
                      <svg class="animate-spin h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                    </span>
                    {{ isSubmitting ? 'Creating Account...' : 'Create Provider Account' }}
                  </button>
                </div>
              </div>
            </form>

            <div class="mt-6">
              <button
                type="button"
                (click)="resetStoreCode()"
                class="text-sm text-indigo-600 hover:text-indigo-500"
              >
                ← Change store code
              </button>
            </div>
          </div>

          <!-- Back to Login -->
          <div class="mt-6">
            <div class="relative">
              <div class="absolute inset-0 flex items-center">
                <div class="w-full border-t border-gray-300"></div>
              </div>
              <div class="relative flex justify-center text-sm">
                <span class="px-2 bg-white text-gray-500">Already have an account?</span>
              </div>
            </div>
            <div class="mt-6">
              <a
                routerLink="/login"
                class="w-full flex justify-center py-2 px-4 border border-gray-300 rounded-md shadow-sm bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
              >
                Sign In
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class RegisterProviderComponent implements OnInit {
  storeCodeForm: FormGroup;
  registrationForm: FormGroup;

  storeCodeValidated = false;
  isValidatingStoreCode = false;
  isSubmitting = false;

  shopName = '';
  storeCodeError = '';
  registrationError = '';
  registrationErrors: string[] = [];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.storeCodeForm = this.fb.group({
      storeCode: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]]
    });

    this.registrationForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(/^[\+]?[1-9][\d]{0,15}$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      preferredPaymentMethod: [''],
      paymentDetails: ['']
    });
  }

  ngOnInit(): void {}

  async validateStoreCode(): Promise<void> {
    if (this.storeCodeForm.invalid) return;

    this.isValidatingStoreCode = true;
    this.storeCodeError = '';

    try {
      const storeCode = this.storeCodeForm.get('storeCode')?.value;
      const validation = await this.authService.validateStoreCode(storeCode);

      if (validation.isValid && validation.shopName) {
        this.storeCodeValidated = true;
        this.shopName = validation.shopName;
      } else {
        this.storeCodeError = validation.errorMessage || 'Invalid store code';
      }
    } catch (error) {
      this.storeCodeError = 'Unable to validate store code. Please try again.';
    } finally {
      this.isValidatingStoreCode = false;
    }
  }

  async onSubmit(): Promise<void> {
    if (this.registrationForm.invalid) return;

    this.isSubmitting = true;
    this.registrationError = '';
    this.registrationErrors = [];

    try {
      const formData = this.registrationForm.value;
      const request = {
        storeCode: this.storeCodeForm.get('storeCode')?.value,
        fullName: formData.fullName,
        email: formData.email,
        password: formData.password,
        phone: formData.phone || undefined,
        preferredPaymentMethod: formData.preferredPaymentMethod || undefined,
        paymentDetails: formData.paymentDetails || undefined
      };

      const result = await this.authService.registerProvider(request);

      if (result.success) {
        this.router.navigate(['/register/success'], {
          queryParams: {
            type: 'provider',
            shopName: this.shopName,
            email: formData.email,
            fullName: formData.fullName
          }
        });
      } else {
        this.registrationError = result.message || 'Registration failed';
        this.registrationErrors = result.errors || [];
      }
    } catch (error: any) {
      this.registrationError = error.message || 'An unexpected error occurred';
    } finally {
      this.isSubmitting = false;
    }
  }

  resetStoreCode(): void {
    this.storeCodeValidated = false;
    this.shopName = '';
    this.storeCodeError = '';
    this.registrationError = '';
    this.registrationErrors = [];
    this.storeCodeForm.reset();
    this.registrationForm.reset();
  }

  getPaymentPlaceholder(): string {
    const method = this.registrationForm.get('preferredPaymentMethod')?.value;
    switch (method) {
      case 'Venmo': return '@username';
      case 'PayPal': return 'email@example.com';
      case 'Zelle': return 'email@example.com or phone number';
      case 'Bank Transfer': return 'Account details';
      case 'Check': return 'Mailing address';
      default: return 'Payment details';
    }
  }

  getPaymentHelpText(): string {
    const method = this.registrationForm.get('preferredPaymentMethod')?.value;
    switch (method) {
      case 'Venmo': return 'Enter your Venmo username (e.g., @john-doe)';
      case 'PayPal': return 'Enter the email address associated with your PayPal account';
      case 'Zelle': return 'Enter email or phone number registered with Zelle';
      case 'Bank Transfer': return 'Bank account information will be collected securely later';
      case 'Check': return 'Enter your mailing address for check delivery';
      case 'Cash': return 'No additional details needed for cash pickup';
      default: return 'Enter relevant payment information';
    }
  }
}