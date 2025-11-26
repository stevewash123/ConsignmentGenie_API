import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderPayout, PagedResult } from '../models/provider.models';

@Component({
  selector: 'app-provider-payouts',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="provider-payouts">
      <!-- Navigation Header -->
      <div class="nav-header">
        <div class="header-content">
          <h1>Main Street Consignment</h1>
          <nav class="nav-links">
            <a routerLink="/provider/dashboard" routerLinkActive="active">Dashboard</a>
            <a routerLink="/provider/items" routerLinkActive="active">Items</a>
            <a routerLink="/provider/sales" routerLinkActive="active">Sales</a>
            <a routerLink="/provider/payouts" routerLinkActive="active">Payouts</a>
          </nav>
        </div>
      </div>

      <div class="content">
        <div class="page-header">
          <h2>My Payouts</h2>
        </div>

        <!-- Current Balance Section -->
        <div class="balance-section">
          <div class="balance-card">
            <div class="balance-icon">💰</div>
            <div class="balance-info">
              <div class="balance-title">Current Pending Balance: <span class="balance-amount">\${{currentBalance.toFixed(2)}}</span> ({{pendingItemCount}} items)</div>
              <div class="balance-note">Next payout typically processed end of month</div>
            </div>
          </div>
        </div>

        <!-- Payout History -->
        <div class="history-section">
          <h3>Payout History</h3>

          <div class="payouts-table" *ngIf="payoutsResult">
            <div class="table-header">
              <div class="col payout-col">Payout #</div>
              <div class="col date-col">Date</div>
              <div class="col amount-col">Amount</div>
              <div class="col items-col">Items</div>
              <div class="col method-col">Method</div>
              <div class="col actions-col">Action</div>
            </div>

            <div class="table-row" *ngFor="let payout of payoutsResult.items">
              <div class="col payout-col">
                <span class="payout-number">{{payout.payoutNumber}}</span>
              </div>
              <div class="col date-col">
                {{formatDate(payout.payoutDate)}}
              </div>
              <div class="col amount-col">
                <span class="amount">\${{payout.amount.toFixed(2)}}</span>
              </div>
              <div class="col items-col">
                {{payout.itemCount}}
              </div>
              <div class="col method-col">
                {{payout.paymentMethod}}
              </div>
              <div class="col actions-col">
                <button
                  class="view-btn"
                  [routerLink]="['/provider/payouts', payout.payoutId]">
                  View
                </button>
              </div>
            </div>

            <!-- No Payouts Message -->
            <div class="no-payouts" *ngIf="payoutsResult.items.length === 0">
              <p>No payouts have been processed yet.</p>
              <p>Your first payout will appear here once the shop owner processes payments.</p>
            </div>
          </div>
        </div>

        <!-- Total Summary -->
        <div class="summary-section">
          <div class="summary-card">
            <div class="summary-title">Total Paid Out (All Time):</div>
            <div class="summary-amount">\${{totalPaidOut.toFixed(2)}}</div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading">
        <p>Loading payouts...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadPayouts()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .provider-payouts {
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
      max-width: 1200px;
      margin: 0 auto;
    }

    .header-content h1 {
      font-size: 1.5rem;
      font-weight: 600;
      color: #111827;
      margin: 0;
    }

    .nav-links {
      display: flex;
      gap: 1.5rem;
    }

    .nav-links a {
      color: #6b7280;
      text-decoration: none;
      font-weight: 500;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
    }

    .nav-links a:hover,
    .nav-links a.active {
      color: #3b82f6;
      background: #f3f4f6;
    }

    .content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .page-header h2 {
      font-size: 1.875rem;
      font-weight: 700;
      color: #111827;
      margin: 0 0 2rem 0;
    }

    .balance-section {
      margin-bottom: 2rem;
    }

    .balance-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      background: #fef3c7;
      border: 1px solid #fbbf24;
      border-radius: 0.5rem;
      padding: 1.5rem;
    }

    .balance-icon {
      font-size: 2rem;
    }

    .balance-info {
      flex: 1;
    }

    .balance-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: #111827;
      margin-bottom: 0.25rem;
    }

    .balance-amount {
      color: #059669;
      font-weight: 700;
    }

    .balance-note {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .history-section {
      margin-bottom: 2rem;
    }

    .history-section h3 {
      font-size: 1.25rem;
      font-weight: 600;
      color: #111827;
      margin: 0 0 1rem 0;
    }

    .payouts-table {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      overflow: hidden;
    }

    .table-header {
      display: grid;
      grid-template-columns: 140px 120px 120px 80px 120px 80px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      background: #f9fafb;
      border-bottom: 1px solid #e5e7eb;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
    }

    .table-row {
      display: grid;
      grid-template-columns: 140px 120px 120px 80px 120px 80px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #f3f4f6;
      align-items: center;
    }

    .table-row:hover {
      background: #f9fafb;
    }

    .table-row:last-child {
      border-bottom: none;
    }

    .col {
      display: flex;
      align-items: center;
    }

    .payout-number {
      font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
      font-size: 0.875rem;
      color: #3b82f6;
      font-weight: 500;
    }

    .amount {
      font-weight: 600;
      color: #059669;
    }

    .view-btn {
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.375rem 0.75rem;
      border-radius: 0.375rem;
      cursor: pointer;
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      display: inline-block;
    }

    .view-btn:hover {
      background: #2563eb;
    }

    .no-payouts {
      padding: 3rem;
      text-align: center;
      color: #6b7280;
    }

    .no-payouts p {
      margin-bottom: 0.5rem;
    }

    .no-payouts p:first-child {
      font-style: italic;
      font-weight: 500;
    }

    .summary-section {
      border-top: 1px solid #e5e7eb;
      padding-top: 1.5rem;
    }

    .summary-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1.5rem;
      text-align: center;
    }

    .summary-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    .summary-amount {
      font-size: 1.875rem;
      font-weight: 700;
      color: #059669;
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

      .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: flex-start;
      }

      .nav-links {
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .content {
        padding: 1rem;
      }

      .balance-card {
        flex-direction: column;
        text-align: center;
        gap: 0.5rem;
      }

      .table-header {
        display: none;
      }

      .table-row {
        grid-template-columns: 1fr;
        gap: 1rem;
        padding: 1.5rem;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        margin-bottom: 1rem;
      }

      .col {
        justify-content: space-between;
        padding: 0.5rem 0;
        border-bottom: 1px solid #f3f4f6;
      }

      .col:last-child {
        border-bottom: none;
        justify-content: center;
      }

      .col:before {
        content: attr(data-label);
        font-weight: 600;
        color: #374151;
      }

      .payout-col:before { content: "Payout #: "; }
      .date-col:before { content: "Date: "; }
      .amount-col:before { content: "Amount: "; }
      .items-col:before { content: "Items: "; }
      .method-col:before { content: "Method: "; }
    }
  `]
})
export class ProviderPayoutsComponent implements OnInit {
  payoutsResult: PagedResult<ProviderPayout> | null = null;
  loading = false;
  error: string | null = null;

  currentBalance = 487.50;
  pendingItemCount = 12;
  totalPaidOut = 735.50;

  constructor(private providerService: ProviderPortalService) {}

  ngOnInit() {
    this.loadPayouts();
  }

  loadPayouts() {
    this.loading = true;
    this.error = null;

    this.providerService.getMyPayouts().subscribe({
      next: (result) => {
        this.payoutsResult = result;
        this.calculateTotals();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load payouts. Please try again.';
        this.loading = false;
        console.error('Payouts error:', err);
      }
    });
  }

  calculateTotals() {
    if (!this.payoutsResult) return;

    this.totalPaidOut = this.payoutsResult.items
      .reduce((sum, payout) => sum + payout.amount, 0);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }
}