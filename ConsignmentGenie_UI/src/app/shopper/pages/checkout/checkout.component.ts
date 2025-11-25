import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperAuthService } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

interface CartItem {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  category: string;
  quantity: number;
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="checkout-container">
      <div class="container">
        <div class="checkout-header">
          <h1>Checkout</h1>
          <p class="store-name" *ngIf="storeInfo">{{ storeInfo.name }}</p>
        </div>

        <div class="checkout-content" *ngIf="cartItems.length > 0; else emptyCartTemplate">
          <div class="checkout-steps">
            <div class="step" [class.active]="currentStep === 1" [class.completed]="currentStep > 1">
              <span class="step-number">1</span>
              <span class="step-title">Contact Info</span>
            </div>
            <div class="step" [class.active]="currentStep === 2" [class.completed]="currentStep > 2">
              <span class="step-number">2</span>
              <span class="step-title">Shipping</span>
            </div>
            <div class="step" [class.active]="currentStep === 3" [class.completed]="currentStep > 3">
              <span class="step-number">3</span>
              <span class="step-title">Payment</span>
            </div>
            <div class="step" [class.active]="currentStep === 4">
              <span class="step-number">4</span>
              <span class="step-title">Review</span>
            </div>
          </div>

          <div class="checkout-main">
            <div class="checkout-form">
              <!-- Step 1: Contact Information -->
              <div class="form-section" *ngIf="currentStep === 1">
                <h2>Contact Information</h2>

                <div class="auth-notice" *ngIf="!isAuthenticated">
                  <p>
                    <a [routerLink]="['/shop', storeSlug, 'login']" [queryParams]="{returnUrl: '/shop/' + storeSlug + '/checkout'}">
                      Sign in
                    </a> for faster checkout, or continue as guest.
                  </p>
                </div>

                <form [formGroup]="contactForm" class="contact-form">
                  <div class="form-row">
                    <div class="form-group">
                      <label for="firstName">First Name *</label>
                      <input
                        id="firstName"
                        type="text"
                        formControlName="firstName"
                        class="form-control"
                        [class.is-invalid]="isFieldInvalid('firstName', contactForm)">
                      <div class="invalid-feedback" *ngIf="isFieldInvalid('firstName', contactForm)">
                        First name is required
                      </div>
                    </div>

                    <div class="form-group">
                      <label for="lastName">Last Name *</label>
                      <input
                        id="lastName"
                        type="text"
                        formControlName="lastName"
                        class="form-control"
                        [class.is-invalid]="isFieldInvalid('lastName', contactForm)">
                      <div class="invalid-feedback" *ngIf="isFieldInvalid('lastName', contactForm)">
                        Last name is required
                      </div>
                    </div>
                  </div>

                  <div class="form-group">
                    <label for="email">Email Address *</label>
                    <input
                      id="email"
                      type="email"
                      formControlName="email"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('email', contactForm)">
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('email', contactForm)">
                      <span *ngIf="contactForm.get('email')?.errors?.['required']">Email is required</span>
                      <span *ngIf="contactForm.get('email')?.errors?.['email']">Please enter a valid email</span>
                    </div>
                  </div>

                  <div class="form-group">
                    <label for="phone">Phone Number</label>
                    <input
                      id="phone"
                      type="tel"
                      formControlName="phone"
                      class="form-control"
                      placeholder="(555) 123-4567">
                  </div>

                  <!-- Optional Account Creation -->
                  <div class="form-group account-creation" *ngIf="!isAuthenticated">
                    <div class="account-option">
                      <label class="checkbox-container">
                        <input
                          type="checkbox"
                          formControlName="createAccount"
                          (change)="onCreateAccountChange($event)">
                        <span class="checkmark"></span>
                        Save my info for faster checkout next time
                      </label>
                    </div>

                    <div class="password-section" *ngIf="contactForm.get('createAccount')?.value">
                      <div class="form-group">
                        <label for="password">Password *</label>
                        <input
                          id="password"
                          type="password"
                          formControlName="password"
                          class="form-control"
                          placeholder="Create a password"
                          [class.is-invalid]="isFieldInvalid('password', contactForm)">
                        <div class="invalid-feedback" *ngIf="isFieldInvalid('password', contactForm)">
                          Password must be at least 8 characters
                        </div>
                      </div>
                    </div>
                  </div>
                </form>

