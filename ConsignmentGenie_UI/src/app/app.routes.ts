import { Routes } from '@angular/router';
import { AdminGuard } from './guards/admin.guard';
import { OwnerGuard } from './guards/owner.guard';
import { ProviderGuard } from './guards/provider.guard';
import { CustomerGuard } from './guards/customer.guard';

export const routes: Routes = [
  // Shopper storefront routes (/shop/{storeSlug}) - Phase 1
  {
    path: 'shop/:storeSlug',
    loadChildren: () => import('./shopper/shopper.routes').then(m => m.shopperRoutes)
  },

  // Public storefront routes (fallback when Square/Shopify unavailable)
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

  // Authentication routes (no auth required)
  {
    path: 'login',
    loadComponent: () => import('./auth/login-simple.component').then(m => m.LoginSimpleComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'register/owner',
    loadComponent: () => import('./auth/register-owner.component').then(m => m.RegisterOwnerComponent)
  },
  {
    path: 'register/provider',
    loadComponent: () => import('./auth/register-provider.component').then(m => m.RegisterProviderComponent)
  },
  {
    path: 'register/success',
    loadComponent: () => import('./auth/register-success.component').then(m => m.RegisterSuccessComponent)
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
