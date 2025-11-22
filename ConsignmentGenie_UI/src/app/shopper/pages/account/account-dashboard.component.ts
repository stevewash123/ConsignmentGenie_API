import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperAuthService, ShopperProfileDto } from '../../services/shopper-auth.service';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

@Component({
  selector: 'app-account-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <div class="account-dashboard">
      <div class="container">
        <!-- Page Header -->
        <div class="page-header">
          <h1 class="page-title">My Account</h1>
          <p class="page-subtitle" *ngIf="storeInfo">
            Shopping at {{ storeInfo.name }}
          </p>
        </div>

        <!-- Account Navigation Tabs -->
        <div class="account-nav">
          <a [routerLink]="['/shop', storeSlug, 'account']"
             routerLinkActive="active"
             [routerLinkActiveOptions]="{exact: true}"
             class="nav-tab">Overview</a>
          <a [routerLink]="['/shop', storeSlug, 'account', 'orders']"
             routerLinkActive="active"
             class="nav-tab">Orders</a>
          <a [routerLink]="['/shop', storeSlug, 'account', 'settings']"
             routerLinkActive="active"
             class="nav-tab">Settings</a>
        </div>

        <!-- Dashboard Content -->
        <div class="dashboard-content">
          <!-- Welcome Section -->
          <div class="welcome-section">
            <div class="welcome-card">
              <div class="welcome-header">
                <div class="user-avatar">
                  <span class="avatar-initial">{{ getInitials() }}</span>
                </div>
                <div class="user-info">
                  <h2 class="user-name">{{ shopperProfile?.fullName || 'Welcome!' }}</h2>
                  <p class="user-email">{{ shopperProfile?.email }}</p>
                  <p class="member-since" *ngIf="shopperProfile?.memberSince">
                    Member since {{ formatDate(shopperProfile.memberSince) }}
                  </p>
                </div>
              </div>
              <div class="welcome-actions">
                <a [routerLink]="['/shop', storeSlug, 'account', 'settings']"
                   class="btn btn-outline-primary">
                  Edit Profile
                </a>
              </div>
            </div>
          </div>

          <!-- Quick Stats -->
          <div class="stats-section">
            <div class="stats-grid">
              <div class="stat-card">
                <div class="stat-icon">📦</div>
                <div class="stat-content">
                  <div class="stat-number">{{ orderStats.total }}</div>
                  <div class="stat-label">Total Orders</div>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon">🚚</div>
                <div class="stat-content">
                  <div class="stat-number">{{ orderStats.pending }}</div>
                  <div class="stat-label">Pending</div>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon">✅</div>
                <div class="stat-content">
                  <div class="stat-number">{{ orderStats.completed }}</div>
                  <div class="stat-label">Completed</div>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon">💝</div>
                <div class="stat-content">
                  <div class="stat-number">{{ favoriteCount }}</div>
                  <div class="stat-label">Favorites</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Recent Orders -->
          <div class="recent-orders-section">
            <div class="section-header">
              <h3 class="section-title">Recent Orders</h3>
              <a [routerLink]="['/shop', storeSlug, 'account', 'orders']"
                 class="view-all-link">View All →</a>
            </div>

            <div class="orders-list" *ngIf="recentOrders.length > 0; else noOrdersTemplate">
              <div class="order-card" *ngFor="let order of recentOrders">
                <div class="order-header">
                  <div class="order-number">Order #{{ order.number }}</div>
                  <div class="order-date">{{ formatDate(order.date) }}</div>
                </div>
                <div class="order-content">
                  <div class="order-items">
                    {{ order.itemCount }} item{{ order.itemCount !== 1 ? 's' : '' }} ·
                    <span class="order-total">\${{ order.total.toFixed(2) }}</span>
                  </div>
                  <div class="order-status" [class]="'status-' + order.status.toLowerCase()">
                    {{ order.statusText }}
                  </div>
                </div>
                <div class="order-actions">
                  <a [routerLink]="['/shop', storeSlug, 'account', 'orders', order.id]"
                     class="btn btn-sm btn-outline-primary">
                    View Details
                  </a>
                </div>
              </div>
            </div>

            <ng-template #noOrdersTemplate>
              <div class="empty-state">
                <div class="empty-icon">🛒</div>
                <h4 class="empty-title">No orders yet</h4>
                <p class="empty-message">Start shopping to see your orders here.</p>
                <a [routerLink]="['/shop', storeSlug]"
                   class="btn btn-primary">
                  Start Shopping
                </a>
              </div>
            </ng-template>
          </div>

          <!-- Quick Actions -->
          <div class="quick-actions-section">
            <h3 class="section-title">Quick Actions</h3>
            <div class="actions-grid">
              <a [routerLink]="['/shop', storeSlug]"
                 class="action-card">
                <div class="action-icon">🛍️</div>
                <div class="action-content">
                  <div class="action-title">Continue Shopping</div>
                  <div class="action-description">Browse our latest items</div>
                </div>
              </a>

              <a [routerLink]="['/shop', storeSlug, 'account', 'favorites']"
                 class="action-card">
                <div class="action-icon">💝</div>
                <div class="action-content">
                  <div class="action-title">My Favorites</div>
                  <div class="action-description">View saved items</div>
                </div>
              </a>

              <a [routerLink]="['/shop', storeSlug, 'account', 'settings']"
                 class="action-card">
                <div class="action-icon">⚙️</div>
                <div class="action-content">
                  <div class="action-title">Account Settings</div>
                  <div class="action-description">Update your preferences</div>
                </div>
              </a>

              <a href="#" (click)="contactSupport($event)"
                 class="action-card">
                <div class="action-icon">💬</div>
                <div class="action-content">
                  <div class="action-title">Contact Support</div>
                  <div class="action-description">Get help when you need it</div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .account-dashboard {
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
      margin-bottom: 2rem;
      text-align: center;
    }

    .page-title {
      font-size: 2rem;
      font-weight: bold;
      color: #343a40;
      margin: 0 0 0.5rem 0;
    }

    .page-subtitle {
      color: #6c757d;
      font-size: 1rem;
      margin: 0;
    }

    .account-nav {
      display: flex;
      border-bottom: 1px solid #dee2e6;
      margin-bottom: 2rem;
      gap: 1rem;
      justify-content: center;
    }

    .nav-tab {
      padding: 1rem 1.5rem;
      color: #6c757d;
      text-decoration: none;
      border-bottom: 2px solid transparent;
      transition: all 0.2s;
    }

    .nav-tab:hover,
    .nav-tab.active {
      color: #007bff;
      border-bottom-color: #007bff;
    }

    .dashboard-content {
      display: grid;
      gap: 2rem;
    }

    .welcome-section {
      margin-bottom: 1rem;
    }

    .welcome-card {
      background: white;
      padding: 2rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .welcome-header {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .user-avatar {
      width: 64px;
      height: 64px;
      background: linear-gradient(135deg, #007bff, #0056b3);
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-size: 1.5rem;
      font-weight: bold;
    }

    .user-info {
      flex: 1;
    }

    .user-name {
      margin: 0 0 0.25rem 0;
      color: #343a40;
      font-size: 1.5rem;
    }

    .user-email {
      margin: 0 0 0.25rem 0;
      color: #6c757d;
    }

    .member-since {
      margin: 0;
      color: #adb5bd;
      font-size: 0.875rem;
    }

    .stats-section {
      margin-bottom: 1rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
    }

    .stat-card {
      background: white;
      padding: 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .stat-icon {
      font-size: 2rem;
    }

    .stat-number {
      font-size: 1.75rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 0.25rem;
    }

    .stat-label {
      color: #6c757d;
      font-size: 0.875rem;
    }

    .recent-orders-section {
      background: white;
      padding: 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }

    .section-title {
      margin: 0;
      color: #343a40;
      font-size: 1.25rem;
    }

    .view-all-link {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .view-all-link:hover {
      text-decoration: underline;
    }

    .orders-list {
      display: grid;
      gap: 1rem;
    }

    .order-card {
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      padding: 1rem;
      transition: box-shadow 0.2s;
    }

    .order-card:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .order-number {
      font-weight: 500;
      color: #343a40;
    }

    .order-date {
      color: #6c757d;
      font-size: 0.875rem;
    }

    .order-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .order-items {
      color: #6c757d;
    }

    .order-total {
      font-weight: 500;
      color: #343a40;
    }

    .order-status {
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .status-pending {
      background-color: #fff3cd;
      color: #856404;
    }

    .status-completed {
      background-color: #d1ecf1;
      color: #0c5460;
    }

    .status-ready {
      background-color: #d4edda;
      color: #155724;
    }

    .empty-state {
      text-align: center;
      padding: 3rem 1rem;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .empty-title {
      margin-bottom: 0.5rem;
      color: #343a40;
    }

    .empty-message {
      color: #6c757d;
      margin-bottom: 2rem;
    }

    .quick-actions-section {
      background: white;
      padding: 1.5rem;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .action-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #dee2e6;
      border-radius: 0.375rem;
      text-decoration: none;
      color: inherit;
      transition: all 0.2s;
    }

    .action-card:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      transform: translateY(-1px);
      color: inherit;
      text-decoration: none;
    }

    .action-icon {
      font-size: 1.5rem;
    }

    .action-title {
      font-weight: 500;
      color: #343a40;
      margin-bottom: 0.25rem;
    }

    .action-description {
      color: #6c757d;
      font-size: 0.875rem;
    }

    .btn {
      display: inline-block;
      font-weight: 400;
      text-align: center;
      text-decoration: none;
      vertical-align: middle;
      cursor: pointer;
      border: 1px solid transparent;
      padding: 0.5rem 1rem;
      font-size: 1rem;
      border-radius: 0.375rem;
      transition: all 0.2s;
    }

    .btn-sm {
      padding: 0.25rem 0.5rem;
      font-size: 0.875rem;
    }

    .btn-primary {
      background-color: #007bff;
      border-color: #007bff;
      color: white;
    }

    .btn-primary:hover {
      background-color: #0056b3;
      border-color: #004085;
      color: white;
      text-decoration: none;
    }

    .btn-outline-primary {
      color: #007bff;
      border-color: #007bff;
      background-color: transparent;
    }

    .btn-outline-primary:hover {
      background-color: #007bff;
      border-color: #007bff;
      color: white;
      text-decoration: none;
    }

    @media (max-width: 768px) {
      .account-nav {
        justify-content: flex-start;
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
      }

      .nav-tab {
        flex-shrink: 0;
      }

      .welcome-card {
        flex-direction: column;
        text-align: center;
        gap: 1.5rem;
      }

      .welcome-header {
        flex-direction: column;
        text-align: center;
      }

      .stats-grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .actions-grid {
        grid-template-columns: 1fr;
      }

      .order-header,
      .order-content {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }

      .section-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }
    }
  `]
})
export class AccountDashboardComponent implements OnInit, OnDestroy {
  storeSlug = '';
  storeInfo: StoreInfoDto | null = null;
  shopperProfile: ShopperProfileDto | null = null;

  // Mock data for Phase 1 - will be replaced with real data in Phase 2
  orderStats = {
    total: 0,
    pending: 0,
    completed: 0
  };

  favoriteCount = 0;

  recentOrders: any[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private shopperAuthService: ShopperAuthService,
    private storeService: ShopperStoreService
  ) {}

  ngOnInit(): void {
    // Get store slug from route
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
    });

    // Get store info
    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    // Get shopper profile
    this.shopperAuthService.currentProfile$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(profile => {
      this.shopperProfile = profile;
    });

    // Load profile if not already loaded
    if (!this.shopperProfile && this.storeSlug) {
      this.loadProfile();
    }

    // TODO: Load actual order data and favorites in Phase 2
    this.loadMockData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getInitials(): string {
    if (!this.shopperProfile?.fullName) {
      return '?';
    }

    const names = this.shopperProfile.fullName.split(' ');
    const initials = names.map(name => name.charAt(0)).join('');
    return initials.substring(0, 2).toUpperCase();
  }

  formatDate(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  contactSupport(event: Event): void {
    event.preventDefault();
    // TODO: Implement contact support functionality
    alert('Contact support functionality will be implemented in a future update.');
  }

  private loadProfile(): void {
    this.shopperAuthService.getProfile(this.storeSlug).subscribe({
      next: (profile) => {
        this.shopperProfile = profile;
      },
      error: (error) => {
        console.error('Failed to load shopper profile:', error);
      }
    });
  }

  private loadMockData(): void {
    // Mock data for demonstration - will be replaced with real API calls in Phase 2
    this.orderStats = {
      total: 0,
      pending: 0,
      completed: 0
    };

    this.favoriteCount = 0;

    this.recentOrders = [];

    // Example of what real orders might look like:
    /*
    this.recentOrders = [
      {
        id: '1234',
        number: '1234',
        date: new Date('2024-01-15'),
        itemCount: 2,
        total: 73.00,
        status: 'ready',
        statusText: 'Ready for Pickup'
      },
      {
        id: '1198',
        number: '1198',
        date: new Date('2024-01-10'),
        itemCount: 1,
        total: 45.00,
        status: 'completed',
        statusText: 'Completed'
      }
    ];
    */
  }
}