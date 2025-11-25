import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProviderService } from '../services/provider.service';
import { Provider } from '../models/provider.model';
import { ProviderInvitationModalComponent } from './provider-invitation-modal.component';

@Component({
  selector: 'app-provider-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ProviderInvitationModalComponent],
  template: `
    <div class="provider-list-container">
      <div class="header">
        <h2>Providers</h2>
        <div class="header-actions">
          <button class="btn-secondary" (click)="showInviteModal()">
            Invite Provider
          </button>
          <button class="btn-primary" routerLink="/providers/new">
            Add New Provider
          </button>
        </div>
      </div>

      <!-- Stats Dashboard -->
      <div class="stats-dashboard" *ngIf="!isLoading()">
        <div class="stat-card">
          <div class="stat-number">{{ getStats().total }}</div>
          <div class="stat-label">Total Providers</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ getStats().active }}</div>
          <div class="stat-label">Active</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ getStats().inactive }}</div>
          <div class="stat-label">Inactive</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ getStats().avgCommission }}%</div>
          <div class="stat-label">Avg Commission</div>
        </div>
      </div>

      <div class="filters">
        <div class="filter-group">
          <label>
            <input
              type="checkbox"
              [checked]="showActiveOnly()"
              (change)="toggleActiveFilter()"
            >
            Show Active Only
          </label>
        </div>
        <div class="sort-group">
          <label for="sortBy">Sort by:</label>
          <select
            id="sortBy"
            [(ngModel)]="sortBy"
            (change)="onSortChange()"
            class="sort-select"
          >
            <option value="name">Name</option>
            <option value="email">Email</option>
            <option value="commissionRate">Commission Rate</option>
            <option value="createdAt">Date Added</option>
          </select>
          <button
            class="sort-direction-btn"
            (click)="toggleSortDirection()"
            [title]="sortDirection === 'asc' ? 'Sort Descending' : 'Sort Ascending'"
          >
            {{ sortDirection === 'asc' ? '↑' : '↓' }}
          </button>
        </div>
        <div class="search-group">
          <input
            type="text"
            placeholder="Search providers..."
            [(ngModel)]="searchTerm"
            (input)="onSearchChange()"
            class="search-input"
          >
        </div>
      </div>

      <div class="provider-grid" *ngIf="!isLoading(); else loading">
        <div class="provider-card" *ngFor="let provider of filteredProviders(); trackBy: trackByProvider">
          <div class="provider-header">
            <h3>{{ provider.name }}</h3>
            <div class="status" [class.active]="provider.isActive" [class.inactive]="!provider.isActive">
              {{ provider.isActive ? 'Active' : 'Inactive' }}
            </div>
          </div>

          <div class="provider-details">
            <div class="detail" *ngIf="provider.email">
              <strong>Email:</strong> {{ provider.email }}
            </div>
            <div class="detail" *ngIf="provider.phone">
              <strong>Phone:</strong> {{ provider.phone }}
            </div>
            <div class="detail">
              <strong>Commission:</strong> {{ provider.commissionRate }}%
            </div>
          </div>

          <div class="provider-actions">
            <button class="btn-secondary" [routerLink]="['/providers', provider.id]">
              View Details
            </button>
            <button class="btn-secondary" [routerLink]="['/providers', provider.id, 'edit']">
              Edit
            </button>
            <button
              class="btn-secondary"
              *ngIf="provider.isActive"
              (click)="deactivateProvider(provider)"
            >
              Deactivate
            </button>
            <button
              class="btn-success"
              *ngIf="!provider.isActive"
              (click)="activateProvider(provider)"
            >
              Activate
            </button>
          </div>
        </div>
      </div>

      <ng-template #loading>
        <div class="loading">Loading providers...</div>
      </ng-template>

      <div class="no-providers" *ngIf="!isLoading() && filteredProviders().length === 0">
        <p>No providers found. <a routerLink="/providers/new">Add your first provider</a></p>
      </div>
    </div>

    <!-- Provider Invitation Modal -->
    <app-provider-invitation-modal
      [isVisible]="isInviteModalVisible"
      (close)="hideInviteModal()"
      (invitationSent)="onInvitationSent()">
    </app-provider-invitation-modal>
  `,
  styles: [`
    .provider-list-container {
      padding: 1.5rem;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }

    .header-actions {
      display: flex;
      gap: 1rem;
    }

    .stats-dashboard {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }

    .stat-card {
      background: white;
      border: 1px solid #e9ecef;
      border-radius: 8px;
      padding: 1.5rem;
      text-align: center;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .stat-number {
      font-size: 2rem;
      font-weight: 600;
      color: #007bff;
      margin-bottom: 0.5rem;
    }

    .stat-label {
      color: #6c757d;
      font-size: 0.875rem;
    }

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 1.5rem;
      align-items: center;
      flex-wrap: wrap;
    }

    .sort-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .sort-select {
      padding: 0.5rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 0.875rem;
    }

    .sort-direction-btn {
      background: #f8f9fa;
      border: 1px solid #ddd;
      border-radius: 4px;
      width: 32px;
      height: 32px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
      transition: background-color 0.15s ease-in-out;
    }

    .sort-direction-btn:hover {
      background: #e9ecef;
    }

    .search-input {
      padding: 0.5rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      width: 300px;
    }

    .provider-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 1.5rem;
    }

    .provider-card {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 1.5rem;
      background: white;
    }

    .provider-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .status {
      padding: 0.25rem 0.75rem;
      border-radius: 4px;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .status.active {
      background: #d4edda;
      color: #155724;
    }

    .status.inactive {
      background: #f8d7da;
      color: #721c24;
    }

    .provider-details {
      margin-bottom: 1.5rem;
    }

    .detail {
      margin-bottom: 0.5rem;
    }

    .provider-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .btn-primary, .btn-secondary, .btn-success {
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      text-decoration: none;
      display: inline-block;
      text-align: center;
      font-size: 0.875rem;
    }

    .btn-primary {
      background: #007bff;
      color: white;
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
    }

    .btn-success {
      background: #28a745;
      color: white;
    }

    .loading, .no-providers {
      text-align: center;
      padding: 2rem;
      color: #6c757d;
    }
  `]
})
export class ProviderListComponent implements OnInit {
  providers = signal<Provider[]>([]);
  filteredProviders = signal<Provider[]>([]);
  isLoading = signal(true);
  showActiveOnly = signal(true);
  searchTerm = '';
  sortBy = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';
  isInviteModalVisible = signal(false);

