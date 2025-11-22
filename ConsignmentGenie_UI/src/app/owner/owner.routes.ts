import { Routes } from '@angular/router';

export const ownerRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/owner-dashboard.component').then(m => m.OwnerDashboardComponent)
  },
  {
    path: 'providers',
    loadComponent: () => import('../components/provider-list.component').then(m => m.ProviderListComponent)
  },
  {
    path: 'inventory',
    loadComponent: () => import('./components/inventory-list.component').then(m => m.InventoryListComponent)
  },
  {
    path: 'sales',
    loadComponent: () => import('./components/owner-sales.component').then(m => m.OwnerSalesComponent)
  },
  {
    path: 'payouts',
    loadComponent: () => import('./components/owner-payouts.component').then(m => m.OwnerPayoutsComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./components/owner-settings.component').then(m => m.OwnerSettingsComponent)
  }
  // Note: Login is now handled by /login route in main app routing
];