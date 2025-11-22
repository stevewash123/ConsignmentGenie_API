import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProviderService } from '../services/provider.service';
import { Provider } from '../models/provider.model';

@Component({
  selector: 'app-provider-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="provider-list-container">
      <div class="header">
        <h2>Providers</h2>
        <button class="btn-primary" routerLink="/providers/new">
          Add New Provider
        </button>
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

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 1.5rem;
      align-items: center;
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

  constructor(private providerService: ProviderService) {}

  ngOnInit(): void {
    this.loadProviders();
  }

  loadProviders(): void {
    this.isLoading.set(true);
    this.providerService.getProviders().subscribe({
      next: (providers) => {
        this.providers.set(providers);
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

    this.filteredProviders.set(filtered);
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
}