import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { ShopperAuthService } from '../services/shopper-auth.service';

export const ShopperGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state) => {
  const shopperAuthService = inject(ShopperAuthService);
  const router = inject(Router);

  // Get store slug from route parameters
  let storeSlug = route.paramMap.get('storeSlug');

  // If not in current route, check parent routes
  if (!storeSlug) {
    let parent = route.parent;
    while (parent && !storeSlug) {
      storeSlug = parent.paramMap.get('storeSlug');
      parent = parent.parent;
    }
  }

  if (!storeSlug) {
    console.error('No store slug found in route');
    router.navigate(['/']);
    return false;
  }

  // Check if shopper is authenticated for this store
  if (shopperAuthService.isAuthenticated(storeSlug)) {
    return true;
  }

  // Redirect to login with return URL
  router.navigate(['/shop', storeSlug, 'login'], {
    queryParams: { returnUrl: state.url }
  });
  return false;
};