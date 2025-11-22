import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ProviderPortalService } from '../services/provider-portal.service';
import { ProviderItemDetail } from '../models/provider.models';

@Component({
  selector: 'app-provider-item-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="item-detail">
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

      <div class="content" *ngIf="item">
        <!-- Back Button -->
        <div class="back-section">
          <a routerLink="/provider/items" class="back-btn">← Back to My Items</a>
        </div>

        <!-- Item Header -->
        <div class="item-header">
          <h2>{{item.title}}</h2>
          <div class="item-sku">SKU: {{item.sku}}</div>
        </div>

        <!-- Item Content Grid -->
        <div class="item-content">
          <!-- Images Section -->
          <div class="images-section">
            <div class="primary-image">
              <img
                *ngIf="item.primaryImageUrl"
                [src]="item.primaryImageUrl"
                [alt]="item.title"
                loading="lazy">
              <div *ngIf="!item.primaryImageUrl" class="image-placeholder">
                <span>📷</span>
                <p>No image available</p>
              </div>
            </div>

            <!-- Additional Images -->
            <div class="thumbnail-grid" *ngIf="item.imageUrls.length > 1">
              <div
                class="thumbnail"
                *ngFor="let imageUrl of item.imageUrls"
                (click)="selectImage(imageUrl)">
                <img [src]="imageUrl" [alt]="item.title">
              </div>
            </div>
          </div>

          <!-- Item Details Section -->
          <div class="details-section">
            <!-- Status Badge -->
            <div class="status-section">
              <span class="status-badge" [class]="getStatusClass(item.status)">
                {{getStatusDisplay(item.status)}}
              </span>
            </div>

            <!-- Pricing Information -->
            <div class="pricing-section">
              <div class="price-row">
                <span class="price-label">Item Price:</span>
                <span class="price-value">\${{item.price.toFixed(2)}}</span>
              </div>
              <div class="price-row earnings">
                <span class="price-label">Your Earnings:</span>
                <span class="price-value">\${{item.myEarnings.toFixed(2)}}</span>
              </div>
              <div class="commission-note">
                Your commission rate applies to this item
              </div>
            </div>

            <!-- Item Information -->
            <div class="info-section">
              <div class="info-row">
                <span class="info-label">Category:</span>
                <span class="info-value">{{item.category}}</span>
              </div>
              <div class="info-row">
                <span class="info-label">Received Date:</span>
                <span class="info-value">{{formatDate(item.receivedDate)}}</span>
              </div>
              <div class="info-row" *ngIf="item.soldDate">
                <span class="info-label">Sold Date:</span>
                <span class="info-value">{{formatDate(item.soldDate)}}</span>
              </div>
              <div class="info-row" *ngIf="item.salePrice">
                <span class="info-label">Sale Price:</span>
                <span class="info-value">\${{item.salePrice.toFixed(2)}}</span>
              </div>
            </div>

            <!-- Description -->
            <div class="description-section" *ngIf="item.description">
              <h3>Description</h3>
              <p class="description-text">{{item.description}}</p>
            </div>

            <!-- Notes -->
            <div class="notes-section" *ngIf="item.notes">
              <h3>Shop Notes</h3>
              <p class="notes-text">{{item.notes}}</p>
            </div>

            <!-- Contact Information -->
            <div class="contact-section">
              <h3>Questions about this item?</h3>
              <p class="contact-text">
                Contact the shop at <a href="tel:+15551234567">(555) 123-4567</a>
                or speak with them during your next visit.
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading">
        <p>Loading item details...</p>
      </div>

      <!-- Error State -->
      <div class="error" *ngIf="error">
        <p>{{error}}</p>
        <button (click)="loadItem()">Retry</button>
      </div>
    </div>
  `,
  styles: [`
    .item-detail {
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

    .back-section {
      margin-bottom: 1rem;
    }

    .back-btn {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
      font-size: 0.875rem;
    }

    .back-btn:hover {
      color: #2563eb;
    }

    .item-header {
      margin-bottom: 2rem;
    }

    .item-header h2 {
      font-size: 2rem;
      font-weight: 700;
      color: #111827;
      margin: 0 0 0.5rem 0;
    }

    .item-sku {
      font-family: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
      font-size: 0.875rem;
      color: #6b7280;
      font-weight: 500;
    }

    .item-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
    }

    .images-section {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1.5rem;
    }

    .primary-image {
      width: 100%;
      aspect-ratio: 1;
      border-radius: 0.5rem;
      overflow: hidden;
      background: #f9fafb;
      margin-bottom: 1rem;
    }

    .primary-image img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .image-placeholder {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #9ca3af;
    }

    .image-placeholder span {
      font-size: 3rem;
      margin-bottom: 0.5rem;
    }

    .thumbnail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
      gap: 0.5rem;
    }

    .thumbnail {
      aspect-ratio: 1;
      border-radius: 0.375rem;
      overflow: hidden;
      cursor: pointer;
      border: 2px solid transparent;
    }

    .thumbnail:hover {
      border-color: #3b82f6;
    }

    .thumbnail img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .details-section {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1.5rem;
    }

    .status-section {
      margin-bottom: 1.5rem;
    }

    .status-badge {
      display: inline-block;
      padding: 0.5rem 1rem;
      border-radius: 9999px;
      font-size: 0.875rem;
      font-weight: 600;
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

    .pricing-section {
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      padding: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .price-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
    }

    .price-row:last-child {
      margin-bottom: 0;
    }

    .price-label {
      font-weight: 500;
      color: #6b7280;
    }

    .price-value {
      font-weight: 600;
      color: #111827;
      font-size: 1.125rem;
    }

    .earnings .price-value {
      color: #059669;
      font-size: 1.25rem;
    }

    .commission-note {
      font-size: 0.875rem;
      color: #6b7280;
      font-style: italic;
      text-align: center;
      margin-top: 0.75rem;
      padding-top: 0.75rem;
      border-top: 1px solid #e5e7eb;
    }

    .info-section {
      margin-bottom: 1.5rem;
    }

    .info-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 0;
      border-bottom: 1px solid #f3f4f6;
    }

    .info-row:last-child {
      border-bottom: none;
    }

    .info-label {
      font-weight: 500;
      color: #6b7280;
    }

    .info-value {
      font-weight: 600;
      color: #111827;
    }

    .description-section,
    .notes-section,
    .contact-section {
      margin-bottom: 1.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .description-section:last-child,
    .notes-section:last-child,
    .contact-section:last-child {
      border-bottom: none;
    }

    .description-section h3,
    .notes-section h3,
    .contact-section h3 {
      font-size: 1.125rem;
      font-weight: 600;
      color: #111827;
      margin: 0 0 0.75rem 0;
    }

    .description-text,
    .notes-text,
    .contact-text {
      color: #374151;
      line-height: 1.6;
      margin: 0;
    }

    .contact-text a {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
    }

    .contact-text a:hover {
      color: #2563eb;
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

      .item-content {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .item-header h2 {
        font-size: 1.5rem;
      }
    }
  `]
})
export class ProviderItemDetailComponent implements OnInit {
  item: ProviderItemDetail | null = null;
  loading = false;
  error: string | null = null;
  itemId: string;
  selectedImageUrl: string = '';

  constructor(
    private providerService: ProviderPortalService,
    private route: ActivatedRoute
  ) {
    this.itemId = this.route.snapshot.paramMap.get('id') || '';
  }

  ngOnInit() {
    if (this.itemId) {
      this.loadItem();
    }
  }

  loadItem() {
    this.loading = true;
    this.error = null;

    this.providerService.getMyItem(this.itemId).subscribe({
      next: (item) => {
        this.item = item;
        this.selectedImageUrl = item.primaryImageUrl;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load item details. Please try again.';
        this.loading = false;
        console.error('Item detail error:', err);
      }
    });
  }

  selectImage(imageUrl: string) {
    this.selectedImageUrl = imageUrl;
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
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
      case 'available': return '● Available';
      case 'sold': return '○ Sold';
      case 'removed': return '✗ Removed';
      default: return status;
    }
  }
}