import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Organization {
  id: string;
  name: string;
  subdomain: string;
  ownerEmail: string;
  subscriptionTier: string;
  subscriptionStatus: string;
  verticalType: string;
  userCount: number;
  itemCount: number;
  monthlyRevenue: number;
  createdAt: Date;
  lastActivity: Date;
}

@Component({
  selector: 'app-organization-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="organization-list">
      <header class="page-header">
        <h1>Organizations</h1>
        <p>Manage all ConsignmentGenie organizations</p>
      </header>

      <div class="filters">
        <div class="filter-group">
          <label for="searchTerm">Search:</label>
          <input
            id="searchTerm"
            type="text"
            [(ngModel)]="searchTerm"
            placeholder="Search by name, subdomain, or email"
            (input)="applyFilters()"
          >
        </div>

        <div class="filter-group">
          <label for="statusFilter">Status:</label>
          <select id="statusFilter" [(ngModel)]="statusFilter" (change)="applyFilters()">
            <option value="">All Statuses</option>
            <option value="Active">Active</option>
            <option value="Trial">Trial</option>
            <option value="Suspended">Suspended</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div class="filter-group">
          <label for="tierFilter">Tier:</label>
          <select id="tierFilter" [(ngModel)]="tierFilter" (change)="applyFilters()">
            <option value="">All Tiers</option>
            <option value="Basic">Basic</option>
            <option value="Pro">Pro</option>
            <option value="Enterprise">Enterprise</option>
          </select>
        </div>
      </div>

      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Organization</th>
              <th>Owner</th>
              <th>Subdomain</th>
              <th>Type</th>
              <th>Subscription</th>
              <th>Status</th>
              <th>Users</th>
              <th>Items</th>
              <th>Revenue</th>
              <th>Created</th>
              <th>Last Active</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (org of filteredOrganizations(); track org.id) {
              <tr>
                <td class="org-name">{{ org.name }}</td>
                <td>{{ org.ownerEmail }}</td>
                <td class="subdomain">
                  <code>{{ org.subdomain }}</code>
                  <button class="link-btn" title="Visit Store" (click)="visitStore(org.subdomain)">🔗</button>
                </td>
                <td>
                  <span class="type-badge">{{ org.verticalType }}</span>
                </td>
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
                <td>{{ getTimeAgo(org.lastActivity) }}</td>
                <td class="actions">
                  <button class="btn-action" title="View Details" (click)="viewDetails(org.id)">👁️</button>
                  <button class="btn-action" title="Login As Owner" (click)="loginAsOwner(org.id)">🔐</button>
                  <button class="btn-action" title="Edit Subscription" (click)="editSubscription(org.id)">💳</button>
                  <button class="btn-action danger" title="Suspend" (click)="suspendOrg(org.id)">⛔</button>
                </td>
              </tr>
            }
            @empty {
              <tr>
                <td colspan="12" class="no-data">
                  No organizations found matching your criteria.
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <div class="pagination">
        <span class="results-count">
          Showing {{ filteredOrganizations().length }} of {{ allOrganizations().length }} organizations
        </span>
      </div>
    </div>
  `,
  styles: [`
    .organization-list {
      padding: 2rem;
      max-width: 1600px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .page-header h1 {
      font-size: 2rem;
      color: #1f2937;
      margin-bottom: 0.5rem;
    }

    .page-header p {
      color: #6b7280;
      font-size: 1.1rem;
    }

    .filters {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr;
      gap: 1rem;
      margin-bottom: 2rem;
      padding: 1.5rem;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .filter-group label {
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
    }

    .filter-group input,
    .filter-group select {
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
    }

    .filter-group input:focus,
    .filter-group select:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
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
      font-size: 0.875rem;
    }

    th {
      font-weight: 600;
      color: #374151;
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
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .subdomain code {
      background: #f3f4f6;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
    }

    .link-btn {
      background: none;
      border: none;
      cursor: pointer;
      font-size: 1rem;
    }

    .type-badge {
      background: #e5e7eb;
      color: #374151;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 500;
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
      white-space: nowrap;
    }

    .btn-action {
      background: none;
      border: none;
      font-size: 1rem;
      cursor: pointer;
      margin: 0 0.25rem;
      padding: 0.5rem;
      border-radius: 6px;
      transition: background-color 0.2s;
    }

    .btn-action:hover {
      background: #f3f4f6;
    }

    .btn-action.danger:hover {
      background: #fee2e2;
    }

    .no-data {
      text-align: center;
      color: #6b7280;
      font-style: italic;
      padding: 2rem;
    }

    .pagination {
      margin-top: 1rem;
      text-align: right;
    }

    .results-count {
      color: #6b7280;
      font-size: 0.875rem;
    }

    @media (max-width: 768px) {
      .filters {
        grid-template-columns: 1fr;
      }

      table {
        font-size: 0.75rem;
      }

      th, td {
        padding: 0.5rem;
      }
    }
  `]
})
export class OrganizationListComponent implements OnInit {
  allOrganizations = signal<Organization[]>([]);
  filteredOrganizations = signal<Organization[]>([]);

  searchTerm = '';
  statusFilter = '';
  tierFilter = '';

  ngOnInit() {
    this.loadOrganizations();
  }

  private loadOrganizations() {
    // Mock data - replace with actual API call
    const mockOrgs: Organization[] = [
      {
        id: '1',
        name: 'Demo Consignment Shop',
        subdomain: 'demo-shop',
        ownerEmail: 'admin@demoshop.com',
        subscriptionTier: 'Pro',
        subscriptionStatus: 'Active',
        verticalType: 'Consignment',
        userCount: 4,
        itemCount: 150,
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
        verticalType: 'Consignment',
        userCount: 2,
        itemCount: 89,
        monthlyRevenue: 750,
        createdAt: new Date('2024-02-01'),
        lastActivity: new Date(Date.now() - 86400000)
      },
      {
        id: '3',
        name: 'Artisan Marketplace',
        subdomain: 'artisan-market',
        ownerEmail: 'mike@artisanmarket.com',
        subscriptionTier: 'Enterprise',
        subscriptionStatus: 'Trial',
        verticalType: 'Marketplace',
        userCount: 8,
        itemCount: 300,
        monthlyRevenue: 2800,
        createdAt: new Date('2024-11-01'),
        lastActivity: new Date(Date.now() - 3600000)
      },
      {
        id: '4',
        name: 'Community Thrift Store',
        subdomain: 'community-thrift',
        ownerEmail: 'jane@communitythrift.org',
        subscriptionTier: 'Basic',
        subscriptionStatus: 'Suspended',
        verticalType: 'Thrift',
        userCount: 1,
        itemCount: 45,
        monthlyRevenue: 120,
        createdAt: new Date('2024-03-10'),
        lastActivity: new Date(Date.now() - 86400000 * 7)
      }
    ];

    this.allOrganizations.set(mockOrgs);
    this.filteredOrganizations.set(mockOrgs);
  }

  applyFilters() {
    const search = this.searchTerm.toLowerCase();
    const status = this.statusFilter;
    const tier = this.tierFilter;

    const filtered = this.allOrganizations().filter(org => {
      const matchesSearch = !search ||
        org.name.toLowerCase().includes(search) ||
        org.subdomain.toLowerCase().includes(search) ||
        org.ownerEmail.toLowerCase().includes(search);

      const matchesStatus = !status || org.subscriptionStatus === status;
      const matchesTier = !tier || org.subscriptionTier === tier;

      return matchesSearch && matchesStatus && matchesTier;
    });

    this.filteredOrganizations.set(filtered);
  }

  getTimeAgo(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 60) {
      return `${minutes}m ago`;
    } else if (hours < 24) {
      return `${hours}h ago`;
    } else {
      return `${days}d ago`;
    }
  }

  visitStore(subdomain: string) {
    // Open store in new tab
    window.open(`/store/${subdomain}`, '_blank');
  }

  viewDetails(orgId: string) {
    console.log('View details for org:', orgId);
    // Navigate to org details page
  }

  loginAsOwner(orgId: string) {
    console.log('Login as owner for org:', orgId);
    // Implement admin impersonation
  }

  editSubscription(orgId: string) {
    console.log('Edit subscription for org:', orgId);
    // Navigate to subscription management
  }

  suspendOrg(orgId: string) {
    if (confirm('Are you sure you want to suspend this organization?')) {
      console.log('Suspend org:', orgId);
      // Implement org suspension
    }
  }
}