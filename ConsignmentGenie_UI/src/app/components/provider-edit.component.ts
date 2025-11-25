import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ProviderService } from '../services/provider.service';
import { Provider, UpdateProviderRequest } from '../models/provider.model';

@Component({
  selector: 'app-provider-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="provider-edit-container">
      <div class="edit-header">
        <div class="breadcrumb">
          <a [routerLink]="['/providers', providerId()]">← Back to Provider</a>
        </div>
        <h1>Edit Provider</h1>
      </div>

      <div class="edit-card" *ngIf="!isLoading(); else loading">
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
                  [(ngModel)]="editData.name"
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
                  [(ngModel)]="editData.email"
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
                  [(ngModel)]="editData.phone"
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
                  [(ngModel)]="editData.address"
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
                  [(ngModel)]="editData.commissionRate"
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
                  [(ngModel)]="editData.preferredPaymentMethod"
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
                [(ngModel)]="editData.paymentDetails"
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
                [(ngModel)]="editData.notes"
                class="form-control"
                rows="4"
                placeholder="Add any additional notes about this provider..."
              ></textarea>
            </div>

            <div class="form-group">
              <label>
                <input
                  type="checkbox"
                  [(ngModel)]="editData.isActive"
                  name="isActive"
                >
                Active Provider
              </label>
              <small class="form-text">Inactive providers cannot add new items or receive payouts</small>
            </div>
          </div>

          <div class="form-actions">
            <button type="button" class="btn-secondary" [routerLink]="['/providers', providerId()]">
              Cancel
            </button>
            <button
              type="submit"
              class="btn-primary"
              [disabled]="providerForm.invalid || isSubmitting()"
            >
              {{ isSubmitting() ? 'Saving...' : 'Save Changes' }}
            </button>
          </div>

          <div class="success-message" *ngIf="successMessage()">
            {{ successMessage() }}
          </div>

          <div class="error-message" *ngIf="errorMessage()">
            {{ errorMessage() }}
          </div>
        </form>
      </div>

      <ng-template #loading>
        <div class="loading">Loading provider...</div>
      </ng-template>
    </div>
  `,
  styles: [`
    .provider-edit-container {
      padding: 1.5rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .edit-header {
      margin-bottom: 2rem;
    }

    .breadcrumb a {
      color: #007bff;
      text-decoration: none;
      font-size: 0.875rem;
    }

    .edit-header h1 {
      margin: 0.5rem 0 0 0;
      color: #212529;
    }

    .edit-card {
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

    label input[type="checkbox"] {
      margin-right: 0.5rem;
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

    .btn-primary, .btn-secondary {
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

    .loading {
      text-align: center;
      padding: 2rem;
      color: #6c757d;
    }
  `]
})
export class ProviderEditComponent implements OnInit {
  providerId = signal<number>(0);
  isLoading = signal(true);
  isSubmitting = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  editData = {
    name: '',
    email: '',
    phone: '',
    address: '',
    commissionRate: 50,
    preferredPaymentMethod: '',
    paymentDetails: '',
    notes: '',
    isActive: true
  };

  constructor(
    private providerService: ProviderService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.providerId.set(parseInt(id));
      this.loadProvider();
    }
  }

  loadProvider(): void {
    this.isLoading.set(true);
    this.providerService.getProvider(this.providerId()).subscribe({
      next: (provider) => {
        this.populateEditData(provider);
      },
      error: (error) => {
        console.error('Error loading provider:', error);
        this.errorMessage.set('Failed to load provider details');
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  populateEditData(provider: Provider): void {
    this.editData = {
      name: provider.name,
      email: provider.email,
      phone: provider.phone || '',
      address: provider.address || '',
      commissionRate: provider.commissionRate,
      preferredPaymentMethod: provider.preferredPaymentMethod || '',
      paymentDetails: provider.paymentDetails || '',
      notes: provider.notes || '',
      isActive: provider.isActive
    };
  }

  onSubmit(): void {
    if (this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const updateRequest: UpdateProviderRequest = {
      name: this.editData.name,
      email: this.editData.email,
      phone: this.editData.phone || undefined,
      address: this.editData.address || undefined,
      commissionRate: this.editData.commissionRate,
      preferredPaymentMethod: this.editData.preferredPaymentMethod || undefined,
      paymentDetails: this.editData.paymentDetails || undefined,
      notes: this.editData.notes || undefined,
      isActive: this.editData.isActive
    };

    this.providerService.updateProvider(this.providerId(), updateRequest).subscribe({
      next: (updated) => {
        this.successMessage.set('Provider updated successfully!');
        // Auto-redirect after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/providers', this.providerId()]);
        }, 2000);
      },
      error: (error) => {
        console.error('Error updating provider:', error);
        const errorMsg = error.error?.message || 'Failed to update provider. Please try again.';
        this.errorMessage.set(errorMsg);
      },
      complete: () => {
        this.isSubmitting.set(false);
      }
    });
  }
}