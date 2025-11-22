import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService
      ]
    });
    service = TestBed.inject(AuthService);
    // Clear localStorage
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return null for getCurrentUser when no user is logged in', () => {
    expect(service.getCurrentUser()).toBeNull();
  });

  it('should return null for getToken when no token is stored', () => {
    expect(service.getToken()).toBeNull();
  });

  it('should return true for isTokenExpired when no token info', () => {
    expect(service.isTokenExpired()).toBe(true);
  });

  it('should have isLoggedIn signal set to false initially', () => {
    expect(service.isLoggedIn()).toBe(false);
  });

  it('should clear all stored data on logout', () => {
    // Set some data first
    localStorage.setItem('token', 'test-token');
    localStorage.setItem('refreshToken', 'refresh-token');
    localStorage.setItem('user', JSON.stringify({ id: 1, email: 'test@example.com' }));
    localStorage.setItem('tokenExpiry', new Date().toISOString());

    service.logout();

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
    expect(localStorage.getItem('tokenExpiry')).toBeNull();
    expect(service.getCurrentUser()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });

  describe('loadStoredAuth', () => {
    it('should load stored auth data when valid token exists', () => {
      const mockUser = {
        id: 123,
        email: 'test@example.com',
        businessName: 'Test Shop',
        ownerName: 'Test Owner',
        organizationId: 456,
        role: 'Owner'
      };
      const futureDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();

      localStorage.setItem('token', 'valid-token');
      localStorage.setItem('user', JSON.stringify(mockUser));
      localStorage.setItem('tokenExpiry', futureDate);

      service.loadStoredAuth();

      expect(service.getCurrentUser()).toEqual(mockUser);
      expect(service.getToken()).toBe('valid-token');
      expect(service.isLoggedIn()).toBe(true);
    });

    it('should logout when stored token is expired', () => {
      const mockUser = {
        id: 123,
        email: 'test@example.com',
        businessName: 'Test Shop',
        ownerName: 'Test Owner',
        organizationId: 456,
        role: 'Owner'
      };
      const pastDate = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString();

      localStorage.setItem('token', 'expired-token');
      localStorage.setItem('user', JSON.stringify(mockUser));
      localStorage.setItem('tokenExpiry', pastDate);

      spyOn(service, 'logout').and.callThrough();
      service.loadStoredAuth();

      expect(service.logout).toHaveBeenCalled();
      expect(service.isLoggedIn()).toBe(false);
    });

    it('should not load auth when required data is missing', () => {
      localStorage.setItem('token', 'test-token');
      // Missing user and tokenExpiry

      service.loadStoredAuth();

      expect(service.getCurrentUser()).toBeNull();
      expect(service.isLoggedIn()).toBe(false);
    });
  });

  describe('token management', () => {
    it('should return correct token from localStorage', () => {
      localStorage.setItem('token', 'my-test-token');
      expect(service.getToken()).toBe('my-test-token');
    });

    it('should correctly identify expired tokens', () => {
      const pastDate = new Date(Date.now() - 60 * 60 * 1000);
      const futureDate = new Date(Date.now() + 60 * 60 * 1000);

      // Manually set token info for testing
      service['tokenInfo'].set({ token: 'test', expiresAt: pastDate });
      expect(service.isTokenExpired()).toBe(true);

      service['tokenInfo'].set({ token: 'test', expiresAt: futureDate });
      expect(service.isTokenExpired()).toBe(false);
    });

    it('should identify missing token as expired', () => {
      service['tokenInfo'].set(null);
      expect(service.isTokenExpired()).toBe(true);
    });
  });
});