                <div class="form-actions">
                  <button class="btn btn-primary" (click)="nextStep()" [disabled]="!contactForm.valid">
                    Continue to Shipping
                  </button>
                </div>
              </div>

              <!-- Step 2: Shipping Address -->
              <div class="form-section" *ngIf="currentStep === 2">
                <h2>Shipping Address</h2>

                <form [formGroup]="shippingForm" class="shipping-form">
                  <div class="form-group">
                    <label for="address">Street Address *</label>
                    <input
                      id="address"
                      type="text"
                      formControlName="address"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('address', shippingForm)">
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('address', shippingForm)">
                      Street address is required
                    </div>
                  </div>

                  <div class="form-group">
                    <label for="apartment">Apartment, suite, etc.</label>
                    <input
                      id="apartment"
                      type="text"
                      formControlName="apartment"
                      class="form-control">
                  </div>

                  <div class="form-row">
                    <div class="form-group">
                      <label for="city">City *</label>
                      <input
                        id="city"
                        type="text"
                        formControlName="city"
                        class="form-control"
                        [class.is-invalid]="isFieldInvalid('city', shippingForm)">
                      <div class="invalid-feedback" *ngIf="isFieldInvalid('city', shippingForm)">
                        City is required
                      </div>
                    </div>

                    <div class="form-group">
                      <label for="state">State *</label>
                      <select
                        id="state"
                        formControlName="state"
                        class="form-control"
                        [class.is-invalid]="isFieldInvalid('state', shippingForm)">
                        <option value="">Select State</option>
                        <option value="AL">Alabama</option>
                        <option value="CA">California</option>
                        <option value="FL">Florida</option>
                        <option value="NY">New York</option>
                        <option value="TX">Texas</option>
                      </select>
                      <div class="invalid-feedback" *ngIf="isFieldInvalid('state', shippingForm)">
                        State is required
                      </div>
                    </div>

                    <div class="form-group">
                      <label for="zipCode">ZIP Code *</label>
                      <input
                        id="zipCode"
                        type="text"
                        formControlName="zipCode"
                        class="form-control"
                        [class.is-invalid]="isFieldInvalid('zipCode', shippingForm)">
                      <div class="invalid-feedback" *ngIf="isFieldInvalid('zipCode', shippingForm)">
                        ZIP code is required
                      </div>
                    </div>
                  </div>
                </form>

                <div class="form-actions">
                  <button class="btn btn-outline-secondary" (click)="previousStep()">
                    Back
                  </button>
                  <button class="btn btn-primary" (click)="nextStep()" [disabled]="!shippingForm.valid">
                    Continue to Payment
                  </button>
                </div>
              </div>

              <!-- Step 3: Payment Information -->
              <div class="form-section" *ngIf="currentStep === 3">
                <h2>Payment Information</h2>

                <div class="payment-notice">
                  <div class="notice-card">
                    <h3>🚧 Payment Integration Coming Soon</h3>
                    <p>
                      For Phase 1 MVP, payment processing is not yet implemented.
                      In the final version, this will include secure credit card processing
                      and other payment methods.
                    </p>
                    <p>
                      For now, you can review your order and we'll contact you to arrange payment.
                    </p>
                  </div>
                </div>

                <div class="form-actions">
                  <button class="btn btn-outline-secondary" (click)="previousStep()">
                    Back
                  </button>
                  <button class="btn btn-primary" (click)="nextStep()">
                    Review Order
                  </button>
                </div>
              </div>

              <!-- Step 4: Order Review -->
              <div class="form-section" *ngIf="currentStep === 4">
                <h2>Review Your Order</h2>

                <div class="review-section">
                  <div class="contact-review">
                    <h3>Contact Information</h3>
                    <p>{{ contactForm.value.firstName }} {{ contactForm.value.lastName }}</p>
                    <p>{{ contactForm.value.email }}</p>
                    <p *ngIf="contactForm.value.phone">{{ contactForm.value.phone }}</p>
                    <button class="btn-link" (click)="goToStep(1)">Edit</button>
                  </div>

                  <div class="shipping-review">
                    <h3>Shipping Address</h3>
                    <p>{{ shippingForm.value.address }}</p>
                    <p *ngIf="shippingForm.value.apartment">{{ shippingForm.value.apartment }}</p>
                    <p>{{ shippingForm.value.city }}, {{ shippingForm.value.state }} {{ shippingForm.value.zipCode }}</p>
                    <button class="btn-link" (click)="goToStep(2)">Edit</button>
                  </div>
                </div>

