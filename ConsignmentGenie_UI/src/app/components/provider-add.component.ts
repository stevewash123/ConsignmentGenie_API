import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ProviderService } from '../services/provider.service';
import { CreateProviderRequest } from '../models/provider.model';

@Component({
  selector: 'app-provider-add',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="provider-add-container">
      <div class="add-header">
        <div class="breadcrumb">
          <a routerLink="/providers">← Back to Providers</a>
        </div>
        <h1>Add New Provider</h1>
        <p class="subtitle">Create a new provider account or consider using <strong>Invite Provider</strong> to send them a registration link instead.</p>
      </div>

      <div class="add-card">
        <form (ngSubmit)="onSubmit()" #providerForm="ngForm">
          <div class="form-section">
            <h3>Basic Information</h3>
            <div class="form-row">
              <div class="form-group">
                <label for="name">Provider Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  [(ngModel)]="providerData.name"
                  required
                  #nameField="ngModel"
                  class="form-control"
                  [class.error]="nameField.invalid && nameField.touched"
                  placeholder="Enter provider name"
                >
                <div class="error-message" *ngIf="nameField.invalid && nameField.touched">
                  Provider name is required
                </div>
              </div>

              <div class="form-group">
                <label for="email">Email Address *</label>
                <input
                  type="email"
                  id="email"
                  name="email"
                  [(ngModel)]="providerData.email"
                  required
                  email
                  #emailField="ngModel"
                  class="form-control"
                  [class.error]="emailField.invalid && emailField.touched"
                  placeholder="Enter email address"
                >
                <div class="error-message" *ngIf="emailField.invalid && emailField.touched">
                  <span *ngIf="emailField.errors?.['required']">Email is required</span>
                  <span *ngIf="emailField.errors?.['email']">Please enter a valid email</span>
                </div>
              </div>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label for="phone">Phone Number</label>
                <input
                  type="tel"
                  id="phone"
                  name="phone"
                  [(ngModel)]="providerData.phone"
                  class="form-control"
                  placeholder="(555) 123-4567"
                >
              </div>

              <div class="form-group">
                <label for="address">Address</label>
                <input
                  type="text"
                  id="address"
                  name="address"
                  [(ngModel)]="providerData.address"
                  class="form-control"
                  placeholder="Enter address"
                >
              </div>
            </div>
          </div>

          <div class="form-section">
            <h3>Business Settings</h3>
            <div class="form-row">
              <div class="form-group">
                <label for="commissionRate">Commission Rate (%) *</label>
                <input
                  type="number"
                  id="commissionRate"
                  name="commissionRate"
                  [(ngModel)]="providerData.commissionRate"
                  required
                  min="0"
                  max="100"
                  step="0.1"
                  #commissionField="ngModel"
                  class="form-control"
                  [class.error]="commissionField.invalid && commissionField.touched"
                  placeholder="50.0"
                >
                <div class="error-message" *ngIf="commissionField.invalid && commissionField.touched">
                  <span *ngIf="commissionField.errors?.['required']">Commission rate is required</span>
                  <span *ngIf="commissionField.errors?.['min']">Rate must be 0 or higher</span>
                  <span *ngIf="commissionField.errors?.['max']">Rate must be 100 or lower</span>
                </div>
              </div>

              <div class="form-group">
                <label for="preferredPaymentMethod">Preferred Payment Method</label>
                <select
                  id="preferredPaymentMethod"
                  name="preferredPaymentMethod"
                  [(ngModel)]="providerData.preferredPaymentMethod"
                  class="form-control"
                >
                  <option value="">Select payment method</option>
                  <option value="Check">Check</option>
                  <option value="Bank Transfer">Bank Transfer</option>
                  <option value="PayPal">PayPal</option>
                  <option value="Cash">Cash</option>
                  <option value="Other">Other</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label for="paymentDetails">Payment Details</label>
              <textarea
                id="paymentDetails"
                name="paymentDetails"
                [(ngModel)]="providerData.paymentDetails"
                class="form-control"
                rows="3"
                placeholder="Enter payment details (e.g., PayPal email, bank account info, mailing address)"
              ></textarea>
              <small class="form-text">This information is used for processing payouts</small>
            </div>
          </div>

          <div class="form-section">
            <h3>Additional Information</h3>
            <div class="form-group">
              <label for="notes">Notes</label>
              <textarea
                id="notes"
                name="notes"
                [(ngModel)]="providerData.notes"
                class="form-control"
                rows="4"
                placeholder="Add any additional notes about this provider..."
              ></textarea>
            </div>
          </div>

          <div class="form-actions">
            <button type="button" class="btn-secondary" routerLink="/providers">
              Cancel
            </button>
            <button
              type="submit"
              class="btn-primary"
              [disabled]="providerForm.invalid || isSubmitting()"
            >
              {{ isSubmitting() ? 'Creating...' : 'Create Provider' }}
            </button>
          </div>

          <div class="success-message" *ngIf="successMessage()">
            {{ successMessage() }}
          </div>

          <div class="error-message" *ngIf="errorMessage()">
            {{ errorMessage() }}
          </div>
        </form>

        <div class="alternative-note">
          <h4>💡 Tip: Consider using "Invite Provider" instead</h4>
          <p>
            Instead of manually creating accounts, you can invite providers to register themselves.
            This ensures they have access to their login credentials and receive welcome emails.
          </p>
          <button class="btn-outline" routerLink="/providers">
            Go back and use "Invite Provider"
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .provider-add-container {
      padding: 1.5rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .add-header {
      margin-bottom: 2rem;
    }

    .breadcrumb a {
      color: #007bff;
      text-decoration: none;
      font-size: 0.875rem;
    }

    .add-header h1 {
      margin: 0.5rem 0;
      color: #212529;
    }

    .subtitle {
      color: #6c757d;
      margin-bottom: 0;
    }

    .add-card {
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 2rem;
    }

    .form-section {
      margin-bottom: 2rem;
      padding-bottom: 2rem;
      border-bottom: 1px solid #e9ecef;
    }

    .form-section:last-of-type {
      border-bottom: none;
    }

    .form-section h3 {
      margin-bottom: 1.5rem;
      color: #212529;
      font-size: 1.25rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    @media (max-width: 768px) {
      .form-row {
        grid-template-columns: 1fr;
      }
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 500;
      color: #212529;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #ced4da;
      border-radius: 4px;
      font-size: 1rem;
      transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
    }

    .form-control:focus {
      border-color: #007bff;
      outline: 0;
      box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
    }

    .form-control.error {
      border-color: #dc3545;
    }

    .form-text {
      display: block;
      margin-top: 0.25rem;
      font-size: 0.875rem;
      color: #6c757d;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
      padding-top: 2rem;
      border-top: 1px solid #e9ecef;
    }

    .btn-primary, .btn-secondary, .btn-outline {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      text-decoration: none;
      display: inline-block;
      text-align: center;
      font-size: 1rem;
      transition: all 0.15s ease-in-out;
    }

    .btn-primary {
      background: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #0056b3;
    }

    .btn-primary:disabled {
      background: #6c757d;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
    }

    .btn-secondary:hover {
      background: #545b62;
    }

    .btn-outline {
      background: transparent;
      color: #007bff;
      border: 1px solid #007bff;
    }

    .btn-outline:hover {
      background: #007bff;
      color: white;
    }

    .error-message {
      color: #dc3545;
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .success-message {
      color: #28a745;
      font-size: 0.875rem;
      margin-top: 1rem;
      padding: 0.75rem;
      background: #d4edda;
      border: 1px solid #c3e6cb;
      border-radius: 4px;
    }

    .alternative-note {
      margin-top: 2rem;
      padding: 1.5rem;
      background: #e7f3ff;
      border: 1px solid #b8daff;
      border-radius: 4px;
    }

    .alternative-note h4 {
      margin-bottom: 0.5rem;
      color: #004085;
    }

    .alternative-note p {
      margin-bottom: 1rem;
      color: #004085;
    }
  `]
})
export class ProviderAddComponent {
  isSubmitting = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  providerData: CreateProviderRequest = {
    name: '',
    email: '',
    phone: '',
    address: '',
    commissionRate: 50,
    preferredPaymentMethod: '',
    paymentDetails: '',
    notes: ''
  };

  constructor(
    private providerService: ProviderService,
    private router: Router
  ) {}

  onSubmit(): void {
    if (this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    // Clean up undefined values
    const request: CreateProviderRequest = {
      name: this.providerData.name,
      email: this.providerData.email,
      phone: this.providerData.phone || undefined,
      address: this.providerData.address || undefined,
      commissionRate: this.providerData.commissionRate,
      preferredPaymentMethod: this.providerData.preferredPaymentMethod || undefined,
      paymentDetails: this.providerData.paymentDetails || undefined,
      notes: this.providerData.notes || undefined
    };

    this.providerService.createProvider(request).subscribe({
      next: (created) => {
        this.successMessage.set('Provider created successfully!');
        // Auto-redirect after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/providers', created.id]);
        }, 2000);
      },
      error: (error) => {
        console.error('Error creating provider:', error);
        const errorMsg = error.error?.message || 'Failed to create provider. Please try again.';
        this.errorMessage.set(errorMsg);
      },
      complete: () => {
        this.isSubmitting.set(false);
      }
    });
  }
}