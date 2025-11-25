import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let mockHttpClient: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    // Create HttpClient spy
    mockHttpClient = jasmine.createSpyObj('HttpClient', ['post', 'get']);

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: HttpClient, useValue: mockHttpClient }
      ]
    });
    service = TestBed.inject(AuthService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should login successfully', fakeAsync(() => {
    const loginRequest = { email: 'test@test.com', password: 'password' };
    const mockResponse = {
      token: 'mock-token',
      refreshToken: 'mock-refresh',
      user: { id: 1, email: 'test@test.com', businessName: 'Test', ownerName: 'Test User', organizationId: 1, role: 'Owner' },
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    };

    mockHttpClient.post.and.returnValue(of(mockResponse));

    service.login(loginRequest).subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(service.isLoggedIn()).toBeTruthy();
    });

    tick();

    expect(mockHttpClient.post).toHaveBeenCalledWith('http://localhost:5000/api/auth/login', loginRequest);
  }));

  it('should register successfully', fakeAsync(() => {
    const registerRequest = {
      email: 'test@test.com',
      password: 'password',
      businessName: 'Test Business',
      ownerName: 'Test User'
    };
    const mockResponse = {
      token: 'mock-token',
      refreshToken: 'mock-refresh',
      user: { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test User', organizationId: 1, role: 'Owner' },
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    };

    mockHttpClient.post.and.returnValue(of(mockResponse));

    service.register(registerRequest).subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(service.isLoggedIn()).toBeTruthy();
    });

    tick();

    expect(mockHttpClient.post).toHaveBeenCalledWith('http://localhost:5000/api/auth/register', registerRequest);
  }));

  it('should register owner successfully', fakeAsync(() => {
    const ownerRequest = {
      fullName: 'Shop Owner',
      email: 'owner@test.com',
      password: 'password123',
      shopName: 'Test Shop'
    };
    const mockResponse = { success: true, message: 'Owner registered successfully' };

    mockHttpClient.post.and.returnValue(of(mockResponse));

    service.registerOwner(ownerRequest).subscribe(result => {
      expect(result).toEqual(mockResponse);
    });

    tick();

    expect(mockHttpClient.post).toHaveBeenCalledWith('http://localhost:5000/api/auth/register/owner', ownerRequest);
  }));

  it('should handle owner registration error', fakeAsync(() => {
    const ownerRequest = {
      fullName: 'Shop Owner',
      email: 'owner@test.com',
      password: 'password123',
      shopName: 'Test Shop'
    };

    mockHttpClient.post.and.returnValue(
      throwError(() => ({
        error: {
          message: 'Email already exists',
          errors: ['Email is already in use']
        }
      }))
    );

    service.registerOwner(ownerRequest).subscribe(result => {
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Email already exists');
      expect(result.errors).toEqual(['Email is already in use']);
    });

    tick();
  }));

  it('should register provider successfully', fakeAsync(() => {
    const providerRequest = {
      storeCode: '1234',
      fullName: 'Provider Name',
      email: 'provider@test.com',
      password: 'password123'
    };
    const mockResponse = { success: true, message: 'Provider registered successfully' };

    mockHttpClient.post.and.returnValue(of(mockResponse));

    service.registerProvider(providerRequest).subscribe(result => {
      expect(result).toEqual(mockResponse);
    });

    tick();

    expect(mockHttpClient.post).toHaveBeenCalledWith('http://localhost:5000/api/auth/register/provider', providerRequest);
  }));

  it('should validate store code successfully', fakeAsync(() => {
    const storeCode = '1234';
    const mockResponse = {
      isValid: true,
      shopName: 'Test Shop'
    };

    mockHttpClient.get.and.returnValue(of(mockResponse));

    service.validateStoreCode(storeCode).subscribe(result => {
      expect(result).toEqual(mockResponse);
    });

    tick();

    expect(mockHttpClient.get).toHaveBeenCalledWith('http://localhost:5000/api/auth/validate-store-code/1234');
  }));

  it('should handle invalid store code', fakeAsync(() => {
    const storeCode = '9999';
    const mockResponse = {
      isValid: false,
      errorMessage: 'Invalid store code'
    };

    mockHttpClient.get.and.returnValue(of(mockResponse));

    service.validateStoreCode(storeCode).subscribe(result => {
      expect(result.isValid).toBeFalse();
      expect(result.errorMessage).toBe('Invalid store code');
    });

    tick();
  }));

  it('should logout successfully', () => {
    // ✅ Use correct localStorage keys that match the service
    localStorage.setItem('auth_token', 'test-token');
    localStorage.setItem('user_data', JSON.stringify({ id: 1, email: 'test@test.com' }));
    localStorage.setItem('refreshToken', 'test-refresh-token');
    localStorage.setItem('tokenExpiry', new Date(Date.now() + 3600000).toISOString());
    service.isLoggedIn.set(true);

    service.logout();

    // ✅ Check the correct keys are removed
    expect(localStorage.getItem('auth_token')).toBeNull();
    expect(localStorage.getItem('user_data')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(localStorage.getItem('tokenExpiry')).toBeNull();
    expect(service.isLoggedIn()).toBeFalse();
    expect(service.getCurrentUser()).toBeNull();
  });

  it('should get token from localStorage', () => {
    // ✅ Use correct key 'auth_token'
    localStorage.setItem('auth_token', 'test-token');
    expect(service.getToken()).toBe('test-token');
  });

  it('should return null if no token', () => {
    expect(service.getToken()).toBeNull();
  });

  it('should detect expired tokens', () => {
    expect(service.isTokenExpired()).toBeTruthy();

    const futureDate = new Date(Date.now() + 3600000);
    service['tokenInfo'].set({ token: 'token', expiresAt: futureDate });
    expect(service.isTokenExpired()).toBeFalsy();
  });

  it('should load stored authentication on init', () => {
    const futureDate = new Date(Date.now() + 3600000);
    const userData = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test Owner', organizationId: 1, role: 'Owner' };

    localStorage.setItem('auth_token', 'stored-token');
    localStorage.setItem('user_data', JSON.stringify(userData));
    localStorage.setItem('tokenExpiry', futureDate.toISOString());

    service.loadStoredAuth();

    expect(service.isLoggedIn()).toBeTruthy();
    expect(service.getCurrentUser()).toEqual(userData);
    expect(service.getToken()).toBe('stored-token');
  });

  it('should logout if stored token is expired', () => {
    const pastDate = new Date(Date.now() - 3600000);
    const userData = { id: 1, email: 'test@test.com', businessName: 'Test Business', ownerName: 'Test Owner', organizationId: 1, role: 'Owner' };

    localStorage.setItem('auth_token', 'expired-token');
    localStorage.setItem('user_data', JSON.stringify(userData));
    localStorage.setItem('tokenExpiry', pastDate.toISOString());

    service.loadStoredAuth();

    expect(service.isLoggedIn()).toBeFalsy();
    expect(service.getCurrentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
  });

  it('should not load auth if no stored data', () => {
    localStorage.clear();

    service.loadStoredAuth();

    expect(service.isLoggedIn()).toBeFalsy();
    expect(service.getCurrentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
  });

  it('should handle partial stored data gracefully', () => {
    localStorage.setItem('auth_token', 'token');
    // Missing user_data and tokenExpiry

    service.loadStoredAuth();

    expect(service.isLoggedIn()).toBeFalsy();
    expect(service.getCurrentUser()).toBeNull();
  });

  it('should refresh token successfully', fakeAsync(() => {
    const refreshToken = 'refresh-token';
    const mockResponse = {
      token: 'new-token',
      refreshToken: 'new-refresh-token',
      user: { id: 1, email: 'test@test.com', businessName: 'Test', ownerName: 'Test User', organizationId: 1, role: 'Owner' },
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    };

    localStorage.setItem('refreshToken', refreshToken);
    mockHttpClient.post.and.returnValue(of(mockResponse));

    service.refreshToken().subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(service.getToken()).toBe('new-token');
    });

    tick();

    expect(mockHttpClient.post).toHaveBeenCalledWith('http://localhost:5000/api/auth/refresh', { refreshToken });
  }));

  it('should handle provider registration errors gracefully', fakeAsync(() => {
    const providerRequest = {
      storeCode: '1234',
      fullName: 'Provider Name',
      email: 'provider@test.com',
      password: 'password123'
    };

    mockHttpClient.post.and.returnValue(
      throwError(() => ({
        error: {
          message: 'Store code not found',
          errors: ['Invalid store code']
        }
      }))
    );

    service.registerProvider(providerRequest).subscribe(result => {
      expect(result.success).toBeFalse();
      expect(result.message).toBe('Store code not found');
      expect(result.errors).toEqual(['Invalid store code']);
    });

    tick();
  }));

  it('should handle store code validation errors', fakeAsync(() => {
    const storeCode = '9999';

    mockHttpClient.get.and.returnValue(
      throwError(() => ({ message: 'Network error' }))
    );

    service.validateStoreCode(storeCode).subscribe(result => {
      expect(result.isValid).toBeFalse();
      expect(result.errorMessage).toBe('Unable to validate store code');
    });

    tick();
  }));

  it('should return current user observable', () => {
    const userData = { id: 1, email: 'test@test.com', businessName: 'Test', ownerName: 'Test User', organizationId: 1, role: 'Owner' };

    service['currentUserSubject'].next(userData);

    service.currentUser$.subscribe(user => {
      expect(user).toEqual(userData);
    });
  });

  it('should set auth data correctly', () => {
    const authResponse = {
      token: 'new-token',
      refreshToken: 'new-refresh-token',
      user: { id: 1, email: 'test@test.com', businessName: 'Test', ownerName: 'Test User', organizationId: 1, role: 'Owner' },
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    };

    service['setAuthData'](authResponse);

    expect(localStorage.getItem('auth_token')).toBe('new-token');
    expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
    expect(JSON.parse(localStorage.getItem('user_data')!)).toEqual(authResponse.user);
    expect(localStorage.getItem('tokenExpiry')).toBe(authResponse.expiresAt);
    expect(service.isLoggedIn()).toBeTruthy();
    expect(service.getCurrentUser()).toEqual(authResponse.user);
  });
});