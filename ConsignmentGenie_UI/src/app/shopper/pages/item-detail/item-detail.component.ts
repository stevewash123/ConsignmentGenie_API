import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';
import {
  ShopperCatalogService,
  ShopperItemDetail,
  ShopperItemImage,
  ShopperItemList
} from '../../services/shopper-catalog.service';
import { ShopperCartService } from '../../services/shopper-cart.service';

@Component({
  selector: 'app-item-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="item-detail-container">
      <div class="container">
        <!-- Back Navigation -->
        <div class="back-navigation">
          <button class="btn btn-outline-secondary" (click)="goBack()">
            ← Back to Catalog
          </button>
          <nav class="breadcrumb">
            <a [routerLink]="['/shop', storeSlug]">{{ storeInfo?.name || 'Store' }}</a>
            <span class="breadcrumb-separator">/</span>
            <span class="current-page">{{ item?.title || 'Item Detail' }}</span>
          </nav>
        </div>

        <!-- Loading State -->
        <div class="loading-state" *ngIf="isLoading">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p>Loading item details...</p>
        </div>

        <!-- Error State -->
        <div class="error-message" *ngIf="error && !isLoading">
          <div class="alert alert-danger">
            {{ error }}
            <button class="btn btn-outline-primary btn-sm ms-2" (click)="loadItemDetail()">Retry</button>
          </div>
        </div>

        <!-- Item Detail Content -->
        <div class="item-detail-content" *ngIf="item && !isLoading && !error">
          <div class="item-detail-grid">
            <!-- Image Gallery -->
            <div class="image-section">
              <div class="main-image">
                <img
                  [src]="selectedImage?.imageUrl || '/assets/placeholder-item.jpg'"
                  [alt]="selectedImage?.altText || item.title"
                  class="main-img"
                  (error)="onImageError($event)">
              </div>
              <div class="image-thumbnails" *ngIf="item.images.length > 1">
                <button
                  *ngFor="let image of item.images"
                  class="thumbnail-btn"
                  [class.active]="selectedImage?.imageId === image.imageId"
                  (click)="selectImage(image)">
                  <img
                    [src]="image.imageUrl"
                    [alt]="image.altText || item.title"
                    class="thumbnail-img"
                    (error)="onImageError($event)">
                </button>
              </div>
            </div>

            <!-- Item Information -->
            <div class="info-section">
              <div class="item-header">
                <h1 class="item-title">{{ item.title }}</h1>
                <div class="item-meta">
                  <span class="item-category" *ngIf="item.category">{{ item.category }}</span>
                  <span class="item-brand" *ngIf="item.brand">{{ item.brand }}</span>
                </div>
                <div class="item-condition">
                  <span class="condition-label">Condition:</span>
                  <span class="condition-value">{{ item.condition }}</span>
                </div>
              </div>

              <div class="item-pricing">
                <div class="price-display">
                  <span class="current-price">\${{ item.price | number:'1.2-2' }}</span>
                </div>
                <div class="availability-status" [class.available]="item.isAvailable">
                  <span *ngIf="item.isAvailable" class="available-badge">Available</span>
                  <span *ngIf="!item.isAvailable" class="sold-badge">Sold</span>
                </div>
              </div>

              <div class="item-actions">
                <button
                  class="btn btn-primary btn-lg add-to-cart-btn"
                  [disabled]="!item.isAvailable"
                  data-cy="add-to-cart-btn"
                  (click)="addToCart()">
                  <span *ngIf="item.isAvailable">🛒 Add to Cart</span>
                  <span *ngIf="!item.isAvailable">Sold Out</span>
                </button>
                <button
                  class="btn btn-outline-secondary btn-lg"
                  data-cy="add-to-favorites-btn"
                  (click)="addToFavorites()">
                  ♡ Add to Favorites
                </button>
              </div>

              <div class="item-details">
                <h3>Description</h3>
                <p class="item-description" *ngIf="item.description; else noDescription">
                  {{ item.description }}
                </p>
                <ng-template #noDescription>
                  <p class="no-description">No description available.</p>
                </ng-template>

                <div class="item-specifications" *ngIf="hasSpecifications()">
                  <h4>Specifications</h4>
                  <div class="spec-grid">
                    <div class="spec-item" *ngIf="item.size">
                      <span class="spec-label">Size:</span>
                      <span class="spec-value">{{ item.size }}</span>
                    </div>
                    <div class="spec-item" *ngIf="item.color">
                      <span class="spec-label">Color:</span>
                      <span class="spec-value">{{ item.color }}</span>
                    </div>
                    <div class="spec-item" *ngIf="item.materials">
                      <span class="spec-label">Materials:</span>
                      <span class="spec-value">{{ item.materials }}</span>
                    </div>
                    <div class="spec-item" *ngIf="item.measurements">
                      <span class="spec-label">Measurements:</span>
                      <span class="spec-value">{{ item.measurements }}</span>
                    </div>
                    <div class="spec-item" *ngIf="item.listedDate">
                      <span class="spec-label">Listed:</span>
                      <span class="spec-value">{{ item.listedDate | date:'mediumDate' }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .item-detail-container {
      min-height: 80vh;
      padding: 2rem 0;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .back-navigation {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid #dee2e6;
    }

    .breadcrumb {
      font-size: 0.9rem;
      color: #6c757d;
    }

    .breadcrumb a {
      color: #007bff;
      text-decoration: none;
    }

    .breadcrumb a:hover {
      text-decoration: underline;
    }

    .breadcrumb-separator {
      margin: 0 0.5rem;
    }

    .current-page {
      color: #343a40;
      font-weight: 500;
    }

    .loading-state {
      text-align: center;
      padding: 3rem 1rem;
    }

    .spinner-border {
      width: 3rem;
      height: 3rem;
      border-width: 0.3em;
      border-style: solid;
      border-color: #007bff transparent #007bff transparent;
      border-radius: 50%;
      animation: spinner-border 1s linear infinite;
      margin-bottom: 1rem;
    }

    @keyframes spinner-border {
      to {
        transform: rotate(360deg);
      }
    }

    .visually-hidden {
      position: absolute !important;
      width: 1px !important;
      height: 1px !important;
      padding: 0 !important;
      margin: -1px !important;
      overflow: hidden !important;
      clip: rect(0, 0, 0, 0) !important;
      white-space: nowrap !important;
      border: 0 !important;
    }

    .alert {
      padding: 1rem;
      margin-bottom: 1rem;
      border-radius: 0.375rem;
    }

    .alert-danger {
      color: #721c24;
      background-color: #f8d7da;
      border: 1px solid #f5c6cb;
    }

    .ms-2 {
      margin-left: 0.5rem;
    }

    .item-detail-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
      margin-bottom: 3rem;
    }

    .image-section {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .main-image {
      aspect-ratio: 1;
      overflow: hidden;
      border-radius: 0.5rem;
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    }

    .main-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .image-thumbnails {
      display: flex;
      gap: 0.5rem;
      overflow-x: auto;
      padding-bottom: 0.5rem;
    }

    .thumbnail-btn {
      flex-shrink: 0;
      width: 80px;
      height: 80px;
      border: 2px solid transparent;
      border-radius: 0.375rem;
      overflow: hidden;
      background: none;
      cursor: pointer;
      transition: border-color 0.2s;
    }

    .thumbnail-btn:hover {
      border-color: #007bff;
    }

    .thumbnail-btn.active {
      border-color: #007bff;
    }

    .thumbnail-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .info-section {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .item-header {
      border-bottom: 1px solid #dee2e6;
      padding-bottom: 1.5rem;
    }

    .item-title {
      font-size: 2rem;
      font-weight: 600;
      color: #343a40;
      margin-bottom: 1rem;
      line-height: 1.3;
    }

    .item-meta {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.75rem;
    }

    .item-category {
      background: #007bff;
      color: white;
      padding: 0.25rem 0.75rem;
      border-radius: 1rem;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .item-brand {
      background: #6c757d;
      color: white;
      padding: 0.25rem 0.75rem;
      border-radius: 1rem;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .item-condition {
      font-size: 1rem;
    }

    .condition-label {
      color: #6c757d;
      margin-right: 0.5rem;
    }

    .condition-value {
      color: #28a745;
      font-weight: 600;
    }

    .item-pricing {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      background: #f8f9fa;
      border-radius: 0.5rem;
      border: 1px solid #dee2e6;
    }

    .current-price {
      font-size: 2rem;
      font-weight: 700;
      color: #28a745;
    }

    .availability-status .available-badge {
      background: #d4edda;
      color: #155724;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      font-weight: 600;
    }

    .availability-status .sold-badge {
      background: #f8d7da;
      color: #721c24;
      padding: 0.5rem 1rem;
      border-radius: 0.375rem;
      font-weight: 600;
    }

    .item-actions {
      display: flex;
      gap: 1rem;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border: 1px solid transparent;
      border-radius: 0.375rem;
      font-size: 1rem;
      font-weight: 500;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
    }

    .btn-lg {
      padding: 1rem 2rem;
      font-size: 1.125rem;
    }

    .btn-primary {
      background-color: #007bff;
      border-color: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
      border-color: #004085;
    }

    .btn-primary:disabled {
      background-color: #6c757d;
      border-color: #6c757d;
      cursor: not-allowed;
    }

    .btn-outline-secondary {
      color: #6c757d;
      border-color: #6c757d;
      background-color: transparent;
    }

    .btn-outline-secondary:hover:not(:disabled) {
      color: white;
      background-color: #6c757d;
    }

    .btn-outline-primary {
      color: #007bff;
      border-color: #007bff;
      background-color: transparent;
    }

    .btn-outline-primary:hover {
      color: white;
      background-color: #007bff;
    }

    .add-to-cart-btn {
      flex: 1;
    }

    .item-details h3 {
      color: #343a40;
      margin-bottom: 1rem;
      font-size: 1.5rem;
    }

    .item-details h4 {
      color: #343a40;
      margin-bottom: 0.75rem;
      font-size: 1.25rem;
    }

    .item-description {
      color: #6c757d;
      line-height: 1.6;
      margin-bottom: 2rem;
    }

    .no-description {
      color: #adb5bd;
      font-style: italic;
      margin-bottom: 2rem;
    }

    .spec-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
    }

    .spec-item {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
      padding: 0.75rem;
      background: #f8f9fa;
      border-radius: 0.375rem;
    }

    .spec-label {
      font-size: 0.875rem;
      color: #6c757d;
      font-weight: 500;
    }

    .spec-value {
      font-size: 1rem;
      color: #343a40;
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .item-detail-grid {
        grid-template-columns: 1fr;
        gap: 2rem;
      }

      .back-navigation {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .item-title {
        font-size: 1.5rem;
      }

      .item-actions {
        flex-direction: column;
      }

      .current-price {
        font-size: 1.5rem;
      }

      .item-pricing {
        flex-direction: column;
        gap: 1rem;
      }

      .spec-grid {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 480px) {
      .container {
        padding: 0 0.5rem;
      }

      .item-detail-container {
        padding: 1rem 0;
      }

      .btn-lg {
        padding: 0.75rem 1.5rem;
        font-size: 1rem;
      }
    }
  `]
})
export class ItemDetailComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  itemId = '';
  item: ShopperItemDetail | null = null;
  selectedImage: ShopperItemImage | null = null;
  isLoading = true;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private storeService: ShopperStoreService,
    private catalogService: ShopperCatalogService,
    private cartService: ShopperCartService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      this.itemId = params.get('itemId') || '';
      if (this.storeSlug && this.itemId) {
        this.cartService.setCurrentStore(this.storeSlug);
        this.loadItemDetail();
      }
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadItemDetail(): void {
    this.isLoading = true;
    this.error = null;

    this.catalogService.getItemDetail(this.storeSlug, this.itemId).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.item = response.data;
          this.selectedImage = this.item.images.find(img => img.isPrimary) || this.item.images[0] || null;
        } else {
          this.error = response.message || 'Failed to load item details';
          this.item = null;
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'An error occurred while loading item details';
        this.item = null;
        this.isLoading = false;
        console.error('Item detail load error:', err);
      }
    });
  }

  selectImage(image: ShopperItemImage): void {
    this.selectedImage = image;
  }

  hasSpecifications(): boolean {
    return !!(this.item?.size || this.item?.color || this.item?.materials ||
             this.item?.measurements || this.item?.listedDate);
  }

  addToCart(): void {
    if (!this.item?.isAvailable) return;

    // Convert item detail to item list format for cart
    const cartItem: ShopperItemList = {
      itemId: this.item.itemId,
      title: this.item.title,
      description: this.item.description,
      price: this.item.price,
      category: this.item.category,
      brand: this.item.brand,
      size: this.item.size,
      color: this.item.color,
      condition: this.item.condition,
      primaryImageUrl: this.selectedImage?.imageUrl,
      listedDate: this.item.listedDate,
      images: this.item.images
    };

    const success = this.cartService.addItem(cartItem, 1);
    if (success) {
      // Show a more user-friendly notification (could be replaced with toast notification)
      console.log(`${this.item.title} added to cart!`);
    } else {
      console.error('Failed to add item to cart');
    }
  }

  addToFavorites(): void {
    // TODO: Implement favorites functionality
    alert(`${this.item?.title} added to favorites!`);
  }

  goBack(): void {
    this.router.navigate(['/shop', this.storeSlug]);
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }
}