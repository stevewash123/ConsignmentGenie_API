import { Routes } from '@angular/router';

export const providerRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/provider-dashboard.component').then(m => m.ProviderDashboardComponent)
  },
  {
    path: 'items',
    loadComponent: () => import('./components/provider-items.component').then(m => m.ProviderItemsComponent)
  },
  {
    path: 'items/:id',
    loadComponent: () => import('./components/provider-item-detail.component').then(m => m.ProviderItemDetailComponent)
  },
  {
    path: 'sales',
    loadComponent: () => import('./components/provider-sales.component').then(m => m.ProviderSalesComponent)
  },
  {
    path: 'payouts',
    loadComponent: () => import('./components/provider-payouts.component').then(m => m.ProviderPayoutsComponent)
  },
  {
    path: 'payouts/:id',
    loadComponent: () => import('./components/provider-payout-detail.component').then(m => m.ProviderPayoutDetailComponent)
  },
  {
    path: 'profile',
    loadComponent: () => import('./components/provider-profile.component').then(m => m.ProviderProfileComponent)
  }
];