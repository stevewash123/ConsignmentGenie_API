import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { UserRole } from './auth.guard';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
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

      // Only allow system admin access
      if (userData.role === UserRole.Admin || (userData.role === UserRole.Owner && userData.email === 'admin@microsaasbuilders.com')) {
        return true;
      }

      // Redirect non-admin users to their appropriate dashboard
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
      case UserRole.Owner:
        this.router.navigate(['/owner/dashboard']);
        break;
      case UserRole.Provider:
        this.router.navigate(['/provider/dashboard']);
        break;
      case UserRole.Customer:
        this.router.navigate(['/customer/dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
    }
  }
}