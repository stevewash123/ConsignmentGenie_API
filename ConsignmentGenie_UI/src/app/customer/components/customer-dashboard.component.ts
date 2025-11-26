import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="customer-dashboard">
      <div class="dashboard-header">
        <h1>Customer Dashboard</h1>
        <p>Welcome to your personal shopping experience</p>
      </div>

      <div class="dashboard-grid">
        <div class="dashboard-card">
          <h3>Recent Purchases</h3>
          <p class="card-value">0</p>
          <p class="card-description">Items purchased this month</p>
        </div>

        <div class="dashboard-card">
          <h3>Total Spent</h3>
          <p class="card-value">$0.00</p>
          <p class="card-description">All time purchases</p>
        </div>

        <div class="dashboard-card">
          <h3>Favorite Shops</h3>
          <p class="card-value">0</p>
          <p class="card-description">Shops you follow</p>
        </div>

        <div class="dashboard-card">
          <h3>Saved Items</h3>
          <p class="card-value">0</p>
          <p class="card-description">Items in your wishlist</p>
        </div>
      </div>

      <div class="coming-soon">
        <h2>🛍️ Customer Portal Coming Soon</h2>
        <p>Full customer shopping experience will be available in Phase 5.</p>
      </div>
    </div>
  `,
  styles: [`
    .customer-dashboard {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .dashboard-header {
      margin-bottom: 2rem;
      text-align: center;
    }

    .dashboard-header h1 {
      color: #06b6d4;
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
      border: 2px solid #06b6d4;
      border-radius: 12px;
      padding: 1.5rem;
      text-align: center;
    }

    .dashboard-card h3 {
      color: #06b6d4;
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
      background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
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
      .customer-dashboard {
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
export class CustomerDashboardComponent {
}