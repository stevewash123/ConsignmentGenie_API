import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProviderService } from '../services/provider.service';

@Component({
  selector: 'app-provider-invitation-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-overlay" *ngIf="isVisible()" (click)="onOverlayClick($event)">
      <div class="modal" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>Invite Provider</h3>
          <button class="close-btn" (click)="close.emit()">×</button>
        </div>

        <div class="modal-body">
          <form (ngSubmit)="onSubmit()" #inviteForm="ngForm">
            <div class="form-group">
              <label for="providerName">Provider Name *</label>
              <input
                type="text"
                id="providerName"
                name="providerName"
                [(ngModel)]="invitation.name"
                required
                #nameField="ngModel"
                class="form-control"
                [class.error]="nameField.invalid && nameField.touched"
                placeholder="Enter provider's full name"
              >
              <div class="error-message" *ngIf="nameField.invalid && nameField.touched">
                Provider name is required
              </div>
            </div>

            <div class="form-group">
              <label for="providerEmail">Email Address *</label>
              <input
                type="email"
                id="providerEmail"
                name="providerEmail"
                [(ngModel)]="invitation.email"
                required
                email
                #emailField="ngModel"
                class="form-control"
                [class.error]="emailField.invalid && emailField.touched"
                placeholder="Enter provider's email"
              >
              <div class="error-message" *ngIf="emailField.invalid && emailField.touched">
                <span *ngIf="emailField.errors?.['required']">Email is required</span>
                <span *ngIf="emailField.errors?.['email']">Please enter a valid email</span>
              </div>
            </div>

            <div class="form-actions">
              <button type="button" class="btn-secondary" (click)="close.emit()">
                Cancel
              </button>
              <button
                type="submit"
                class="btn-primary"
                [disabled]="inviteForm.invalid || isSubmitting()"
              >
                {{ isSubmitting() ? 'Sending...' : 'Send Invitation' }}
              </button>
            </div>
          </form>

          <div class="success-message" *ngIf="successMessage()">
            {{ successMessage() }}
          </div>

          <div class="error-message" *ngIf="errorMessage()">
            {{ errorMessage() }}
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .modal {
      background: white;
      border-radius: 8px;
      max-width: 500px;
      width: 90%;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e9ecef;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #6c757d;
      padding: 0;
      width: 24px;
      height: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .close-btn:hover {
      color: #000;
    }

    .modal-body {
      padding: 1.5rem;
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

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
    }

    .btn-primary, .btn-secondary {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
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
  `]
})
export class ProviderInvitationModalComponent {
  @Input() isVisible!: () => boolean;
  @Output() close = new EventEmitter<void>();
  @Output() invitationSent = new EventEmitter<void>();

  invitation = {
    name: '',
    email: ''
  };

  isSubmitting = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  constructor(private providerService: ProviderService) {}

  onOverlayClick(event: Event): void {
    this.close.emit();
  }

  onSubmit(): void {
    if (this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.providerService.inviteProvider(this.invitation).subscribe({
      next: (response) => {
        this.successMessage.set('Invitation sent successfully!');
        this.resetForm();
        this.invitationSent.emit();

        // Auto-close after 2 seconds
        setTimeout(() => {
          this.close.emit();
        }, 2000);
      },
      error: (error) => {
        console.error('Error sending invitation:', error);
        const errorMsg = error.error?.message || 'Failed to send invitation. Please try again.';
        this.errorMessage.set(errorMsg);
      },
      complete: () => {
        this.isSubmitting.set(false);
      }
    });
  }

  private resetForm(): void {
    this.invitation = {
      name: '',
      email: ''
    };
  }
}