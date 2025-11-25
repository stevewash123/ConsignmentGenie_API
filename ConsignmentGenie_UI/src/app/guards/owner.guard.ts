import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { UserRole } from './auth.guard';

@Injectable({
  providedIn: 'root'
})
export class OwnerGuard implements CanActivate {
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

      // Allow Owner and Admin roles access to owner area
      const allowedRoles = [
        UserRole.Owner,
        UserRole.Admin
      ];

      // Ensure role comparison works by converting to number
      const userRoleNum = Number(userData.role);

      if (allowedRoles.includes(userRoleNum)) {
        return true;
      }

      // Redirect non-owner users to their appropriate dashboard
      this.redirectToUserDashboard(userRoleNum);
      return false;
    } catch (error) {
      console.error('Invalid user data in localStorage');
      this.router.navigate(['/login']);
      return false;
    }
  }

  private redirectToUserDashboard(userRole: number) {
    switch (userRole) {
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