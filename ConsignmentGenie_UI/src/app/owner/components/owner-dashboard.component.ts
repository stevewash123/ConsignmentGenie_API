import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OwnerLayoutComponent } from './owner-layout.component';
import { ProviderService } from '../../services/provider.service';
import { TransactionService, SalesMetrics, MetricsQueryParams } from '../../services/transaction.service';
import { PayoutService, PayoutStatus } from '../../services/payout.service';
import { PendingPayoutData } from '../../models/payout.model';
import { AuthService } from '../../services/auth.service';

interface ShopSummary {
  activeProviders: number;
  inventoryValue: number;
  totalItems: number;
  recentSales: number;
  recentSalesCount: number;
  pendingPayouts: number;
  pendingPayoutCount: number;
  recentTransactions: Transaction[];
}

interface Transaction {
  id: string;
  date: Date;
  itemName: string;
  provider: string;
  amount: number;
  commission: number;
}

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, OwnerLayoutComponent],
  template: `
    <app-owner-layout>
      <div class="owner-dashboard">
        <div class="dashboard-header">
          <h1>{{ getCurrentUser()?.businessName || 'Shop' }} Dashboard</h1>
          <p>Welcome back, {{ getCurrentUser()?.email?.split('@')[0] || 'Owner' }}! Here's what's happening in your consignment shop.</p>
        </div>

        <!-- Key Metrics -->
        <div class="metrics-grid" *ngIf="summary(); else loadingMetrics">
          <div class="metric-card">
            <div class="metric-content">
              <h3>Inventory Value</h3>
              <div class="metric-value">\${{ summary()!.inventoryValue | number:'1.2-2' }}</div>
            </div>
          </div>

          <a routerLink="/owner/payouts" class="metric-card pending-payouts"
             [ngClass]="{ 'has-pending': summary()!.pendingPayoutCount > 0 }">
            <div class="metric-content">
              <h3>Pending Payouts</h3>
              <div class="metric-value">\${{ summary()!.pendingPayouts | number:'1.2-2' }}</div>
              <span *ngIf="summary()!.pendingPayoutCount > 0" class="action-hint">→ Click to process</span>
            </div>
          </a>
        </div>

        <ng-template #loadingMetrics>
          <div class="loading" data-cy="loading-indicator">Loading dashboard data...</div>
        </ng-template>

        <!-- Quick Actions -->
        <div class="actions-section">
          <h2>Quick Actions</h2>
          <div class="action-grid">
            <a routerLink="/owner/sales" class="action-card">
              <h3>Process Sale</h3>
              <p>Record a new transaction and automatically calculate splits</p>
            </a>

            <a routerLink="/owner/providers" class="action-card">
              <h3>Manage Providers</h3>
              <p>View providers, update commission rates, and track performance</p>
            </a>

            <a routerLink="/owner/payouts" class="action-card">
              <h3>Generate Payouts</h3>
              <p>Create payout reports and process provider payments</p>
            </a>
          </div>
        </div>

        <!-- Recent Activity -->
        <div class="activity-section" *ngIf="summary()?.recentTransactions?.length">
          <h2>Recent Transactions</h2>
          <div class="transactions-table">
            <div class="table-header">
              <div class="col-date">Date</div>
              <div class="col-item">Item</div>
              <div class="col-provider">Provider</div>
              <div class="col-amount">Sale Amount</div>
              <div class="col-commission">Commission</div>
            </div>
            <div class="table-body">
              <div class="transaction-row" *ngFor="let transaction of summary()!.recentTransactions">
                <div class="col-date">{{ transaction.date | date:'short' }}</div>
                <div class="col-item">{{ transaction.itemName }}</div>
                <div class="col-provider">{{ transaction.provider }}</div>
                <div class="col-amount">\${{ transaction.amount | number:'1.2-2' }}</div>
                <div class="col-commission">\${{ transaction.commission | number:'1.2-2' }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </app-owner-layout>
  `,
  styles: [`
    .owner-dashboard {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .dashboard-header {
      margin-bottom: 3rem;
      text-align: center;
    }

    .dashboard-header h1 {
      font-size: 2.5rem;
      color: #047857;
      margin-bottom: 0.5rem;
    }

    .dashboard-header p {
      color: #6b7280;
      font-size: 1.1rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 3rem;
    }

    .metric-card {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      transition: transform 0.2s;
    }

    .metric-card:hover {
      transform: translateY(-2px);
    }

    .metric-card.sales { border-left-color: #059669; }
    .metric-card.profit { border-left-color: #fbbf24; }
    .metric-card.providers { border-left-color: #3b82f6; }
    .metric-card.inventory { border-left-color: #8b5cf6; }
    .metric-card.pending-payouts {
      border-left-color: #f59e0b;
      text-decoration: none;
      color: inherit;
    }

    .metric-card.pending-payouts.has-pending {
      border-left-color: #ef4444;
      background: linear-gradient(135deg, #fff 0%, #fef2f2 100%);
      cursor: pointer;
    }

    .metric-card.pending-payouts.has-pending:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 25px rgba(239, 68, 68, 0.15);
    }

    .metric-icon {
      font-size: 3rem;
      opacity: 0.8;
    }

    .metric-content h3 {
      color: #6b7280;
      font-size: 0.875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0.5rem;
    }

    .metric-value {
      font-size: 2.5rem;
      font-weight: bold;
      color: #1f2937;
      margin-bottom: 0.25rem;
    }

    .metric-change {
      font-size: 0.875rem;
      color: #059669;
      font-weight: 600;
    }

    .action-hint {
      color: #ef4444;
      font-weight: 700;
      margin-left: 0.5rem;
      opacity: 0.8;
    }

    .loading {
      text-align: center;
      color: #6b7280;
      font-size: 1.1rem;
      padding: 3rem;
    }

    .actions-section, .activity-section {
      margin-bottom: 3rem;
    }

    .actions-section h2, .activity-section h2 {
      font-size: 1.75rem;
      color: #047857;
      margin-bottom: 1.5rem;
    }

    .action-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 1.5rem;
    }

    .action-card {
      background: white;
      border-radius: 12px;
      padding: 1.5rem;
      text-decoration: none;
      color: inherit;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      border: 1px solid #e5e7eb;
      transition: all 0.2s;
      display: block;
    }

    .action-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0,0,0,0.1);
      border-color: #059669;
    }

    .action-icon {
      font-size: 2.5rem;
      margin-bottom: 1rem;
    }

    .action-card h3 {
      color: #047857;
      font-size: 1.25rem;
      margin-bottom: 0.5rem;
    }

    .action-card p {
      color: #6b7280;
      margin: 0;
      line-height: 1.5;
    }

    .transactions-table {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    }

    .table-header, .transaction-row {
      display: grid;
      grid-template-columns: 140px 1fr 150px 120px 120px;
      gap: 1rem;
      padding: 1rem;
      align-items: center;
    }

    .table-header {
      background: #f9fafb;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .transaction-row {
      border-bottom: 1px solid #e5e7eb;
    }

    .transaction-row:last-child {
      border-bottom: none;
    }

    .col-amount, .col-commission {
      font-weight: 600;
      color: #047857;
    }

    @media (max-width: 768px) {
      .owner-dashboard {
        padding: 1rem;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }

      .action-grid {
        grid-template-columns: 1fr;
      }

      .table-header, .transaction-row {
        grid-template-columns: 1fr;
        gap: 0.5rem;
      }

      .table-header {
        display: none;
      }

      .transaction-row {
        display: block;
        padding: 1rem;
        border-bottom: 1px solid #e5e7eb;
      }

      .col-date, .col-item, .col-provider, .col-amount, .col-commission {
        display: flex;
        justify-content: space-between;
        margin-bottom: 0.25rem;
      }

      .col-date::before { content: "Date: "; font-weight: 600; }
      .col-item::before { content: "Item: "; font-weight: 600; }
      .col-provider::before { content: "Provider: "; font-weight: 600; }
      .col-amount::before { content: "Amount: "; font-weight: 600; }
      .col-commission::before { content: "Commission: "; font-weight: 600; }
    }
  `]
})
export class OwnerDashboardComponent implements OnInit {
  summary = signal<ShopSummary | null>(null);
  activeProviderCount = signal<number>(0);

