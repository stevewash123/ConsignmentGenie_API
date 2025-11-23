import { Routes } from '@angular/router';

export const providerRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/provider-layout.component').then(m => m.ProviderLayoutComponent),
    children: [
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
        path: 'statements',
        loadComponent: () => import('./components/provider-statements.component').then(m => m.ProviderStatementsComponent)
      },
      {
        path: 'statements/:id',
        loadComponent: () => import('./components/provider-statement-detail.component').then(m => m.ProviderStatementDetailComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/provider-notifications.component').then(m => m.ProviderNotificationsComponent)
      },
      {
        path: 'notifications/preferences',
        loadComponent: () => import('./components/provider-notification-preferences.component').then(m => m.ProviderNotificationPreferencesComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/provider-profile.component').then(m => m.ProviderProfileComponent)
      }
    ]
  }
];