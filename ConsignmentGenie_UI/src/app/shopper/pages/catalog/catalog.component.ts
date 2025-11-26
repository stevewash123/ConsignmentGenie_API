import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';
import {
  ShopperCatalogService,
  ShopperItemList,
  ShopperCategory,
  CatalogRequest
} from '../../services/shopper-catalog.service';
import { ShopperCartService } from '../../services/shopper-cart.service';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="catalog-container">
      <div class="container">
        <div class="catalog-header">
          <div class="header-content">
            <h1>Shop {{ storeInfo?.name || 'Our Store' }}</h1>
            <p class="store-description" *ngIf="storeInfo?.description">
              {{ storeInfo.description }}
            </p>
          </div>
        </div>

        <div class="catalog-filters">
          <div class="search-section">
            <div class="search-input">
              <input
                type="text"
                [(ngModel)]="searchTerm"
                (input)="onSearch()"
                placeholder="Search items..."
                class="form-control">
              <button class="search-btn" (click)="onSearch()">
                🔍
              </button>
            </div>
          </div>

          <div class="filter-section">
            <select [(ngModel)]="selectedCategory" (change)="onCategoryChange()" class="form-select">
              <option value="">All Categories</option>
              <option *ngFor="let category of categories" [value]="category.name">
                {{ category.name }} ({{ category.itemCount }})
              </option>
            </select>

            <select [(ngModel)]="sortBy" (change)="onSortChange()" class="form-select">
              <option value="name">Sort by Name</option>
              <option value="price-low">Price: Low to High</option>
              <option value="price-high">Price: High to Low</option>
              <option value="newest">Newest First</option>
            </select>
          </div>
        </div>

        <div class="error-message" *ngIf="error && !isLoading">
          <div class="alert alert-danger">
            {{ error }}
            <button class="btn btn-outline-primary btn-sm ms-2" (click)="loadCatalogData()">Retry</button>
          </div>
        </div>

        <div class="catalog-content" *ngIf="!isLoading && !error; else loadingTemplate">
          <div class="items-grid" *ngIf="items.length > 0; else noItemsTemplate">
            <div class="item-card" *ngFor="let item of items" data-cy="item-card">
              <div class="item-image" (click)="viewItemDetail(item)" style="cursor: pointer;">
                <img
                  [src]="item.primaryImageUrl || '/assets/placeholder-item.jpg'"
                  [alt]="item.title"
                  class="item-img"
                  (error)="onImageError($event)">
              </div>

              <div class="item-details">
                <h3 class="item-name" (click)="viewItemDetail(item)" style="cursor: pointer;">{{ item.title }}</h3>
                <p class="item-category" *ngIf="item.category">{{ item.category }}</p>
                <p class="item-brand" *ngIf="item.brand">{{ item.brand }}</p>
                <p class="item-condition">{{ item.condition }}</p>
                <p class="item-description" *ngIf="item.description">{{ item.description }}</p>
                <div class="item-footer">
                  <span class="item-price">\${{ item.price | number:'1.2-2' }}</span>
                  <button
                    class="btn btn-primary btn-sm"
                    data-cy="add-to-cart-btn"
                    (click)="addToCart(item)">
                    Add to Cart
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="pagination" *ngIf="totalPages > 1">
            <button
              class="btn btn-outline-secondary"
              [disabled]="currentPage === 1"
              (click)="goToPage(currentPage - 1)">
              Previous
            </button>

            <span class="page-info">
              Page {{ currentPage }} of {{ totalPages }} ({{ totalCount }} total items)
            </span>

            <button
              class="btn btn-outline-secondary"
              [disabled]="currentPage === totalPages"
              (click)="goToPage(currentPage + 1)">
              Next
            </button>
          </div>
        </div>

        <ng-template #loadingTemplate>
          <div class="loading-state">
            <div class="spinner-border" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
            <p>Loading items...</p>
          </div>
        </ng-template>

        <ng-template #noItemsTemplate>
          <div class="empty-state">
            <div class="empty-icon">📦</div>
            <h3>No Items Found</h3>
            <p *ngIf="searchTerm || selectedCategory">
              Try adjusting your search or filter criteria.
            </p>
            <p *ngIf="!searchTerm && !selectedCategory">
              This store doesn't have any items available right now. Check back soon!
            </p>
            <button class="btn btn-outline-primary" (click)="clearFilters()">
              Clear Filters
            </button>
          </div>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .catalog-container {
      min-height: 80vh;
      padding: 2rem 0;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .catalog-header {
      text-align: center;
      margin-bottom: 3rem;
      padding: 2rem 0;
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
      border-radius: 0.5rem;
    }

    .catalog-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 1rem;
    }

    .store-description {
      font-size: 1.1rem;
      color: #6c757d;
      max-width: 600px;
      margin: 0 auto;
      line-height: 1.6;
    }

    .catalog-filters {
      background: white;
      padding: 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      margin-bottom: 2rem;
    }

    .search-section {
      margin-bottom: 1rem;
    }

    .search-input {
      position: relative;
      max-width: 400px;
      margin: 0 auto;
    }

    .search-input input {
      width: 100%;
      padding: 0.75rem 3rem 0.75rem 1rem;
      border: 1px solid #ced4da;
      border-radius: 0.375rem;
      font-size: 1rem;
    }

    .search-btn {
      position: absolute;
      right: 0.5rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.5rem;
      font-size: 1.1rem;
    }

    .filter-section {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .form-select {
      padding: 0.5rem 1rem;
      border: 1px solid #ced4da;
      border-radius: 0.375rem;
      background-color: white;
      cursor: pointer;
      min-width: 150px;
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

    .catalog-content {
      margin-bottom: 3rem;
    }

    .items-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .item-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .item-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }

    .item-image {
      position: relative;
      height: 200px;
      overflow: hidden;
    }

    .item-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .item-details {
      padding: 1rem;
    }

    .item-name {
      font-size: 1.1rem;
      font-weight: 600;
      color: #343a40;
      margin-bottom: 0.25rem;
      line-height: 1.3;
    }

    .item-category {
      font-size: 0.875rem;
      color: #007bff;
      margin-bottom: 0.25rem;
      font-weight: 500;
    }

    .item-brand {
      font-size: 0.875rem;
      color: #6c757d;
      margin-bottom: 0.25rem;
      font-style: italic;
    }

    .item-condition {
      font-size: 0.875rem;
      color: #28a745;
      margin-bottom: 0.5rem;
      font-weight: 500;
    }

    .item-description {
      font-size: 0.875rem;
      color: #6c757d;
      line-height: 1.4;
      margin-bottom: 1rem;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .item-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .item-price {
      font-size: 1.25rem;
      font-weight: bold;
      color: #28a745;
    }

    .btn {
      padding: 0.5rem 1rem;
      border: 1px solid transparent;
      border-radius: 0.375rem;
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      display: inline-block;
      text-align: center;
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

    .btn-outline-secondary {
      color: #6c757d;
      border-color: #6c757d;
      background-color: transparent;
    }

    .btn-outline-secondary:hover:not(:disabled) {
      color: white;
      background-color: #6c757d;
    }

    .btn-outline-secondary:disabled {
      color: #6c757d;
      background-color: transparent;
      cursor: not-allowed;
      opacity: 0.5;
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

    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.8rem;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      margin-top: 2rem;
    }

    .page-info {
      color: #6c757d;
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

    .empty-state {
      text-align: center;
      padding: 3rem 1rem;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .empty-state h3 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .empty-state p {
      color: #6c757d;
      margin-bottom: 1.5rem;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
    }

    @media (max-width: 768px) {
      .catalog-header h1 {
        font-size: 2rem;
      }

      .items-grid {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .filter-section {
        flex-direction: column;
        align-items: stretch;
      }

      .form-select {
        min-width: auto;
      }

      .search-input {
        max-width: none;
      }
    }

    @media (max-width: 480px) {
      .catalog-filters {
        padding: 1rem;
      }

      .pagination {
        flex-direction: column;
        gap: 0.5rem;
      }
    }
  `]
})
export class CatalogComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';

  // Real data from API
  items: ShopperItemList[] = [];
  categories: ShopperCategory[] = [];

  // Search and filter state
  searchTerm = '';
  selectedCategory = '';
  sortBy = 'newest';
  sortDirection = 'desc';
  currentPage = 1;
  itemsPerPage = 20;
  totalCount = 0;
  totalPages = 1;
  isLoading = true;
  error: string | null = null;

  // Search debouncing
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private storeService: ShopperStoreService,
    private catalogService: ShopperCatalogService,
    private cartService: ShopperCartService
  ) {}

  ngOnInit(): void {
    // Get store slug from route
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      if (this.storeSlug) {
        this.cartService.setCurrentStore(this.storeSlug);
        this.loadCatalogData();
        this.loadCategories();
      }
    });

    // Get store info
    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    // Setup search debouncing
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.currentPage = 1;
      this.loadCatalogData();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCatalogData(): void {
    this.isLoading = true;
    this.error = null;

    const request: CatalogRequest = {
      page: this.currentPage,
      pageSize: this.itemsPerPage,
      sortBy: this.getSortByValue(),
      sortDirection: this.sortDirection
    };

    if (this.selectedCategory) {
      request.category = this.selectedCategory;
    }

    // Use search if there's a search term
    const serviceCall = this.searchTerm
      ? this.catalogService.searchItems(this.storeSlug, { ...request, searchQuery: this.searchTerm })
      : this.catalogService.getCatalogItems(this.storeSlug, request);

    serviceCall.pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.items = response.data.items;
          this.totalCount = response.data.totalCount;
          this.totalPages = response.data.totalPages;
          this.currentPage = response.data.page;
        } else {
          this.error = response.message || 'Failed to load catalog items';
          this.items = [];
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'An error occurred while loading catalog items';
        this.items = [];
        this.isLoading = false;
        console.error('Catalog load error:', err);
      }
    });
  }

  private loadCategories(): void {
    this.catalogService.getCategories(this.storeSlug).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.categories = response.data;
        }
      },
      error: (err) => {
        console.error('Categories load error:', err);
      }
    });
  }

  private getSortByValue(): string {
    switch (this.sortBy) {
      case 'name':
        this.sortDirection = 'asc';
        return 'title';
      case 'price-low':
        this.sortDirection = 'asc';
        return 'price';
      case 'price-high':
        this.sortDirection = 'desc';
        return 'price';
      case 'newest':
        this.sortDirection = 'desc';
        return 'ListedDate';
      default:
        this.sortDirection = 'desc';
        return 'ListedDate';
    }
  }

  onSearch(): void {
    this.searchSubject.next(this.searchTerm);
  }

  onCategoryChange(): void {
    this.currentPage = 1;
    this.loadCatalogData();
  }

  onSortChange(): void {
    this.loadCatalogData();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = '';
    this.sortBy = 'newest';
    this.sortDirection = 'desc';
    this.currentPage = 1;
    this.loadCatalogData();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadCatalogData();
    }
  }

  addToCart(item: ShopperItemList): void {
    const success = this.cartService.addItem(item, 1);
    if (success) {
      // Show a more user-friendly notification (could be replaced with toast notification)
      console.log(`${item.title} added to cart!`);
    } else {
      console.error('Failed to add item to cart');
    }
  }

  viewItemDetail(item: ShopperItemList): void {
    this.router.navigate(['/shop', this.storeSlug, 'items', item.itemId]);
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }
}