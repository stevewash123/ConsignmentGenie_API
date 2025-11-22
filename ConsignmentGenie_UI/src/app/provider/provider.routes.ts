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
  }
];