import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, switchMap } from 'rxjs';
import { ProviderPortalService } from '../services/provider-portal.service';
import { StatementDto } from '../models/provider.models';
import { LoadingService } from '../../shared/services/loading.service';
import { LOADING_KEYS } from '../constants/loading-keys';

@Component({
  selector: 'app-provider-statement-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="statement-detail-container">
      <!-- Loading State -->
      <div *ngIf="loadingService.isLoading(KEYS.STATEMENT)" class="loading-container">
        <div class="loading-spinner"></div>
        <p>Loading statement...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="error" class="error-container">
        <div class="error-icon">⚠️</div>
        <h3>Unable to load statement</h3>
        <p>{{ error }}</p>
        <div class="error-actions">
          <button class="btn btn-primary" (click)="loadStatement()">Try Again</button>
          <a class="btn btn-secondary" routerLink="/provider/statements">Back to Statements</a>
        </div>
      </div>

      <!-- Statement Content -->
      <div *ngIf="!loadingService.isLoading(KEYS.STATEMENT) && !error && statement" class="statement-content">
        <!-- Header -->
        <div class="statement-header">
          <div class="header-nav">
            <a routerLink="/provider/statements" class="back-link">
              ← Back to Statements
            </a>
          </div>
          <div class="header-title">
            <h1>{{ statement.periodLabel }} Statement</h1>
            <div class="statement-number">{{ statement.statementNumber }}</div>
          </div>
          <div class="header-actions">
            <button
              class="btn btn-primary"
              (click)="downloadPdf()"
              [disabled]="!statement.hasPdf || loadingService.isLoading(KEYS.STATEMENT_PDF)">
              <span *ngIf="loadingService.isLoading(KEYS.STATEMENT_PDF)">Downloading...</span>
              <span *ngIf="!loadingService.isLoading(KEYS.STATEMENT_PDF)">
                {{ statement.hasPdf ? '📄 Download PDF' : 'PDF Not Available' }}
              </span>
            </button>
            <button
              class="btn btn-secondary"
              (click)="regenerateStatement()"
              [disabled]="loadingService.isLoading(KEYS.STATEMENT_REGENERATE)">
              <span *ngIf="loadingService.isLoading(KEYS.STATEMENT_REGENERATE)">Regenerating...</span>
              <span *ngIf="!loadingService.isLoading(KEYS.STATEMENT_REGENERATE)">🔄 Regenerate</span>
            </button>
          </div>
        </div>

        <!-- Statement Info -->
        <div class="statement-info-card">
          <div class="info-grid">
            <div class="info-item">
              <label>Provider</label>
              <span>{{ statement.providerName }}</span>
            </div>
            <div class="info-item">
              <label>Shop</label>
              <span>{{ statement.shopName }}</span>
            </div>
            <div class="info-item">
              <label>Period</label>
              <span>{{ formatDateRange(statement.periodStart, statement.periodEnd) }}</span>
            </div>
            <div class="info-item">
              <label>Status</label>
              <span class="status-badge" [class]="getStatusClass(statement.status)">
                {{ statement.status }}
              </span>
            </div>
            <div class="info-item">
              <label>Generated</label>
              <span>{{ formatDate(statement.generatedAt) }}</span>
            </div>
            <div class="info-item" *ngIf="statement.viewedAt">
              <label>Last Viewed</label>
              <span>{{ formatDate(statement.viewedAt) }}</span>
            </div>
          </div>
        </div>

        <!-- Financial Summary -->
        <div class="financial-summary">
          <h2>Financial Summary</h2>
          <div class="summary-grid">
            <div class="summary-card opening">
              <div class="summary-label">Opening Balance</div>
              <div class="summary-value">{{ statement.openingBalance | currency }}</div>
            </div>
            <div class="summary-card sales">
              <div class="summary-label">Total Sales</div>
              <div class="summary-value">{{ statement.totalSales | currency }}</div>
              <div class="summary-detail">{{ statement.itemsSold }} items sold</div>
            </div>
            <div class="summary-card earnings">
              <div class="summary-label">Total Earnings</div>
              <div class="summary-value">{{ statement.totalEarnings | currency }}</div>
              <div class="summary-detail">Your commission</div>
            </div>
            <div class="summary-card payouts">
              <div class="summary-label">Total Payouts</div>
              <div class="summary-value">{{ statement.totalPayouts | currency }}</div>
              <div class="summary-detail">{{ statement.payoutCount }} payouts</div>
            </div>
            <div class="summary-card closing">
              <div class="summary-label">Closing Balance</div>
              <div class="summary-value">{{ statement.closingBalance | currency }}</div>
            </div>
          </div>
        </div>

        <!-- Sales Activity -->
        <div *ngIf="statement.sales && statement.sales.length > 0" class="activity-section">
          <h2>Sales Activity ({{ statement.sales.length }} items)</h2>
          <div class="activity-table">
            <div class="table-header">
              <div class="table-cell">Date</div>
              <div class="table-cell">Item</div>
              <div class="table-cell">SKU</div>
              <div class="table-cell">Sale Price</div>
              <div class="table-cell">Commission</div>
              <div class="table-cell">Your Cut</div>
            </div>
            <div
              *ngFor="let sale of statement.sales; trackBy: trackBySaleDate"
              class="table-row">
              <div class="table-cell">{{ formatDate(sale.date) }}</div>
              <div class="table-cell item-title">{{ sale.itemTitle }}</div>
              <div class="table-cell sku">{{ sale.itemSku }}</div>
              <div class="table-cell amount">{{ sale.salePrice | currency }}</div>
              <div class="table-cell commission">{{ sale.commissionRate }}%</div>
              <div class="table-cell earnings">{{ sale.earningsAmount | currency }}</div>
            </div>
          </div>
        </div>

        <!-- Payout Activity -->
        <div *ngIf="statement.payouts && statement.payouts.length > 0" class="activity-section">
          <h2>Payout Activity ({{ statement.payouts.length }} payouts)</h2>
          <div class="activity-table">
            <div class="table-header">
              <div class="table-cell">Date</div>
              <div class="table-cell">Payout #</div>
              <div class="table-cell">Payment Method</div>
              <div class="table-cell">Amount</div>
            </div>
            <div
              *ngFor="let payout of statement.payouts; trackBy: trackByPayoutNumber"
              class="table-row">
              <div class="table-cell">{{ formatDate(payout.date) }}</div>
              <div class="table-cell payout-number">{{ payout.payoutNumber }}</div>
              <div class="table-cell payment-method">{{ payout.paymentMethod }}</div>
              <div class="table-cell amount">{{ payout.amount | currency }}</div>
            </div>
          </div>
        </div>

        <!-- No Activity Messages -->
        <div *ngIf="statement.sales && statement.sales.length === 0" class="no-activity">
          <h3>No Sales This Period</h3>
          <p>There were no sales during this statement period.</p>
        </div>

        <div *ngIf="statement.payouts && statement.payouts.length === 0" class="no-activity">
          <h3>No Payouts This Period</h3>
          <p>There were no payouts processed during this statement period.</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .statement-detail-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
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

    .error-actions {
      display: flex;
      gap: 10px;
      justify-content: center;
      margin-top: 20px;
    }

    .statement-header {
      margin-bottom: 30px;
      display: flex;
      flex-direction: column;
      gap: 15px;
    }

    .header-nav {
      display: flex;
      align-items: center;
    }

    .back-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 5px;
    }

    .back-link:hover {
      text-decoration: underline;
    }

    .header-title {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }

    .header-title h1 {
      margin: 0;
      color: #333;
      font-size: 28px;
    }

    .statement-number {
      color: #666;
      font-size: 16px;
    }

    .header-actions {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
    }

    .statement-info-card {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 30px;
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .info-item label {
      font-size: 12px;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;
    }

    .info-item span {
      font-size: 14px;
      color: #1f2937;
      font-weight: 500;
    }

    .status-badge {
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      display: inline-block;
      width: fit-content;
    }

    .status-badge.generated {
      background-color: #dcfce7;
      color: #16a34a;
    }

    .status-badge.viewed {
      background-color: #e0e7ff;
      color: #4338ca;
    }

    .financial-summary {
      margin-bottom: 40px;
    }

    .financial-summary h2 {
      margin: 0 0 20px;
      color: #333;
      font-size: 20px;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
    }

    .summary-card {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 20px;
      text-align: center;
    }

    .summary-card.opening {
      border-left: 4px solid #6b7280;
    }

    .summary-card.sales {
      border-left: 4px solid #3b82f6;
    }

    .summary-card.earnings {
      border-left: 4px solid #16a34a;
    }

    .summary-card.payouts {
      border-left: 4px solid #dc2626;
    }

    .summary-card.closing {
      border-left: 4px solid #0891b2;
    }

    .summary-label {
      font-size: 12px;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;
      margin-bottom: 8px;
    }

    .summary-value {
      font-size: 24px;
      font-weight: 700;
      color: #1f2937;
      margin-bottom: 4px;
    }

    .summary-detail {
      font-size: 12px;
      color: #9ca3af;
    }

    .activity-section {
      margin-bottom: 40px;
    }

    .activity-section h2 {
      margin: 0 0 20px;
      color: #333;
      font-size: 20px;
    }

    .activity-table {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: hidden;
    }

    .table-header {
      display: grid;
      grid-template-columns: 100px 2fr 120px 100px 80px 100px;
      background-color: #f8f9fa;
      border-bottom: 1px solid #e2e8f0;
    }

    .table-row {
      display: grid;
      grid-template-columns: 100px 2fr 120px 100px 80px 100px;
      border-bottom: 1px solid #f1f5f9;
    }

    .table-row:last-child {
      border-bottom: none;
    }

    .table-row:hover {
      background-color: #f8f9fa;
    }

    .table-cell {
      padding: 12px 8px;
      font-size: 13px;
      display: flex;
      align-items: center;
    }

    .table-header .table-cell {
      font-weight: 600;
      color: #374151;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .item-title {
      font-weight: 500;
      color: #1f2937;
    }

    .sku {
      font-family: monospace;
      color: #6b7280;
    }

    .amount,
    .earnings {
      color: #16a34a;
      font-weight: 600;
    }

    .commission {
      color: #6b7280;
    }

    .payout-number {
      font-family: monospace;
      color: #4338ca;
    }

    .payment-method {
      color: #6b7280;
    }

    .no-activity {
      text-align: center;
      padding: 40px 20px;
      color: #6b7280;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    .no-activity h3 {
      margin: 0 0 10px;
      color: #374151;
    }

    .btn {
      padding: 10px 20px;
      border: 1px solid #ddd;
      border-radius: 6px;
      background-color: white;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      justify-content: center;
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

    /* Payout table specific columns */
    .activity-section:last-of-type .table-header {
      grid-template-columns: 120px 2fr 150px 120px;
    }

    .activity-section:last-of-type .table-row {
      grid-template-columns: 120px 2fr 150px 120px;
    }

    @media (max-width: 768px) {
      .statement-header {
        text-align: center;
      }

      .header-actions {
        justify-content: center;
      }

      .summary-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .info-grid {
        grid-template-columns: 1fr;
      }

      .activity-table {
        overflow-x: auto;
      }

      .table-header,
      .table-row {
        min-width: 600px;
      }
    }

    @media (max-width: 480px) {
      .summary-grid {
        grid-template-columns: 1fr;
      }

      .header-actions {
        flex-direction: column;
      }

      .statement-detail-container {
        padding: 15px;
      }
    }
  `]
})
export class ProviderStatementDetailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  statement: StatementDto | null = null;
  error: string | null = null;

  // Expose for template
  readonly KEYS = LOADING_KEYS;

  constructor(
    private route: ActivatedRoute,
    private providerService: ProviderPortalService,
    public loadingService: LoadingService
  ) {}

  ngOnInit() {
    this.route.paramMap
      .pipe(
        takeUntil(this.destroy$),
        switchMap(params => {
          const statementId = params.get('id');
          if (!statementId) {
            throw new Error('Statement ID is required');
          }
          this.loadingService.start(LOADING_KEYS.STATEMENT);
          this.error = null;
          return this.providerService.getStatement(statementId);
        })
      )
      .subscribe({
        next: (statement) => {
          this.statement = statement;
        },
        error: (error) => {
          console.error('Error loading statement:', error);
          if (error.status === 404) {
            this.error = 'Statement not found.';
          } else {
            this.error = 'Failed to load statement. Please try again later.';
          }
        },
        complete: () => {
          this.loadingService.stop(LOADING_KEYS.STATEMENT);
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadStatementById(statementId: string) {
    this.loadingService.start(LOADING_KEYS.STATEMENT);
    this.error = null;

    return this.providerService.getStatement(statementId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (statement) => {
          this.statement = statement;
        },
        error: (error) => {
          console.error('Error loading statement:', error);
          if (error.status === 404) {
            this.error = 'Statement not found.';
          } else {
            this.error = 'Failed to load statement. Please try again later.';
          }
        },
        complete: () => {
          this.loadingService.stop(LOADING_KEYS.STATEMENT);
        }
      });
  }

  loadStatement() {
    const statementId = this.route.snapshot.paramMap.get('id');
    if (statementId) {
      this.loadStatementById(statementId);
    }
  }

  downloadPdf() {
    if (!this.statement?.hasPdf || this.loadingService.isLoading(LOADING_KEYS.STATEMENT_PDF)) {
      return;
    }

    this.loadingService.start(LOADING_KEYS.STATEMENT_PDF);

    this.providerService.downloadStatementPdf(this.statement.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          this.downloadFile(blob, `${this.statement?.statementNumber}.pdf`);
        },
        error: (error) => {
          console.error('Error downloading PDF:', error);
          alert('Failed to download PDF. Please try again.');
        },
        complete: () => {
          this.loadingService.stop(LOADING_KEYS.STATEMENT_PDF);
        }
      });
  }

  regenerateStatement() {
    if (!this.statement || this.loadingService.isLoading(LOADING_KEYS.STATEMENT_REGENERATE)) {
      return;
    }

    const confirmed = confirm('Are you sure you want to regenerate this statement? This will recalculate all amounts based on current data.');
    if (!confirmed) {
      return;
    }

    this.loadingService.start(LOADING_KEYS.STATEMENT_REGENERATE);

    this.providerService.regenerateStatement(this.statement.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedStatement) => {
          this.statement = updatedStatement;
          alert('Statement has been regenerated successfully.');
        },
        error: (error) => {
          console.error('Error regenerating statement:', error);
          alert('Failed to regenerate statement. Please try again.');
        },
        complete: () => {
          this.loadingService.stop(LOADING_KEYS.STATEMENT_REGENERATE);
        }
      });
  }

  private downloadFile(blob: Blob, filename: string) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  }

  formatDate(date: Date | string): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  formatDateRange(startDate: string, endDate: string): string {
    const start = new Date(startDate);
    const end = new Date(endDate);

    const startStr = start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    const endStr = end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

    return `${startStr} - ${endStr}`;
  }

  getStatusClass(status: string): string {
    return status.toLowerCase();
  }

  trackBySaleDate(index: number, sale: any): string {
    return `${sale.date}-${sale.itemSku}`;
  }

  trackByPayoutNumber(index: number, payout: any): string {
    return payout.payoutNumber;
  }
}