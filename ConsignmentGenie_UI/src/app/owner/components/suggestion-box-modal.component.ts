import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface SuggestionType {
  value: string;
  label: string;
}

export interface SuggestionFormData {
  type: string;
  message: string;
}

@Component({
  selector: 'app-suggestion-box-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-overlay" *ngIf="isVisible" (click)="onOverlayClick($event)">
      <div class="modal-content" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h2>💡 Suggestion Box</h2>
          <button class="close-button" (click)="onClose()">×</button>
        </div>

        <div class="modal-body">
          <p class="description">
            Help us improve ConsignmentGenie! Share your ideas, report issues, or suggest new features.
            Your feedback is valuable to us.
          </p>

          <form (ngSubmit)="onSubmit()" #suggestionForm="ngForm">
            <div class="form-group">
              <label for="suggestionType">Suggestion Type <span class="required">*</span></label>
              <select
                id="suggestionType"
                [(ngModel)]="formData().type"
                name="type"
                class="form-control"
                required>
                <option value="">Select a type...</option>
                <option *ngFor="let type of suggestionTypes" [value]="type.value">
                  {{ type.label }}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label for="suggestionMessage">Your Suggestion <span class="required">*</span></label>
              <textarea
                id="suggestionMessage"
                [(ngModel)]="formData().message"
                name="message"
                class="form-control"
                placeholder="Please describe your suggestion in detail (up to 500 words)..."
                rows="8"
                maxlength="2000"
                required>
              </textarea>
              <div class="character-count">
                {{ formData().message.length }}/2000 characters
              </div>
            </div>

            <div class="form-actions">
              <button type="button" class="btn btn-cancel" (click)="onClose()">Cancel</button>
              <button
                type="submit"
                class="btn btn-submit"
                [disabled]="!suggestionForm.valid || isSubmitting()"
                [class.loading]="isSubmitting()">
                {{ isSubmitting() ? 'Sending...' : 'Send Suggestion' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
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
      padding: 1rem;
    }

    .modal-content {
      background: white;
      border-radius: 12px;
      max-width: 600px;
      width: 100%;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem 2rem 1rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .modal-header h2 {
      margin: 0;
      color: #047857;
      font-size: 1.5rem;
      font-weight: 600;
    }

    .close-button {
      background: none;
      border: none;
      font-size: 2rem;
      color: #6b7280;
      cursor: pointer;
      padding: 0;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      transition: all 0.2s;
    }

    .close-button:hover {
      background: #f3f4f6;
      color: #374151;
    }

    .modal-body {
      padding: 1.5rem 2rem 2rem;
    }

    .description {
      color: #6b7280;
      margin-bottom: 1.5rem;
      line-height: 1.6;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
    }

    .required {
      color: #ef4444;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .form-control:focus {
      outline: none;
      border-color: #047857;
      box-shadow: 0 0 0 3px rgba(4, 120, 87, 0.1);
    }

    select.form-control {
      cursor: pointer;
    }

    textarea.form-control {
      resize: vertical;
      min-height: 120px;
      font-family: inherit;
    }

    .character-count {
      text-align: right;
      font-size: 0.75rem;
      color: #6b7280;
      margin-top: 0.25rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
    }

    .btn-cancel {
      background: #f3f4f6;
      color: #374151;
    }

    .btn-cancel:hover {
      background: #e5e7eb;
    }

    .btn-submit {
      background: #047857;
      color: white;
    }

    .btn-submit:hover:not(:disabled) {
      background: #059669;
    }

    .btn-submit:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-submit.loading {
      position: relative;
    }

    .btn-submit.loading::after {
      content: '';
      position: absolute;
      width: 16px;
      height: 16px;
      margin: auto;
      border: 2px solid transparent;
      border-top-color: white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    @media (max-width: 640px) {
      .modal-content {
        margin: 1rem;
        max-height: calc(100vh - 2rem);
      }

      .modal-header {
        padding: 1rem 1.5rem 0.75rem;
      }

      .modal-body {
        padding: 1rem 1.5rem 1.5rem;
      }

      .form-actions {
        flex-direction: column;
      }
    }
  `]
})
export class SuggestionBoxModalComponent {
  @Input() isVisible = false;
  @Output() close = new EventEmitter<void>();
  @Output() submit = new EventEmitter<SuggestionFormData>();

  isSubmitting = signal<boolean>(false);
  formData = signal<SuggestionFormData>({
    type: '',
    message: ''
  });

  suggestionTypes: SuggestionType[] = [
    { value: 'FeatureRequest', label: 'Feature Request' },
    { value: 'BugReport', label: 'Bug Report' },
    { value: 'Improvement', label: 'Improvement' },
    { value: 'Integration', label: 'Integration' },
    { value: 'UserExperience', label: 'User Experience' },
    { value: 'Performance', label: 'Performance' },
    { value: 'Documentation', label: 'Documentation' },
    { value: 'Other', label: 'Other' }
  ];

  onClose() {
    this.resetForm();
    this.close.emit();
  }

  onOverlayClick(event: Event) {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  async onSubmit() {
    if (!this.formData().type || !this.formData().message.trim()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      // Emit the suggestion data to parent
      this.submit.emit({
        type: this.formData().type,
        message: this.formData().message.trim()
      });

      // Simulate API call delay
      await new Promise(resolve => setTimeout(resolve, 1000));

      this.resetForm();
      this.close.emit();
    } catch (error) {
      console.error('Error submitting suggestion:', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private resetForm() {
    this.formData.set({
      type: '',
      message: ''
    });
    this.isSubmitting.set(false);
  }
}