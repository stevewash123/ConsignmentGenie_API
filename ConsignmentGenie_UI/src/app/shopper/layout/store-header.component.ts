import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { StoreInfoDto } from '../services/shopper-store.service';
import { ShopperAuthService, ShopperProfileDto } from '../services/shopper-auth.service';

@Component({
  selector: 'app-store-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule],
  template: `
    <header class="store-header">
      <div class="container">
        <nav class="navbar">
          <!-- Store Logo/Name -->
          <div class="navbar-brand">
            <a [routerLink]="['/shop', storeSlug]" class="brand-link">
              <img *ngIf="storeInfo?.logoUrl" [src]="storeInfo.logoUrl" [alt]="storeInfo.name" class="store-logo">
              <span class="store-name">{{ storeInfo?.name || 'Store' }}</span>
            </a>
          </div>

          <!-- Navigation Links -->
          <div class="navbar-nav">
            <a [routerLink]="['/shop', storeSlug]"
               routerLinkActive="active"
               [routerLinkActiveOptions]="{exact: true}"
               class="nav-link">Shop</a>

            <!-- Category dropdown would go here in Phase 2 -->
            <div class="nav-dropdown" #categoryDropdown>
              <button class="nav-link dropdown-toggle"
                      (click)="toggleDropdown(categoryDropdown)">
                Categories
              </button>
              <div class="dropdown-menu">
                <a [routerLink]="['/shop', storeSlug, 'catalog']" class="dropdown-item">All Items</a>
                <!-- Categories will be populated in Phase 2 -->
              </div>
            </div>
          </div>

          <!-- Search Bar -->
          <div class="search-container">
            <form (ngSubmit)="onSearch()" class="search-form">
              <input
                type="text"
                [(ngModel)]="searchQuery"
                name="searchQuery"
                placeholder="Search items..."
                class="search-input">
              <button type="submit" class="search-button" [disabled]="!searchQuery?.trim()">
                <span class="search-icon">🔍</span>
              </button>
            </form>
          </div>

          <!-- User Actions -->
          <div class="navbar-actions">
            <!-- Cart -->
            <a [routerLink]="['/shop', storeSlug, 'cart']" class="nav-action cart-link">
              <span class="cart-icon">🛒</span>
              <span class="cart-count" *ngIf="cartItemCount > 0">{{ cartItemCount }}</span>
            </a>

            <!-- User Menu -->
            <div class="user-menu" *ngIf="!isAuthenticated">
              <a [routerLink]="['/shop', storeSlug, 'login']" class="nav-action">Sign In</a>
              <a [routerLink]="['/shop', storeSlug, 'register']" class="nav-action register-link">Register</a>
            </div>

            <div class="user-menu" *ngIf="isAuthenticated" #userDropdown>
              <button class="nav-action user-button"
                      (click)="toggleDropdown(userDropdown)">
                <span class="user-icon">👤</span>
                <span class="user-name">{{ shopperProfile?.fullName || 'Account' }}</span>
              </button>
              <div class="dropdown-menu dropdown-menu-right">
                <a [routerLink]="['/shop', storeSlug, 'account']" class="dropdown-item">My Account</a>
                <a [routerLink]="['/shop', storeSlug, 'account', 'orders']" class="dropdown-item">Order History</a>
                <a [routerLink]="['/shop', storeSlug, 'account', 'settings']" class="dropdown-item">Settings</a>
                <div class="dropdown-divider"></div>
                <button (click)="logout()" class="dropdown-item logout-item">Sign Out</button>
              </div>
            </div>
          </div>

          <!-- Mobile Menu Toggle -->
          <button class="mobile-menu-toggle"
                  (click)="toggleMobileMenu()"
                  [class.active]="showMobileMenu">
            <span></span>
            <span></span>
            <span></span>
          </button>
        </nav>

        <!-- Mobile Navigation -->
        <div class="mobile-nav" [class.show]="showMobileMenu">
          <div class="mobile-search">
            <form (ngSubmit)="onSearch()" class="search-form">
              <input
                type="text"
                [(ngModel)]="searchQuery"
                name="searchQuery"
                placeholder="Search items..."
                class="search-input">
              <button type="submit" class="search-button">🔍</button>
            </form>
          </div>

          <div class="mobile-nav-links">
            <a [routerLink]="['/shop', storeSlug]"
               (click)="closeMobileMenu()"
               class="mobile-nav-link">Shop</a>
            <a [routerLink]="['/shop', storeSlug, 'catalog']"
               (click)="closeMobileMenu()"
               class="mobile-nav-link">All Items</a>
            <a [routerLink]="['/shop', storeSlug, 'cart']"
               (click)="closeMobileMenu()"
               class="mobile-nav-link">
               Cart <span *ngIf="cartItemCount > 0">({{ cartItemCount }})</span>
            </a>
          </div>

          <div class="mobile-user-actions">
            <ng-container *ngIf="!isAuthenticated">
              <a [routerLink]="['/shop', storeSlug, 'login']"
                 (click)="closeMobileMenu()"
                 class="mobile-nav-link">Sign In</a>
              <a [routerLink]="['/shop', storeSlug, 'register']"
                 (click)="closeMobileMenu()"
                 class="mobile-nav-link">Register</a>
            </ng-container>

            <ng-container *ngIf="isAuthenticated">
              <div class="mobile-user-info">
                <span>{{ shopperProfile?.fullName || 'Account' }}</span>
              </div>
              <a [routerLink]="['/shop', storeSlug, 'account']"
                 (click)="closeMobileMenu()"
                 class="mobile-nav-link">My Account</a>
              <a [routerLink]="['/shop', storeSlug, 'account', 'orders']"
                 (click)="closeMobileMenu()"
                 class="mobile-nav-link">Order History</a>
              <a [routerLink]="['/shop', storeSlug, 'account', 'settings']"
                 (click)="closeMobileMenu()"
                 class="mobile-nav-link">Settings</a>
              <button (click)="logout(); closeMobileMenu()"
                      class="mobile-nav-link logout-button">Sign Out</button>
            </ng-container>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .store-header {
      background-color: #ffffff;
      border-bottom: 1px solid #dee2e6;
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .navbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 0;
      position: relative;
    }

    .navbar-brand {
      flex-shrink: 0;
    }

    .brand-link {
      display: flex;
      align-items: center;
      text-decoration: none;
      color: #343a40;
      font-weight: bold;
      font-size: 1.25rem;
    }

    .store-logo {
      height: 40px;
      width: auto;
      margin-right: 0.5rem;
    }

    .store-name {
      color: #343a40;
    }

    .navbar-nav {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      margin-left: 2rem;
    }

    .nav-link {
      color: #6c757d;
      text-decoration: none;
      padding: 0.5rem 1rem;
      border-radius: 0.25rem;
      transition: all 0.2s;
      background: none;
      border: none;
      cursor: pointer;
      font-size: 1rem;
    }

    .nav-link:hover,
    .nav-link.active {
      color: #007bff;
      background-color: #f8f9fa;
    }

    .nav-dropdown {
      position: relative;
    }

    .dropdown-toggle::after {
      content: ' ▾';
      font-size: 0.75rem;
    }

    .dropdown-menu {
      position: absolute;
      top: 100%;
      left: 0;
      background-color: #ffffff;
      border: 1px solid #dee2e6;
      border-radius: 0.25rem;
      box-shadow: 0 4px 8px rgba(0,0,0,0.1);
      min-width: 200px;
      z-index: 1000;
      display: none;
    }

    .dropdown-menu.show {
      display: block;
    }

    .dropdown-menu-right {
      right: 0;
      left: auto;
    }

    .dropdown-item {
      display: block;
      padding: 0.5rem 1rem;
      color: #343a40;
      text-decoration: none;
      transition: background-color 0.2s;
      background: none;
      border: none;
      width: 100%;
      text-align: left;
      cursor: pointer;
      font-size: 1rem;
    }

    .dropdown-item:hover {
      background-color: #f8f9fa;
    }

    .dropdown-divider {
      border-top: 1px solid #dee2e6;
      margin: 0.5rem 0;
    }

    .logout-item {
      color: #dc3545;
    }

    .search-container {
      flex: 1;
      max-width: 400px;
      margin: 0 2rem;
    }

    .search-form {
      display: flex;
      border: 1px solid #dee2e6;
      border-radius: 0.25rem;
      overflow: hidden;
    }

    .search-input {
      flex: 1;
      padding: 0.5rem 1rem;
      border: none;
      outline: none;
    }

    .search-button {
      background-color: #007bff;
      color: white;
      border: none;
      padding: 0.5rem 1rem;
      cursor: pointer;
      transition: background-color 0.2s;
    }

    .search-button:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .search-button:disabled {
      background-color: #6c757d;
      cursor: not-allowed;
    }

    .navbar-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .nav-action {
      color: #6c757d;
      text-decoration: none;
      padding: 0.5rem 1rem;
      border-radius: 0.25rem;
      transition: all 0.2s;
      background: none;
      border: none;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1rem;
    }

    .nav-action:hover {
      color: #007bff;
      background-color: #f8f9fa;
    }

    .register-link {
      background-color: #007bff;
      color: white;
    }

    .register-link:hover {
      background-color: #0056b3;
      color: white;
    }

    .cart-link {
      position: relative;
    }

    .cart-count {
      position: absolute;
      top: -8px;
      right: -8px;
      background-color: #dc3545;
      color: white;
      border-radius: 50%;
      width: 20px;
      height: 20px;
      font-size: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .user-menu {
      display: flex;
      align-items: center;
      gap: 1rem;
      position: relative;
    }

    .user-button {
      background: none;
      border: none;
    }

    .mobile-menu-toggle {
      display: none;
      flex-direction: column;
      background: none;
      border: none;
      padding: 0.5rem;
      cursor: pointer;
    }

    .mobile-menu-toggle span {
      width: 25px;
      height: 3px;
      background-color: #343a40;
      margin: 3px 0;
      transition: 0.3s;
    }

    .mobile-menu-toggle.active span:nth-child(1) {
      transform: rotate(-45deg) translate(-5px, 6px);
    }

    .mobile-menu-toggle.active span:nth-child(2) {
      opacity: 0;
    }

    .mobile-menu-toggle.active span:nth-child(3) {
      transform: rotate(45deg) translate(-5px, -6px);
    }

    .mobile-nav {
      display: none;
      padding: 1rem 0;
      border-top: 1px solid #dee2e6;
    }

    .mobile-nav.show {
      display: block;
    }

    .mobile-search {
      margin-bottom: 1rem;
    }

    .mobile-nav-links,
    .mobile-user-actions {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .mobile-nav-link {
      display: block;
      padding: 0.75rem 0;
      color: #343a40;
      text-decoration: none;
      border-bottom: 1px solid #f8f9fa;
      background: none;
      border: none;
      text-align: left;
      cursor: pointer;
      font-size: 1rem;
      width: 100%;
    }

    .mobile-nav-link:hover {
      color: #007bff;
    }

    .mobile-user-info {
      padding: 0.75rem 0;
      font-weight: bold;
      border-bottom: 1px solid #f8f9fa;
    }

    .logout-button {
      color: #dc3545;
    }

    @media (max-width: 768px) {
      .navbar-nav,
      .search-container {
        display: none;
      }

      .mobile-menu-toggle {
        display: flex;
      }

      .navbar-actions {
        gap: 0.5rem;
      }

      .nav-action {
        padding: 0.25rem 0.5rem;
        font-size: 0.875rem;
      }
    }
  `]
})
export class StoreHeaderComponent implements OnInit, OnDestroy {
  @Input() storeInfo: StoreInfoDto | null = null;
  @Input() storeSlug: string = '';

