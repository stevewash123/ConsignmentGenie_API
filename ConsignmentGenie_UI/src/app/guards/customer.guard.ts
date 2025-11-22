import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { UserRole } from './auth.guard';

@Injectable({
  providedIn: 'root'
})
export class CustomerGuard implements CanActivate {
  constructor(private router: Router) {}

  canActivate(): boolean {
    const token = localStorage.getItem('auth_token');
    const userDataStr = localStorage.getItem('user_data');

    if (!token || !userDataStr) {
      this.router.navigate(['/login']);
      return false;
    }

    try {
      const userData = JSON.parse(userDataStr);

      // Allow Customer and Provider roles access to customer area
      // Providers often need customer-style access to see public views
      const allowedRoles = [
        UserRole.Customer,
        UserRole.Provider
      ];

      if (allowedRoles.includes(userData.role)) {
        return true;
      }

      // Redirect non-customer users to their appropriate dashboard
      this.redirectToUserDashboard(userData.role);
      return false;
    } catch (error) {
      console.error('Invalid user data in localStorage');
      this.router.navigate(['/login']);
      return false;
    }
  }

  private redirectToUserDashboard(userRole: number) {
    switch (userRole) {
      case UserRole.Owner:
      case UserRole.Manager:
      case UserRole.Staff:
      case UserRole.Cashier:
      case UserRole.Accountant:
        this.router.navigate(['/owner/dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
    }
  }
}