  constructor(
    private providerService: ProviderService,
    private transactionService: TransactionService,
    private payoutService: PayoutService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  getCurrentUser() {
    return this.authService.getCurrentUser();
  }

  private loadDashboardData() {
    // Load real provider count and sales metrics
    const providersPromise = this.providerService.getProviders().toPromise();

    // Get last 30 days sales metrics
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

    const metricsParams: MetricsQueryParams = {
      startDate: thirtyDaysAgo,
      endDate: new Date()
    };

    const salesPromise = this.transactionService.getSalesMetrics(metricsParams).toPromise();
    const pendingPayoutsPromise = this.payoutService.getPendingPayouts().toPromise();

    Promise.all([providersPromise, salesPromise, pendingPayoutsPromise])
      .then(([providers, salesMetrics, pendingPayouts]) => {
        console.log('Dashboard data loaded:', { providers, salesMetrics, pendingPayouts });

        // Handle providers response - might be wrapped in API response object
        let providersArray = providers;
        if (providers && typeof providers === 'object' && !Array.isArray(providers)) {
          // Check if it's wrapped in a response object with proper type casting
          const response = providers as any;
          providersArray = response.data || response.providers || providers;
        }

        const activeProviderCount = Array.isArray(providersArray) ?
          providersArray.filter((p: any) => p?.isActive).length : 0;
        this.activeProviderCount.set(activeProviderCount);

        // Calculate real pending payout totals from new API
        const totalPendingAmount = pendingPayouts?.reduce((total, pending) => total + pending.pendingAmount, 0) || 0;
        const pendingProviderCount = pendingPayouts?.length || 0;

        const mockSummary: ShopSummary = {
          activeProviders: activeProviderCount,
          inventoryValue: 42750.80, // Still mock - needs inventory API
          totalItems: 342, // Still mock - needs inventory API
          recentSales: salesMetrics?.totalSales || 0,
          recentSalesCount: salesMetrics?.transactionCount || 0,
          pendingPayouts: totalPendingAmount,
          pendingPayoutCount: pendingProviderCount,
          recentTransactions: [
            {
              id: '1',
              date: new Date(2024, 10, 20, 14, 30),
              itemName: 'Vintage Leather Jacket',
              provider: 'Sarah Thompson',
              amount: 125.00,
              commission: 62.50
            },
            {
              id: '2',
              date: new Date(2024, 10, 20, 12, 15),
              itemName: 'Antique Jewelry Box',
              provider: 'Mike Chen',
              amount: 89.99,
              commission: 44.99
            },
            {
              id: '3',
              date: new Date(2024, 10, 19, 16, 45),
              itemName: 'Designer Handbag',
              provider: 'Emma Rodriguez',
              amount: 275.00,
              commission: 137.50
            }
          ]
        };

        this.summary.set(mockSummary);
      })
      .catch((error) => {
        console.error('Failed to load dashboard data:', error);
        this.activeProviderCount.set(0);

        // Fallback to mock data if API fails
        const mockSummary: ShopSummary = {
          activeProviders: 0,
          inventoryValue: 42750.80,
          totalItems: 342,
          recentSales: 0, // Show 0 for failed sales data
          recentSalesCount: 0,
          pendingPayouts: 3247.60,
          pendingPayoutCount: 8,
          recentTransactions: []
        };
        this.summary.set(mockSummary);
      });
  }
}