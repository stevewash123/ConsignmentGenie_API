import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperAuthService } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';
import { ShopperCartService, Cart, CartItem } from '../../services/shopper-cart.service';

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

        <div class="cart-content" *ngIf="cart && cart.items.length > 0; else emptyCartTemplate">
          <div class="cart-items">
            <div class="cart-item" *ngFor="let item of cart.items; trackBy: trackByItemId" data-cy="cart-item">
              <div class="item-image">
                <img
                  [src]="item.primaryImageUrl || '/assets/placeholder-item.jpg'"
                  [alt]="item.title"
                  class="item-img"
                  (error)="onImageError($event)">
              </div>

              <div class="item-details">
                <h3 class="item-name">
                  <a [routerLink]="['/shop', storeSlug, 'items', item.itemId]" class="item-link">
                    {{ item.title }}
                  </a>
                </h3>
                <p class="item-category" *ngIf="item.category">{{ item.category }}</p>
                <p class="item-brand" *ngIf="item.brand">{{ item.brand }}</p>
                <p class="item-condition">{{ item.condition }}</p>
                <p class="item-added">Added {{ item.addedAt | date:'short' }}</p>
              </div>

              <div class="item-quantity">
                <label class="quantity-label">Qty:</label>
                <div class="quantity-controls">
                  <button
                    class="qty-btn"
                    data-cy="decrease-quantity-btn"
                    (click)="updateQuantity(item.itemId, item.quantity - 1)"
                    [disabled]="item.quantity <= 1">
                    -
                  </button>
                  <span class="quantity" data-cy="item-quantity">{{ item.quantity }}</span>
                  <button
                    class="qty-btn"
                    data-cy="increase-quantity-btn"
                    (click)="updateQuantity(item.itemId, item.quantity + 1)">
                    +
                  </button>
                </div>
              </div>

              <div class="item-price">
                <span class="price" data-cy="item-total-price">\${{ (item.price * item.quantity) | number:'1.2-2' }}</span>
                <span class="unit-price" *ngIf="item.quantity > 1">
                  (\${{ item.price | number:'1.2-2' }} each)
                </span>
              </div>

              <div class="item-actions">
                <button
                  class="btn btn-outline-danger btn-sm"
                  data-cy="remove-item-btn"
                  (click)="removeItem(item.itemId)">
                  Remove
                </button>
              </div>
            </div>
          </div>

          <div class="cart-summary">
            <div class="summary-card">
              <h3>Order Summary</h3>

              <div class="summary-row">
                <span>Subtotal ({{ cart.itemCount }} items)</span>
                <span data-cy="cart-subtotal">\${{ cart.subtotal | number:'1.2-2' }}</span>
              </div>

              <div class="summary-row">
                <span>Tax (estimated)</span>
                <span data-cy="cart-tax">\${{ cart.tax | number:'1.2-2' }}</span>
              </div>

              <div class="summary-row total">
                <span>Total</span>
                <span data-cy="cart-total">\${{ cart.total | number:'1.2-2' }}</span>
              </div>

              <div class="checkout-actions">
                <button
                  class="btn btn-primary btn-block"
                  data-cy="proceed-to-checkout-btn"
                  (click)="proceedToCheckout()">
                  Proceed to Checkout
                </button>

                <button
                  class="btn btn-outline-secondary btn-block"
                  [routerLink]="['/shop', storeSlug]">
                  Continue Shopping
                </button>

                <button
                  class="btn btn-outline-warning btn-block"
                  data-cy="clear-cart-btn"
                  (click)="clearCart()"
                  *ngIf="cart.items.length > 0">
                  Clear Cart
                </button>
              </div>

              <div class="guest-notice" *ngIf="!isAuthenticated">
                <p class="notice-text">
                  💡 <a [routerLink]="['/shop', storeSlug, 'login']" [queryParams]="{returnUrl: '/shop/' + storeSlug + '/cart'}">
                    Sign in
                  </a> to save your cart and track your orders.
                </p>
              </div>

              <div class="cart-persistence-info">
                <p class="info-text">
                  <small>Cart last updated: {{ cart.lastUpdated | date:'medium' }}</small>
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

    .item-link {
      color: inherit;
      text-decoration: none;
      transition: color 0.2s;
    }

    .item-link:hover {
      color: #007bff;
      text-decoration: underline;
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
      margin-bottom: 0.25rem;
      font-weight: 500;
    }

    .item-added {
      font-size: 0.75rem;
      color: #adb5bd;
      margin: 0;
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
      transition: all 0.2s;
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

    .btn-outline-warning {
      color: #ffc107;
      border-color: #ffc107;
      background-color: transparent;
    }

    .btn-outline-warning:hover {
      color: #212529;
      background-color: #ffc107;
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
      margin-bottom: 1rem;
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

    .cart-persistence-info {
      border-top: 1px solid #dee2e6;
      padding-top: 1rem;
    }

    .info-text {
      margin: 0;
      text-align: center;
    }

    .info-text small {
      color: #6c757d;
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
    }

    @media (max-width: 480px) {
      .cart-header h1 {
        font-size: 2rem;
      }

      .empty-cart {
        padding: 2rem 1rem;
      }
    }
  `]
})
export class CartComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';
  isAuthenticated = false;
  cart: Cart | null = null;
  isLoading = true;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: ShopperAuthService,
    private storeService: ShopperStoreService,
    private cartService: ShopperCartService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      this.isAuthenticated = this.authService.isAuthenticated(this.storeSlug);

      if (this.storeSlug) {
        this.initializeCart();
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

  private initializeCart(): void {
    this.cartService.setCurrentStore(this.storeSlug);

    this.cartService.getCurrentCart$().pipe(
      takeUntil(this.destroy$)
    ).subscribe(cart => {
      this.cart = cart;
      this.isLoading = false;
    });
  }

  trackByItemId(index: number, item: CartItem): string {
    return item.itemId;
  }

  updateQuantity(itemId: string, newQuantity: number): void {
    this.cartService.updateItemQuantity(itemId, newQuantity);
  }

  removeItem(itemId: string): void {
    this.cartService.removeItem(itemId);
  }

  clearCart(): void {
    if (confirm('Are you sure you want to clear your entire cart?')) {
      this.cartService.clearCart();
    }
  }

  proceedToCheckout(): void {
    if (!this.cart || this.cart.items.length === 0) return;

    this.router.navigate(['/shop', this.storeSlug, 'checkout']);
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }
}