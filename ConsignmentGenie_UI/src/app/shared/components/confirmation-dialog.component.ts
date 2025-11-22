import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  ConfirmationDialogService,
  ConfirmationDialogData,
  ConfirmationResult
} from '../services/confirmation-dialog.service';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (showDialog()) {
      <div class="confirmation-overlay" (click)="onOverlayClick()">
        <div class="confirmation-dialog" (click)="$event.stopPropagation()" [@slideIn]>
          <div class="dialog-header" [class.destructive]="dialogData()?.isDestructive">
            <div class="dialog-icon">
              @if (dialogData()?.isDestructive) {
                <span class="warning-icon">⚠️</span>
              } @else {
                <span class="question-icon">❓</span>
              }
            </div>
            <h3 class="dialog-title">{{ dialogData()?.title }}</h3>
            <button class="dialog-close" (click)="onCancel()" type="button">✕</button>
          </div>

          <div class="dialog-body">
            <p class="dialog-message">{{ dialogData()?.message }}</p>

            @if (dialogData()?.showInput) {
              <div class="input-group">
                <label class="input-label">{{ dialogData()?.inputLabel }}</label>
                <textarea
                  [(ngModel)]="inputValue"
                  [placeholder]="dialogData()?.inputPlaceholder || ''"
                  class="dialog-input"
                  rows="3"
                  #textareaRef
                ></textarea>
              </div>
            }
          </div>

          <div class="dialog-actions">
            <button
              class="dialog-btn cancel-btn"
              (click)="onCancel()"
              type="button"
            >
              {{ dialogData()?.cancelButtonText }}
            </button>
            <button
              class="dialog-btn confirm-btn"
              [class.destructive]="dialogData()?.isDestructive"
              (click)="onConfirm()"
              type="button"
              [disabled]="isConfirming()"
            >
              @if (isConfirming()) {
                <span class="spinner"></span>
              }
              {{ dialogData()?.confirmButtonText }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .confirmation-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
      backdrop-filter: blur(2px);
      animation: fadeIn 0.2s ease-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .confirmation-dialog {
      background: white;
      border-radius: 12px;
      max-width: 500px;
      width: 90%;
      max-height: 90vh;
      overflow-y: auto;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      animation: slideIn 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
    }

    @keyframes slideIn {
      from {
        transform: scale(0.9) translateY(-20px);
        opacity: 0;
      }
      to {
        transform: scale(1) translateY(0);
        opacity: 1;
      }
    }

    .dialog-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.5rem 1.5rem 1rem 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      position: relative;
    }

    .dialog-header.destructive {
      border-bottom-color: #fecaca;
    }

    .dialog-icon {
      flex-shrink: 0;
    }

    .warning-icon {
      font-size: 1.5rem;
      filter: hue-rotate(0deg);
    }

    .question-icon {
      font-size: 1.5rem;
      filter: hue-rotate(200deg);
    }

    .dialog-title {
      flex: 1;
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: #111827;
    }

    .dialog-close {
      background: none;
      border: none;
      font-size: 1.25rem;
      color: #6b7280;
      cursor: pointer;
      padding: 0.5rem;
      border-radius: 4px;
      transition: all 0.2s;
      position: absolute;
      top: 0.75rem;
      right: 1rem;
    }

    .dialog-close:hover {
      background: #f3f4f6;
      color: #374151;
    }

    .dialog-body {
      padding: 1.5rem;
    }

    .dialog-message {
      margin: 0 0 1rem 0;
      color: #374151;
      line-height: 1.6;
    }

    .input-group {
      margin-top: 1.5rem;
    }

    .input-label {
      display: block;
      font-weight: 500;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .dialog-input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      font-family: inherit;
      font-size: 0.875rem;
      resize: vertical;
      transition: border-color 0.2s;
    }

    .dialog-input:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .dialog-actions {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      padding: 1rem 1.5rem 1.5rem 1.5rem;
      border-top: 1px solid #e5e7eb;
    }

    .dialog-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 8px;
      font-weight: 500;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;
      min-width: 80px;
    }

    .cancel-btn {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .cancel-btn:hover {
      background: #e5e7eb;
      border-color: #9ca3af;
    }

    .confirm-btn {
      background: #3b82f6;
      color: white;
    }

    .confirm-btn:hover:not(:disabled) {
      background: #2563eb;
    }

    .confirm-btn.destructive {
      background: #dc2626;
    }

    .confirm-btn.destructive:hover:not(:disabled) {
      background: #b91c1c;
    }

    .confirm-btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .spinner {
      display: inline-block;
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

    @media (max-width: 640px) {
      .confirmation-dialog {
        margin: 1rem;
        width: calc(100% - 2rem);
      }

      .dialog-header {
        padding: 1rem;
      }

      .dialog-body {
        padding: 1rem;
      }

      .dialog-actions {
        padding: 1rem;
        flex-direction: column;
      }

      .dialog-btn {
        width: 100%;
        justify-content: center;
      }
    }
  `],
  animations: []
})
export class ConfirmationDialogComponent implements OnInit, OnDestroy {
  showDialog = signal(false);
  dialogData = signal<ConfirmationDialogData | null>(null);
  isConfirming = signal(false);
  inputValue = '';

  private destroy$ = new Subject<void>();

  constructor(private confirmationService: ConfirmationDialogService) {}

  ngOnInit(): void {
    this.confirmationService.dialog$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(data => {
      this.dialogData.set(data);
      this.inputValue = data.inputValue || '';
      this.showDialog.set(true);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onOverlayClick(): void {
    this.onCancel();
  }

  onCancel(): void {
    this.showDialog.set(false);
    this.confirmationService.sendResult({ confirmed: false });
  }

  onConfirm(): void {
    this.isConfirming.set(true);

    // Small delay to show the loading state
    setTimeout(() => {
      const result: ConfirmationResult = {
        confirmed: true,
        inputValue: this.dialogData()?.showInput ? this.inputValue : undefined
      };

      this.showDialog.set(false);
      this.isConfirming.set(false);
      this.confirmationService.sendResult(result);
    }, 300);
  }
}