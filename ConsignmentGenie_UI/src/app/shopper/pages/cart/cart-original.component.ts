import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperAuthService } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

interface CartItem {
  id: string;
  name: string;
  description: string;
  price: number;
  imageUrl?: string;
  category: string;
  quantity: number;
}

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="cart-container">
      <div class="container">
        <div class="cart-header">
          <h1>Shopping Cart</h1>
          <p class="store-name" *ngIf="storeInfo">{{ storeInfo.name }}</p>
        </div>

        <div class="cart-content" *ngIf="cartItems.length > 0; else emptyCartTemplate">
          <div class="cart-items">
            <div class="cart-item" *ngFor="let item of cartItems; trackBy: trackByItemId">
              <div class="item-image">
                <img
                  [src]="item.imageUrl || '/assets/placeholder-item.jpg'"
                  [alt]="item.name"
                  class="item-img"
                  (error)="onImageError($event)">
              </div>

              <div class="item-details">
                <h3 class="item-name">{{ item.name }}</h3>
                <p class="item-category">{{ item.category }}</p>
                <p class="item-description">{{ item.description }}</p>
              </div>

              <div class="item-quantity">
                <label class="quantity-label">Qty:</label>
                <div class="quantity-controls">
                  <button
                    class="qty-btn"
                    (click)="updateQuantity(item, item.quantity - 1)"
                    [disabled]="item.quantity <= 1">
                    -
                  </button>
                  <span class="quantity">{{ item.quantity }}</span>
                  <button
                    class="qty-btn"
                    (click)="updateQuantity(item, item.quantity + 1)">
                    +
                  </button>
                </div>
              </div>

              <div class="item-price">
                <span class="price">\${{ (item.price * item.quantity) | number:'1.2-2' }}</span>
                <span class="unit-price" *ngIf="item.quantity > 1">
                  (\${{ item.price | number:'1.2-2' }} each)
                </span>
              </div>

              <div class="item-actions">
                <button
                  class="btn btn-outline-danger btn-sm"
                  (click)="removeItem(item)">
                  Remove
                </button>
              </div>
            </div>
          </div>

          <div class="cart-summary">
            <div class="summary-card">
              <h3>Order Summary</h3>

              <div class="summary-row">
                <span>Subtotal ({{ getTotalItems() }} items)</span>
                <span>\${{ getSubtotal() | number:'1.2-2' }}</span>
              </div>

              <div class="summary-row">
                <span>Tax (estimated)</span>
                <span>\${{ getTax() | number:'1.2-2' }}</span>
              </div>

              <div class="summary-row total">
                <span>Total</span>
                <span>\${{ getTotal() | number:'1.2-2' }}</span>
              </div>

              <div class="checkout-actions">
                <button
                  class="btn btn-primary btn-block"
                  (click)="proceedToCheckout()">
                  Proceed to Checkout
                </button>

                <button
                  class="btn btn-outline-secondary btn-block"
                  [routerLink]="['/shop', storeSlug]">
                  Continue Shopping
                </button>
              </div>

              <div class="guest-notice" *ngIf="!isAuthenticated">
                <p class="notice-text">
                  💡 <a [routerLink]="['/shop', storeSlug, 'login']" [queryParams]="{returnUrl: '/shop/' + storeSlug + '/cart'}">
                    Sign in
                  </a> to save your cart and track your orders.
                </p>
              </div>
            </div>
          </div>
        </div>

        <ng-template #emptyCartTemplate>
          <div class="empty-cart">
            <div class="empty-icon">🛒</div>
            <h2>Your cart is empty</h2>
            <p>Looks like you haven't added anything to your cart yet.</p>
            <button
              class="btn btn-primary"
              [routerLink]="['/shop', storeSlug]">
              Start Shopping
            </button>
          </div>
        </ng-template>

        <div class="recommended-section" *ngIf="cartItems.length > 0">
          <h3>You might also like</h3>
          <div class="recommended-items">
            <div class="recommended-item" *ngFor="let item of recommendedItems">
              <img
                [src]="item.imageUrl || '/assets/placeholder-item.jpg'"
                [alt]="item.name"
                class="recommended-img"
                (error)="onImageError($event)">
              <h4>{{ item.name }}</h4>
              <p class="recommended-price">\${{ item.price | number:'1.2-2' }}</p>
              <button class="btn btn-outline-primary btn-sm" (click)="addRecommendedToCart(item)">
                Add to Cart
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .cart-container {
      min-height: 80vh;
      padding: 2rem 0;
      background-color: #f8f9fa;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .cart-header {
      text-align: center;
      margin-bottom: 2rem;
      padding: 1rem 0;
    }

    .cart-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 0.5rem;
    }

    .store-name {
      font-size: 1.1rem;
      color: #007bff;
      margin: 0;
    }

    .cart-content {
      display: grid;
      grid-template-columns: 1fr 350px;
      gap: 2rem;
      align-items: start;
    }

    .cart-items {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .cart-item {
      display: grid;
      grid-template-columns: 120px 1fr auto auto auto;
      gap: 1rem;
      padding: 1.5rem;
      border-bottom: 1px solid #dee2e6;
      align-items: center;
    }

    .cart-item:last-child {
      border-bottom: none;
    }

    .item-image {
      width: 120px;
      height: 120px;
      border-radius: 0.5rem;
      overflow: hidden;
    }

    .item-img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .item-details {
      min-width: 0;
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
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .item-quantity {
      text-align: center;
    }

    .quantity-label {
      display: block;
      font-size: 0.875rem;
      color: #6c757d;
      margin-bottom: 0.5rem;
    }

    .quantity-controls {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
    }

    .qty-btn {
      width: 32px;
      height: 32px;
      border: 1px solid #ced4da;
      background: white;
      border-radius: 0.25rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
    }

    .qty-btn:hover:not(:disabled) {
      background-color: #e9ecef;
    }

    .qty-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .quantity {
      font-weight: 600;
      min-width: 2rem;
      text-align: center;
    }

    .item-price {
      text-align: right;
      min-width: 100px;
    }

    .price {
      font-size: 1.1rem;
      font-weight: bold;
      color: #28a745;
      display: block;
    }

    .unit-price {
      font-size: 0.75rem;
      color: #6c757d;
    }

    .item-actions {
      text-align: center;
    }

    .cart-summary {
      position: sticky;
      top: 2rem;
    }

    .summary-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 1.5rem;
    }

    .summary-card h3 {
      font-size: 1.25rem;
      font-weight: 600;
      color: #343a40;
      margin-bottom: 1.5rem;
      text-align: center;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
    }

    .summary-row:not(:last-child) {
      border-bottom: 1px solid #dee2e6;
    }

    .summary-row.total {
      font-size: 1.1rem;
      font-weight: bold;
      color: #343a40;
      border-bottom: none;
      margin-bottom: 1.5rem;
    }

    .checkout-actions {
      margin-bottom: 1.5rem;
    }

    .btn {
      padding: 0.75rem 1rem;
      border: 1px solid transparent;
      border-radius: 0.375rem;
      font-size: 1rem;
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

    .btn-primary:hover {
      background-color: #0056b3;
      border-color: #004085;
    }

    .btn-outline-secondary {
      color: #6c757d;
      border-color: #6c757d;
      background-color: transparent;
    }

    .btn-outline-secondary:hover {
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

    .btn-outline-primary {
      color: #007bff;
      border-color: #007bff;
      background-color: transparent;
    }

    .btn-outline-primary:hover {
      color: white;
      background-color: #007bff;
    }

    .btn-block {
      width: 100%;
      margin-bottom: 0.75rem;
    }

    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.875rem;
    }

    .guest-notice {
      background-color: #e7f3ff;
      border: 1px solid #b3d7ff;
      border-radius: 0.375rem;
      padding: 1rem;
    }

    .notice-text {
      font-size: 0.875rem;
      color: #1a5490;
      margin: 0;
    }

    .notice-text a {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .notice-text a:hover {
      text-decoration: underline;
    }

    .empty-cart {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 4rem 2rem;
      text-align: center;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .empty-cart h2 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .empty-cart p {
      color: #6c757d;
      margin-bottom: 2rem;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
    }

    .recommended-section {
      margin-top: 3rem;
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 2rem;
    }

    .recommended-section h3 {
      text-align: center;
      color: #343a40;
      margin-bottom: 2rem;
    }

    .recommended-items {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .recommended-item {
      text-align: center;
      padding: 1rem;
      border: 1px solid #dee2e6;
      border-radius: 0.5rem;
      transition: box-shadow 0.2s;
    }

    .recommended-item:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .recommended-img {
      width: 100%;
      height: 150px;
      object-fit: cover;
      border-radius: 0.375rem;
      margin-bottom: 1rem;
    }

    .recommended-item h4 {
      font-size: 1rem;
      color: #343a40;
      margin-bottom: 0.5rem;
    }

    .recommended-price {
      font-size: 1.1rem;
      font-weight: bold;
      color: #28a745;
      margin-bottom: 1rem;
    }

    @media (max-width: 968px) {
      .cart-content {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .cart-summary {
        position: static;
        order: -1;
      }
    }

    @media (max-width: 768px) {
      .cart-item {
        grid-template-columns: 1fr;
        gap: 1rem;
        text-align: center;
      }

      .item-image {
        justify-self: center;
      }

      .item-details {
        text-align: center;
      }

      .item-price {
        text-align: center;
      }

      .recommended-items {
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 1rem;
      }
    }

    @media (max-width: 480px) {
      .cart-header h1 {
        font-size: 2rem;
      }

      .empty-cart {
        padding: 2rem 1rem;
      }

      .recommended-section {
        padding: 1rem;
      }
    }
  `]
})
export class CartComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  isAuthenticated = false;

  // Sample cart data for Phase 1 MVP
  cartItems: CartItem[] = [
    {
      id: '1',
      name: 'Vintage Leather Jacket',
      description: 'Classic brown leather jacket in excellent condition. Perfect for fall weather.',
      price: 125.00,
      category: 'Clothing',
      quantity: 1
    },
    {
      id: '2',
      name: 'Antique Wooden Table',
      description: 'Beautiful oak dining table from the 1960s. Seats up to 6 people.',
      price: 450.00,
      category: 'Furniture',
      quantity: 1
    }
  ];

  recommendedItems: CartItem[] = [
    {
      id: '5',
      name: 'Modern Art Print',
      description: 'Limited edition print by local artist.',
      price: 75.00,
      category: 'Art',
      quantity: 1
    },
    {
      id: '6',
      name: 'Retro Coffee Table',
      description: 'Mid-century modern coffee table.',
      price: 180.00,
      category: 'Furniture',
      quantity: 1
    }
  ];

  private taxRate = 0.08; // 8% tax rate
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      this.isAuthenticated = this.authService.isAuthenticated(this.storeSlug);
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    // In a real implementation, load cart from localStorage or API
    this.loadCartFromStorage();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  trackByItemId(index: number, item: CartItem): string {
    return item.id;
  }

  updateQuantity(item: CartItem, newQuantity: number): void {
    if (newQuantity >= 1) {
      item.quantity = newQuantity;
      this.saveCartToStorage();
    }
  }

  removeItem(item: CartItem): void {
    const index = this.cartItems.findIndex(cartItem => cartItem.id === item.id);
    if (index > -1) {
      this.cartItems.splice(index, 1);
      this.saveCartToStorage();
    }
  }

  getTotalItems(): number {
    return this.cartItems.reduce((total, item) => total + item.quantity, 0);
  }

  getSubtotal(): number {
    return this.cartItems.reduce((total, item) => total + (item.price * item.quantity), 0);
  }

  getTax(): number {
    return this.getSubtotal() * this.taxRate;
  }

  getTotal(): number {
    return this.getSubtotal() + this.getTax();
  }

  proceedToCheckout(): void {
    if (this.cartItems.length === 0) return;

    this.router.navigate(['/shop', this.storeSlug, 'checkout']);
  }

  addRecommendedToCart(item: CartItem): void {
    const existingItem = this.cartItems.find(cartItem => cartItem.id === item.id);

    if (existingItem) {
      existingItem.quantity += 1;
    } else {
      this.cartItems.push({ ...item, quantity: 1 });
    }

    this.saveCartToStorage();

    // Remove from recommended items
    const index = this.recommendedItems.findIndex(rec => rec.id === item.id);
    if (index > -1) {
      this.recommendedItems.splice(index, 1);
    }
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }

  private loadCartFromStorage(): void {
    try {
      const cartData = localStorage.getItem(`cart_${this.storeSlug}`);
      if (cartData) {
        this.cartItems = JSON.parse(cartData);
      }
    } catch (error) {
      console.error('Error loading cart from storage:', error);
    }
  }

  private saveCartToStorage(): void {
    try {
      localStorage.setItem(`cart_${this.storeSlug}`, JSON.stringify(this.cartItems));
    } catch (error) {
      console.error('Error saving cart to storage:', error);
    }
  }
}