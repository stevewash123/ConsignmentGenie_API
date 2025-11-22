import { Routes } from '@angular/router';
import { AdminGuard } from './guards/admin.guard';
import { OwnerGuard } from './guards/owner.guard';
import { ProviderGuard } from './guards/provider.guard';
import { CustomerGuard } from './guards/customer.guard';

export const routes: Routes = [
  // Public storefront routes (no auth required)
  {
    path: 'store/:orgSlug',
    loadChildren: () => import('./public-store/public-store.routes').then(m => m.publicStoreRoutes)
  },

  // System admin routes (admin role only)
  {
    path: 'admin',
    canActivate: [AdminGuard],
    loadChildren: () => import('./admin/admin.routes').then(m => m.adminRoutes)
  },

  // Owner area routes (owner/manager/staff roles)
  {
    path: 'owner',
    canActivate: [OwnerGuard],
    loadChildren: () => import('./owner/owner.routes').then(m => m.ownerRoutes)
  },

  // Provider area routes (provider role + owners/managers)
  {
    path: 'provider',
    canActivate: [ProviderGuard],
    loadChildren: () => import('./provider/provider.routes').then(m => m.providerRoutes)
  },

  // Customer area routes (customer/provider roles)
  {
    path: 'customer',
    canActivate: [CustomerGuard],
    loadChildren: () => import('./customer/customer.routes').then(m => m.customerRoutes)
  },

  // Unified login route (no auth required)
  {
    path: 'login',
    loadComponent: () => import('./auth/login.component').then(m => m.LoginComponent)
  },

  // Unauthorized access route
  {
    path: 'unauthorized',
    loadComponent: () => import('./auth/unauthorized.component').then(m => m.UnauthorizedComponent)
  },

  // Default redirects
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },

  // Catch-all route
  {
    path: '**',
    redirectTo: '/login'
  }
];
