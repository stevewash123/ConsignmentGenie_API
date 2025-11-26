import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PublicStoreService } from '../../services/public-store.service';
import { ShoppingCartService } from '../../../customer/services/shopping-cart.service';
import { CustomerAuthService } from '../../../customer/services/customer-auth.service';
import { PublicStoreInfo } from '../../../shared/models/api.models';

@Component({
  selector: 'app-store-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, FormsModule],
  template: `
    <div class="store-layout">
      <!-- Header -->
      <header class="store-header">
        <div class="header-container">
          <!-- Logo -->
          <div class="logo">
            <a [routerLink]="['/store', orgSlug()]" class="logo-link">
              @if (storeInfo()?.logoUrl) {
                <img [src]="storeInfo()!.logoUrl" [alt]="storeInfo()!.storeName" class="logo-image">
              } @else {
                <h1 class="logo-text">{{ storeInfo()?.storeName || 'Store' }}</h1>
              }
            </a>
          </div>

          <!-- Navigation -->
          <nav class="main-nav">
            <a [routerLink]="['/store', orgSlug()]" class="nav-link">Home</a>
            <a [routerLink]="['/store', orgSlug(), 'products']" class="nav-link">Products</a>
            <div class="nav-dropdown">
              <span class="nav-link">Categories</span>
              <div class="dropdown-content">
                @for (category of categories(); track category.id) {
                  <a [routerLink]="['/store', orgSlug(), 'category', category.slug]" class="dropdown-link">
                    {{ category.name }}
                  </a>
                }
              </div>
            </div>
            <a [routerLink]="['/store', orgSlug(), 'about']" class="nav-link">About</a>
            <a [routerLink]="['/store', orgSlug(), 'contact']" class="nav-link">Contact</a>
          </nav>

          <!-- Search -->
          <div class="search-bar">
            <input
              type="text"
              placeholder="Search products..."
              [(ngModel)]="searchQuery"
              (keyup.enter)="performSearch()"
              class="search-input"
            >
            <button type="button" (click)="performSearch()" class="search-button">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M21 21L16.514 16.506L21 21ZM19 10.5C19 15.194 15.194 19 10.5 19C5.806 19 2 15.194 2 10.5C2 5.806 5.806 2 10.5 2C15.194 2 19 5.806 19 10.5Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </button>
          </div>

          <!-- User Actions -->
          <div class="user-actions">
            <!-- Cart -->
            <button type="button" class="cart-button" (click)="toggleCart()">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 3H5L5.4 5M7 13H17L21 5H5.4M7 13L5.4 5M7 13L4.7 15.3C4.3 15.7 4.6 16.5 5.1 16.5H17M17 13V17C17 18.1 16.1 19 15 19H9C7.9 19 7 18.1 7 17V13H17Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              @if (cartItemCount() > 0) {
                <span class="cart-badge">{{ cartItemCount() }}</span>
              }
            </button>

            <!-- User Menu -->
            @if (authService.isAuthenticated()) {
              <div class="user-menu">
                <button type="button" class="user-button" (click)="toggleUserMenu()">
                  {{ authService.currentCustomer()?.firstName || 'Account' }}
                </button>
                @if (showUserMenu()) {
                  <div class="user-dropdown">
                    <a [routerLink]="['/customer/dashboard']" class="dropdown-link">Dashboard</a>
                    <a [routerLink]="['/customer/orders']" class="dropdown-link">Orders</a>
                    <a [routerLink]="['/customer/wishlist']" class="dropdown-link">Wishlist</a>
                    <a [routerLink]="['/customer/profile']" class="dropdown-link">Profile</a>
                    <hr class="dropdown-divider">
                    <button type="button" (click)="logout()" class="dropdown-button">Logout</button>
                  </div>
                }
              </div>
            } @else {
              <div class="auth-links">
                <a [routerLink]="['/customer/login']" class="auth-link">Login</a>
                <a [routerLink]="['/customer/register']" class="auth-link primary">Sign Up</a>
              </div>
            }
          </div>
        </div>
      </header>

      <!-- Mobile Menu Toggle -->
      <button type="button" class="mobile-menu-toggle" (click)="toggleMobileMenu()">
        <span></span>
        <span></span>
        <span></span>
      </button>

      <!-- Mobile Menu -->
      @if (showMobileMenu()) {
        <div class="mobile-menu">
          <a [routerLink]="['/store', orgSlug()]" (click)="closeMobileMenu()" class="mobile-nav-link">Home</a>
          <a [routerLink]="['/store', orgSlug(), 'products']" (click)="closeMobileMenu()" class="mobile-nav-link">Products</a>
          @for (category of categories(); track category.id) {
            <a [routerLink]="['/store', orgSlug(), 'category', category.slug]" (click)="closeMobileMenu()" class="mobile-nav-link category">
              {{ category.name }}
            </a>
          }
          <a [routerLink]="['/store', orgSlug(), 'about']" (click)="closeMobileMenu()" class="mobile-nav-link">About</a>
          <a [routerLink]="['/store', orgSlug(), 'contact']" (click)="closeMobileMenu()" class="mobile-nav-link">Contact</a>
        </div>
      }

      <!-- Main Content -->
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <!-- Footer -->
      <footer class="store-footer">
        <div class="footer-container">
          <div class="footer-section">
            <h3>{{ storeInfo()?.storeName }}</h3>
            @if (storeInfo()?.storeDescription) {
              <p>{{ storeInfo()!.storeDescription }}</p>
            }
          </div>

          <div class="footer-section">
            <h4>Quick Links</h4>
            <ul>
              <li><a [routerLink]="['/store', orgSlug()]">Home</a></li>
              <li><a [routerLink]="['/store', orgSlug(), 'products']">Products</a></li>
              <li><a [routerLink]="['/store', orgSlug(), 'about']">About</a></li>
              <li><a [routerLink]="['/store', orgSlug(), 'contact']">Contact</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Customer Service</h4>
            <ul>
              <li><a [routerLink]="['/customer/orders']">Track Order</a></li>
              <li><a href="mailto:support@example.com">Contact Support</a></li>
              <li><a href="#">Shipping Info</a></li>
              <li><a href="#">Return Policy</a></li>
            </ul>
          </div>

          @if (storeInfo()?.contactInfo) {
            <div class="footer-section">
              <h4>Contact Info</h4>
              <!-- Contact info would be parsed from JSON -->
            </div>
          }
        </div>

        <div class="footer-bottom">
          <p>&copy; 2024 {{ storeInfo()?.storeName }}. All rights reserved.</p>
        </div>
      </footer>

      <!-- Cart Sidebar -->
      @if (showCartSidebar()) {
        <div class="cart-overlay" (click)="closeCart()">
          <div class="cart-sidebar" (click)="$event.stopPropagation()">
            <div class="cart-header">
              <h3>Shopping Cart</h3>
              <button type="button" (click)="closeCart()" class="close-button">&times;</button>
            </div>

            <div class="cart-content">
              @if (cartService.isEmpty()) {
                <div class="empty-cart">
                  <p>Your cart is empty</p>
                  <a [routerLink]="['/store', orgSlug(), 'products']" (click)="closeCart()" class="shop-button">
                    Continue Shopping
                  </a>
                </div>
              } @else {
                <div class="cart-items">
                  @for (item of cartService.cart()?.items || []; track item.id) {
                    <div class="cart-item">
                      <div class="item-image">
                        @if (item.item.photos?.[0]) {
                          <img [src]="item.item.photos[0].url" [alt]="item.item.title">
                        }
                      </div>
                      <div class="item-details">
                        <h4>{{ item.item.title }}</h4>
                        <p class="item-price">\${{ item.item.price }}</p>
                        <div class="quantity-controls">
                          <button (click)="updateQuantity(item.itemId, item.quantity - 1)">-</button>
                          <span>{{ item.quantity }}</span>
                          <button (click)="updateQuantity(item.itemId, item.quantity + 1)">+</button>
                        </div>
                      </div>
                      <button type="button" (click)="removeFromCart(item.itemId)" class="remove-button">&times;</button>
                    </div>
                  }
                </div>

                <div class="cart-footer">
                  <div class="cart-total">
                    <strong>Total: \${{ cartService.totalAmount() }}</strong>
                  </div>
                  <div class="cart-actions">
                    <a [routerLink]="['/customer/cart']" (click)="closeCart()" class="view-cart-button">
                      View Cart
                    </a>
                    <a [routerLink]="['/customer/checkout']" (click)="closeCart()" class="checkout-button">
                      Checkout
                    </a>
                  </div>
                </div>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styleUrls: ['./store-layout.component.scss']
})
export class StoreLayoutComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly storeService = inject(PublicStoreService);
  protected readonly cartService = inject(ShoppingCartService);
  protected readonly authService = inject(CustomerAuthService);

  // Signals
  protected orgSlug = signal<string>('');
  protected storeInfo = signal<PublicStoreInfo | null>(null);
  protected categories = signal<any[]>([]);
  protected searchQuery = '';

  // UI state
  protected showCartSidebar = signal(false);
  protected showUserMenu = signal(false);
  protected showMobileMenu = signal(false);

  // Computed values
  protected cartItemCount = this.cartService.itemCount;

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const orgSlug = params['orgSlug'];
      if (orgSlug) {
        this.orgSlug.set(orgSlug);
        this.loadStoreData(orgSlug);
      }
    });
  }

  private loadStoreData(orgSlug: string): void {
    this.storeService.getStoreInfo(orgSlug).subscribe({
      next: (storeInfo) => {
        if (storeInfo) {
          this.storeInfo.set(storeInfo);
          document.title = storeInfo.storeName;
        }
      },
      error: (error) => {
        console.error('Error loading store info:', error);
        // Redirect to 404 or error page
      }
    });

    this.storeService.getCategories(orgSlug).subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  protected performSearch(): void {
    if (this.searchQuery.trim()) {
      this.router.navigate(['/store', this.orgSlug(), 'search'], {
        queryParams: { q: this.searchQuery.trim() }
      });
    }
  }

  protected toggleCart(): void {
    this.showCartSidebar.set(!this.showCartSidebar());
  }

  protected closeCart(): void {
    this.showCartSidebar.set(false);
  }

  protected toggleUserMenu(): void {
    this.showUserMenu.set(!this.showUserMenu());
  }

  protected toggleMobileMenu(): void {
    this.showMobileMenu.set(!this.showMobileMenu());
  }

  protected closeMobileMenu(): void {
    this.showMobileMenu.set(false);
  }

  protected logout(): void {
    this.authService.logout();
    this.showUserMenu.set(false);
  }

  protected updateQuantity(itemId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeFromCart(itemId);
    } else {
      this.cartService.updateCartItem(itemId, quantity).subscribe();
    }
  }

  protected removeFromCart(itemId: string): void {
    this.cartService.removeFromCart(itemId).subscribe();
  }
}