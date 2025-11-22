import { Routes } from '@angular/router';
import { ShopperGuard } from './guards/shopper.guard';

export const shopperRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/store-layout.component').then(m => m.StoreLayoutComponent),
    children: [
      // Public routes (no auth required)
      {
        path: '',
        redirectTo: 'catalog',
        pathMatch: 'full'
      },
      {
        path: 'catalog',
        loadComponent: () => import('./pages/catalog/catalog.component').then(m => m.CatalogComponent),
        title: 'Shop Catalog'
      },
      {
        path: 'item/:itemId',
        loadComponent: () => import('./pages/item-detail/item-detail.component').then(m => m.ItemDetailComponent),
        title: 'Item Details'
      },
      {
        path: 'category/:category',
        loadComponent: () => import('./pages/catalog/catalog.component').then(m => m.CatalogComponent),
        title: 'Browse Category'
      },
      {
        path: 'search',
        loadComponent: () => import('./pages/catalog/catalog.component').then(m => m.CatalogComponent),
        title: 'Search Results'
      },
      {
        path: 'cart',
        loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent),
        title: 'Shopping Cart'
      },

      // Authentication routes
      {
        path: 'login',
        loadComponent: () => import('./pages/auth/shopper-login.component').then(m => m.ShopperLoginComponent),
        title: 'Sign In'
      },
      {
        path: 'register',
        loadComponent: () => import('./pages/auth/shopper-register.component').then(m => m.ShopperRegisterComponent),
        title: 'Create Account'
      },

      // Checkout routes (can be guest or authenticated)
      {
        path: 'checkout',
        loadComponent: () => import('./pages/checkout/checkout.component').then(m => m.CheckoutComponent),
        title: 'Checkout'
      },
      {
        path: 'checkout/confirm',
        loadComponent: () => import('./pages/checkout/checkout-confirmation.component').then(m => m.CheckoutConfirmationComponent),
        title: 'Order Confirmation'
      },

      // Authenticated shopper routes
      {
        path: 'account',
        canActivate: [ShopperGuard],
        loadComponent: () => import('./pages/account/account-dashboard.component').then(m => m.AccountDashboardComponent),
        title: 'My Account'
      },
      {
        path: 'account/orders',
        canActivate: [ShopperGuard],
        loadComponent: () => import('./pages/account/order-history.component').then(m => m.OrderHistoryComponent),
        title: 'Order History'
      },
      {
        path: 'account/favorites',
        canActivate: [ShopperGuard],
        loadComponent: () => import('./pages/account/favorites.component').then(m => m.FavoritesComponent),
        title: 'My Favorites'
      },
      {
        path: 'account/settings',
        canActivate: [ShopperGuard],
        loadComponent: () => import('./pages/account/account-settings.component').then(m => m.AccountSettingsComponent),
        title: 'Account Settings'
      }
    ]
  }
];