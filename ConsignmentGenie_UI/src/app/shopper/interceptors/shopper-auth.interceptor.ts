import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ShopperAuthService } from '../services/shopper-auth.service';

@Injectable()
export class ShopperAuthInterceptor implements HttpInterceptor {

  constructor(
    private shopperAuthService: ShopperAuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Only handle requests to shopper endpoints
    if (!req.url.includes('/api/shop/')) {
      return next.handle(req);
    }

    // Extract store slug from URL pattern: /api/shop/{storeSlug}/...
    const shopMatch = req.url.match(/\/api\/shop\/([^\/]+)/);
    if (!shopMatch) {
      return next.handle(req);
    }

    const storeSlug = shopMatch[1];
    const token = this.shopperAuthService.getToken(storeSlug);

    // Clone request and add auth header if token exists
    let authReq = req;
    if (token) {
      authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle 401 Unauthorized errors for authenticated routes
        if (error.status === 401 && token) {
          // Token might be expired, logout and redirect to login
          this.shopperAuthService.logout(storeSlug);
          this.router.navigate(['/shop', storeSlug, 'login'], {
            queryParams: { returnUrl: this.router.url }
          });
        }

        return throwError(() => error);
      })
    );
  }
}