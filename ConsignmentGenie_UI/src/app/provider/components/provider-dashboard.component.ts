import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderDashboard } from '../models/provider.models';

@Component({
  selector: 'app-provider-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="provider-dashboard">
      <!-- Header -->
      <div class="dashboard-header">
        <div class="header-content">
          <h1>{{dashboard?.shopName || 'Provider Portal'}}</h1>
          <button class="logout-btn" (click)="logout()">Logout</button>
        </div>
        <div class="welcome-message">
          <h2>Welcome back, {{dashboard?.providerName || 'Provider'}}!</h2>
        </div>
      </div>

      <div class="dashboard-content" *ngIf="dashboard">
        <!-- Metrics Cards -->
        <div class="metrics-grid">
          <div class="metric-card">
            <div class="metric-title">MY ITEMS</div>
            <div class="metric-title">ON FLOOR</div>
            <div class="metric-value">{{dashboard.availableItems}}</div>
          </div>

          <div class="metric-card">
            <div class="metric-title">PENDING</div>
            <div class="metric-title">EARNINGS</div>
            <div class="metric-value">\${{dashboard.pendingBalance.toFixed(2)}}</div>
          </div>

          <div class="metric-card">
            <div class="metric-title">SOLD THIS</div>
            <div class="metric-title">MONTH</div>
            <div class="metric-value">{{dashboard.soldItems}}</div>
          </div>

          <div class="metric-card">
            <div class="metric-title">ALL-TIME</div>
            <div class="metric-title">EARNINGS</div>
            <div class="metric-value">\${{dashboard.totalEarningsAllTime.toFixed(2)}}</div>
          </div>
        </div>

        <!-- Recent Sales -->
        <div class="section">
          <div class="section-header">
            <h3>Recent Sales</h3>
            <a routerLink="/provider/sales" class="view-all-link">View All →</a>
          </div>
          <div class="sales-table">
            <div class="table-header">
              <div class="col">Date</div>
              <div class="col">Item</div>
              <div class="col">Price</div>
              <div class="col">You earn</div>
            </div>
            <div class="table-row" *ngFor="let sale of dashboard.recentSales">
              <div class="col">{{formatDate(sale.saleDate)}}</div>
              <div class="col">{{sale.itemTitle}}</div>
              <div class="col">\${{sale.salePrice.toFixed(2)}}</div>
              <div class="col">\${{sale.myEarnings.toFixed(2)}}</div>
            </div>
            <div class="no-data" *ngIf="dashboard.recentSales.length === 0">
              No recent sales
            </div>
          </div>
        </div>

        <!-- Last Payout -->
        <div class="section">
          <div class="section-header">
            <h3>Last Payout</h3>
            <a routerLink="/provider/payouts" class="view-all-link">View All →</a>
          </div>
          <div class="payout-info" *ngIf="dashboard.lastPayout">
            <div class="payout-row">
              <div class="col">{{formatDate(dashboard.lastPayout.payoutDate)}}</div>
              <div class="col">\${{dashboard.lastPayout.amount.toFixed(2)}}</div>
              <div class="col">{{dashboard.lastPayout.itemCount}} items</div>
              <div class="col">Via {{dashboard.lastPayout.paymentMethod}}</div>
            </div>
          </div>
          <div class="no-data" *ngIf="!dashboard.lastPayout">
            No payouts yet
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="!dashboard && !error">
        <p>Loading dashboard...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadDashboard()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .provider-dashboard {
      min-height: 100vh;
      background: #f9fafb;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }

    .dashboard-header {
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

    .logout-btn {
      background: #ef4444;
      color: white;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      cursor: pointer;
      font-weight: 500;
    }

    .logout-btn:hover {
      background: #dc2626;
    }

    .welcome-message {
      max-width: 1200px;
      margin: 1rem auto 0;
    }

    .welcome-message h2 {
      font-size: 1.875rem;
      font-weight: 700;
      color: #111827;
      margin: 0;
    }

    .dashboard-content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .metric-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1.5rem;
      text-align: center;
    }

    .metric-title {
      font-size: 0.75rem;
      font-weight: 600;
      color: #6b7280;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .metric-value {
      font-size: 2rem;
      font-weight: 700;
      color: #111827;
      margin-top: 0.5rem;
    }

    .section {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      margin-bottom: 2rem;
      overflow: hidden;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .section-header h3 {
      font-size: 1.125rem;
      font-weight: 600;
      color: #111827;
      margin: 0;
    }

    .view-all-link {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
    }

    .view-all-link:hover {
      color: #2563eb;
    }

    .sales-table, .payout-info {
      padding: 0;
    }

    .table-header, .table-row, .payout-row {
      display: grid;
      grid-template-columns: 1fr 2fr 1fr 1fr;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .table-header {
      background: #f9fafb;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
    }

    .table-row:hover {
      background: #f9fafb;
    }

    .table-row:last-child, .payout-row:last-child {
      border-bottom: none;
    }

    .col {
      display: flex;
      align-items: center;
    }

    .no-data {
      padding: 2rem;
      text-align: center;
      color: #6b7280;
      font-style: italic;
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
      .dashboard-header {
        padding: 1rem;
      }

      .header-content h1 {
        font-size: 1.25rem;
      }

      .welcome-message h2 {
        font-size: 1.5rem;
      }

      .dashboard-content {
        padding: 1rem;
      }

      .metrics-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }

      .table-header, .table-row, .payout-row {
        grid-template-columns: 1fr;
        gap: 0.5rem;
        padding: 1rem;
      }

      .table-header {
        display: none;
      }

      .col {
        padding: 0.25rem 0;
        border-bottom: 1px solid #f3f4f6;
      }

      .col:last-child {
        border-bottom: none;
      }
    }
  `]
})
export class ProviderDashboardComponent implements OnInit {
  dashboard: ProviderDashboard | null = null;
  error: string | null = null;

  constructor(private providerService: ProviderPortalService) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.error = null;
    this.providerService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
      },
      error: (err) => {
        this.error = 'Failed to load dashboard data. Please try again.';
        console.error('Dashboard error:', err);
      }
    });
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  }

  logout() {
    // TODO: Implement logout logic
    localStorage.removeItem('token');
    window.location.href = '/login';
  }
}