import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ProviderPortalService } from '../services/provider-portal.service';
import {
  NotificationPreferencesDto,
  UpdateNotificationPreferencesRequest
} from '../models/provider.models';

@Component({
  selector: 'app-provider-notification-preferences',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="preferences-container">
      <!-- Header -->
      <div class="preferences-header">
        <div class="header-nav">
          <a routerLink="/provider/notifications" class="back-link">
            ← Back to Notifications
          </a>
        </div>
        <h1>Notification Preferences</h1>
        <p>Control how and when you receive notifications about your consignment activities.</p>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="loading-container">
        <div class="loading-spinner"></div>
        <p>Loading preferences...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="error" class="error-container">
        <div class="error-icon">⚠️</div>
        <h3>Unable to load preferences</h3>
        <p>{{ error }}</p>
        <button class="btn btn-primary" (click)="loadPreferences()">Try Again</button>
      </div>

      <!-- Preferences Form -->
      <form *ngIf="!loading && !error && preferencesForm" [formGroup]="preferencesForm" (ngSubmit)="savePreferences()">

        <!-- Email Notifications Section -->
        <div class="preferences-section">
          <h2>Email Notifications</h2>
          <p class="section-description">Choose which notifications you'd like to receive via email.</p>

          <div class="preference-group">
            <!-- Master Email Toggle -->
            <div class="preference-item master-toggle">
              <div class="preference-content">
                <label for="emailEnabled" class="preference-label">
                  <strong>Enable Email Notifications</strong>
                </label>
                <p class="preference-description">
                  Turn email notifications on or off completely. When disabled, you'll only receive in-app notifications.
                </p>
              </div>
              <div class="preference-control">
                <label class="toggle-switch">
                  <input
                    type="checkbox"
                    id="emailEnabled"
                    formControlName="emailEnabled"
                    (change)="onMasterEmailToggle()">
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>

            <!-- Individual Email Preferences -->
            <div class="email-preferences" [class.disabled]="!preferencesForm.get('emailEnabled')?.value">

              <div class="preference-item">
                <div class="preference-content">
                  <label for="emailItemSold" class="preference-label">Item Sold 💰</label>
                  <p class="preference-description">Get notified when one of your items sells</p>
                </div>
                <div class="preference-control">
                  <label class="toggle-switch">
                    <input
                      type="checkbox"
                      id="emailItemSold"
                      formControlName="emailItemSold"
                      [disabled]="!preferencesForm.get('emailEnabled')?.value">
                    <span class="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div class="preference-item">
                <div class="preference-content">
                  <label for="emailPayoutProcessed" class="preference-label">Payout Processed 💳</label>
                  <p class="preference-description">Get notified when a payout has been processed</p>
                </div>
                <div class="preference-control">
                  <label class="toggle-switch">
                    <input
                      type="checkbox"
                      id="emailPayoutProcessed"
                      formControlName="emailPayoutProcessed"
                      [disabled]="!preferencesForm.get('emailEnabled')?.value">
                    <span class="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div class="preference-item">
                <div class="preference-content">
                  <label for="emailPayoutPending" class="preference-label">Payout Pending ⏳</label>
                  <p class="preference-description">Get notified when you have earnings ready for payout</p>
                </div>
                <div class="preference-control">
                  <label class="toggle-switch">
                    <input
                      type="checkbox"
                      id="emailPayoutPending"
                      formControlName="emailPayoutPending"
                      [disabled]="!preferencesForm.get('emailEnabled')?.value">
                    <span class="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div class="preference-item">
                <div class="preference-content">
                  <label for="emailStatementReady" class="preference-label">Monthly Statement 📄</label>
                  <p class="preference-description">Get notified when your monthly statement is ready</p>
                </div>
                <div class="preference-control">
                  <label class="toggle-switch">
                    <input
                      type="checkbox"
                      id="emailStatementReady"
                      formControlName="emailStatementReady"
                      [disabled]="!preferencesForm.get('emailEnabled')?.value">
                    <span class="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div class="preference-item">
                <div class="preference-content">
                  <label for="emailAccountUpdate" class="preference-label">Account Updates 👤</label>
                  <p class="preference-description">Important updates about your account and policies</p>
                </div>
                <div class="preference-control">
                  <label class="toggle-switch">
                    <input
                      type="checkbox"
                      id="emailAccountUpdate"
                      formControlName="emailAccountUpdate"
                      [disabled]="!preferencesForm.get('emailEnabled')?.value">
                    <span class="toggle-slider"></span>
                  </label>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Digest Settings Section -->
        <div class="preferences-section">
          <h2>Digest Settings</h2>
          <p class="section-description">Control how often you receive email notifications.</p>

          <div class="preference-group">
            <div class="form-group">
              <label for="digestMode" class="form-label">Email Frequency</label>
              <select
                id="digestMode"
                formControlName="digestMode"
                class="form-select"
                [disabled]="!preferencesForm.get('emailEnabled')?.value">
                <option value="instant">Instant (as they happen)</option>
                <option value="daily">Daily Digest</option>
                <option value="weekly">Weekly Digest</option>
              </select>
              <div class="form-help">
                Choose how often you want to receive email notifications
              </div>
            </div>

            <div *ngIf="preferencesForm.get('digestMode')?.value === 'daily'" class="form-group">
              <label for="digestTime" class="form-label">Daily Digest Time</label>
              <input
                type="time"
                id="digestTime"
                formControlName="digestTime"
                class="form-input"
                [disabled]="!preferencesForm.get('emailEnabled')?.value">
              <div class="form-help">
                What time of day should we send your daily digest?
              </div>
            </div>

            <div *ngIf="preferencesForm.get('digestMode')?.value === 'weekly'" class="form-group">
              <label for="digestDay" class="form-label">Weekly Digest Day</label>
              <select
                id="digestDay"
                formControlName="digestDay"
                class="form-select"
                [disabled]="!preferencesForm.get('emailEnabled')?.value">
                <option value="1">Monday</option>
                <option value="2">Tuesday</option>
                <option value="3">Wednesday</option>
                <option value="4">Thursday</option>
                <option value="5">Friday</option>
                <option value="6">Saturday</option>
                <option value="0">Sunday</option>
              </select>
              <div class="form-help">
                What day of the week should we send your weekly digest?
              </div>
            </div>
          </div>
        </div>

        <!-- Payout Threshold Section -->
        <div class="preferences-section">
          <h2>Payout Alerts</h2>
          <p class="section-description">Get notified when your pending earnings reach a certain amount.</p>

          <div class="preference-group">
            <div class="form-group">
              <label for="payoutPendingThreshold" class="form-label">Pending Payout Threshold</label>
              <div class="input-group">
                <span class="input-group-text">$</span>
                <input
                  type="number"
                  id="payoutPendingThreshold"
                  formControlName="payoutPendingThreshold"
                  class="form-input"
                  min="0"
                  step="0.01"
                  [disabled]="!preferencesForm.get('emailEnabled')?.value || !preferencesForm.get('emailPayoutPending')?.value">
              </div>
              <div class="form-help">
                Get notified when your pending earnings reach this amount (minimum $10)
              </div>
              <div *ngIf="preferencesForm.get('payoutPendingThreshold')?.errors?.['min']" class="form-error">
                Minimum threshold is $10.00
              </div>
            </div>
          </div>
        </div>

        <!-- Save Actions -->
        <div class="save-actions">
          <button
            type="button"
            class="btn btn-secondary"
            (click)="resetForm()"
            [disabled]="!preferencesForm.dirty || saving">
            Reset Changes
          </button>
          <button
            type="submit"
            class="btn btn-primary"
            [disabled]="!preferencesForm.dirty || preferencesForm.invalid || saving">
            <span *ngIf="saving">Saving...</span>
            <span *ngIf="!saving">Save Preferences</span>
          </button>
        </div>

        <!-- Save Status -->
        <div *ngIf="saveMessage" class="save-status" [class.success]="saveSuccess" [class.error]="!saveSuccess">
          {{ saveMessage }}
        </div>
      </form>
    </div>
  `,
  styles: [`
    .preferences-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 20px;
    }

    .preferences-header {
      margin-bottom: 30px;
    }

    .header-nav {
      margin-bottom: 20px;
    }

    .back-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .back-link:hover {
      text-decoration: underline;
    }

    .preferences-header h1 {
      margin: 0 0 10px;
      color: #333;
      font-size: 28px;
    }

    .preferences-header p {
      margin: 0;
      color: #666;
      font-size: 16px;
    }

    .loading-container,
    .error-container {
      text-align: center;
      padding: 60px 20px;
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #007bff;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 20px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .error-container {
      color: #dc3545;
    }

    .error-icon {
      font-size: 48px;
      margin-bottom: 20px;
    }

    .preferences-section {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 24px;
      margin-bottom: 24px;
    }

    .preferences-section h2 {
      margin: 0 0 8px;
      color: #1f2937;
      font-size: 20px;
      font-weight: 600;
    }

    .section-description {
      margin: 0 0 20px;
      color: #6b7280;
      font-size: 14px;
    }

    .preference-group {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .preference-item {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 16px;
      border: 1px solid #f1f5f9;
      border-radius: 8px;
      transition: all 0.2s ease;
    }

    .preference-item:hover {
      border-color: #e2e8f0;
    }

    .preference-item.master-toggle {
      background-color: #f8f9ff;
      border-color: #e0e7ff;
    }

    .preference-content {
      flex: 1;
      margin-right: 16px;
    }

    .preference-label {
      display: block;
      font-size: 14px;
      font-weight: 600;
      color: #374151;
      margin-bottom: 4px;
      cursor: pointer;
    }

    .preference-description {
      font-size: 13px;
      color: #6b7280;
      margin: 0;
      line-height: 1.4;
    }

    .preference-control {
      flex-shrink: 0;
    }

    .toggle-switch {
      position: relative;
      display: inline-block;
      width: 48px;
      height: 24px;
    }

    .toggle-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }

    .toggle-slider {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: #cbd5e0;
      transition: 0.3s;
      border-radius: 24px;
    }

    .toggle-slider:before {
      position: absolute;
      content: "";
      height: 20px;
      width: 20px;
      left: 2px;
      bottom: 2px;
      background-color: white;
      transition: 0.3s;
      border-radius: 50%;
    }

    input:checked + .toggle-slider {
      background-color: #007bff;
    }

    input:checked + .toggle-slider:before {
      transform: translateX(24px);
    }

    input:disabled + .toggle-slider {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .email-preferences.disabled {
      opacity: 0.6;
      pointer-events: none;
    }

    .form-group {
      margin-bottom: 16px;
    }

    .form-label {
      display: block;
      font-size: 14px;
      font-weight: 600;
      color: #374151;
      margin-bottom: 6px;
    }

    .form-select,
    .form-input {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 14px;
      transition: border-color 0.2s ease;
      background-color: white;
    }

    .form-select:focus,
    .form-input:focus {
      outline: none;
      border-color: #007bff;
      box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
    }

    .form-select:disabled,
    .form-input:disabled {
      background-color: #f9fafb;
      color: #9ca3af;
      cursor: not-allowed;
    }

    .input-group {
      display: flex;
      align-items: center;
    }

    .input-group-text {
      padding: 10px 12px;
      background-color: #f9fafb;
      border: 1px solid #d1d5db;
      border-right: none;
      border-radius: 6px 0 0 6px;
      font-size: 14px;
      color: #6b7280;
    }

    .input-group .form-input {
      border-radius: 0 6px 6px 0;
      border-left: none;
    }

    .form-help {
      font-size: 12px;
      color: #6b7280;
      margin-top: 4px;
    }

    .form-error {
      font-size: 12px;
      color: #dc2626;
      margin-top: 4px;
    }

    .save-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      margin-top: 32px;
      padding-top: 20px;
      border-top: 1px solid #e2e8f0;
    }

    .save-status {
      margin-top: 16px;
      padding: 12px 16px;
      border-radius: 6px;
      font-size: 14px;
      text-align: center;
    }

    .save-status.success {
      background-color: #dcfce7;
      color: #16a34a;
      border: 1px solid #bbf7d0;
    }

    .save-status.error {
      background-color: #fef2f2;
      color: #dc2626;
      border: 1px solid #fecaca;
    }

    .btn {
      padding: 10px 20px;
      border: 1px solid #ddd;
      border-radius: 6px;
      background-color: white;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s ease;
    }

    .btn:hover:not(:disabled) {
      background-color: #f8f9fa;
    }

    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
      border-color: #007bff;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .btn-secondary {
      background-color: #6c757d;
      color: white;
      border-color: #6c757d;
    }

    .btn-secondary:hover:not(:disabled) {
      background-color: #545b62;
    }

    @media (max-width: 768px) {
      .preference-item {
        flex-direction: column;
        gap: 12px;
        align-items: flex-start;
      }

      .preference-content {
        margin-right: 0;
      }

      .save-actions {
        flex-direction: column;
      }

      .preferences-container {
        padding: 15px;
      }
    }
  `]
})
export class ProviderNotificationPreferencesComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  preferencesForm!: FormGroup;
  originalPreferences: NotificationPreferencesDto | null = null;
  loading = false;
  saving = false;
  error: string | null = null;
  saveMessage: string | null = null;
  saveSuccess = false;

  constructor(
    private fb: FormBuilder,
    private providerService: ProviderPortalService
  ) {}

  ngOnInit() {
    this.initializeForm();
    this.loadPreferences();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm() {
    this.preferencesForm = this.fb.group({
      emailEnabled: [true],
      emailItemSold: [true],
      emailPayoutProcessed: [true],
      emailPayoutPending: [false],
      emailItemExpired: [false],
      emailStatementReady: [true],
      emailAccountUpdate: [true],
      digestMode: ['instant'],
      digestTime: ['09:00'],
      digestDay: [1],
      payoutPendingThreshold: [50.00, [Validators.min(10)]]
    });
  }

  loadPreferences() {
    this.loading = true;
    this.error = null;

    this.providerService.getNotificationPreferences()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (preferences) => {
          this.originalPreferences = preferences;
          this.preferencesForm.patchValue(preferences);
          this.preferencesForm.markAsPristine();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading preferences:', error);
          this.error = 'Failed to load notification preferences. Please try again later.';
          this.loading = false;
        }
      });
  }

  onMasterEmailToggle() {
    const emailEnabled = this.preferencesForm.get('emailEnabled')?.value;

    if (!emailEnabled) {
      // Disable all email notifications when master toggle is off
      this.preferencesForm.patchValue({
        emailItemSold: false,
        emailPayoutProcessed: false,
        emailPayoutPending: false,
        emailItemExpired: false,
        emailStatementReady: false,
        emailAccountUpdate: false
      });
    }
  }

  savePreferences() {
    if (this.preferencesForm.invalid || this.saving) {
      return;
    }

    this.saving = true;
    this.saveMessage = null;

    const formValue = this.preferencesForm.value;
    const request: UpdateNotificationPreferencesRequest = {
      emailEnabled: formValue.emailEnabled,
      emailItemSold: formValue.emailItemSold,
      emailPayoutProcessed: formValue.emailPayoutProcessed,
      emailPayoutPending: formValue.emailPayoutPending,
      emailItemExpired: formValue.emailItemExpired,
      emailStatementReady: formValue.emailStatementReady,
      emailAccountUpdate: formValue.emailAccountUpdate,
      digestMode: formValue.digestMode,
      digestTime: formValue.digestTime,
      digestDay: formValue.digestDay,
      payoutPendingThreshold: formValue.payoutPendingThreshold
    };

    this.providerService.updateNotificationPreferences(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedPreferences) => {
          this.originalPreferences = updatedPreferences;
          this.preferencesForm.markAsPristine();
          this.saveMessage = 'Your notification preferences have been saved successfully.';
          this.saveSuccess = true;
          this.saving = false;

          // Clear the success message after 3 seconds
          setTimeout(() => {
            this.saveMessage = null;
          }, 3000);
        },
        error: (error) => {
          console.error('Error saving preferences:', error);
          this.saveMessage = 'Failed to save your preferences. Please try again.';
          this.saveSuccess = false;
          this.saving = false;

          // Clear the error message after 5 seconds
          setTimeout(() => {
            this.saveMessage = null;
          }, 5000);
        }
      });
  }

  resetForm() {
    if (this.originalPreferences) {
      this.preferencesForm.patchValue(this.originalPreferences);
      this.preferencesForm.markAsPristine();
    }
    this.saveMessage = null;
  }
}