import { Routes } from '@angular/router';

export const customerRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/customer-dashboard.component').then(m => m.CustomerDashboardComponent)
  }
];