import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient, HTTP_INTERCEPTORS, HttpErrorResponse } from '@angular/common/http';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { of, throwError } from 'rxjs';

describe('AuthInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;
  let interceptor: AuthInterceptor;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', [
      'getToken',
      'isTokenExpired',
      'refreshToken',
      'logout'
    ]);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthInterceptor,
        { provide: AuthService, useValue: authServiceSpy },
        {
          provide: HTTP_INTERCEPTORS,
          useClass: AuthInterceptor,
          multi: true
        }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    interceptor = TestBed.inject(AuthInterceptor);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should add Authorization header when valid token exists', () => {
    authService.getToken.and.returnValue('valid-token');
    authService.isTokenExpired.and.returnValue(false);

    httpClient.get('/api/test').subscribe();

    const req = httpTestingController.expectOne('/api/test');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-token');
    req.flush({ data: 'test' });
  });

  it('should not add Authorization header when no token exists', () => {
    authService.getToken.and.returnValue(null);

    httpClient.get('/api/test').subscribe();

    const req = httpTestingController.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({ data: 'test' });
  });

  it('should not add Authorization header when token is expired', () => {
    authService.getToken.and.returnValue('expired-token');
    authService.isTokenExpired.and.returnValue(true);

    httpClient.get('/api/test').subscribe();

    const req = httpTestingController.expectOne('/api/test');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({ data: 'test' });
  });

  it('should handle 401 error by attempting token refresh', () => {
    const refreshResponse = {
      token: 'new-token',
      refreshToken: 'new-refresh',
      user: { id: 1, email: 'test@example.com' },
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString()
    };

    authService.getToken.and.returnValue('old-token');
    authService.isTokenExpired.and.returnValue(false);
    authService.refreshToken.and.returnValue(of(refreshResponse));

    let response: any;
    let error: any;

    httpClient.get('/api/test').subscribe({
      next: (res) => response = res,
      error: (err) => error = err
    });

    // First request with old token gets 401
    const firstReq = httpTestingController.expectOne('/api/test');
    expect(firstReq.request.headers.get('Authorization')).toBe('Bearer old-token');
    firstReq.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    // Second request with new token should succeed
    const secondReq = httpTestingController.expectOne('/api/test');
    expect(secondReq.request.headers.get('Authorization')).toBe('Bearer new-token');
    secondReq.flush({ data: 'success' });

    expect(response).toEqual({ data: 'success' });
    expect(error).toBeUndefined();
  });

  it('should logout user when token refresh fails', () => {
    authService.getToken.and.returnValue('old-token');
    authService.isTokenExpired.and.returnValue(false);
    authService.refreshToken.and.returnValue(throwError(() => new Error('Refresh failed')));

    let error: any;

    httpClient.get('/api/test').subscribe({
      error: (err) => error = err
    });

    // First request with old token gets 401
    const firstReq = httpTestingController.expectOne('/api/test');
    firstReq.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalled();
    expect(error).toBeTruthy();
  });

  it('should pass through non-401 errors without attempting refresh', () => {
    authService.getToken.and.returnValue('valid-token');
    authService.isTokenExpired.and.returnValue(false);

    let error: HttpErrorResponse;

    httpClient.get('/api/test').subscribe({
      error: (err) => error = err
    });

    const req = httpTestingController.expectOne('/api/test');
    req.flush({ error: 'Server Error' }, { status: 500, statusText: 'Internal Server Error' });

    expect(error.status).toBe(500);
    expect(authService.refreshToken).not.toHaveBeenCalled();
    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('should handle 401 error when no token exists', () => {
    authService.getToken.and.returnValue(null);

    let error: HttpErrorResponse;

    httpClient.get('/api/test').subscribe({
      error: (err) => error = err
    });

    const req = httpTestingController.expectOne('/api/test');
    req.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(error.status).toBe(401);
    expect(authService.refreshToken).not.toHaveBeenCalled();
  });
});