import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-provider-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="provider-dashboard">
      <div class="dashboard-header">
        <h1>Provider Dashboard</h1>
        <p>Manage your inventory and track your sales</p>
      </div>

      <div class="dashboard-grid">
        <div class="dashboard-card">
          <h3>My Items</h3>
          <p class="card-value">0</p>
          <p class="card-description">Items on consignment</p>
        </div>

        <div class="dashboard-card">
          <h3>Pending Sales</h3>
          <p class="card-value">$0.00</p>
          <p class="card-description">Awaiting payout</p>
        </div>

        <div class="dashboard-card">
          <h3>Total Earnings</h3>
          <p class="card-value">$0.00</p>
          <p class="card-description">All time earnings</p>
        </div>

        <div class="dashboard-card">
          <h3>Active Shops</h3>
          <p class="card-value">0</p>
          <p class="card-description">Shops displaying your items</p>
        </div>
      </div>

      <div class="coming-soon">
        <h2>🚧 Provider Dashboard Coming Soon</h2>
        <p>Full provider functionality will be available in Phase 3.</p>
      </div>
    </div>
  `,
  styles: [`
    .provider-dashboard {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .dashboard-header {
      margin-bottom: 2rem;
      text-align: center;
    }

    .dashboard-header h1 {
      color: #8b5cf6;
      margin-bottom: 0.5rem;
    }

    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      margin-bottom: 3rem;
    }

    .dashboard-card {
      background: white;
      border: 2px solid #8b5cf6;
      border-radius: 12px;
      padding: 1.5rem;
      text-align: center;
    }

    .dashboard-card h3 {
      color: #8b5cf6;
      margin-bottom: 1rem;
      font-size: 1.1rem;
    }

    .card-value {
      font-size: 2rem;
      font-weight: bold;
      color: #1f2937;
      margin-bottom: 0.5rem;
    }

    .card-description {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .coming-soon {
      background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
      color: white;
      border-radius: 16px;
      padding: 3rem;
      text-align: center;
    }

    .coming-soon h2 {
      margin-bottom: 1rem;
      font-size: 1.5rem;
    }

    .coming-soon p {
      font-size: 1.1rem;
      opacity: 0.9;
    }

    @media (max-width: 768px) {
      .provider-dashboard {
        padding: 1rem;
      }

      .dashboard-grid {
        grid-template-columns: 1fr;
      }

      .coming-soon {
        padding: 2rem;
      }
    }
  `]
})
export class ProviderDashboardComponent {
}