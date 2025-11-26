import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OwnerInvitationService } from '../../services/owner-invitation.service';
import { CreateOwnerInvitationRequest } from '../../models/owner-invitation.model';

@Component({
  selector: 'app-invite-owner-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (isOpen()) {
      <div class="modal-overlay" (click)="closeModal()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2>Invite New Shop Owner</h2>
            <button class="close-btn" (click)="closeModal()" type="button">×</button>
          </div>

          <form (ngSubmit)="onSubmit()" #inviteForm="ngForm">
            <div class="modal-body">
              @if (errorMessage()) {
                <div class="error-message">
                  <span class="error-icon">⚠️</span>
                  {{ errorMessage() }}
                </div>
              }

              @if (successMessage()) {
                <div class="success-message">
                  <span class="success-icon">✅</span>
                  {{ successMessage() }}
                </div>
              }

              <div class="form-group">
                <label for="name">Full Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  [(ngModel)]="formData.name"
                  required
                  maxlength="100"
                  placeholder="Enter the owner's full name"
                  [disabled]="isLoading()"
                  #nameInput="ngModel"
                />
                @if (nameInput.invalid && nameInput.touched) {
                  <div class="field-error">Full name is required</div>
                }
              </div>

              <div class="form-group">
                <label for="email">Email Address *</label>
                <input
                  type="email"
                  id="email"
                  name="email"
                  [(ngModel)]="formData.email"
                  required
                  maxlength="254"
                  placeholder="Enter the owner's email address"
                  [disabled]="isLoading()"
                  #emailInput="ngModel"
                />
                @if (emailInput.invalid && emailInput.touched) {
                  <div class="field-error">
                    @if (emailInput.errors?.['required']) {
                      Email address is required
                    } @else if (emailInput.errors?.['email']) {
                      Please enter a valid email address
                    }
                  </div>
                }
              </div>

              <div class="info-box">
                <div class="info-icon">💡</div>
                <div class="info-content">
                  <strong>What happens next?</strong>
                  <ul>
                    <li>An invitation email will be sent to the provided address</li>
                    <li>The invitation expires in 7 days</li>
                    <li>The owner can complete their registration and shop setup</li>
                    <li>You can track the invitation status and resend if needed</li>
                  </ul>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="cancel-btn" (click)="closeModal()" [disabled]="isLoading()">
                Cancel
              </button>
              <button
                type="submit"
                class="invite-btn"
                [disabled]="inviteForm.invalid || isLoading()"
              >
                @if (isLoading()) {
                  <span class="spinner"></span>
                  Sending Invitation...
                } @else {
                  📧 Send Invitation
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .modal-content {
      background: white;
      border-radius: 12px;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
      width: 100%;
      max-width: 500px;
      max-height: 90vh;
      overflow-y: auto;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .modal-header h2 {
      margin: 0;
      color: #1f2937;
      font-size: 1.25rem;
      font-weight: 600;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #6b7280;
      padding: 0.25rem;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .close-btn:hover {
      color: #374151;
      background: #f3f4f6;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      color: #374151;
      font-weight: 500;
      font-size: 0.875rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
      transition: border-color 0.2s, box-shadow 0.2s;
    }

    .form-group input:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .form-group input:disabled {
      background: #f9fafb;
      cursor: not-allowed;
    }

    .field-error {
      color: #dc2626;
      font-size: 0.75rem;
      margin-top: 0.25rem;
    }

    .error-message, .success-message {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem;
      border-radius: 6px;
      margin-bottom: 1rem;
      font-size: 0.875rem;
    }

    .error-message {
      background: #fee2e2;
      color: #dc2626;
      border: 1px solid #fecaca;
    }

    .success-message {
      background: #dcfce7;
      color: #166534;
      border: 1px solid #bbf7d0;
    }

    .info-box {
      background: #eff6ff;
      border: 1px solid #c7d2fe;
      border-radius: 6px;
      padding: 1rem;
      display: flex;
      gap: 0.75rem;
      margin-top: 1rem;
    }

    .info-icon {
      font-size: 1.25rem;
      flex-shrink: 0;
    }

    .info-content {
      font-size: 0.875rem;
      color: #1e40af;
    }

    .info-content strong {
      display: block;
      margin-bottom: 0.5rem;
    }

    .info-content ul {
      margin: 0;
      padding-left: 1.25rem;
    }

    .info-content li {
      margin-bottom: 0.25rem;
    }

    .modal-footer {
      padding: 1.5rem;
      border-top: 1px solid #e5e7eb;
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
    }

    .cancel-btn, .invite-btn {
      padding: 0.75rem 1.5rem;
      border-radius: 6px;
      font-weight: 500;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .cancel-btn {
      background: #f9fafb;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .cancel-btn:hover:not(:disabled) {
      background: #f3f4f6;
    }

    .invite-btn {
      background: #3b82f6;
      color: white;
    }

    .invite-btn:hover:not(:disabled) {
      background: #2563eb;
    }

    .invite-btn:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .cancel-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid transparent;
      border-top: 2px solid currentColor;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class InviteOwnerModalComponent {
  @Input() isOpen = signal(false);
  @Output() invitationSent = new EventEmitter<void>();
  @Output() modalClosed = new EventEmitter<void>();

  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  formData: CreateOwnerInvitationRequest = {
    name: '',
    email: ''
  };

  constructor(private ownerInvitationService: OwnerInvitationService) {}

  closeModal() {
    if (!this.isLoading()) {
      this.resetForm();
      this.modalClosed.emit();
    }
  }

  onSubmit() {
    if (this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ownerInvitationService.createInvitation(this.formData).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success) {
          this.successMessage.set('Invitation sent successfully! The owner will receive an email with registration instructions.');
          this.invitationSent.emit();
          // Close modal after a short delay
          setTimeout(() => {
            this.closeModal();
          }, 2000);
        } else {
          this.errorMessage.set(response.message || 'Failed to send invitation. Please try again.');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.error?.message || 'An error occurred while sending the invitation. Please try again.');
      }
    });
  }

  private resetForm() {
    this.formData = {
      name: '',
      email: ''
    };
    this.errorMessage.set('');
    this.successMessage.set('');
    this.isLoading.set(false);
  }
}