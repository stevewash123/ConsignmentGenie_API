import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('Functional AuthInterceptor called for:', req.url);

  const authService = inject(AuthService);
  const token = authService.getToken();

  console.log('Token from service:', token ? 'exists' : 'null');

  let authReq = req;
  if (token && !authService.isTokenExpired()) {
    authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    console.log('Added Bearer token to request');
  } else {
    console.log('No token or token expired');
  }

  return next(authReq).pipe(
    catchError((error) => {
      console.log('HTTP Error:', error.status, error.message);
      if (error.status === 401 && token) {
        return authService.refreshToken().pipe(
          switchMap((authResponse) => {
            const newRequest = req.clone({
              headers: req.headers.set('Authorization', `Bearer ${authResponse.token}`)
            });
            return next(newRequest);
          }),
          catchError((refreshError) => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};