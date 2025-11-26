import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/admin-dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'owner-approvals',
    loadComponent: () => import('./components/owner-approval.component').then(m => m.OwnerApprovalComponent)
  }
  // Note: Login is now handled by /login route in main app routing
];