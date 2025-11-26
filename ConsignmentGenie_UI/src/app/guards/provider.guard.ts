import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { UserRole } from './auth.guard';

@Injectable({
  providedIn: 'root'
})
export class ProviderGuard implements CanActivate {
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

      // Allow Provider role access to provider area
      // Note: Owners can also access provider areas for their shop's providers
      const allowedRoles = [
        UserRole.Provider,
        UserRole.Owner  // Owners can see provider pages for their shop
      ];

      if (allowedRoles.includes(userData.role)) {
        return true;
      }

      // Redirect non-provider users to their appropriate dashboard
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
      case UserRole.Admin:
        this.router.navigate(['/admin/dashboard']);
        break;
      case UserRole.Customer:
        this.router.navigate(['/customer/dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
    }
  }
}