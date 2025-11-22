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
    path: 'sales',
    loadComponent: () => import('./components/owner-sales.component').then(m => m.OwnerSalesComponent)
  },
  {
    path: 'settings',
    loadComponent: () => import('./components/owner-settings.component').then(m => m.OwnerSettingsComponent)
  }
  // Note: Login is now handled by /login route in main app routing
];