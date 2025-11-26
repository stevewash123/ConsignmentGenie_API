import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

interface FavoriteItem {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  category: string;
  isAvailable: boolean;
  dateAdded: Date;
}

@Component({
  selector: 'app-favorites',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="favorites-container">
      <div class="container">
        <div class="page-header">
          <h1>My Favorites</h1>
          <p class="store-name" *ngIf="storeInfo">{{ storeInfo.name }}</p>
          <p class="item-count" *ngIf="favoriteItems.length > 0">{{ favoriteItems.length }} item(s) saved</p>
        </div>

        <div class="favorites-content" *ngIf="favoriteItems.length > 0; else noFavoritesTemplate">
          <div class="favorites-toolbar">
            <div class="sort-options">
              <label for="sortSelect">Sort by:</label>
              <select id="sortSelect" [(ngModel)]="sortBy" (change)="onSortChange()" class="form-select">
                <option value="dateAdded">Date Added</option>
                <option value="name">Name A-Z</option>
                <option value="price-low">Price: Low to High</option>
                <option value="price-high">Price: High to Low</option>
              </select>
            </div>

            <div class="view-options">
              <button
                class="btn btn-outline-secondary btn-sm"
                [class.active]="viewMode === 'grid'"
                (click)="setViewMode('grid')">
                Grid
              </button>
              <button
                class="btn btn-outline-secondary btn-sm"
                [class.active]="viewMode === 'list'"
                (click)="setViewMode('list')">
                List
              </button>
            </div>
          </div>

          <div class="favorites-grid" [class.list-view]="viewMode === 'list'">
            <div class="favorite-card" *ngFor="let item of sortedItems">
              <div class="item-image">
                <img
                  [src]="item.imageUrl || '/assets/placeholder-item.jpg'"
                  [alt]="item.name"
                  class="item-img"
                  (error)="onImageError($event)">
                <div class="item-overlay" *ngIf="!item.isAvailable">
                  <span class="sold-badge">SOLD</span>
                </div>
                <button
                  class="favorite-btn active"
                  (click)="removeFromFavorites(item)"
                  title="Remove from favorites">
                  ❤️
                </button>
              </div>

              <div class="item-details">
                <h3 class="item-name">{{ item.name }}</h3>
                <p class="item-category">{{ item.category }}</p>
                <p class="item-description">{{ item.description }}</p>

                <div class="item-meta">
                  <span class="date-added">Added {{ item.dateAdded | date:'shortDate' }}</span>
                </div>

                <div class="item-footer">
                  <span class="item-price">\${{ item.price | number:'1.2-2' }}</span>
                  <div class="item-actions">
                    <button
                      class="btn btn-primary btn-sm"
                      [disabled]="!item.isAvailable"
                      (click)="addToCart(item)">
                      {{ item.isAvailable ? 'Add to Cart' : 'Sold Out' }}
                    </button>
                    <button
                      class="btn btn-outline-secondary btn-sm"
                      (click)="shareItem(item)">
                      Share
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="favorites-actions" *ngIf="favoriteItems.length > 1">
            <button class="btn btn-outline-danger" (click)="clearAllFavorites()">
              Clear All Favorites
            </button>
          </div>
        </div>

        <ng-template #noFavoritesTemplate>
          <div class="no-favorites">
            <div class="empty-icon">💝</div>
            <h2>No favorites yet</h2>
            <p>Save items you love by clicking the heart icon when browsing our store.</p>
            <button class="btn btn-primary" [routerLink]="['/shop', storeSlug]">
              Start Shopping
            </button>
          </div>
        </ng-template>

        <div class="favorites-tips">
          <div class="tips-card">
            <h3>💡 Did you know?</h3>
            <ul>
              <li>Items in your favorites are saved across devices when you're signed in</li>
              <li>You'll be notified if the price drops on favorited items</li>
              <li>Use favorites to create a wishlist for special occasions</li>
              <li>Share your favorite items with friends and family</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .favorites-container {
      min-height: 80vh;
      padding: 2rem 0;
      background-color: #f8f9fa;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .page-header {
      text-align: center;
      margin-bottom: 3rem;
    }

    .page-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 0.5rem;
    }

    .store-name {
      font-size: 1.1rem;
      color: #007bff;
      margin: 0 0 0.5rem 0;
    }

    .item-count {
      color: #6c757d;
      margin: 0;
      font-size: 0.9rem;
    }

    .favorites-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: white;
      padding: 1rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      margin-bottom: 2rem;
    }

    .sort-options {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .sort-options label {
      font-weight: 500;
      color: #343a40;
    }

    .form-select {
      padding: 0.375rem 0.75rem;
      border: 1px solid #ced4da;
      border-radius: 0.375rem;
      background-color: white;
      cursor: pointer;
    }

    .view-options {
      display: flex;
      gap: 0.25rem;
    }

    .favorites-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .favorites-grid.list-view {
      grid-template-columns: 1fr;
    }

    .favorites-grid.list-view .favorite-card {
      display: flex;
      gap: 1rem;
      align-items: center;
    }

    .favorites-grid.list-view .item-image {
      width: 150px;
      height: 150px;
      flex-shrink: 0;
    }

    .favorites-grid.list-view .item-details {
      flex: 1;
    }

    .favorite-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .favorite-card:hover {
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

    .item-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.7);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .sold-badge {
      background: #dc3545;
      color: white;
      padding: 0.5rem 1rem;
      border-radius: 0.25rem;
      font-weight: bold;
      transform: rotate(-10deg);
    }

    .favorite-btn {
      position: absolute;
      top: 0.75rem;
      right: 0.75rem;
      background: white;
      border: none;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.2rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      transition: transform 0.2s;
    }

    .favorite-btn:hover {
      transform: scale(1.1);
    }

    .favorite-btn.active {
      background: #fff5f5;
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
      margin-bottom: 0.5rem;
      font-weight: 500;
    }

    .item-description {
      font-size: 0.875rem;
      color: #6c757d;
      line-height: 1.4;
      margin-bottom: 0.75rem;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .item-meta {
      margin-bottom: 1rem;
    }

    .date-added {
      font-size: 0.8rem;
      color: #adb5bd;
      font-style: italic;
    }

    .item-footer {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
    }

    .item-price {
      font-size: 1.25rem;
      font-weight: bold;
      color: #28a745;
      flex-shrink: 0;
    }

    .item-actions {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex-shrink: 0;
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

    .btn-outline-secondary:hover:not(.active) {
      color: white;
      background-color: #6c757d;
    }

    .btn-outline-secondary.active {
      color: white;
      background-color: #6c757d;
    }

    .btn-outline-danger {
      color: #dc3545;
      border-color: #dc3545;
      background-color: transparent;
    }

    .btn-outline-danger:hover {
      color: white;
      background-color: #dc3545;
    }

    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.8rem;
    }

    .favorites-actions {
      text-align: center;
      margin-top: 2rem;
      padding-top: 2rem;
      border-top: 1px solid #dee2e6;
    }

    .no-favorites {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 4rem 2rem;
      text-align: center;
      margin-bottom: 2rem;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .no-favorites h2 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .no-favorites p {
      color: #6c757d;
      margin-bottom: 2rem;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
    }

    .favorites-tips {
      margin-top: 3rem;
    }

    .tips-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 2rem;
    }

    .tips-card h3 {
      color: #343a40;
      margin-bottom: 1.5rem;
    }

    .tips-card ul {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .tips-card li {
      color: #6c757d;
      margin-bottom: 0.75rem;
      padding-left: 1.5rem;
      position: relative;
    }

    .tips-card li:before {
      content: '✓';
      position: absolute;
      left: 0;
      color: #28a745;
      font-weight: bold;
    }

    @media (max-width: 968px) {
      .favorites-toolbar {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .sort-options, .view-options {
        justify-content: center;
      }

      .favorites-grid {
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      }

      .favorites-grid.list-view {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .favorites-grid {
        grid-template-columns: 1fr;
      }

      .favorites-grid.list-view .favorite-card {
        flex-direction: column;
        text-align: center;
      }

      .favorites-grid.list-view .item-image {
        width: 100%;
        height: 200px;
      }

      .item-footer {
        flex-direction: column;
        align-items: center;
        text-align: center;
      }

      .item-actions {
        flex-direction: row;
        justify-content: center;
      }
    }

    @media (max-width: 480px) {
      .page-header h1 {
        font-size: 2rem;
      }

      .no-favorites {
        padding: 2rem 1rem;
      }

      .tips-card {
        padding: 1.5rem;
      }
    }
  `]
})
export class FavoritesComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  viewMode: 'grid' | 'list' = 'grid';
  sortBy = 'dateAdded';

  // Sample favorites data for Phase 1 MVP
  favoriteItems: FavoriteItem[] = [
    {
      id: '1',
      name: 'Vintage Leather Jacket',
      description: 'Classic brown leather jacket in excellent condition. Perfect for fall weather.',
      price: 125.00,
      category: 'Clothing',
      isAvailable: true,
      dateAdded: new Date('2024-01-20')
    },
    {
      id: '2',
      name: 'Designer Handbag',
      description: 'Authentic designer handbag with original tags. Like new condition.',
      price: 280.00,
      category: 'Accessories',
      isAvailable: false,
      dateAdded: new Date('2024-01-18')
    },
    {
      id: '3',
      name: 'Modern Art Print',
      description: 'Limited edition print by local artist. Framed and ready to hang.',
      price: 75.00,
      category: 'Art',
      isAvailable: true,
      dateAdded: new Date('2024-01-15')
    },
    {
      id: '4',
      name: 'Retro Coffee Table',
      description: 'Mid-century modern coffee table with sleek lines and original finish.',
      price: 180.00,
      category: 'Furniture',
      isAvailable: true,
      dateAdded: new Date('2024-01-12')
    }
  ];

  sortedItems: FavoriteItem[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private storeService: ShopperStoreService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    this.loadFavoritesFromStorage();
    this.applySorting();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode = mode;
    this.saveFavoritesToStorage();
  }

  onSortChange(): void {
    this.applySorting();
  }

  removeFromFavorites(item: FavoriteItem): void {
    const index = this.favoriteItems.findIndex(fav => fav.id === item.id);
    if (index > -1) {
      this.favoriteItems.splice(index, 1);
      this.applySorting();
      this.saveFavoritesToStorage();
    }
  }

  addToCart(item: FavoriteItem): void {
    if (!item.isAvailable) return;

    // TODO: Implement actual cart functionality
    alert(`${item.name} added to cart!`);
  }

  shareItem(item: FavoriteItem): void {
    // TODO: Implement sharing functionality
    if (navigator.share) {
      navigator.share({
        title: item.name,
        text: item.description,
        url: window.location.href
      });
    } else {
      // Fallback for browsers that don't support Web Share API
      const url = `${window.location.origin}/shop/${this.storeSlug}/item/${item.id}`;
      navigator.clipboard.writeText(url).then(() => {
        alert('Item link copied to clipboard!');
      }).catch(() => {
        alert('Unable to copy link. Please try again.');
      });
    }
  }

  clearAllFavorites(): void {
    if (confirm('Are you sure you want to remove all items from your favorites?')) {
      this.favoriteItems = [];
      this.sortedItems = [];
      this.saveFavoritesToStorage();
    }
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }

  private applySorting(): void {
    let sorted = [...this.favoriteItems];

    switch (this.sortBy) {
      case 'name':
        sorted.sort((a, b) => a.name.localeCompare(b.name));
        break;
      case 'price-low':
        sorted.sort((a, b) => a.price - b.price);
        break;
      case 'price-high':
        sorted.sort((a, b) => b.price - a.price);
        break;
      case 'dateAdded':
      default:
        sorted.sort((a, b) => b.dateAdded.getTime() - a.dateAdded.getTime());
        break;
    }

    this.sortedItems = sorted;
  }

  private loadFavoritesFromStorage(): void {
    try {
      const favoritesData = localStorage.getItem(`favorites_${this.storeSlug}`);
      const viewModeData = localStorage.getItem(`favorites_viewmode_${this.storeSlug}`);

      if (favoritesData) {
        const parsed = JSON.parse(favoritesData);
        this.favoriteItems = parsed.map((item: any) => ({
          ...item,
          dateAdded: new Date(item.dateAdded)
        }));
      }

      if (viewModeData) {
        this.viewMode = viewModeData as 'grid' | 'list';
      }
    } catch (error) {
      console.error('Error loading favorites from storage:', error);
    }
  }

  private saveFavoritesToStorage(): void {
    try {
      localStorage.setItem(`favorites_${this.storeSlug}`, JSON.stringify(this.favoriteItems));
      localStorage.setItem(`favorites_viewmode_${this.storeSlug}`, this.viewMode);
    } catch (error) {
      console.error('Error saving favorites to storage:', error);
    }
  }
}