  constructor(private providerService: ProviderService) {}

  ngOnInit(): void {
    this.loadProviders();
  }

  loadProviders(): void {
    this.isLoading.set(true);
    this.providerService.getProviders().subscribe({
      next: (providers) => {
        console.log('Providers API response:', providers);

        // Handle API response - might be wrapped in response object
        let providersArray = providers;
        if (providers && typeof providers === 'object' && !Array.isArray(providers)) {
          const response = providers as any;
          providersArray = response.items || response.data || response.providers || [];
          console.log('Extracted providers array:', providersArray);
        }

        // Ensure we have an array and transform to match frontend model
        let finalProviders = Array.isArray(providersArray) ? providersArray : [];

        // Transform API response to match frontend Provider model
        finalProviders = finalProviders.map((apiProvider: any) => ({
          id: apiProvider.providerId || apiProvider.id,
          name: apiProvider.fullName || apiProvider.name || 'Unknown Provider',
          email: apiProvider.email,
          phone: apiProvider.phone,
          address: apiProvider.address || apiProvider.addressLine1,
          commissionRate: (apiProvider.commissionRate * 100) || 0, // Convert decimal to percentage
          preferredPaymentMethod: apiProvider.preferredPaymentMethod,
          paymentDetails: apiProvider.paymentDetails,
          notes: apiProvider.notes,
          isActive: apiProvider.status === 'Active' || apiProvider.isActive === true,
          organizationId: apiProvider.organizationId,
          providerNumber: apiProvider.providerNumber,
          createdAt: new Date(apiProvider.createdAt),
          updatedAt: new Date(apiProvider.updatedAt || apiProvider.createdAt)
        }));

        console.log('Transformed providers:', finalProviders);
        this.providers.set(finalProviders);
        this.applyFilters();
      },
      error: (error) => {
        console.error('Error loading providers:', error);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  toggleActiveFilter(): void {
    this.showActiveOnly.update(value => !value);
    this.applyFilters();
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = this.providers();

    // Ensure filtered is an array before applying filters
    if (!Array.isArray(filtered)) {
      filtered = [];
    }

    if (this.showActiveOnly()) {
      filtered = filtered.filter(p => p.isActive);
    }

    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(p =>
        p.name.toLowerCase().includes(term) ||
        p.email?.toLowerCase().includes(term) ||
        p.phone?.includes(term)
      );
    }

    // Apply sorting
    filtered = this.sortProviders(filtered);

    this.filteredProviders.set(filtered);
  }

  sortProviders(providers: Provider[]): Provider[] {
    return [...providers].sort((a, b) => {
      let aValue: any = a[this.sortBy as keyof Provider];
      let bValue: any = b[this.sortBy as keyof Provider];

      // Handle undefined values
      if (aValue === undefined) aValue = '';
      if (bValue === undefined) bValue = '';

      // Convert to strings for comparison
      if (typeof aValue === 'string') aValue = aValue.toLowerCase();
      if (typeof bValue === 'string') bValue = bValue.toLowerCase();

      let result: number;
      if (aValue < bValue) {
        result = -1;
      } else if (aValue > bValue) {
        result = 1;
      } else {
        result = 0;
      }

      return this.sortDirection === 'asc' ? result : -result;
    });
  }

  onSortChange(): void {
    this.applyFilters();
  }

  toggleSortDirection(): void {
    this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.applyFilters();
  }

  getStats() {
    const providers = this.providers();
    const total = providers.length;
    const active = providers.filter(p => p.isActive).length;
    const inactive = total - active;

    const avgCommission = total > 0
      ? Math.round(providers.reduce((sum, p) => sum + p.commissionRate, 0) / total * 10) / 10
      : 0;

    return {
      total,
      active,
      inactive,
      avgCommission
    };
  }

  deactivateProvider(provider: Provider): void {
    this.providerService.deactivateProvider(provider.id).subscribe({
      next: (updated) => {
        const providers = this.providers();
        const index = providers.findIndex(p => p.id === updated.id);
        if (index >= 0) {
          providers[index] = updated;
          this.providers.set([...providers]);
          this.applyFilters();
        }
      },
      error: (error) => {
        console.error('Error deactivating provider:', error);
      }
    });
  }

  activateProvider(provider: Provider): void {
    this.providerService.activateProvider(provider.id).subscribe({
      next: (updated) => {
        const providers = this.providers();
        const index = providers.findIndex(p => p.id === updated.id);
        if (index >= 0) {
          providers[index] = updated;
          this.providers.set([...providers]);
          this.applyFilters();
        }
      },
      error: (error) => {
        console.error('Error activating provider:', error);
      }
    });
  }

  trackByProvider(index: number, provider: Provider): number {
    return provider.id;
  }

  showInviteModal(): void {
    this.isInviteModalVisible.set(true);
  }

  hideInviteModal(): void {
    this.isInviteModalVisible.set(false);
  }

  onInvitationSent(): void {
    // Optionally reload providers to refresh the list
    // For now, we'll just hide the modal and show a success message
    console.log('Provider invitation sent successfully');
  }
}