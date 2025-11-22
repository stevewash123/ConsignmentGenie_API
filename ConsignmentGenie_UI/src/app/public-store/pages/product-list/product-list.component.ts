import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PublicStoreService } from '../../services/public-store.service';
import { ShoppingCartService } from '../../../customer/services/shopping-cart.service';
import { PublicItem, PagedResult } from '../../../shared/models/api.models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="product-list-page">
      <div class="container">
        <!-- Page Header -->
        <div class="page-header">
          <h1>{{ pageTitle() }}</h1>
          <p class="subtitle">{{ totalItems() }} {{ totalItems() === 1 ? 'item' : 'items' }} found</p>
        </div>

        <!-- Filters and Sort -->
        <div class="filters-section">
          <div class="filter-row">
            <!-- Category Filter -->
            <div class="filter-group">
              <label>Category</label>
              <select [(ngModel)]="selectedCategory" (change)="applyFilters()" class="filter-select">
                <option value="">All Categories</option>
                @for (category of categories(); track category.id) {
                  <option [value]="category.slug">{{ category.name }}</option>
                }
              </select>
            </div>

            <!-- Price Range Filter -->
            <div class="filter-group">
              <label>Price Range</label>
              <div class="price-range">
                <input
                  type="number"
                  placeholder="Min"
                  [(ngModel)]="minPrice"
                  (change)="applyFilters()"
                  class="price-input"
                >
                <span>to</span>
                <input
                  type="number"
                  placeholder="Max"
                  [(ngModel)]="maxPrice"
                  (change)="applyFilters()"
                  class="price-input"
                >
              </div>
            </div>

            <!-- Sort Options -->
            <div class="filter-group">
              <label>Sort by</label>
              <select [(ngModel)]="sortBy" (change)="applyFilters()" class="filter-select">
                <option value="created">Newest First</option>
                <option value="price">Price: Low to High</option>
                <option value="price_desc">Price: High to Low</option>
                <option value="name">Name: A to Z</option>
                <option value="name_desc">Name: Z to A</option>
              </select>
            </div>

            <!-- View Toggle -->
            <div class="view-toggle">
              <button
                type="button"
                [class.active]="viewMode() === 'grid'"
                (click)="setViewMode('grid')"
                class="view-button"
              >
                Grid
              </button>
              <button
                type="button"
                [class.active]="viewMode() === 'list'"
                (click)="setViewMode('list')"
                class="view-button"
              >
                List
              </button>
            </div>
          </div>

          <!-- Active Filters -->
          @if (hasActiveFilters()) {
            <div class="active-filters">
              <span class="filter-label">Active filters:</span>
              @if (selectedCategory) {
                <span class="filter-tag">
                  Category: {{ getCategoryName(selectedCategory) }}
                  <button type="button" (click)="clearCategoryFilter()">&times;</button>
                </span>
              }
              @if (minPrice || maxPrice) {
                <span class="filter-tag">
                  Price: \${{ minPrice || 0 }} - \${{ maxPrice || '∞' }}
                  <button type="button" (click)="clearPriceFilter()">&times;</button>
                </span>
              }
              <button type="button" (click)="clearAllFilters()" class="clear-all-btn">Clear All</button>
            </div>
          }
        </div>

        <!-- Loading State -->
        @if (loading()) {
          <div class="loading-state">
            <div class="loading-spinner"></div>
            <p>Loading products...</p>
          </div>
        }

        <!-- Empty State -->
        @else if (products().length === 0 && !loading()) {
          <div class="empty-state">
            <h3>No products found</h3>
            <p>Try adjusting your filters or search criteria.</p>
            @if (hasActiveFilters()) {
              <button type="button" (click)="clearAllFilters()" class="clear-filters-btn">
                Clear Filters
              </button>
            }
          </div>
        }

        <!-- Products Grid/List -->
        @else {
          <div class="products-container" [class]="viewMode()">
            @for (product of products(); track product.id) {
              <div class="product-card">
                <!-- Product Image -->
                <div class="product-image">
                  <a [routerLink]="['/store', orgSlug(), 'products', product.id]">
                    @if (product.photos?.[0]) {
                      <img [src]="product.photos[0].url" [alt]="product.title" loading="lazy">
                    } @else {
                      <div class="placeholder-image">No Image</div>
                    }
                  </a>

                  <!-- Quick Actions -->
                  <div class="product-actions">
                    <button
                      type="button"
                      (click)="addToCart(product)"
                      [disabled]="!product.isAvailable"
                      class="add-to-cart-btn"
                    >
                      @if (cartService.isItemInCart(product.id)) {
                        <span>✓ In Cart</span>
                      } @else {
                        <span>Add to Cart</span>
                      }
                    </button>

                    <button
                      type="button"
                      (click)="toggleWishlist(product)"
                      class="wishlist-btn"
                      [class.active]="isInWishlist(product.id)"
                    >
                      ♡
                    </button>
                  </div>
                </div>

                <!-- Product Info -->
                <div class="product-info">
                  <div class="product-category">{{ product.category }}</div>
                  <h3 class="product-title">
                    <a [routerLink]="['/store', orgSlug(), 'products', product.id]">
                      {{ product.title }}
                    </a>
                  </h3>

                  @if (viewMode() === 'list') {
                    <p class="product-description">{{ product.description | slice:0:150 }}...</p>
                  }

                  <div class="product-meta">
                    <div class="provider">by {{ product.providerName }}</div>
                    <div class="condition">{{ product.condition }}</div>
                  </div>

                  <div class="product-price">
                    <span class="current-price">\${{ product.price }}</span>
                    @if (product.originalPrice && product.originalPrice > product.price) {
                      <span class="original-price">\${{ product.originalPrice }}</span>
                      <span class="discount">
                        {{ ((product.originalPrice - product.price) / product.originalPrice * 100) | number:'1.0-0' }}% off
                      </span>
                    }
                  </div>

                  <div class="product-tags">
                    @for (tag of product.tags?.slice(0, 3); track tag) {
                      <span class="tag">{{ tag }}</span>
                    }
                  </div>
                </div>
              </div>
            }
          </div>

          <!-- Pagination -->
          @if (totalPages() > 1) {
            <div class="pagination">
              <button
                type="button"
                (click)="goToPage(currentPage() - 1)"
                [disabled]="currentPage() <= 1"
                class="page-btn"
              >
                Previous
              </button>

              @for (page of getPageNumbers(); track page) {
                <button
                  type="button"
                  (click)="goToPage(page)"
                  [class.active]="page === currentPage()"
                  class="page-btn"
                >
                  {{ page }}
                </button>
              }

              <button
                type="button"
                (click)="goToPage(currentPage() + 1)"
                [disabled]="currentPage() >= totalPages()"
                class="page-btn"
              >
                Next
              </button>
            </div>
          }
        }
      </div>
    </div>
  `,
  styleUrls: ['./product-list.component.scss']
})
export class ProductListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly storeService = inject(PublicStoreService);
  protected readonly cartService = inject(ShoppingCartService);

  // Signals
  protected orgSlug = signal<string>('');
  protected products = signal<PublicItem[]>([]);
  protected categories = signal<any[]>([]);
  protected loading = signal(false);
  protected totalItems = signal(0);
  protected totalPages = signal(0);
  protected currentPage = signal(1);
  protected viewMode = signal<'grid' | 'list'>('grid');

  // Filter state
  protected selectedCategory = '';
  protected minPrice: number | null = null;
  protected maxPrice: number | null = null;
  protected sortBy = 'created';
  protected pageSize = 20;

  // Computed
  protected pageTitle = computed(() => {
    const category = this.selectedCategory;
    if (category) {
      const categoryObj = this.categories().find(c => c.slug === category);
      return categoryObj ? `${categoryObj.name} Products` : 'Products';
    }
    return 'All Products';
  });

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.orgSlug.set(params['orgSlug'] || '');
    });

    this.route.queryParams.subscribe(queryParams => {
      // Set filters from URL parameters
      this.selectedCategory = queryParams['category'] || '';
      this.minPrice = queryParams['minPrice'] ? parseFloat(queryParams['minPrice']) : null;
      this.maxPrice = queryParams['maxPrice'] ? parseFloat(queryParams['maxPrice']) : null;
      this.sortBy = queryParams['sortBy'] || 'created';
      this.currentPage.set(parseInt(queryParams['page']) || 1);

      this.loadData();
    });

    this.loadCategories();

    // Load view mode preference
    const savedViewMode = localStorage.getItem('cg_view_mode') as 'grid' | 'list';
    if (savedViewMode) {
      this.viewMode.set(savedViewMode);
    }
  }

  private loadData(): void {
    this.loading.set(true);

    const request = {
      orgSlug: this.orgSlug(),
      page: this.currentPage(),
      pageSize: this.pageSize,
      category: this.selectedCategory || undefined,
      minPrice: this.minPrice || undefined,
      maxPrice: this.maxPrice || undefined,
      sortBy: this.sortBy,
      sortOrder: this.getSortOrder()
    };

    this.storeService.searchItems(request).subscribe({
      next: (result) => {
        if (result) {
          this.products.set(result.items);
          this.totalItems.set(result.totalCount);
          this.totalPages.set(result.totalPages);
        }
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.loading.set(false);
      }
    });
  }

  private loadCategories(): void {
    this.storeService.getCategories(this.orgSlug()).subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  private getSortOrder(): string {
    return this.sortBy.endsWith('_desc') ? 'desc' : 'asc';
  }

  protected setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode.set(mode);
    localStorage.setItem('cg_view_mode', mode);
  }

  protected applyFilters(): void {
    this.currentPage.set(1);
    this.updateUrl();
  }

  protected goToPage(page: number): void {
    this.currentPage.set(page);
    this.updateUrl();
  }

  private updateUrl(): void {
    const queryParams: any = {
      page: this.currentPage()
    };

    if (this.selectedCategory) queryParams.category = this.selectedCategory;
    if (this.minPrice) queryParams.minPrice = this.minPrice;
    if (this.maxPrice) queryParams.maxPrice = this.maxPrice;
    if (this.sortBy !== 'created') queryParams.sortBy = this.sortBy;

    // Note: In a real implementation, you'd use Router.navigate()
    // this.router.navigate([], { queryParams, replaceUrl: true });
    this.loadData();
  }

  protected hasActiveFilters(): boolean {
    return !!(this.selectedCategory || this.minPrice || this.maxPrice);
  }

  protected getCategoryName(slug: string): string {
    const category = this.categories().find(c => c.slug === slug);
    return category ? category.name : slug;
  }

  protected clearCategoryFilter(): void {
    this.selectedCategory = '';
    this.applyFilters();
  }

  protected clearPriceFilter(): void {
    this.minPrice = null;
    this.maxPrice = null;
    this.applyFilters();
  }

  protected clearAllFilters(): void {
    this.selectedCategory = '';
    this.minPrice = null;
    this.maxPrice = null;
    this.sortBy = 'created';
    this.applyFilters();
  }

  protected addToCart(product: PublicItem): void {
    if (!product.isAvailable) return;

    this.cartService.addToCart(product.id, 1).subscribe({
      next: () => {
        // Success feedback could be shown here
      },
      error: (error) => {
        console.error('Error adding to cart:', error);
        // Error feedback could be shown here
      }
    });
  }

  protected toggleWishlist(product: PublicItem): void {
    // Wishlist functionality would be implemented here
    console.log('Toggle wishlist for product:', product.id);
  }

  protected isInWishlist(productId: string): boolean {
    // Wishlist check would be implemented here
    return false;
  }

  protected getPageNumbers(): number[] {
    const pages: number[] = [];
    const current = this.currentPage();
    const total = this.totalPages();

    // Show up to 5 page numbers around current page
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }
}