import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderPayoutDetail } from '../models/provider.models';

@Component({
  selector: 'app-provider-payout-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="payout-detail">
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

      <div class="content" *ngIf="payoutDetail">
        <!-- Back Button -->
        <div class="back-section">
          <a routerLink="/provider/payouts" class="back-btn">← Back to Payouts</a>
        </div>

        <!-- Payout Header -->
        <div class="payout-header">
          <h2>Payout Details</h2>
          <div class="payout-number">{{payoutDetail.payoutNumber}}</div>
        </div>

        <!-- Payout Summary -->
        <div class="summary-card">
          <div class="summary-grid">
            <div class="summary-item">
              <div class="label">Payout Date</div>
              <div class="value">{{formatFullDate(payoutDetail.payoutDate)}}</div>
            </div>
            <div class="summary-item">
              <div class="label">Amount</div>
              <div class="value amount">\${{payoutDetail.amount.toFixed(2)}}</div>
            </div>
            <div class="summary-item">
              <div class="label">Payment Method</div>
              <div class="value">{{payoutDetail.paymentMethod}}</div>
            </div>
            <div class="summary-item">
              <div class="label">Items Included</div>
              <div class="value">{{payoutDetail.itemCount}} items</div>
            </div>
            <div class="summary-item" *ngIf="payoutDetail.paymentReference">
              <div class="label">Payment Reference</div>
              <div class="value reference">{{payoutDetail.paymentReference}}</div>
            </div>
            <div class="summary-item">
              <div class="label">Period</div>
              <div class="value">{{formatFullDate(payoutDetail.periodStart)}} - {{formatFullDate(payoutDetail.periodEnd)}}</div>
            </div>
          </div>
        </div>

        <!-- Items Breakdown -->
        <div class="items-section">
          <h3>Items Included in This Payout</h3>

          <div class="items-table">
            <div class="table-header">
              <div class="col date-col">Sale Date</div>
              <div class="col item-col">Item</div>
              <div class="col price-col">Sale Price</div>
              <div class="col earnings-col">Your Earnings</div>
            </div>

            <div class="table-row" *ngFor="let item of payoutDetail.items">
              <div class="col date-col">
                {{formatDate(item.saleDate)}}
              </div>
              <div class="col item-col">
                <div class="item-info">
                  <div class="item-title">{{item.itemTitle}}</div>
                  <div class="item-sku">{{item.itemSku}}</div>
                </div>
              </div>
              <div class="col price-col">
                \${{item.salePrice.toFixed(2)}}
              </div>
              <div class="col earnings-col">
                \${{item.myEarnings.toFixed(2)}}
              </div>
            </div>

            <!-- Total Row -->
            <div class="total-row">
              <div class="col date-col"></div>
              <div class="col item-col">
                <div class="total-label">Total ({{payoutDetail.items.length}} items)</div>
              </div>
              <div class="col price-col">
                <div class="total-amount">\${{getTotalSalePrice().toFixed(2)}}</div>
              </div>
              <div class="col earnings-col">
                <div class="total-amount">\${{payoutDetail.amount.toFixed(2)}}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="footer-actions">
          <a routerLink="/provider/payouts" class="btn-secondary">Back to Payouts</a>
          <button class="btn-primary" (click)="printPayout()">Print Receipt</button>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading">
        <p>Loading payout details...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadPayoutDetail()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .payout-detail {
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

    .back-section {
      margin-bottom: 1rem;
    }

    .back-btn {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
      font-size: 0.875rem;
    }

    .back-btn:hover {
      color: #2563eb;
    }

    .payout-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }

    .payout-header h2 {
      font-size: 1.875rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }

    .payout-number {
      font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
      font-size: 1rem;
      color: #3b82f6;
      font-weight: 600;
    }

    .summary-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 2rem;
      margin-bottom: 2rem;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .summary-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .label {
      font-size: 0.875rem;
      font-weight: 500;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .value {
      font-size: 1rem;
      font-weight: 600;
      color: #111827;
    }

    .value.amount {
      font-size: 1.25rem;
      color: #059669;
    }

    .value.reference {
      font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
      font-size: 0.875rem;
      color: #3b82f6;
    }

    .items-section {
      margin-bottom: 2rem;
    }

    .items-section h3 {
      font-size: 1.25rem;
      font-weight: 600;
      color: #111827;
      margin: 0 0 1rem 0;
    }

    .items-table {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      overflow: hidden;
    }

    .table-header {
      display: grid;
      grid-template-columns: 120px 2fr 120px 120px;
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
      grid-template-columns: 120px 2fr 120px 120px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .table-row:last-of-type {
      border-bottom: 1px solid #e5e7eb;
    }

    .total-row {
      display: grid;
      grid-template-columns: 120px 2fr 120px 120px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      background: #f9fafb;
      font-weight: 600;
    }

    .col {
      display: flex;
      align-items: center;
    }

    .item-info {
      min-width: 0;
    }

    .item-title {
      font-weight: 600;
      color: #111827;
      margin-bottom: 0.25rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .item-sku {
      font-size: 0.875rem;
      color: #6b7280;
    }

    .total-label {
      font-weight: 600;
      color: #111827;
    }

    .total-amount {
      font-weight: 700;
      color: #059669;
    }

    .footer-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 1.5rem;
      border-top: 1px solid #e5e7eb;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
      padding: 0.75rem 1.5rem;
      border-radius: 0.375rem;
      text-decoration: none;
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

      .payout-header {
        flex-direction: column;
        gap: 0.5rem;
        align-items: flex-start;
      }

      .summary-grid {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .table-header {
        display: none;
      }

      .table-row, .total-row {
        grid-template-columns: 1fr;
        gap: 0.5rem;
        padding: 1rem;
      }

      .col {
        justify-content: space-between;
        padding: 0.5rem 0;
        border-bottom: 1px solid #f3f4f6;
      }

      .col:last-child {
        border-bottom: none;
      }

      .footer-actions {
        flex-direction: column;
        gap: 1rem;
      }

      .footer-actions > * {
        width: 100%;
        text-align: center;
      }
    }
  `]
})
export class ProviderPayoutDetailComponent implements OnInit {
  payoutDetail: ProviderPayoutDetail | null = null;
  loading = false;
  error: string | null = null;
  payoutId: string;

  constructor(
    private providerService: ProviderPortalService,
    private route: ActivatedRoute
  ) {
    this.payoutId = this.route.snapshot.paramMap.get('id') || '';
  }

  ngOnInit() {
    if (this.payoutId) {
      this.loadPayoutDetail();
    }
  }

  loadPayoutDetail() {
    this.loading = true;
    this.error = null;

    this.providerService.getMyPayout(this.payoutId).subscribe({
      next: (detail) => {
        this.payoutDetail = detail;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load payout details. Please try again.';
        this.loading = false;
        console.error('Payout detail error:', err);
      }
    });
  }

  getTotalSalePrice(): number {
    if (!this.payoutDetail) return 0;
    return this.payoutDetail.items.reduce((sum, item) => sum + item.salePrice, 0);
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }

  formatFullDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  printPayout() {
    window.print();
  }
}