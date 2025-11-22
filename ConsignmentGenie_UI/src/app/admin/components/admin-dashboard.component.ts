import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminLayoutComponent } from './admin-layout.component';

interface OrganizationSummary {
  id: string;
  name: string;
  subdomain: string;
  ownerEmail: string;
  subscriptionTier: string;
  subscriptionStatus: string;
  userCount: number;
  itemCount: number;
  transactionCount: number;
  monthlyRevenue: number;
  createdAt: Date;
  lastActivity: Date;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, AdminLayoutComponent],
  template: `
    <app-admin-layout>
      <div class="admin-dashboard">
      <header class="dashboard-header">
        <h1>System Administrator Dashboard</h1>
        <p class="subtitle">ConsignmentGenie Platform Overview</p>
      </header>

      <div class="stats-grid">
        <div class="stat-card">
          <h3>Total Organizations</h3>
          <div class="stat-value">{{ totalOrgs() }}</div>
        </div>
        <div class="stat-card">
          <h3>Active Organizations</h3>
          <div class="stat-value">{{ activeOrgs() }}</div>
        </div>
        <div class="stat-card">
          <h3>Total Users</h3>
          <div class="stat-value">{{ totalUsers() }}</div>
        </div>
        <div class="stat-card">
          <h3>Monthly Revenue</h3>
          <div class="stat-value">\${{ monthlyRevenue().toLocaleString() }}</div>
        </div>
      </div>

      <div class="recent-activity">
        <h2>Recent Organizations</h2>
        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Organization</th>
                <th>Owner</th>
                <th>Subdomain</th>
                <th>Subscription</th>
                <th>Status</th>
                <th>Users</th>
                <th>Items</th>
                <th>Revenue</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (org of organizations(); track org.id) {
                <tr>
                  <td class="org-name">{{ org.name }}</td>
                  <td>{{ org.ownerEmail }}</td>
                  <td class="subdomain">{{ org.subdomain }}</td>
                  <td>
                    <span class="tier-badge" [class]="'tier-' + org.subscriptionTier.toLowerCase()">
                      {{ org.subscriptionTier }}
                    </span>
                  </td>
                  <td>
                    <span class="status-badge" [class]="'status-' + org.subscriptionStatus.toLowerCase()">
                      {{ org.subscriptionStatus }}
                    </span>
                  </td>
                  <td class="text-center">{{ org.userCount }}</td>
                  <td class="text-center">{{ org.itemCount }}</td>
                  <td class="text-right">\${{ org.monthlyRevenue.toLocaleString() }}</td>
                  <td>{{ org.createdAt | date:'short' }}</td>
                  <td class="actions">
                    <button class="btn-action" title="View Details">👁️</button>
                    <button class="btn-action" title="Login As Owner">🔐</button>
                    <button class="btn-action" title="Manage Subscription">💳</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
      </div>
    </app-admin-layout>
  `,
  styles: [`
    .admin-dashboard {
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
      color: #1f2937;
      margin-bottom: 0.5rem;
    }

    .subtitle {
      color: #6b7280;
      font-size: 1.1rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      margin-bottom: 3rem;
    }

    .stat-card {
      background: white;
      border-radius: 12px;
      padding: 2rem;
      text-align: center;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      border: 1px solid #e5e7eb;
    }

    .stat-card h3 {
      font-size: 1rem;
      color: #6b7280;
      margin-bottom: 1rem;
      font-weight: 500;
    }

    .stat-value {
      font-size: 3rem;
      font-weight: bold;
      color: #1f2937;
    }

    .recent-activity h2 {
      font-size: 1.5rem;
      color: #1f2937;
      margin-bottom: 1.5rem;
    }

    .table-container {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      border: 1px solid #e5e7eb;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    thead {
      background: #f9fafb;
    }

    th, td {
      padding: 1rem;
      text-align: left;
      border-bottom: 1px solid #e5e7eb;
    }

    th {
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    tbody tr:hover {
      background: #f9fafb;
    }

    .org-name {
      font-weight: 600;
      color: #1f2937;
    }

    .subdomain {
      font-family: monospace;
      color: #6b7280;
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
    .status-cancelled { background: #f3f4f6; color: #6b7280; }

    .text-center { text-align: center; }
    .text-right { text-align: right; }

    .actions {
      text-align: center;
    }

    .btn-action {
      background: none;
      border: none;
      font-size: 1.2rem;
      cursor: pointer;
      margin: 0 0.25rem;
      padding: 0.5rem;
      border-radius: 6px;
      transition: background-color 0.2s;
    }

    .btn-action:hover {
      background: #f3f4f6;
    }
  `]
})
export class AdminDashboardComponent implements OnInit {
  organizations = signal<OrganizationSummary[]>([]);

  totalOrgs = signal(0);
  activeOrgs = signal(0);
  totalUsers = signal(0);
  monthlyRevenue = signal(0);

  ngOnInit() {
    this.loadDashboardData();
  }

  private loadDashboardData() {
    // Mock data for now - replace with actual API calls
    const mockOrgs: OrganizationSummary[] = [
      {
        id: '1',
        name: 'Demo Consignment Shop',
        subdomain: 'demo-shop',
        ownerEmail: 'admin@demoshop.com',
        subscriptionTier: 'Pro',
        subscriptionStatus: 'Active',
        userCount: 4,
        itemCount: 150,
        transactionCount: 45,
        monthlyRevenue: 1250,
        createdAt: new Date('2024-01-15'),
        lastActivity: new Date()
      },
      {
        id: '2',
        name: 'Vintage Treasures',
        subdomain: 'vintage-treasures',
        ownerEmail: 'sarah@vintagetreasures.com',
        subscriptionTier: 'Basic',
        subscriptionStatus: 'Active',
        userCount: 2,
        itemCount: 89,
        transactionCount: 23,
        monthlyRevenue: 750,
        createdAt: new Date('2024-02-01'),
        lastActivity: new Date(Date.now() - 86400000) // 1 day ago
      },
      {
        id: '3',
        name: 'Artisan Marketplace',
        subdomain: 'artisan-market',
        ownerEmail: 'mike@artisanmarket.com',
        subscriptionTier: 'Enterprise',
        subscriptionStatus: 'Trial',
        userCount: 8,
        itemCount: 300,
        transactionCount: 120,
        monthlyRevenue: 2800,
        createdAt: new Date('2024-11-01'),
        lastActivity: new Date(Date.now() - 3600000) // 1 hour ago
      }
    ];

    this.organizations.set(mockOrgs);
    this.totalOrgs.set(mockOrgs.length);
    this.activeOrgs.set(mockOrgs.filter(org => org.subscriptionStatus === 'Active').length);
    this.totalUsers.set(mockOrgs.reduce((sum, org) => sum + org.userCount, 0));
    this.monthlyRevenue.set(mockOrgs.reduce((sum, org) => sum + org.monthlyRevenue, 0));
  }
}