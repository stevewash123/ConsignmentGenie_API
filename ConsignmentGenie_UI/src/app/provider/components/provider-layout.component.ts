import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { Subject, takeUntil, interval, startWith } from 'rxjs';
import { ProviderPortalService } from '../services/provider-portal.service';

@Component({
  selector: 'app-provider-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet],
  template: `
    <div class="provider-layout">
      <!-- Header -->
      <header class="provider-header">
        <div class="header-container">
          <!-- Logo/Brand -->
          <div class="brand">
            <h1 routerLink="/provider/dashboard" class="brand-link">Provider Portal</h1>
          </div>

          <!-- Navigation -->
          <nav class="main-nav">
            <a
              routerLink="/provider/dashboard"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{exact: true}"
              class="nav-link">
              Dashboard
            </a>
            <a
              routerLink="/provider/items"
              routerLinkActive="active"
              class="nav-link">
              My Items
            </a>
            <a
              routerLink="/provider/sales"
              routerLinkActive="active"
              class="nav-link">
              Sales
            </a>
            <a
              routerLink="/provider/payouts"
              routerLinkActive="active"
              class="nav-link">
              Payouts
            </a>
            <a
              routerLink="/provider/statements"
              routerLinkActive="active"
              class="nav-link">
              Statements
            </a>
          </nav>

          <!-- User Actions -->
          <div class="user-actions">
            <!-- Notification Bell -->
            <div class="notification-bell" routerLink="/provider/notifications">
              <div class="bell-icon">🔔</div>
              <span
                *ngIf="unreadCount > 0"
                class="notification-badge"
                [attr.data-count]="unreadCount > 99 ? '99+' : unreadCount">
                {{ unreadCount > 99 ? '99+' : unreadCount }}
              </span>
            </div>

            <!-- User Menu -->
            <div class="user-menu" [class.open]="userMenuOpen" (click)="toggleUserMenu()">
              <div class="user-avatar">👤</div>
              <div class="user-dropdown" *ngIf="userMenuOpen">
                <a routerLink="/provider/profile" class="dropdown-item" (click)="closeUserMenu()">
                  My Profile
                </a>
                <a routerLink="/provider/notifications/preferences" class="dropdown-item" (click)="closeUserMenu()">
                  Notification Settings
                </a>
                <div class="dropdown-divider"></div>
                <button class="dropdown-item logout-item" (click)="logout()">
                  Logout
                </button>
              </div>
            </div>
          </div>
        </div>
      </header>

      <!-- Main Content -->
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .provider-layout {
      min-height: 100vh;
      background: #f9fafb;
    }

    .provider-header {
      background: white;
      border-bottom: 1px solid #e5e7eb;
      position: sticky;
      top: 0;
      z-index: 100;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .header-container {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 1rem;
      height: 64px;
    }

    .brand {
      display: flex;
      align-items: center;
    }

    .brand-link {
      font-size: 1.25rem;
      font-weight: 700;
      color: #1f2937;
      text-decoration: none;
      cursor: pointer;
      margin: 0;
    }

    .brand-link:hover {
      color: #007bff;
    }

    .main-nav {
      display: flex;
      align-items: center;
      gap: 2rem;
    }

    .nav-link {
      color: #6b7280;
      text-decoration: none;
      font-weight: 500;
      padding: 0.5rem 0;
      border-bottom: 2px solid transparent;
      transition: all 0.2s ease;
    }

    .nav-link:hover {
      color: #1f2937;
    }

    .nav-link.active {
      color: #007bff;
      border-bottom-color: #007bff;
    }

    .user-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .notification-bell {
      position: relative;
      cursor: pointer;
      padding: 0.5rem;
      border-radius: 50%;
      transition: background-color 0.2s ease;
    }

    .notification-bell:hover {
      background-color: #f3f4f6;
    }

    .bell-icon {
      font-size: 1.5rem;
      color: #6b7280;
    }

    .notification-badge {
      position: absolute;
      top: 0;
      right: 0;
      background-color: #ef4444;
      color: white;
      font-size: 0.75rem;
      font-weight: 600;
      min-width: 1.25rem;
      height: 1.25rem;
      border-radius: 0.625rem;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 0.25rem;
      transform: translate(25%, -25%);
    }

    .user-menu {
      position: relative;
      cursor: pointer;
    }

    .user-avatar {
      width: 2rem;
      height: 2rem;
      background-color: #e5e7eb;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
      color: #6b7280;
      transition: background-color 0.2s ease;
    }

    .user-menu:hover .user-avatar {
      background-color: #d1d5db;
    }

    .user-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
      min-width: 12rem;
      margin-top: 0.5rem;
      z-index: 50;
    }

    .dropdown-item {
      display: block;
      width: 100%;
      padding: 0.75rem 1rem;
      text-align: left;
      color: #374151;
      text-decoration: none;
      background: none;
      border: none;
      cursor: pointer;
      transition: background-color 0.2s ease;
    }

    .dropdown-item:hover {
      background-color: #f9fafb;
    }

    .dropdown-item:first-child {
      border-top-left-radius: 0.5rem;
      border-top-right-radius: 0.5rem;
    }

    .dropdown-item:last-child {
      border-bottom-left-radius: 0.5rem;
      border-bottom-right-radius: 0.5rem;
    }

    .dropdown-divider {
      height: 1px;
      background-color: #e5e7eb;
      margin: 0.25rem 0;
    }

    .logout-item {
      color: #dc2626;
    }

    .logout-item:hover {
      background-color: #fef2f2;
    }

    .main-content {
      flex: 1;
    }

    @media (max-width: 768px) {
      .header-container {
        padding: 0 0.5rem;
      }

      .main-nav {
        display: none; /* Hide on mobile, could implement hamburger menu later */
      }

      .brand-link {
        font-size: 1rem;
      }

      .user-actions {
        gap: 0.5rem;
      }

      .notification-bell {
        padding: 0.25rem;
      }

      .bell-icon {
        font-size: 1.25rem;
      }
    }

    /* Mobile Navigation (could be expanded later) */
    @media (max-width: 768px) {
      .provider-layout.mobile-nav-open .main-nav {
        display: flex;
        flex-direction: column;
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: white;
        border-top: 1px solid #e5e7eb;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
        padding: 1rem;
        gap: 0.5rem;
      }
    }
  `]
})
export class ProviderLayoutComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  unreadCount = 0;
  userMenuOpen = false;

  constructor(private providerService: ProviderPortalService) {}

  ngOnInit() {
    // Load initial unread count
    this.loadUnreadCount();

    // Auto-refresh unread count every 30 seconds
    interval(30000)
      .pipe(
        startWith(0),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.loadUnreadCount());

    // Close user menu when clicking outside
    document.addEventListener('click', this.onDocumentClick.bind(this));
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    document.removeEventListener('click', this.onDocumentClick.bind(this));
  }

  private loadUnreadCount() {
    this.providerService.getUnreadNotificationCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.unreadCount = response.count;
        },
        error: (error) => {
          console.error('Error loading unread notification count:', error);
          // Don't show error to user for this background operation
        }
      });
  }

  toggleUserMenu() {
    this.userMenuOpen = !this.userMenuOpen;
  }

  closeUserMenu() {
    this.userMenuOpen = false;
  }

  private onDocumentClick(event: Event) {
    const target = event.target as Element;
    if (!target.closest('.user-menu')) {
      this.userMenuOpen = false;
    }
  }

  logout() {
    // TODO: Implement proper logout logic with auth service
    localStorage.removeItem('token');
    window.location.href = '/login';
  }
}