import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginRequest, RegisterRequest, AuthResponse, User } from '../models/auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:5000/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should perform login and set auth data', () => {
      const loginRequest: LoginRequest = { email: 'test@test.com', password: 'password' };
      const mockResponse: AuthResponse = {
        token: 'mock-token',
        refreshToken: 'mock-refresh-token',
        user: { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User,
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      service.login(loginRequest).subscribe(response => {
        expect(response).toEqual(mockResponse);
        expect(service.isLoggedIn()).toBeTruthy();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginRequest);
      req.flush(mockResponse);

      expect(localStorage.getItem('token')).toBe('mock-token');
      expect(localStorage.getItem('refreshToken')).toBe('mock-refresh-token');
    });
  });

  describe('register', () => {
    it('should perform registration and set auth data', () => {
      const registerRequest: RegisterRequest = {
        email: 'test@test.com',
        password: 'password',
        businessName: 'Test Business',
        ownerName: 'Test User'
      };
      const mockResponse: AuthResponse = {
        token: 'mock-token',
        refreshToken: 'mock-refresh-token',
        user: { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User,
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      service.register(registerRequest).subscribe(response => {
        expect(response).toEqual(mockResponse);
        expect(service.isLoggedIn()).toBeTruthy();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(registerRequest);
      req.flush(mockResponse);
    });
  });

  describe('registerOwner', () => {
    it('should register owner successfully', async () => {
      const ownerRequest = {
        fullName: 'Shop Owner',
        email: 'owner@test.com',
        password: 'password123',
        shopName: 'Test Shop',
        phone: '1234567890'
      };
      const mockResponse = { success: true, message: 'Owner registered successfully' };

      const resultPromise = service.registerOwner(ownerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/owner`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(ownerRequest);
      req.flush(mockResponse);

      const result = await resultPromise;
      expect(result).toEqual(mockResponse);
    });

    it('should handle owner registration error', async () => {
      const ownerRequest = {
        fullName: 'Shop Owner',
        email: 'owner@test.com',
        password: 'password123',
        shopName: 'Test Shop'
      };
      const mockError = {
        error: {
          message: 'Email already exists',
          errors: ['Email is already in use']
        }
      };

      const resultPromise = service.registerOwner(ownerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/owner`);
      req.flush(mockError, { status: 400, statusText: 'Bad Request' });

      const result = await resultPromise;
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Email already exists');
      expect(result.errors).toEqual(['Email is already in use']);
    });

    it('should handle network error in owner registration', async () => {
      const ownerRequest = {
        fullName: 'Shop Owner',
        email: 'owner@test.com',
        password: 'password123',
        shopName: 'Test Shop'
      };

      const resultPromise = service.registerOwner(ownerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/owner`);
      req.error(new ErrorEvent('Network error'));

      const result = await resultPromise;
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Registration failed');
    });
  });

  describe('registerProvider', () => {
    it('should register provider successfully', async () => {
      const providerRequest = {
        storeCode: '1234',
        fullName: 'Provider Name',
        email: 'provider@test.com',
        password: 'password123',
        phone: '1234567890',
        preferredPaymentMethod: 'Venmo',
        paymentDetails: '@provider'
      };
      const mockResponse = { success: true, message: 'Provider registered successfully' };

      const resultPromise = service.registerProvider(providerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/provider`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(providerRequest);
      req.flush(mockResponse);

      const result = await resultPromise;
      expect(result).toEqual(mockResponse);
    });

    it('should handle provider registration error', async () => {
      const providerRequest = {
        storeCode: '9999',
        fullName: 'Provider Name',
        email: 'provider@test.com',
        password: 'password123'
      };
      const mockError = {
        error: {
          message: 'Invalid store code',
          errors: ['Store code not found']
        }
      };

      const resultPromise = service.registerProvider(providerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/provider`);
      req.flush(mockError, { status: 400, statusText: 'Bad Request' });

      const result = await resultPromise;
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Invalid store code');
      expect(result.errors).toEqual(['Store code not found']);
    });

    it('should handle optional fields correctly', async () => {
      const providerRequest = {
        storeCode: '1234',
        fullName: 'Provider Name',
        email: 'provider@test.com',
        password: 'password123'
      };
      const mockResponse = { success: true, message: 'Provider registered successfully' };

      const resultPromise = service.registerProvider(providerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/provider`);
      expect(req.request.body).toEqual(providerRequest);
      req.flush(mockResponse);

      const result = await resultPromise;
      expect(result.success).toBeTruthy();
    });
  });

  describe('validateStoreCode', () => {
    it('should validate store code successfully', async () => {
      const storeCode = '1234';
      const mockResponse = {
        isValid: true,
        shopName: 'Test Shop'
      };

      const resultPromise = service.validateStoreCode(storeCode);

      const req = httpMock.expectOne(`${apiUrl}/auth/validate-store-code/${storeCode}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);

      const result = await resultPromise;
      expect(result).toEqual(mockResponse);
    });

    it('should handle invalid store code', async () => {
      const storeCode = '9999';
      const mockResponse = {
        isValid: false,
        errorMessage: 'Invalid store code'
      };

      const resultPromise = service.validateStoreCode(storeCode);

      const req = httpMock.expectOne(`${apiUrl}/auth/validate-store-code/${storeCode}`);
      req.flush(mockResponse);

      const result = await resultPromise;
      expect(result.isValid).toBeFalse();
      expect(result.errorMessage).toBe('Invalid store code');
    });

    it('should handle validation error', async () => {
      const storeCode = '1234';

      const resultPromise = service.validateStoreCode(storeCode);

      const req = httpMock.expectOne(`${apiUrl}/auth/validate-store-code/${storeCode}`);
      req.error(new ErrorEvent('Network error'));

      const result = await resultPromise;
      expect(result.isValid).toBeFalse();
      expect(result.errorMessage).toBe('Unable to validate store code');
    });
  });

  describe('logout', () => {
    it('should clear all stored data and reset state', () => {
      // Set up some stored data
      localStorage.setItem('auth_token', 'token');
      localStorage.setItem('refreshToken', 'refresh');
      localStorage.setItem('user_data', JSON.stringify({ id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' }));
      localStorage.setItem('tokenExpiry', new Date().toISOString());
      service['currentUserSubject'].next({ id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User);
      service.isLoggedIn.set(true);

      service.logout();

      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(localStorage.getItem('user_data')).toBeNull();
      expect(localStorage.getItem('tokenExpiry')).toBeNull();
      expect(service.getCurrentUser()).toBeNull();
      expect(service.isLoggedIn()).toBeFalse();
    });
  });

  describe('refreshToken', () => {
    it('should refresh token and set auth data', () => {
      localStorage.setItem('refreshToken', 'old-refresh-token');

      const mockResponse: AuthResponse = {
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        user: { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User,
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      service.refreshToken().subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'old-refresh-token' });
      req.flush(mockResponse);
    });
  });

  describe('getToken', () => {
    it('should return stored token', () => {
      localStorage.setItem('auth_token', 'test-token');
      expect(service.getToken()).toBe('test-token');
    });

    it('should return null if no token stored', () => {
      expect(service.getToken()).toBeNull();
    });
  });

  describe('getCurrentUser', () => {
    it('should return current user', () => {
      const user = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User;
      service['currentUserSubject'].next(user);
      expect(service.getCurrentUser()).toEqual(user);
    });
  });

  describe('isTokenExpired', () => {
    it('should return true if no token info', () => {
      expect(service.isTokenExpired()).toBeTruthy();
    });

    it('should return true if token is expired', () => {
      const pastDate = new Date(Date.now() - 3600000);
      service['tokenInfo'].set({ token: 'token', expiresAt: pastDate });
      expect(service.isTokenExpired()).toBeTruthy();
    });

    it('should return false if token is not expired', () => {
      const futureDate = new Date(Date.now() + 3600000);
      service['tokenInfo'].set({ token: 'token', expiresAt: futureDate });
      expect(service.isTokenExpired()).toBeFalsy();
    });
  });

  describe('loadStoredAuth', () => {
    it('should load valid stored auth data', () => {
      const user = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' };
      const expiry = new Date(Date.now() + 3600000);

      localStorage.setItem('auth_token', 'stored-token');
      localStorage.setItem('user_data', JSON.stringify(user));
      localStorage.setItem('tokenExpiry', expiry.toISOString());

      service.loadStoredAuth();

      expect(service.getCurrentUser()).toEqual(user);
      expect(service.isLoggedIn()).toBeTruthy();
    });

    it('should logout if token is expired', () => {
      const user = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' };
      const pastExpiry = new Date(Date.now() - 3600000);

      localStorage.setItem('auth_token', 'expired-token');
      localStorage.setItem('user_data', JSON.stringify(user));
      localStorage.setItem('tokenExpiry', pastExpiry.toISOString());

      spyOn(service, 'logout').and.callThrough();
      service.loadStoredAuth();

      expect(service.logout).toHaveBeenCalled();
      expect(service.isLoggedIn()).toBeFalsy();
    });

    it('should do nothing if no stored data', () => {
      service.loadStoredAuth();

      expect(service.getCurrentUser()).toBeNull();
      expect(service.isLoggedIn()).toBeFalsy();
    });

    it('should do nothing if incomplete stored data', () => {
      localStorage.setItem('auth_token', 'token');
      // Missing user_data and tokenExpiry

      service.loadStoredAuth();

      expect(service.getCurrentUser()).toBeNull();
      expect(service.isLoggedIn()).toBeFalsy();
    });
  });

  describe('setAuthData', () => {
    it('should set auth data correctly', () => {
      const authResponse: AuthResponse = {
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        user: { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User,
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      // Call setAuthData through login to test the private method
      service.login({ email: 'test@test.com', password: 'password' }).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush(authResponse);

      expect(localStorage.getItem('token')).toBe('new-token');
      expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
      expect(localStorage.getItem('user')).toBe(JSON.stringify(authResponse.user));
      expect(localStorage.getItem('tokenExpiry')).toBe(authResponse.expiresAt);
      expect(service.getCurrentUser()).toEqual(authResponse.user);
      expect(service.isLoggedIn()).toBeTruthy();
    });
  });

  describe('observable streams', () => {
    it('should emit current user changes', () => {
      const user = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' } as User;
      let emittedUser: User | null = null;

      service.currentUser$.subscribe(u => emittedUser = u);
      service['currentUserSubject'].next(user);

      expect(emittedUser).toEqual(user);
    });
  });

  describe('error handling', () => {
    it('should handle malformed error responses in registerOwner', async () => {
      const ownerRequest = {
        fullName: 'Shop Owner',
        email: 'owner@test.com',
        password: 'password123',
        shopName: 'Test Shop'
      };

      const resultPromise = service.registerOwner(ownerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/owner`);
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

      const result = await resultPromise;
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Registration failed');
    });

    it('should handle malformed error responses in registerProvider', async () => {
      const providerRequest = {
        storeCode: '1234',
        fullName: 'Provider Name',
        email: 'provider@test.com',
        password: 'password123'
      };

      const resultPromise = service.registerProvider(providerRequest);

      const req = httpMock.expectOne(`${apiUrl}/auth/register/provider`);
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

      const result = await resultPromise;
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Registration failed');
    });
  });
});