import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';

export enum UserRole {
  Admin = 0,
  Owner = 1,
  Provider = 2,
  Customer = 3
}

export interface UserData {
  userId: string;
  email: string;
  role: number;
  organizationId: string;
  organizationName: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const token = localStorage.getItem('auth_token');
    const userDataStr = localStorage.getItem('user_data');

    // Check if user is authenticated
    if (!token || !userDataStr) {
      this.router.navigate(['/login']);
      return false;
    }

    let userData: UserData;
    try {
      userData = JSON.parse(userDataStr);

      // Validate that userData has required properties
      if (!userData.userId || !userData.email || userData.role === undefined || !userData.organizationId) {
        console.error('User data missing required properties:', userData);
        localStorage.removeItem('auth_token');
        localStorage.removeItem('user_data');
        localStorage.removeItem('tokenExpiry');
        this.router.navigate(['/login']);
        return false;
      }
    } catch (error) {
      console.error('Invalid user data in localStorage:', error, 'Raw data:', userDataStr);
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user_data');
      localStorage.removeItem('tokenExpiry');
      this.router.navigate(['/login']);
      return false;
    }

    // Get required roles from route data
    const allowedRoles = route.data?.['roles'] as number[] | undefined;

    if (!allowedRoles || allowedRoles.length === 0) {
      // No role restriction, just need to be authenticated
      return true;
    }

    // Check if user has required role
    if (allowedRoles.includes(userData.role)) {
      return true;
    }

    // User doesn't have required role, redirect to unauthorized
    this.redirectToUnauthorized(userData.role);
    return false;
  }

  private redirectToUnauthorized(userRole: number) {
    // Redirect to appropriate dashboard based on user's actual role
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