  searchQuery: string = '';
  showMobileMenu: boolean = false;
  cartItemCount: number = 0;
  isAuthenticated: boolean = false;
  shopperProfile: ShopperProfileDto | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private shopperAuthService: ShopperAuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to authentication status
    this.shopperAuthService.authStatus$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(isAuth => {
      this.isAuthenticated = isAuth && this.shopperAuthService.isAuthenticated(this.storeSlug);
    });

    // Subscribe to profile changes
    this.shopperAuthService.currentProfile$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(profile => {
      this.shopperProfile = profile;
    });

    // Load stored profile if authenticated
    if (this.isAuthenticated) {
      const storedProfile = this.shopperAuthService.getStoredProfile(this.storeSlug);
      if (storedProfile) {
        this.shopperProfile = storedProfile;
      }
    }

    // TODO: Subscribe to cart changes in Phase 2
    // For now, set a placeholder value
    this.cartItemCount = 0;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearch(): void {
    if (!this.searchQuery?.trim()) {
      return;
    }

    this.router.navigate(['/shop', this.storeSlug, 'search'], {
      queryParams: { q: this.searchQuery.trim() }
    });

    this.searchQuery = '';
    this.closeMobileMenu();
  }

  toggleDropdown(dropdownElement: HTMLElement): void {
    const menu = dropdownElement.querySelector('.dropdown-menu');
    if (menu) {
      menu.classList.toggle('show');
    }

    // Close other dropdowns
    document.querySelectorAll('.dropdown-menu.show').forEach(el => {
      if (el !== menu) {
        el.classList.remove('show');
      }
    });
  }

  toggleMobileMenu(): void {
    this.showMobileMenu = !this.showMobileMenu;
  }

  closeMobileMenu(): void {
    this.showMobileMenu = false;
  }

  logout(): void {
    this.shopperAuthService.logout(this.storeSlug);
    this.router.navigate(['/shop', this.storeSlug]);
  }

  // Close dropdowns when clicking outside
  onDocumentClick = (event: Event): void => {
    const target = event.target as HTMLElement;
    if (!target.closest('.nav-dropdown')) {
      document.querySelectorAll('.dropdown-menu.show').forEach(el => {
        el.classList.remove('show');
      });
    }
  };

  ngAfterViewInit(): void {
    document.addEventListener('click', this.onDocumentClick);
  }

  ngOnDestroy2(): void {
    document.removeEventListener('click', this.onDocumentClick);
  }
}