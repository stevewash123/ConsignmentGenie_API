import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

interface UserData {
  userId: string;
  email: string;
  role: number;
  organizationId: string;
  organizationName: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header class="app-header">
      <div class="header-container">
        <div class="header-left">
          <div class="logo">
            <h1>ConsignmentGenie</h1>
          </div>

          <nav class="main-nav" *ngIf="currentUser()">
            <!-- Admin Navigation -->
            <div *ngIf="isAdmin()" class="nav-links">
              <a routerLink="/admin/dashboard" routerLinkActive="active">Dashboard</a>
              <a routerLink="/admin/organizations" routerLinkActive="active">Organizations</a>
              <a routerLink="/admin/billing" routerLinkActive="active">Billing</a>
              <a routerLink="/admin/support" routerLinkActive="active">Support</a>
            </div>

            <!-- Owner Navigation -->
            <div *ngIf="isOwner()" class="nav-links">
              <a routerLink="/owner/dashboard" routerLinkActive="active">Dashboard</a>
              <a routerLink="/owner/providers" routerLinkActive="active">Providers</a>
              <a routerLink="/owner/inventory" routerLinkActive="active">Inventory</a>
              <a routerLink="/owner/transactions" routerLinkActive="active">Transactions</a>
              <a routerLink="/owner/payouts" routerLinkActive="active">Payouts</a>
              <a routerLink="/owner/reports" routerLinkActive="active">Reports</a>
              <a routerLink="/owner/settings" routerLinkActive="active">Settings</a>
            </div>

            <!-- Customer/Provider Navigation -->
            <div *ngIf="isCustomerOrProvider()" class="nav-links">
              <a routerLink="/customer/dashboard" routerLinkActive="active">Dashboard</a>
              <a routerLink="/customer/items" routerLinkActive="active">My Items</a>
              <a routerLink="/customer/transactions" routerLinkActive="active">Transactions</a>
              <a routerLink="/customer/profile" routerLinkActive="active">Profile</a>
            </div>
          </nav>
        </div>

        <div class="header-right" *ngIf="currentUser()">
          <div class="user-info">
            <div class="user-details">
              <div class="user-email">{{ currentUser()?.email }}</div>
              <div class="user-role">{{ getRoleName(currentUser()?.role) }}</div>
              <div class="org-name">{{ currentUser()?.organizationName }}</div>
            </div>
          </div>

          <div class="user-menu">
            <button class="profile-btn" (click)="toggleUserMenu()">
              <span class="user-avatar">{{ getInitials(currentUser()?.email) }}</span>
              <span class="dropdown-arrow">▼</span>
            </button>

            <div class="user-dropdown" [class.show]="showUserMenu()">
              <a routerLink="/profile" class="dropdown-item">Profile</a>
              <a routerLink="/settings" class="dropdown-item">Settings</a>
              <hr>
              <button class="dropdown-item logout-btn" (click)="logout()">Logout</button>
            </div>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .app-header {
      background: #1f2937;
      color: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      position: sticky;
      top: 0;
      z-index: 1000;
    }

    .header-container {
      max-width: 1400px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 2rem;
      height: 64px;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 2rem;
    }

    .logo h1 {
      margin: 0;
      font-size: 1.5rem;
      font-weight: bold;
      color: #60a5fa;
    }

    .main-nav {
      display: flex;
      align-items: center;
    }

    .nav-links {
      display: flex;
      gap: 1.5rem;
    }

    .nav-links a {
      color: #d1d5db;
      text-decoration: none;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .nav-links a:hover {
      color: white;
      background: rgba(255,255,255,0.1);
    }

    .nav-links a.active {
      color: #60a5fa;
      background: rgba(96, 165, 250, 0.1);
    }

    .header-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .user-info {
      text-align: right;
    }

    .user-details {
      font-size: 0.875rem;
    }

    .user-email {
      font-weight: 600;
      color: white;
    }

    .user-role {
      color: #9ca3af;
      font-size: 0.75rem;
      text-transform: uppercase;
    }

    .org-name {
      color: #60a5fa;
      font-size: 0.75rem;
    }

    .user-menu {
      position: relative;
    }

    .profile-btn {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      background: none;
      border: none;
      color: white;
      cursor: pointer;
      padding: 0.5rem;
      border-radius: 8px;
      transition: background 0.2s;
    }

    .profile-btn:hover {
      background: rgba(255,255,255,0.1);
    }

    .user-avatar {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: #4f46e5;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .dropdown-arrow {
      font-size: 0.75rem;
      transition: transform 0.2s;
    }

    .profile-btn:hover .dropdown-arrow {
      transform: rotate(180deg);
    }

    .user-dropdown {
      position: absolute;
      top: 100%;
      right: 0;
      background: white;
      color: #1f2937;
      border-radius: 8px;
      box-shadow: 0 10px 25px -5px rgba(0,0,0,0.1);
      min-width: 200px;
      opacity: 0;
      visibility: hidden;
      transform: translateY(-10px);
      transition: all 0.2s;
      margin-top: 0.5rem;
    }

    .user-dropdown.show {
      opacity: 1;
      visibility: visible;
      transform: translateY(0);
    }

    .dropdown-item {
      display: block;
      width: 100%;
      padding: 0.75rem 1rem;
      text-decoration: none;
      color: #374151;
      border: none;
      background: none;
      text-align: left;
      cursor: pointer;
      font-size: 0.875rem;
      transition: background 0.2s;
    }

    .dropdown-item:hover {
      background: #f3f4f6;
    }

    .dropdown-item:first-child {
      border-radius: 8px 8px 0 0;
    }

    .dropdown-item:last-child {
      border-radius: 0 0 8px 8px;
    }

    .logout-btn {
      color: #dc2626 !important;
      font-weight: 600;
    }

    .logout-btn:hover {
      background: #fee2e2 !important;
    }

    hr {
      margin: 0;
      border: none;
      border-top: 1px solid #e5e7eb;
    }

    @media (max-width: 768px) {
      .header-container {
        padding: 0 1rem;
      }

      .nav-links {
        display: none;
      }

      .user-details {
        display: none;
      }
    }
  `]
})
export class AppHeaderComponent implements OnInit {
  currentUser = signal<UserData | null>(null);
  showUserMenu = signal(false);

  constructor(private router: Router) {}

  ngOnInit() {
    this.loadUserData();
  }

  private loadUserData() {
    const userData = localStorage.getItem('user_data');
    if (userData) {
      this.currentUser.set(JSON.parse(userData));
    }
  }

  isAdmin(): boolean {
    return this.currentUser()?.role === 1; // Owner role can act as admin
  }

  isOwner(): boolean {
    return [1, 2, 3, 4, 5].includes(this.currentUser()?.role || 0); // Owner, Manager, Staff, Cashier, Accountant
  }

  isCustomerOrProvider(): boolean {
    return [6, 7].includes(this.currentUser()?.role || 0); // Provider, Customer
  }

  getRoleName(role?: number): string {
    const roleMap: { [key: number]: string } = {
      1: 'Owner',
      2: 'Manager',
      3: 'Staff',
      4: 'Cashier',
      5: 'Accountant',
      6: 'Provider',
      7: 'Customer'
    };
    return roleMap[role || 0] || 'Unknown';
  }

  getInitials(email?: string): string {
    if (!email) return '?';
    return email.substring(0, 2).toUpperCase();
  }

  toggleUserMenu() {
    this.showUserMenu.update(show => !show);
  }

  logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_data');
    this.router.navigate(['/login']);
  }
}