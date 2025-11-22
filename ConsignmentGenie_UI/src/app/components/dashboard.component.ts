import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { TransactionService, SalesMetrics } from '../services/transaction.service';
import { AppLayoutComponent } from '../shared/components/app-layout.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, AppLayoutComponent],
  template: `
    <app-layout>
      <div class="dashboard-container">
      <div class="welcome-header">
        <h1>Welcome to {{ currentUser()?.businessName || 'ConsignmentGenie' }}</h1>
        <p *ngIf="currentUser()">Hello, {{ currentUser()?.businessName }}!</p>
      </div>

      <div class="stats-grid" *ngIf="summary(); else loadingStats">
        <div class="stat-card">
          <h3>Total Sales</h3>
          <div class="stat-value">\${{ summary()!.totalSales | number:'1.2-2' }}</div>
        </div>

        <div class="stat-card">
          <h3>Total Commission</h3>
          <div class="stat-value">\${{ summary()!.totalProviderAmount | number:'1.2-2' }}</div>
        </div>

        <div class="stat-card">
          <h3>Your Profit</h3>
          <div class="stat-value">\${{ summary()!.totalShopAmount | number:'1.2-2' }}</div>
        </div>

        <div class="stat-card">
          <h3>Transactions</h3>
          <div class="stat-value">{{ summary()!.transactionCount }}</div>
        </div>
      </div>

      <ng-template #loadingStats>
        <div class="loading">Loading dashboard data...</div>
      </ng-template>

      <div class="quick-actions">
        <h2>Quick Actions</h2>
        <div class="action-grid">
          <a routerLink="/providers" class="action-card">
            <div class="action-icon">👥</div>
            <h3>Manage Providers</h3>
            <p>View and manage your consignment providers</p>
          </a>

          <a routerLink="/items" class="action-card">
            <div class="action-icon">📦</div>
            <h3>Manage Inventory</h3>
            <p>Track items from your providers</p>
          </a>

          <a routerLink="/transactions" class="action-card">
            <div class="action-icon">💰</div>
            <h3>Record Sale</h3>
            <p>Record new transactions and sales</p>
          </a>

          <a routerLink="/payouts" class="action-card">
            <div class="action-icon">📊</div>
            <h3>Generate Payouts</h3>
            <p>Create payout reports for providers</p>
          </a>
        </div>
      </div>
      </div>
    </app-layout>
  `,
  styles: [`
    .dashboard-container {
      padding: 1.5rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .welcome-header {
      margin-bottom: 2rem;
      text-align: center;
    }

    .welcome-header h1 {
      color: #333;
      margin-bottom: 0.5rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
      margin-bottom: 3rem;
    }

    .stat-card {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 1.5rem;
      border-radius: 8px;
      text-align: center;
    }

    .stat-card h3 {
      margin: 0 0 0.75rem 0;
      font-size: 0.875rem;
      font-weight: 500;
      opacity: 0.9;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 700;
    }

    .quick-actions h2 {
      margin-bottom: 1.5rem;
      color: #333;
    }

    .action-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .action-card {
      background: white;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      padding: 1.5rem;
      text-decoration: none;
      color: inherit;
      transition: transform 0.2s, box-shadow 0.2s;
      text-align: center;
    }

    .action-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }

    .action-icon {
      font-size: 2.5rem;
      margin-bottom: 1rem;
    }

    .action-card h3 {
      margin: 0 0 0.75rem 0;
      color: #333;
      font-size: 1.125rem;
    }

    .action-card p {
      margin: 0;
      color: #666;
      font-size: 0.875rem;
      line-height: 1.5;
    }

    .loading {
      text-align: center;
      padding: 2rem;
      color: #666;
    }
  `]
})
export class DashboardComponent implements OnInit {
  currentUser = signal(null as any);
  summary = signal<SalesMetrics | null>(null);

  constructor(
    private authService: AuthService,
    private transactionService: TransactionService
  ) {}

  ngOnInit(): void {
    this.currentUser.set(this.authService.getCurrentUser());
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    // Load last 30 days summary
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);

    this.transactionService.getSalesMetrics({ startDate, endDate }).subscribe({
      next: (summary: SalesMetrics) => {
        this.summary.set(summary);
      },
      error: (error: any) => {
        console.error('Error loading dashboard data:', error);
      }
    });
  }
}