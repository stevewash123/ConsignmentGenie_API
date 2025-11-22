import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderItem, PagedResult, ProviderItemQuery } from '../models/provider.models';

@Component({
  selector: 'app-provider-items',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="provider-items">
      <!-- Navigation Header -->
      <div class="nav-header">
        <div class="header-content">
          <h1>Main Street Consignment</h1>
          <nav class="nav-links">
            <a routerLink="/provider/dashboard" routerLinkActive="active">Dashboard</a>
            <a routerLink="/provider/items" routerLinkActive="active">Items</a>
            <a routerLink="/provider/sales" routerLinkActive="active">Sales</a>
            <a routerLink="/provider/payouts" routerLinkActive="active">Payouts</a>
          </nav>
        </div>
      </div>

      <div class="content">
        <div class="page-header">
          <h2>My Items</h2>
        </div>

        <!-- Status Filter Tabs -->
        <div class="filter-tabs">
          <button
            class="tab"
            [class.active]="selectedStatus === null"
            (click)="filterByStatus(null)">
            All ({{totalItemsCount}})
          </button>
          <button
            class="tab"
            [class.active]="selectedStatus === 'Available'"
            (click)="filterByStatus('Available')">
            Available ({{availableCount}})
          </button>
          <button
            class="tab"
            [class.active]="selectedStatus === 'Sold'"
            (click)="filterByStatus('Sold')">
            Sold ({{soldCount}})
          </button>
          <button
            class="tab"
            [class.active]="selectedStatus === 'Removed'"
            (click)="filterByStatus('Removed')">
            Removed ({{removedCount}})
          </button>
        </div>

        <!-- Items Table -->
        <div class="items-table" *ngIf="itemsResult">
          <div class="table-header">
            <div class="col photo-col">Photo</div>
            <div class="col item-col">Item</div>
            <div class="col price-col">Price</div>
            <div class="col earnings-col">My Earnings</div>
            <div class="col status-col">Status</div>
          </div>

          <div class="table-row"
               *ngFor="let item of itemsResult.items"
               [routerLink]="['/provider/items', item.itemId]">
            <div class="col photo-col">
              <div class="item-photo">
                <img *ngIf="item.primaryImageUrl"
                     [src]="item.primaryImageUrl"
                     [alt]="item.title"
                     loading="lazy">
                <div *ngIf="!item.primaryImageUrl" class="photo-placeholder">📷</div>
              </div>
            </div>
            <div class="col item-col">
              <div class="item-info">
                <div class="item-title">{{item.title}}</div>
                <div class="item-sku">{{item.sku}}</div>
              </div>
            </div>
            <div class="col price-col">
              \${{item.price.toFixed(2)}}
            </div>
            <div class="col earnings-col">
              \${{item.myEarnings.toFixed(2)}}
            </div>
            <div class="col status-col">
              <span class="status-badge" [class]="getStatusClass(item.status)">
                {{getStatusDisplay(item.status)}}
              </span>
            </div>
          </div>

          <!-- No Items Message -->
          <div class="no-items" *ngIf="itemsResult.items.length === 0">
            <p>No items found.</p>
          </div>
        </div>

        <!-- Pagination -->
        <div class="pagination" *ngIf="itemsResult && itemsResult.totalPages > 1">
          <button
            class="pagination-btn"
            [disabled]="!itemsResult.hasPrevious"
            (click)="goToPage(currentPage - 1)">
            ← Previous
          </button>

          <span class="pagination-info">
            Page {{currentPage}} of {{itemsResult.totalPages}}
          </span>

          <button
            class="pagination-btn"
            [disabled]="!itemsResult.hasNext"
            (click)="goToPage(currentPage + 1)">
            Next →
          </button>
        </div>

        <!-- Footer Info -->
        <div class="footer-info">
          <div class="commission-info">
            Your commission rate: {{commissionRate}}%
          </div>
          <div class="contact-info">
            Questions about an item? Contact the shop at (555) 123-4567
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading">
        <p>Loading items...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadItems()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .provider-items {
      min-height: 100vh;
      background: #f9fafb;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }

    .nav-header {
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

    .nav-links {
      display: flex;
      gap: 1.5rem;
    }

    .nav-links a {
      color: #6b7280;
      text-decoration: none;
      font-weight: 500;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
    }

    .nav-links a:hover,
    .nav-links a.active {
      color: #3b82f6;
      background: #f3f4f6;
    }

    .content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .page-header h2 {
      font-size: 1.875rem;
      font-weight: 700;
      color: #111827;
      margin: 0 0 2rem 0;
    }

    .filter-tabs {
      display: flex;
      gap: 0.5rem;
      margin-bottom: 2rem;
      border-bottom: 1px solid #e5e7eb;
      padding-bottom: 1rem;
    }

    .tab {
      background: none;
      border: none;
      padding: 0.5rem 1rem;
      font-weight: 500;
      color: #6b7280;
      cursor: pointer;
      border-radius: 0.375rem;
    }

    .tab:hover {
      background: #f3f4f6;
      color: #374151;
    }

    .tab.active {
      background: #3b82f6;
      color: white;
    }

    .items-table {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      overflow: hidden;
      margin-bottom: 2rem;
    }

    .table-header {
      display: grid;
      grid-template-columns: 80px 2fr 120px 120px 100px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      background: #f9fafb;
      border-bottom: 1px solid #e5e7eb;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
    }

    .table-row {
      display: grid;
      grid-template-columns: 80px 2fr 120px 120px 100px;
      gap: 1rem;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #f3f4f6;
      cursor: pointer;
      transition: background-color 0.15s ease;
      text-decoration: none;
      color: inherit;
    }

    .table-row:hover {
      background: #f9fafb;
    }

    .table-row:last-child {
      border-bottom: none;
    }

    .col {
      display: flex;
      align-items: center;
    }

    .item-photo {
      width: 60px;
      height: 60px;
      border-radius: 0.375rem;
      overflow: hidden;
      background: #f3f4f6;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .item-photo img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .photo-placeholder {
      font-size: 1.5rem;
      color: #9ca3af;
    }

    .item-info {
      min-width: 0;
    }

    .item-title {
      font-weight: 600;
      color: #111827;
      margin-bottom: 0.25rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .item-sku {
      font-size: 0.875rem;
      color: #6b7280;
    }

    .status-badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .status-available {
      background: #dcfce7;
      color: #166534;
    }

    .status-sold {
      background: #e0e7ff;
      color: #3730a3;
    }

    .status-removed {
      background: #fef2f2;
      color: #991b1b;
    }

    .no-items {
      padding: 3rem;
      text-align: center;
      color: #6b7280;
      font-style: italic;
    }

    .pagination {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }

    .pagination-btn {
      background: #3b82f6;
      color: white;
      border: none;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      cursor: pointer;
      font-weight: 500;
    }

    .pagination-btn:disabled {
      background: #d1d5db;
      cursor: not-allowed;
    }

    .pagination-info {
      font-weight: 500;
      color: #374151;
    }

    .footer-info {
      border-top: 1px solid #e5e7eb;
      padding-top: 1.5rem;
      text-align: center;
    }

    .commission-info {
      font-weight: 600;
      color: #111827;
      margin-bottom: 0.5rem;
    }

    .contact-info {
      color: #6b7280;
      font-size: 0.875rem;
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
      .nav-header {
        padding: 1rem;
      }

      .header-content {
        flex-direction: column;
        gap: 1rem;
        align-items: flex-start;
      }

      .nav-links {
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .content {
        padding: 1rem;
      }

      .filter-tabs {
        flex-wrap: wrap;
      }

      .table-header {
        display: none;
      }

      .table-row {
        grid-template-columns: 1fr;
        gap: 1rem;
        padding: 1.5rem;
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        margin-bottom: 1rem;
      }

      .col {
        justify-content: space-between;
        padding: 0.5rem 0;
        border-bottom: 1px solid #f3f4f6;
      }

      .col:last-child {
        border-bottom: none;
      }

      .photo-col {
        justify-content: center;
      }

      .pagination {
        flex-direction: column;
        gap: 1rem;
      }
    }
  `]
})
export class ProviderItemsComponent implements OnInit {
  itemsResult: PagedResult<ProviderItem> | null = null;
  loading = false;
  error: string | null = null;

  selectedStatus: string | null = null;
  currentPage = 1;
  pageSize = 20;
  commissionRate = 50;

  // Counts for tabs
  totalItemsCount = 0;
  availableCount = 0;
  soldCount = 0;
  removedCount = 0;

  constructor(private providerService: ProviderPortalService) {}

  ngOnInit() {
    this.loadItems();
    this.loadItemCounts();
  }

  loadItems() {
    this.loading = true;
    this.error = null;

    const query: ProviderItemQuery = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.selectedStatus) {
      query.status = this.selectedStatus;
    }

    this.providerService.getMyItems(query).subscribe({
      next: (result) => {
        this.itemsResult = result;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load items. Please try again.';
        this.loading = false;
        console.error('Items error:', err);
      }
    });
  }

  loadItemCounts() {
    // Load counts for each status to display in tabs
    // This would require separate API calls or the API could return counts in the main response
    // For now, using placeholder values
    this.totalItemsCount = 24;
    this.availableCount = 12;
    this.soldCount = 10;
    this.removedCount = 2;
  }

  filterByStatus(status: string | null) {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadItems();
  }

  goToPage(page: number) {
    if (page >= 1 && this.itemsResult && page <= this.itemsResult.totalPages) {
      this.currentPage = page;
      this.loadItems();
    }
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'available': return 'status-available';
      case 'sold': return 'status-sold';
      case 'removed': return 'status-removed';
      default: return 'status-available';
    }
  }

  getStatusDisplay(status: string): string {
    switch (status.toLowerCase()) {
      case 'available': return '● Avail';
      case 'sold': return '○ Sold';
      case 'removed': return '✗ Removed';
      default: return status;
    }
  }
}