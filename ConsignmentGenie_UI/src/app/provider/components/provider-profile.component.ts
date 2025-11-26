import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, NgForm } from '@angular/forms';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderProfile, UpdateProviderProfile } from '../models/provider.models';

@Component({
  selector: 'app-provider-profile',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="provider-profile">
      <!-- Navigation Header -->
      <div class="nav-header">
        <div class="header-content">
          <h1>Main Street Consignment</h1>
          <nav class="nav-links">
            <a routerLink="/provider/dashboard">Dashboard</a>
          </nav>
        </div>
      </div>

      <div class="content" *ngIf="profile">
        <div class="page-header">
          <h2>My Profile</h2>
        </div>

        <form #profileForm="ngForm" (ngSubmit)="onSubmit(profileForm)" class="profile-form">
          <!-- Personal Information Section -->
          <div class="form-section">
            <h3>PERSONAL INFORMATION</h3>

            <div class="form-group">
              <label for="fullName">Full Name</label>
              <input
                type="text"
                id="fullName"
                name="fullName"
                [(ngModel)]="formData.fullName"
                required
                class="form-input">
            </div>

            <div class="form-group">
              <label for="email">Email</label>
              <input
                type="email"
                id="email"
                [value]="profile.email"
                readonly
                class="form-input readonly">
              <div class="field-note">(cannot change)</div>
            </div>

            <div class="form-group">
              <label for="phone">Phone</label>
              <input
                type="tel"
                id="phone"
                name="phone"
                [(ngModel)]="formData.phone"
                class="form-input"
                placeholder="(555) 123-4567">
            </div>
          </div>

          <!-- Payment Preferences Section -->
          <div class="form-section">
            <h3>PAYMENT PREFERENCES</h3>

            <div class="form-group">
              <label for="paymentMethod">Preferred Payment Method</label>
              <select
                id="paymentMethod"
                name="paymentMethod"
                [(ngModel)]="formData.preferredPaymentMethod"
                class="form-select">
                <option value="">Select payment method</option>
                <option value="Venmo">Venmo</option>
                <option value="Zelle">Zelle</option>
                <option value="PayPal">PayPal</option>
                <option value="Bank Transfer">Bank Transfer</option>
                <option value="Check">Check</option>
              </select>
            </div>

            <div class="form-group">
              <label for="paymentDetails">Payment Details</label>
              <input
                type="text"
                id="paymentDetails"
                name="paymentDetails"
                [(ngModel)]="formData.paymentDetails"
                class="form-input"
                placeholder="@username, email, account info, etc.">
              <div class="field-note">(Venmo username, Zelle email, etc.)</div>
            </div>
          </div>

          <!-- Notifications Section -->
          <div class="form-section">
            <h3>NOTIFICATIONS</h3>

            <div class="checkbox-group">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  name="emailNotifications"
                  [(ngModel)]="formData.emailNotifications"
                  class="checkbox">
                <span class="checkmark"></span>
                Email me when my items sell
              </label>
            </div>

            <div class="checkbox-group">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  name="payoutNotifications"
                  [(ngModel)]="payoutNotifications"
                  class="checkbox">
                <span class="checkmark"></span>
                Email me when a payout is processed
              </label>
            </div>
          </div>

          <!-- Account Info Section (Read-only) -->
          <div class="form-section readonly-section">
            <h3>ACCOUNT INFO (read only)</h3>

            <div class="readonly-grid">
              <div class="readonly-item">
                <div class="readonly-label">Commission Rate:</div>
                <div class="readonly-value">{{profile.commissionRate}}%</div>
              </div>

              <div class="readonly-item">
                <div class="readonly-label">Member Since:</div>
                <div class="readonly-value">{{formatMemberSince(profile.memberSince)}}</div>
              </div>

              <div class="readonly-item">
                <div class="readonly-label">Shop:</div>
                <div class="readonly-value">{{profile.organizationName}}</div>
              </div>
            </div>
          </div>

          <!-- Form Actions -->
          <div class="form-actions">
            <button
              type="button"
              class="btn-secondary"
              (click)="resetForm()">
              Cancel
            </button>
            <button
              type="submit"
              class="btn-primary"
              [disabled]="profileForm.invalid || saving">
              {{saving ? 'Saving...' : 'Save Changes'}}
            </button>
          </div>
        </form>

        <!-- Success Message -->
        <div class="success-message" *ngIf="successMessage">
          <div class="success-content">
            <span class="success-icon">✓</span>
            {{successMessage}}
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading">
        <p>Loading profile...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadProfile()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .provider-profile {
      min-height: 100vh;
      background: #f9fafb;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }

    .nav-header {
      background: white;
      border-bottom: 1px solid #e5e7eb;
      padding: 1rem 2rem;
    }

    .header-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      max-width: 800px;
      margin: 0 auto;
    }

    .header-content h1 {
      font-size: 1.5rem;
      font-weight: 600;
      color: #111827;
      margin: 0;
    }

    .nav-links a {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
    }

    .nav-links a:hover {
      color: #2563eb;
    }

    .content {
      max-width: 800px;
      margin: 0 auto;
      padding: 2rem;
    }

    .page-header h2 {
      font-size: 1.875rem;
      font-weight: 700;
      color: #111827;
      margin: 0 0 2rem 0;
    }

    .profile-form {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 2rem;
    }

    .form-section {
      margin-bottom: 2rem;
      padding-bottom: 2rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .form-section:last-of-type {
      border-bottom: none;
    }

    .form-section h3 {
      font-size: 1rem;
      font-weight: 600;
      color: #111827;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin: 0 0 1.5rem 0;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      font-weight: 500;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 0.375rem;
      background: white;
      color: #374151;
      font-size: 1rem;
    }

    .form-input:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 1px #3b82f6;
    }

    .form-input.readonly {
      background: #f9fafb;
      color: #6b7280;
      cursor: not-allowed;
    }

    .form-select {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 0.375rem;
      background: white;
      color: #374151;
      font-size: 1rem;
    }

    .form-select:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 1px #3b82f6;
    }

    .field-note {
      font-size: 0.875rem;
      color: #6b7280;
      margin-top: 0.25rem;
    }

    .checkbox-group {
      margin-bottom: 1rem;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      cursor: pointer;
      font-weight: 500;
      color: #374151;
    }

    .checkbox {
      width: 1.125rem;
      height: 1.125rem;
      border: 2px solid #d1d5db;
      border-radius: 0.25rem;
      background: white;
      cursor: pointer;
    }

    .checkbox:checked {
      background: #3b82f6;
      border-color: #3b82f6;
    }

    .readonly-section {
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 0.375rem;
      padding: 1.5rem;
      margin-bottom: 2rem;
    }

    .readonly-grid {
      display: grid;
      grid-template-columns: 1fr;
      gap: 1rem;
    }

    .readonly-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 0;
    }

    .readonly-label {
      font-weight: 500;
      color: #6b7280;
    }

    .readonly-value {
      font-weight: 600;
      color: #111827;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      margin-top: 2rem;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
      padding: 0.75rem 1.5rem;
      border-radius: 0.375rem;
      cursor: pointer;
      font-weight: 500;
    }

    .btn-secondary:hover {
      background: #e5e7eb;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.75rem 1.5rem;
      border-radius: 0.375rem;
      cursor: pointer;
      font-weight: 500;
    }

    .btn-primary:hover {
      background: #2563eb;
    }

    .btn-primary:disabled {
      background: #d1d5db;
      cursor: not-allowed;
    }

    .success-message {
      position: fixed;
      top: 2rem;
      right: 2rem;
      background: #10b981;
      color: white;
      padding: 1rem 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      z-index: 1000;
    }

    .success-content {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .success-icon {
      font-weight: bold;
    }

    .loading, .error {
      text-align: center;
      padding: 2rem;
    }

    .error button {
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      cursor: pointer;
      margin-top: 1rem;
    }

    @media (max-width: 768px) {
      .nav-header {
        padding: 1rem;
      }

      .content {
        padding: 1rem;
      }

      .profile-form {
        padding: 1.5rem;
      }

      .form-actions {
        flex-direction: column;
      }

      .form-actions button {
        width: 100%;
      }

      .success-message {
        top: 1rem;
        right: 1rem;
        left: 1rem;
      }
    }
  `]
})
export class ProviderProfileComponent implements OnInit {
  profile: ProviderProfile | null = null;
  loading = false;
  saving = false;
  error: string | null = null;
  successMessage: string | null = null;

  formData: UpdateProviderProfile = {
    fullName: '',
    phone: '',
    preferredPaymentMethod: '',
    paymentDetails: '',
    emailNotifications: true
  };

  payoutNotifications = true;

  constructor(private providerService: ProviderPortalService) {}

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.loading = true;
    this.error = null;

    this.providerService.getProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.populateForm(profile);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load profile. Please try again.';
        this.loading = false;
        console.error('Profile error:', err);
      }
    });
  }

  populateForm(profile: ProviderProfile) {
    this.formData = {
      fullName: profile.fullName,
      phone: profile.phone || '',
      preferredPaymentMethod: profile.preferredPaymentMethod || '',
      paymentDetails: profile.paymentDetails || '',
      emailNotifications: profile.emailNotifications
    };
  }

  onSubmit(form: NgForm) {
    if (form.valid) {
      this.saving = true;

      this.providerService.updateProfile(this.formData).subscribe({
        next: (updatedProfile) => {
          this.profile = updatedProfile;
          this.saving = false;
          this.showSuccessMessage('Profile updated successfully!');
        },
        error: (err) => {
          this.saving = false;
          this.error = 'Failed to update profile. Please try again.';
          console.error('Update error:', err);
        }
      });
    }
  }

  resetForm() {
    if (this.profile) {
      this.populateForm(this.profile);
    }
    this.error = null;
  }

  showSuccessMessage(message: string) {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = null;
    }, 3000);
  }

  formatMemberSince(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
}