import { Routes } from '@angular/router';

export const publicStoreRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/store-layout/store-layout.component').then(m => m.StoreLayoutComponent),
    children: [
      {
        path: '',
        redirectTo: 'products',
        pathMatch: 'full'
      },
      {
        path: 'products',
        loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent)
      }
    ]
  }
];