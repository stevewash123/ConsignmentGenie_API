import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { ShopperAuthService } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="settings-container">
      <div class="container">
        <div class="page-header">
          <h1>Account Settings</h1>
          <p class="store-name" *ngIf="storeInfo">{{ storeInfo.name }}</p>
        </div>

        <div class="settings-content">
          <div class="settings-nav">
            <nav class="nav-pills">
              <button
                class="nav-item"
                [class.active]="activeTab === 'profile'"
                (click)="setActiveTab('profile')">
                Profile Information
              </button>
              <button
                class="nav-item"
                [class.active]="activeTab === 'shipping'"
                (click)="setActiveTab('shipping')">
                Shipping Address
              </button>
              <button
                class="nav-item"
                [class.active]="activeTab === 'password'"
                (click)="setActiveTab('password')">
                Change Password
              </button>
              <button
                class="nav-item"
                [class.active]="activeTab === 'preferences'"
                (click)="setActiveTab('preferences')">
                Preferences
              </button>
            </nav>
          </div>

          <div class="settings-panel">
            <!-- Profile Information Tab -->
            <div class="tab-content" *ngIf="activeTab === 'profile'">
              <div class="tab-header">
                <h2>Profile Information</h2>
                <p>Update your personal information and contact details.</p>
              </div>

              <form [formGroup]="profileForm" (ngSubmit)="updateProfile()" class="settings-form">
                <div class="form-row">
                  <div class="form-group">
                    <label for="firstName">First Name *</label>
                    <input
                      id="firstName"
                      type="text"
                      formControlName="firstName"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('firstName', profileForm)">
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('firstName', profileForm)">
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
                      [class.is-invalid]="isFieldInvalid('lastName', profileForm)">
                    <div class="invalid-feedback" *ngIf="isFieldInvalid('lastName', profileForm)">
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
                    [class.is-invalid]="isFieldInvalid('email', profileForm)">
                  <div class="invalid-feedback" *ngIf="isFieldInvalid('email', profileForm)">
                    <span *ngIf="profileForm.get('email')?.errors?.['required']">Email is required</span>
                    <span *ngIf="profileForm.get('email')?.errors?.['email']">Please enter a valid email</span>
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

                <div class="form-actions">
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="profileForm.invalid || isUpdatingProfile">
                    <span *ngIf="isUpdatingProfile" class="spinner-border spinner-border-sm" role="status"></span>
                    {{ isUpdatingProfile ? 'Updating...' : 'Update Profile' }}
                  </button>
                </div>
              </form>
            </div>

            <!-- Shipping Address Tab -->
            <div class="tab-content" *ngIf="activeTab === 'shipping'">
              <div class="tab-header">
                <h2>Shipping Address</h2>
                <p>Manage your default shipping address for faster checkout.</p>
              </div>

              <form [formGroup]="shippingForm" (ngSubmit)="updateShipping()" class="settings-form">
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

                <div class="form-actions">
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="shippingForm.invalid || isUpdatingShipping">
                    <span *ngIf="isUpdatingShipping" class="spinner-border spinner-border-sm" role="status"></span>
                    {{ isUpdatingShipping ? 'Updating...' : 'Update Address' }}
                  </button>
                </div>
              </form>
            </div>

            <!-- Change Password Tab -->
            <div class="tab-content" *ngIf="activeTab === 'password'">
              <div class="tab-header">
                <h2>Change Password</h2>
                <p>Update your password to keep your account secure.</p>
              </div>

              <form [formGroup]="passwordForm" (ngSubmit)="changePassword()" class="settings-form">
                <div class="form-group">
                  <label for="currentPassword">Current Password *</label>
                  <div class="password-input">
                    <input
                      id="currentPassword"
                      [type]="showCurrentPassword ? 'text' : 'password'"
                      formControlName="currentPassword"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('currentPassword', passwordForm)">
                    <button
                      type="button"
                      class="password-toggle"
                      (click)="toggleCurrentPasswordVisibility()">
                      {{ showCurrentPassword ? '👁️' : '👁️‍🗨️' }}
                    </button>
                  </div>
                  <div class="invalid-feedback" *ngIf="isFieldInvalid('currentPassword', passwordForm)">
                    Current password is required
                  </div>
                </div>

                <div class="form-group">
                  <label for="newPassword">New Password *</label>
                  <div class="password-input">
                    <input
                      id="newPassword"
                      [type]="showNewPassword ? 'text' : 'password'"
                      formControlName="newPassword"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('newPassword', passwordForm)">
                    <button
                      type="button"
                      class="password-toggle"
                      (click)="toggleNewPasswordVisibility()">
                      {{ showNewPassword ? '👁️' : '👁️‍🗨️' }}
                    </button>
                  </div>
                  <div class="invalid-feedback" *ngIf="isFieldInvalid('newPassword', passwordForm)">
                    <span *ngIf="passwordForm.get('newPassword')?.errors?.['required']">New password is required</span>
                    <span *ngIf="passwordForm.get('newPassword')?.errors?.['minlength']">Password must be at least 8 characters</span>
                  </div>
                  <small class="form-text text-muted">Password must be at least 8 characters long</small>
                </div>

                <div class="form-group">
                  <label for="confirmPassword">Confirm New Password *</label>
                  <div class="password-input">
                    <input
                      id="confirmPassword"
                      [type]="showConfirmPassword ? 'text' : 'password'"
                      formControlName="confirmPassword"
                      class="form-control"
                      [class.is-invalid]="isFieldInvalid('confirmPassword', passwordForm) || passwordForm.hasError('passwordMismatch')">
                    <button
                      type="button"
                      class="password-toggle"
                      (click)="toggleConfirmPasswordVisibility()">
                      {{ showConfirmPassword ? '👁️' : '👁️‍🗨️' }}
                    </button>
                  </div>
                  <div class="invalid-feedback" *ngIf="isFieldInvalid('confirmPassword', passwordForm) || passwordForm.hasError('passwordMismatch')">
                    <span *ngIf="passwordForm.get('confirmPassword')?.errors?.['required']">Please confirm your password</span>
                    <span *ngIf="passwordForm.hasError('passwordMismatch')">Passwords do not match</span>
                  </div>
                </div>

                <div class="form-actions">
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="passwordForm.invalid || isChangingPassword">
                    <span *ngIf="isChangingPassword" class="spinner-border spinner-border-sm" role="status"></span>
                    {{ isChangingPassword ? 'Changing Password...' : 'Change Password' }}
                  </button>
                </div>
              </form>
            </div>

            <!-- Preferences Tab -->
            <div class="tab-content" *ngIf="activeTab === 'preferences'">
              <div class="tab-header">
                <h2>Preferences</h2>
                <p>Customize your shopping experience and notification preferences.</p>
              </div>

              <form [formGroup]="preferencesForm" (ngSubmit)="updatePreferences()" class="settings-form">
                <div class="preference-section">
                  <h3>Email Notifications</h3>

                  <div class="form-check">
                    <input
                      id="orderUpdates"
                      type="checkbox"
                      formControlName="orderUpdates"
                      class="form-check-input">
                    <label for="orderUpdates" class="form-check-label">
                      Order status updates
                    </label>
                    <small class="form-text">Receive emails when your order status changes</small>
                  </div>

                  <div class="form-check">
                    <input
                      id="promotions"
                      type="checkbox"
                      formControlName="promotions"
                      class="form-check-input">
                    <label for="promotions" class="form-check-label">
                      Promotions and deals
                    </label>
                    <small class="form-text">Get notified about sales and special offers</small>
                  </div>

                  <div class="form-check">
                    <input
                      id="newArrivals"
                      type="checkbox"
                      formControlName="newArrivals"
                      class="form-check-input">
                    <label for="newArrivals" class="form-check-label">
                      New arrivals
                    </label>
                    <small class="form-text">Be first to know about new items</small>
                  </div>

                  <div class="form-check">
                    <input
                      id="priceDrops"
                      type="checkbox"
                      formControlName="priceDrops"
                      class="form-check-input">
                    <label for="priceDrops" class="form-check-label">
                      Price drop alerts
                    </label>
                    <small class="form-text">Get notified when favorited items go on sale</small>
                  </div>
                </div>

                <div class="preference-section">
                  <h3>Privacy</h3>

                  <div class="form-check">
                    <input
                      id="profilePublic"
                      type="checkbox"
                      formControlName="profilePublic"
                      class="form-check-input">
                    <label for="profilePublic" class="form-check-label">
                      Make my profile public
                    </label>
                    <small class="form-text">Allow other shoppers to see your public wishlist</small>
                  </div>

                  <div class="form-check">
                    <input
                      id="shareData"
                      type="checkbox"
                      formControlName="shareData"
                      class="form-check-input">
                    <label for="shareData" class="form-check-label">
                      Share usage data for improvements
                    </label>
                    <small class="form-text">Help us improve your shopping experience</small>
                  </div>
                </div>

                <div class="form-actions">
                  <button
                    type="submit"
                    class="btn btn-primary"
                    [disabled]="isUpdatingPreferences">
                    <span *ngIf="isUpdatingPreferences" class="spinner-border spinner-border-sm" role="status"></span>
                    {{ isUpdatingPreferences ? 'Updating...' : 'Update Preferences' }}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        <div class="danger-zone">
          <div class="danger-card">
            <h3>Account Deletion</h3>
            <p>
              Permanently delete your account and all associated data. This action cannot be undone.
            </p>
            <button class="btn btn-danger" (click)="requestAccountDeletion()">
              Delete Account
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .settings-container {
      min-height: 80vh;
      padding: 2rem 0;
      background-color: #f8f9fa;
    }

    .container {
      max-width: 1000px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .page-header {
      text-align: center;
      margin-bottom: 3rem;
    }

    .page-header h1 {
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

    .settings-content {
      display: grid;
      grid-template-columns: 250px 1fr;
      gap: 2rem;
      margin-bottom: 3rem;
    }

    .settings-nav {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 1rem;
      height: fit-content;
    }

    .nav-pills {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .nav-item {
      background: none;
      border: none;
      padding: 0.75rem 1rem;
      border-radius: 0.375rem;
      cursor: pointer;
      text-align: left;
      color: #6c757d;
      font-size: 0.9rem;
      transition: all 0.2s;
    }

    .nav-item:hover {
      background-color: #e9ecef;
      color: #343a40;
    }

    .nav-item.active {
      background-color: #007bff;
      color: white;
    }

    .settings-panel {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .tab-content {
      padding: 2rem;
    }

    .tab-header {
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid #dee2e6;
    }

    .tab-header h2 {
      color: #343a40;
      margin-bottom: 0.5rem;
      font-size: 1.5rem;
    }

    .tab-header p {
      color: #6c757d;
      margin: 0;
    }

    .settings-form {
      max-width: 600px;
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

    .form-text {
      display: block;
      margin-top: 0.25rem;
      font-size: 0.8rem;
      color: #6c757d;
    }

    .text-muted {
      color: #6c757d !important;
    }

    .preference-section {
      margin-bottom: 2rem;
      padding-bottom: 2rem;
      border-bottom: 1px solid #dee2e6;
    }

    .preference-section:last-child {
      border-bottom: none;
      margin-bottom: 0;
      padding-bottom: 0;
    }

    .preference-section h3 {
      color: #343a40;
      font-size: 1.25rem;
      margin-bottom: 1.5rem;
    }

    .form-check {
      display: flex;
      align-items: flex-start;
      margin-bottom: 1rem;
      gap: 0.75rem;
    }

    .form-check-input {
      margin-top: 0.125rem;
      flex-shrink: 0;
    }

    .form-check-label {
      font-weight: 500;
      color: #343a40;
      cursor: pointer;
    }

    .form-check .form-text {
      margin-top: 0.25rem;
      margin-left: 0;
    }

    .form-actions {
      padding-top: 1.5rem;
      border-top: 1px solid #dee2e6;
      margin-top: 2rem;
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

    .btn-danger {
      background-color: #dc3545;
      border-color: #dc3545;
      color: white;
    }

    .btn-danger:hover {
      background-color: #c82333;
      border-color: #bd2130;
    }

    .btn:disabled {
      opacity: 0.65;
      cursor: not-allowed;
    }

    .spinner-border-sm {
      width: 1rem;
      height: 1rem;
      border-width: 0.125em;
    }

    .danger-zone {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .danger-card {
      padding: 2rem;
      border-left: 4px solid #dc3545;
    }

    .danger-card h3 {
      color: #dc3545;
      margin-bottom: 1rem;
    }

    .danger-card p {
      color: #6c757d;
      margin-bottom: 1.5rem;
    }

    @media (max-width: 968px) {
      .settings-content {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .nav-pills {
        flex-direction: row;
        overflow-x: auto;
        gap: 0.25rem;
      }

      .nav-item {
        white-space: nowrap;
        flex-shrink: 0;
      }

      .form-row {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .tab-content {
        padding: 1.5rem;
      }

      .settings-form {
        max-width: none;
      }

      .danger-card {
        padding: 1.5rem;
      }
    }

    @media (max-width: 480px) {
      .page-header h1 {
        font-size: 2rem;
      }

      .tab-content {
        padding: 1rem;
      }

      .nav-pills {
        flex-direction: column;
      }
    }
  `]
})
export class AccountSettingsComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  activeTab: 'profile' | 'shipping' | 'password' | 'preferences' = 'profile';

  profileForm: FormGroup;
  shippingForm: FormGroup;
  passwordForm: FormGroup;
  preferencesForm: FormGroup;

  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  isUpdatingProfile = false;
  isUpdatingShipping = false;
  isChangingPassword = false;
  isUpdatingPreferences = false;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['']
    });

    this.shippingForm = this.fb.group({
      address: ['', [Validators.required]],
      apartment: [''],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      zipCode: ['', [Validators.required]]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.preferencesForm = this.fb.group({
      orderUpdates: [true],
      promotions: [false],
      newArrivals: [false],
      priceDrops: [true],
      profilePublic: [false],
      shareData: [true]
    });
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    this.loadUserProfile();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setActiveTab(tab: 'profile' | 'shipping' | 'password' | 'preferences'): void {
    this.activeTab = tab;
  }

  toggleCurrentPasswordVisibility(): void {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  toggleNewPasswordVisibility(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  updateProfile(): void {
    if (this.profileForm.invalid) {
      this.markFormGroupTouched(this.profileForm);
      return;
    }

    this.isUpdatingProfile = true;

    // TODO: Call API to update profile
    setTimeout(() => {
      this.isUpdatingProfile = false;
      alert('Profile updated successfully!');
    }, 1500);
  }

  updateShipping(): void {
    if (this.shippingForm.invalid) {
      this.markFormGroupTouched(this.shippingForm);
      return;
    }

    this.isUpdatingShipping = true;

    // TODO: Call API to update shipping address
    setTimeout(() => {
      this.isUpdatingShipping = false;
      alert('Shipping address updated successfully!');
    }, 1500);
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.markFormGroupTouched(this.passwordForm);
      return;
    }

    this.isChangingPassword = true;

    // TODO: Call API to change password
    setTimeout(() => {
      this.isChangingPassword = false;
      this.passwordForm.reset();
      alert('Password changed successfully!');
    }, 1500);
  }

  updatePreferences(): void {
    this.isUpdatingPreferences = true;

    // TODO: Call API to update preferences
    setTimeout(() => {
      this.isUpdatingPreferences = false;
      alert('Preferences updated successfully!');
    }, 1500);
  }

  requestAccountDeletion(): void {
    const confirmed = confirm(
      'Are you sure you want to delete your account? This action cannot be undone and will permanently remove all your data.'
    );

    if (confirmed) {
      const finalConfirm = confirm(
        'This is your final warning. Deleting your account will remove all orders, favorites, and personal information. Are you absolutely sure?'
      );

      if (finalConfirm) {
        // TODO: Implement account deletion
        alert('Account deletion request submitted. You will receive a confirmation email shortly.');
      }
    }
  }

  isFieldInvalid(fieldName: string, form: FormGroup): boolean {
    const field = form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  private passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  private loadUserProfile(): void {
    // TODO: Load user profile data from API
    // For now, we'll set some sample data
    this.profileForm.patchValue({
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@example.com',
      phone: '(555) 123-4567'
    });

    this.shippingForm.patchValue({
      address: '123 Main St',
      apartment: 'Apt 2B',
      city: 'Springfield',
      state: 'IL',
      zipCode: '62701'
    });
  }
}