                <div class="form-actions">
                  <button class="btn btn-outline-secondary" (click)="previousStep()">
                    Back
                  </button>
                  <button class="btn btn-success btn-lg" (click)="submitOrder()" [disabled]="isSubmitting">
                    <span *ngIf="isSubmitting" class="spinner-border spinner-border-sm" role="status"></span>
                    {{ isSubmitting ? 'Placing Order...' : 'Place Order' }}
                  </button>
                </div>
              </div>
            </div>

            <div class="order-summary">
              <div class="summary-card">
                <h3>Order Summary</h3>

                <div class="order-items">
                  <div class="order-item" *ngFor="let item of cartItems">
                    <img
                      [src]="item.imageUrl || '/assets/placeholder-item.jpg'"
                      [alt]="item.name"
                      class="order-item-img">
                    <div class="order-item-details">
                      <h4>{{ item.name }}</h4>
                      <p>Qty: {{ item.quantity }}</p>
                      <p class="item-total">\${{ (item.price * item.quantity) | number:'1.2-2' }}</p>
                    </div>
                  </div>
                </div>

                <div class="summary-totals">
                  <div class="summary-row">
                    <span>Subtotal</span>
                    <span>\${{ getSubtotal() | number:'1.2-2' }}</span>
                  </div>
                  <div class="summary-row">
                    <span>Shipping</span>
                    <span>{{ getShipping() === 0 ? 'FREE' : '$' + (getShipping() | number:'1.2-2') }}</span>
                  </div>
                  <div class="summary-row">
                    <span>Tax</span>
                    <span>\${{ getTax() | number:'1.2-2' }}</span>
                  </div>
                  <div class="summary-row total">
                    <span>Total</span>
                    <span>\${{ getTotal() | number:'1.2-2' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <ng-template #emptyCartTemplate>
          <div class="empty-cart">
            <div class="empty-icon">🛒</div>
            <h2>Your cart is empty</h2>
            <p>Add items to your cart before checking out.</p>
            <button class="btn btn-primary" [routerLink]="['/shop', storeSlug]">
              Start Shopping
            </button>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .checkout-container {
      min-height: 80vh;
      padding: 2rem 0;
      background-color: #f8f9fa;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .checkout-header {
      text-align: center;
      margin-bottom: 2rem;
    }

    .checkout-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 0.5rem;
    }

    .store-name {
      font-size: 1.1rem;
      color: #007bff;
      margin: 0;
    }

    .checkout-steps {
      display: flex;
      justify-content: center;
      margin-bottom: 3rem;
      background: white;
      padding: 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .step {
      display: flex;
      align-items: center;
      margin: 0 1rem;
      color: #6c757d;
    }

    .step.active {
      color: #007bff;
    }

    .step.completed {
      color: #28a745;
    }

    .step-number {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background-color: #e9ecef;
      color: #6c757d;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      margin-right: 0.5rem;
    }

    .step.active .step-number {
      background-color: #007bff;
      color: white;
    }

    .step.completed .step-number {
      background-color: #28a745;
      color: white;
    }

    .checkout-main {
      display: grid;
      grid-template-columns: 1fr 350px;
      gap: 2rem;
      align-items: start;
    }

    .checkout-form {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 2rem;
    }

    .form-section h2 {
      color: #343a40;
      margin-bottom: 1.5rem;
      padding-bottom: 0.5rem;
      border-bottom: 1px solid #dee2e6;
    }

    .auth-notice {
      background-color: #e7f3ff;
      border: 1px solid #b3d7ff;
      border-radius: 0.375rem;
      padding: 1rem;
      margin-bottom: 1.5rem;
    }

    .auth-notice p {
      margin: 0;
      color: #1a5490;
    }

    .auth-notice a {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .auth-notice a:hover {
      text-decoration: underline;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
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

    .invalid-feedback {
      display: block;
      color: #dc3545;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .account-creation {
      background-color: #f8f9fa;
      border: 1px solid #e9ecef;
      border-radius: 0.375rem;
      padding: 1.5rem;
      margin: 1.5rem 0;
    }

    .account-option {
      margin-bottom: 1rem;
    }

    .checkbox-container {
      display: flex;
      align-items: center;
      cursor: pointer;
      font-size: 1rem;
      color: #495057;
      gap: 0.75rem;
    }

    .checkbox-container input[type="checkbox"] {
      width: auto;
      margin: 0;
    }

    .password-section {
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid #dee2e6;
    }

    .password-section .form-group {
      margin-bottom: 0;
    }

    .payment-notice {
      margin-bottom: 2rem;
    }

    .notice-card {
      background-color: #fff3cd;
      border: 1px solid #ffeaa7;
      border-radius: 0.5rem;
      padding: 1.5rem;
    }

    .notice-card h3 {
      color: #856404;
      margin-bottom: 1rem;
    }

    .notice-card p {
      color: #856404;
      margin-bottom: 1rem;
    }

    .notice-card p:last-child {
      margin-bottom: 0;
    }

    .review-section {
      margin-bottom: 2rem;
    }

    .contact-review, .shipping-review {
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      padding: 1rem;
      margin-bottom: 1rem;
      position: relative;
    }

    .contact-review h3, .shipping-review h3 {
      color: #343a40;
      margin-bottom: 1rem;
      font-size: 1.1rem;
    }

    .contact-review p, .shipping-review p {
      margin: 0.25rem 0;
      color: #6c757d;
    }

    .btn-link {
      background: none;
      border: none;
      color: #007bff;
      text-decoration: none;
      cursor: pointer;
      font-size: 0.875rem;
      position: absolute;
      top: 1rem;
      right: 1rem;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
      padding-top: 1rem;
      border-top: 1px solid #dee2e6;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border: 1px solid transparent;
      border-radius: 0.375rem;
      font-size: 1rem;
      font-weight: 500;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
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

    .btn-success {
      background-color: #28a745;
      border-color: #28a745;
      color: white;
    }

    .btn-success:hover:not(:disabled) {
      background-color: #218838;
      border-color: #1e7e34;
    }

    .btn-outline-secondary {
      color: #6c757d;
      border-color: #6c757d;
      background-color: transparent;
    }

    .btn-outline-secondary:hover {
      color: white;
      background-color: #6c757d;
    }

    .btn:disabled {
      opacity: 0.65;
      cursor: not-allowed;
    }

    .btn-lg {
      padding: 1rem 2rem;
      font-size: 1.125rem;
    }

    .spinner-border-sm {
      width: 1rem;
      height: 1rem;
      border-width: 0.125em;
    }

    .order-summary {
      position: sticky;
      top: 2rem;
    }

    .summary-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 1.5rem;
    }

    .summary-card h3 {
      font-size: 1.25rem;
      font-weight: 600;
      color: #343a40;
      margin-bottom: 1.5rem;
      text-align: center;
    }

    .order-items {
      margin-bottom: 1.5rem;
    }

    .order-item {
      display: flex;
      gap: 1rem;
      margin-bottom: 1rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid #dee2e6;
    }

    .order-item:last-child {
      border-bottom: none;
      margin-bottom: 0;
    }

    .order-item-img {
      width: 60px;
      height: 60px;
      object-fit: cover;
      border-radius: 0.375rem;
    }

    .order-item-details h4 {
      font-size: 0.9rem;
      color: #343a40;
      margin-bottom: 0.25rem;
    }

    .order-item-details p {
      font-size: 0.8rem;
      color: #6c757d;
      margin: 0.125rem 0;
    }

    .item-total {
      font-weight: bold;
      color: #28a745 !important;
    }

    .summary-totals {
      border-top: 1px solid #dee2e6;
      padding-top: 1rem;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .summary-row.total {
      font-size: 1.1rem;
      font-weight: bold;
      color: #343a40;
      border-top: 1px solid #dee2e6;
      padding-top: 0.75rem;
      margin-top: 0.75rem;
    }

    .empty-cart {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 4rem 2rem;
      text-align: center;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .empty-cart h2 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .empty-cart p {
      color: #6c757d;
      margin-bottom: 2rem;
    }

    @media (max-width: 968px) {
      .checkout-main {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .order-summary {
        position: static;
        order: -1;
      }

      .form-row {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .checkout-steps {
        flex-direction: column;
        text-align: center;
      }

      .step {
        justify-content: center;
        margin: 0.5rem 0;
      }

      .form-actions {
        flex-direction: column;
      }
    }

    @media (max-width: 480px) {
      .checkout-header h1 {
        font-size: 2rem;
      }

      .checkout-form {
        padding: 1rem;
      }

      .summary-card {
        padding: 1rem;
      }
    }
  `]
})
export class CheckoutComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  isAuthenticated = false;
  currentStep = 1;
  isSubmitting = false;

  contactForm: FormGroup;
  shippingForm: FormGroup;

  // Sample cart data for Phase 1 MVP
  cartItems: CartItem[] = [
    {
      id: '1',
      name: 'Vintage Leather Jacket',
      description: 'Classic brown leather jacket',
      price: 125.00,
      category: 'Clothing',
      quantity: 1
    },
    {
      id: '2',
      name: 'Antique Wooden Table',
      description: 'Beautiful oak dining table',
      price: 450.00,
      category: 'Furniture',
      quantity: 1
    }
  ];

  private taxRate = 0.08; // 8% tax rate
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {
    this.contactForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      createAccount: [false],
      password: ['']
    });

    this.shippingForm = this.fb.group({
      address: ['', [Validators.required]],
      apartment: [''],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      zipCode: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      this.isAuthenticated = this.authService.isAuthenticated(this.storeSlug);
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    this.loadCartFromStorage();
    this.prefillFormsIfAuthenticated();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  nextStep(): void {
    if (this.currentStep < 4) {
      this.currentStep++;
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  goToStep(step: number): void {
    this.currentStep = step;
  }

  isFieldInvalid(fieldName: string, form: FormGroup): boolean {
    const field = form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getSubtotal(): number {
    return this.cartItems.reduce((total, item) => total + (item.price * item.quantity), 0);
  }

  getShipping(): number {
    return this.getSubtotal() > 100 ? 0 : 15.00; // Free shipping over $100
  }

  getTax(): number {
    return this.getSubtotal() * this.taxRate;
  }

  getTotal(): number {
    return this.getSubtotal() + this.getShipping() + this.getTax();
  }

  onCreateAccountChange(event: any): void {
    const createAccount = event.target.checked;
    const passwordControl = this.contactForm.get('password');

    if (createAccount) {
      passwordControl?.setValidators([Validators.required, Validators.minLength(8)]);
    } else {
      passwordControl?.clearValidators();
      passwordControl?.setValue('');
    }
    passwordControl?.updateValueAndValidity();
  }

  submitOrder(): void {
    if (!this.contactForm.valid || !this.shippingForm.valid) {
      return;
    }

    this.isSubmitting = true;

    // Prepare order data
    const orderData = {
      contact: this.contactForm.value,
      shipping: this.shippingForm.value,
      items: this.cartItems,
      totals: {
        subtotal: this.getSubtotal(),
        shipping: this.getShipping(),
        tax: this.getTax(),
        total: this.getTotal()
      },
      createAccount: this.contactForm.get('createAccount')?.value || false
    };

    // Simulate order submission
    setTimeout(() => {
      this.isSubmitting = false;

      // If user chose to create account, create it
      if (orderData.createAccount && orderData.contact.password) {
        this.createCustomerAccount(orderData.contact);
      }

      // Clear cart and redirect to success page
      this.clearCart();
      alert('Order submitted successfully! You will receive a confirmation email shortly.' +
            (orderData.createAccount ? ' Your account has been created for faster future checkouts.' : ''));
      this.router.navigate(['/shop', this.storeSlug, 'account', 'orders']);
    }, 2000);
  }

  private createCustomerAccount(contactData: any): void {
    // TODO: Implement customer account creation API call
    // This would call the backend to create a customer account with the provided data
    const customerData = {
      firstName: contactData.firstName,
      lastName: contactData.lastName,
      email: contactData.email,
      phone: contactData.phone,
      password: contactData.password,
      storeSlug: this.storeSlug
    };

    console.log('Creating customer account:', customerData);
    // Future implementation would call shopper auth service to create account
  }

  private loadCartFromStorage(): void {
    try {
      const cartData = localStorage.getItem(`cart_${this.storeSlug}`);
      if (cartData) {
        this.cartItems = JSON.parse(cartData);
      }
    } catch (error) {
      console.error('Error loading cart from storage:', error);
    }
  }

  private clearCart(): void {
    try {
      localStorage.removeItem(`cart_${this.storeSlug}`);
      this.cartItems = [];
    } catch (error) {
      console.error('Error clearing cart:', error);
    }
  }

  private prefillFormsIfAuthenticated(): void {
    if (this.isAuthenticated) {
      // TODO: Load user profile data from API and prefill forms
      // For now, we'll just leave forms empty for guest checkout
    }
  }
}