import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AppLayoutComponent } from '../../shared/components/app-layout.component';

interface OrganizationSettings {
  id: string;
  name: string;
  subscriptionTier: string;
  subscriptionStatus: string;
  quickBooksConnected: boolean;
  quickBooksLastSync?: string;
  squareConnected: boolean;
  squareLocationId?: string;
}

interface QuickBooksAuthResponse {
  success: boolean;
  authUrl?: string;
  message: string;
}

interface SquareAuthResponse {
  success: boolean;
  authUrl?: string;
  message: string;
}

@Component({
  selector: 'app-owner-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, AppLayoutComponent],
  template: `
    <app-layout>
      <div class="settings-container">
        <div class="settings-header">
          <h1>Organization Settings</h1>
          <p>Manage your shop settings and integrations</p>
        </div>

        <!-- General Settings -->
        <div class="settings-section">
          <h2>General Information</h2>
          <div class="settings-card">
            <div class="setting-group" *ngIf="organization()">
              <label for="orgName">Organization Name</label>
              <input
                type="text"
                id="orgName"
                [(ngModel)]="organization()!.name"
                class="form-input"
                placeholder="Your shop name"
              >
            </div>

            <div class="setting-group">
              <label>Subscription</label>
              <div class="subscription-info" *ngIf="organization()">
                <span class="tier-badge" [class]="'tier-' + organization()!.subscriptionTier.toLowerCase()">
                  {{ organization()!.subscriptionTier }}
                </span>
                <span class="status-badge" [class]="'status-' + organization()!.subscriptionStatus.toLowerCase()">
                  {{ organization()!.subscriptionStatus }}
                </span>
              </div>
            </div>

            <div class="setting-actions">
              <button class="btn-primary" (click)="saveGeneralSettings()" [disabled]="saving()">
                {{ saving() ? 'Saving...' : 'Save Changes' }}
              </button>
            </div>
          </div>
        </div>

        <!-- QuickBooks Integration -->
        <div class="settings-section">
          <h2>QuickBooks Integration</h2>
          <div class="settings-card">
            <div class="integration-header">
              <div class="integration-info">
                <h3>QuickBooks Desktop/Online</h3>
                <p>Sync customers, items, and transactions with QuickBooks for seamless accounting.</p>
              </div>
              <div class="integration-status">
                <span class="status-indicator" [class.connected]="organization()?.quickBooksConnected">
                  {{ organization()?.quickBooksConnected ? 'Connected' : 'Not Connected' }}
                </span>
              </div>
            </div>

            <div class="integration-details" *ngIf="organization()?.quickBooksConnected">
              <div class="detail-item">
                <span class="detail-label">Last Sync:</span>
                <span class="detail-value">{{ organization()!.quickBooksLastSync ? (organization()!.quickBooksLastSync | date:'medium') : 'Never' }}</span>
              </div>
              <div class="sync-actions">
                <button class="btn-secondary" (click)="syncQuickBooks()" [disabled]="syncing()">
                  {{ syncing() ? 'Syncing...' : 'Sync Now' }}
                </button>
                <button class="btn-danger" (click)="disconnectQuickBooks()" [disabled]="syncing()">
                  Disconnect
                </button>
              </div>
            </div>

            <div class="integration-setup" *ngIf="!organization()?.quickBooksConnected">
              <div class="setup-features">
                <h4>What you'll get:</h4>
                <ul>
                  <li>✅ Automatic customer sync</li>
                  <li>✅ Item and inventory sync</li>
                  <li>✅ Sales transaction export</li>
                  <li>✅ Provider payout tracking</li>
                  <li>✅ Real-time financial reporting</li>
                </ul>
              </div>
              <button class="btn-primary btn-large" (click)="connectQuickBooks()" [disabled]="connecting()">
                {{ connecting() ? 'Connecting...' : 'Connect to QuickBooks' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Square Integration -->
        <div class="settings-section">
          <h2>Square Integration</h2>
          <div class="settings-card">
            <div class="integration-header">
              <div class="integration-info">
                <h3>Square Point of Sale</h3>
                <p>Connect your Square POS system for unified payment processing and inventory management.</p>
              </div>
              <div class="integration-status">
                <span class="status-indicator" [class.connected]="organization()?.squareConnected">
                  {{ organization()?.squareConnected ? 'Connected' : 'Not Connected' }}
                </span>
              </div>
            </div>

            <div class="integration-details" *ngIf="organization()?.squareConnected">
              <div class="detail-item">
                <span class="detail-label">Location ID:</span>
                <span class="detail-value">{{ organization()!.squareLocationId || 'Not set' }}</span>
              </div>
              <div class="sync-actions">
                <button class="btn-secondary" (click)="syncSquare()" [disabled]="syncing()">
                  {{ syncing() ? 'Syncing...' : 'Sync Inventory' }}
                </button>
                <button class="btn-danger" (click)="disconnectSquare()" [disabled]="syncing()">
                  Disconnect
                </button>
              </div>
            </div>

            <div class="integration-setup" *ngIf="!organization()?.squareConnected">
              <div class="setup-features">
                <h4>What you'll get:</h4>
                <ul>
                  <li>✅ Unified inventory management</li>
                  <li>✅ Real-time payment processing</li>
                  <li>✅ Customer data synchronization</li>
                  <li>✅ Sales analytics and reporting</li>
                  <li>✅ Multi-location support</li>
                </ul>
              </div>
              <button class="btn-primary btn-large" (click)="connectSquare()" [disabled]="connecting()">
                {{ connecting() ? 'Connecting...' : 'Connect to Square' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Success/Error Messages -->
        <div class="message-container">
          <div *ngIf="successMessage()" class="message success">
            <span class="message-icon">✅</span>
            {{ successMessage() }}
          </div>
          <div *ngIf="errorMessage()" class="message error">
            <span class="message-icon">❌</span>
            {{ errorMessage() }}
          </div>
        </div>
      </div>
    </app-layout>
  `,
  styles: [`
    .settings-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    .settings-header {
      margin-bottom: 3rem;
      text-align: center;
    }

    .settings-header h1 {
      font-size: 2.5rem;
      color: #1f2937;
      margin-bottom: 0.5rem;
    }

    .settings-header p {
      color: #6b7280;
      font-size: 1.1rem;
    }

    .settings-section {
      margin-bottom: 3rem;
    }

    .settings-section h2 {
      font-size: 1.5rem;
      color: #1f2937;
      margin-bottom: 1.5rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .settings-card {
      background: white;
      border-radius: 12px;
      padding: 2rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      border: 1px solid #e5e7eb;
    }

    .setting-group {
      margin-bottom: 1.5rem;
    }

    .setting-group label {
      display: block;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .form-input {
      width: 100%;
      max-width: 400px;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      font-size: 1rem;
      transition: border-color 0.2s, box-shadow 0.2s;
    }

    .form-input:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .subscription-info {
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .tier-badge, .status-badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .tier-basic { background: #dbeafe; color: #1e40af; }
    .tier-pro { background: #dcfce7; color: #166534; }
    .tier-enterprise { background: #fef3c7; color: #92400e; }

    .status-active { background: #dcfce7; color: #166534; }
    .status-trial { background: #fef3c7; color: #92400e; }
    .status-suspended { background: #fee2e2; color: #dc2626; }

    .setting-actions {
      margin-top: 2rem;
      padding-top: 2rem;
      border-top: 1px solid #e5e7eb;
    }

    .integration-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 2rem;
    }

    .integration-info h3 {
      color: #1f2937;
      font-size: 1.25rem;
      margin-bottom: 0.5rem;
    }

    .integration-info p {
      color: #6b7280;
      margin: 0;
    }

    .status-indicator {
      padding: 0.5rem 1rem;
      border-radius: 8px;
      font-weight: 600;
      font-size: 0.875rem;
      background: #fee2e2;
      color: #dc2626;
    }

    .status-indicator.connected {
      background: #dcfce7;
      color: #166534;
    }

    .integration-details {
      padding: 1.5rem;
      background: #f9fafb;
      border-radius: 8px;
      margin-bottom: 1rem;
    }

    .detail-item {
      display: flex;
      justify-content: space-between;
      margin-bottom: 1rem;
    }

    .detail-label {
      font-weight: 600;
      color: #374151;
    }

    .detail-value {
      color: #6b7280;
    }

    .sync-actions {
      display: flex;
      gap: 1rem;
    }

    .integration-setup {
      text-align: center;
    }

    .setup-features {
      background: #f9fafb;
      border-radius: 8px;
      padding: 1.5rem;
      margin-bottom: 2rem;
    }

    .setup-features h4 {
      color: #1f2937;
      margin-bottom: 1rem;
    }

    .setup-features ul {
      list-style: none;
      padding: 0;
      margin: 0;
      text-align: left;
    }

    .setup-features li {
      color: #374151;
      margin-bottom: 0.5rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .btn-primary, .btn-secondary, .btn-danger {
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
      font-size: 1rem;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2563eb;
    }

    .btn-primary:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .btn-large {
      padding: 1rem 2rem;
      font-size: 1.1rem;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #e5e7eb;
    }

    .btn-danger {
      background: #dc2626;
      color: white;
    }

    .btn-danger:hover:not(:disabled) {
      background: #b91c1c;
    }

    .message-container {
      margin-top: 2rem;
    }

    .message {
      padding: 1rem;
      border-radius: 8px;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1rem;
      font-weight: 600;
    }

    .message.success {
      background: #dcfce7;
      color: #166534;
      border: 1px solid #bbf7d0;
    }

    .message.error {
      background: #fee2e2;
      color: #dc2626;
      border: 1px solid #fecaca;
    }

    .message-icon {
      font-size: 1.2rem;
    }

    @media (max-width: 768px) {
      .settings-container {
        padding: 1rem;
      }

      .settings-card {
        padding: 1.5rem;
      }

      .integration-header {
        flex-direction: column;
        gap: 1rem;
      }

      .sync-actions {
        flex-direction: column;
      }

      .subscription-info {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }
    }
  `]
})
export class OwnerSettingsComponent implements OnInit {
  organization = signal<OrganizationSettings | null>(null);
  saving = signal(false);
  connecting = signal(false);
  syncing = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadOrganizationSettings();
  }

  async loadOrganizationSettings() {
    try {
      const userData = JSON.parse(localStorage.getItem('user_data') || '{}');
      if (!userData.organizationId) {
        this.showError('No organization found');
        return;
      }

      const response = await this.http.get<any>(
        `${environment.apiUrl}/api/organizations/${userData.organizationId}/settings`
      ).toPromise();

      if (response?.data) {
        this.organization.set(response.data);
      }
    } catch (error) {
      console.error('Failed to load organization settings:', error);
      // Use mock data for development
      this.organization.set({
        id: '1',
        name: 'Demo Consignment Shop',
        subscriptionTier: 'Pro',
        subscriptionStatus: 'Active',
        quickBooksConnected: false,
        squareConnected: false
      });
    }
  }

  async saveGeneralSettings() {
    if (!this.organization()) return;

    this.saving.set(true);
    try {
      const response = await this.http.put<any>(
        `${environment.apiUrl}/api/organizations/${this.organization()!.id}`,
        {
          name: this.organization()!.name
        }
      ).toPromise();

      if (response?.success) {
        this.showSuccess('Settings saved successfully');
      }
    } catch (error) {
      this.showError('Failed to save settings');
    } finally {
      this.saving.set(false);
    }
  }

  async connectQuickBooks() {
    this.connecting.set(true);
    try {
      const response = await this.http.post<QuickBooksAuthResponse>(
        `${environment.apiUrl}/api/quickbooks/connect`,
        {}
      ).toPromise();

      if (response?.success && response.authUrl) {
        // Open QuickBooks OAuth flow in new window
        const authWindow = window.open(response.authUrl, 'quickbooks-auth', 'width=600,height=700');

        // Listen for the auth completion
        const checkClosed = setInterval(() => {
          if (authWindow?.closed) {
            clearInterval(checkClosed);
            this.loadOrganizationSettings(); // Reload to get updated status
            this.showSuccess('QuickBooks connection initiated. Please complete the authorization.');
          }
        }, 1000);
      }
    } catch (error) {
      this.showError('Failed to initiate QuickBooks connection');
    } finally {
      this.connecting.set(false);
    }
  }

  async disconnectQuickBooks() {
    if (!confirm('Are you sure you want to disconnect QuickBooks? This will stop all syncing.')) {
      return;
    }

    try {
      const response = await this.http.delete<any>(
        `${environment.apiUrl}/api/quickbooks/disconnect`
      ).toPromise();

      if (response?.success) {
        await this.loadOrganizationSettings();
        this.showSuccess('QuickBooks disconnected successfully');
      }
    } catch (error) {
      this.showError('Failed to disconnect QuickBooks');
    }
  }

  async syncQuickBooks() {
    this.syncing.set(true);
    try {
      const response = await this.http.post<any>(
        `${environment.apiUrl}/api/quickbooks/sync`,
        {}
      ).toPromise();

      if (response?.success) {
        await this.loadOrganizationSettings();
        this.showSuccess('QuickBooks sync completed successfully');
      }
    } catch (error) {
      this.showError('QuickBooks sync failed');
    } finally {
      this.syncing.set(false);
    }
  }

  async connectSquare() {
    this.connecting.set(true);
    try {
      const response = await this.http.post<SquareAuthResponse>(
        `${environment.apiUrl}/api/square/connect`,
        {}
      ).toPromise();

      if (response?.success && response.authUrl) {
        // Open Square OAuth flow in new window
        const authWindow = window.open(response.authUrl, 'square-auth', 'width=600,height=700');

        // Listen for the auth completion
        const checkClosed = setInterval(() => {
          if (authWindow?.closed) {
            clearInterval(checkClosed);
            this.loadOrganizationSettings(); // Reload to get updated status
            this.showSuccess('Square connection initiated. Please complete the authorization.');
          }
        }, 1000);
      }
    } catch (error) {
      this.showError('Failed to initiate Square connection');
    } finally {
      this.connecting.set(false);
    }
  }

  async disconnectSquare() {
    if (!confirm('Are you sure you want to disconnect Square? This will stop all syncing.')) {
      return;
    }

    try {
      const response = await this.http.delete<any>(
        `${environment.apiUrl}/api/square/disconnect`
      ).toPromise();

      if (response?.success) {
        await this.loadOrganizationSettings();
        this.showSuccess('Square disconnected successfully');
      }
    } catch (error) {
      this.showError('Failed to disconnect Square');
    }
  }

  async syncSquare() {
    this.syncing.set(true);
    try {
      const response = await this.http.post<any>(
        `${environment.apiUrl}/api/square/sync`,
        {}
      ).toPromise();

      if (response?.success) {
        await this.loadOrganizationSettings();
        this.showSuccess('Square sync completed successfully');
      }
    } catch (error) {
      this.showError('Square sync failed');
    } finally {
      this.syncing.set(false);
    }
  }

  private showSuccess(message: string) {
    this.successMessage.set(message);
    this.errorMessage.set('');
    setTimeout(() => this.successMessage.set(''), 5000);
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    this.successMessage.set('');
    setTimeout(() => this.errorMessage.set(''), 5000);
  }
}