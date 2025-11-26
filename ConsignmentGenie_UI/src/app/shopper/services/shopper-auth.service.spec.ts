import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ShopperAuthService, ShopperRegisterRequest, ShopperLoginRequest, GuestSessionRequest, UpdateShopperProfileRequest, ChangePasswordRequest } from './shopper-auth.service';
import { environment } from '../../../environments/environment';

describe('ShopperAuthService', () => {
  let service: ShopperAuthService;
  let httpMock: HttpTestingController;
  const testStoreSlug = 'test-store';
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ShopperAuthService]
    });
    service = TestBed.inject(ShopperAuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('register', () => {
    it('should register shopper successfully', () => {
      const request: ShopperRegisterRequest = {
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123',
        phone: '555-0123',
        emailNotifications: true
      };

      const mockResponse = {
        success: true,
        data: {
          success: true,
          token: 'test-token',
          expiresAt: new Date(),
          profile: {
            shopperId: '123',
            fullName: 'John Doe',
            email: 'john@example.com',
            phone: '555-0123',
            emailNotifications: true,
            memberSince: new Date()
          }
        }
      };

      service.register(testStoreSlug, request).subscribe(result => {
        expect(result.success).toBeTrue();
        expect(result.token).toBe('test-token');
        expect(result.profile?.fullName).toBe('John Doe');
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });
  });

  describe('login', () => {
    xit('should login shopper successfully', () => {
      const request: ShopperLoginRequest = {
        email: 'john@example.com',
        password: 'password123',
        rememberMe: true
      };

      const mockResponse = {
        success: true,
        data: {
          success: true,
          token: 'test-token',
          expiresAt: new Date(),
          profile: {
            shopperId: '123',
            fullName: 'John Doe',
            email: 'john@example.com',
            emailNotifications: true,
            memberSince: new Date()
          }
        }
      };

      service.login(testStoreSlug, request).subscribe(result => {
        expect(result.success).toBeTrue();
        expect(result.token).toBe('test-token');
        expect(service.isAuthenticated(testStoreSlug)).toBeTrue();
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('createGuestSession', () => {
    it('should create guest session successfully', () => {
      const request: GuestSessionRequest = {
        email: 'guest@example.com',
        fullName: 'Guest User',
        phone: '555-0123'
      };

      const mockResponse = {
        success: true,
        data: {
          sessionToken: 'guest-token',
          expiresAt: new Date()
        }
      };

      service.createGuestSession(testStoreSlug, request).subscribe(result => {
        expect(result.sessionToken).toBe('guest-token');
        expect(result.expiresAt).toBeDefined();
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/auth/guest`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('getProfile', () => {
    it('should get shopper profile successfully', () => {
      const mockProfile = {
        shopperId: '123',
        fullName: 'John Doe',
        email: 'john@example.com',
        phone: '555-0123',
        emailNotifications: true,
        memberSince: new Date()
      };

      const mockResponse = {
        success: true,
        data: mockProfile
      };

      service.getProfile(testStoreSlug).subscribe(profile => {
        expect(profile).toEqual(mockProfile);
        expect(service.getCurrentProfile()).toEqual(mockProfile);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/account`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('updateProfile', () => {
    it('should update shopper profile successfully', () => {
      const request: UpdateShopperProfileRequest = {
        fullName: 'John Updated',
        phone: '555-9999',
        emailNotifications: false,
        shippingAddress: {
          address1: '123 Main St',
          city: 'Anytown',
          state: 'CA',
          zip: '12345'
        }
      };

      const mockUpdatedProfile = {
        shopperId: '123',
        fullName: 'John Updated',
        email: 'john@example.com',
        phone: '555-9999',
        emailNotifications: false,
        memberSince: new Date(),
        shippingAddress: request.shippingAddress
      };

      const mockResponse = {
        success: true,
        data: mockUpdatedProfile
      };

      service.updateProfile(testStoreSlug, request).subscribe(profile => {
        expect(profile.fullName).toBe('John Updated');
        expect(profile.phone).toBe('555-9999');
        expect(service.getCurrentProfile()).toEqual(mockUpdatedProfile);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/account`);
      expect(req.request.method).toBe('PUT');
      req.flush(mockResponse);
    });
  });

  describe('changePassword', () => {
    it('should change password successfully', () => {
      const request: ChangePasswordRequest = {
        currentPassword: 'oldpassword',
        newPassword: 'newpassword123'
      };

      const mockResponse = {
        success: true,
        data: { success: true }
      };

      service.changePassword(testStoreSlug, request).subscribe(result => {
        expect(result.success).toBeTrue();
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/account/change-password`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('logout', () => {
    it('should logout and clear stored data', () => {
      // Set up some stored data
      localStorage.setItem('shopper_token_' + testStoreSlug, 'test-token');
      localStorage.setItem('shopper_profile_' + testStoreSlug, JSON.stringify({ fullName: 'Test' }));

      service.logout(testStoreSlug);

      expect(service.getToken(testStoreSlug)).toBeNull();
      expect(service.getStoredProfile(testStoreSlug)).toBeNull();
      expect(service.getCurrentProfile()).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no token exists', () => {
      expect(service.isAuthenticated(testStoreSlug)).toBeFalse();
    });

    it('should return false when token is invalid', () => {
      localStorage.setItem('shopper_token_' + testStoreSlug, 'invalid-token');
      expect(service.isAuthenticated(testStoreSlug)).toBeFalse();
    });

    it('should return true for valid token', () => {
      // Create a valid JWT token for testing
      const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
      const payload = btoa(JSON.stringify({
        exp: Math.floor(Date.now() / 1000) + 3600, // Valid for 1 hour
        StoreSlug: testStoreSlug
      }));
      const signature = 'test-signature';
      const validToken = `${header}.${payload}.${signature}`;

      localStorage.setItem('shopper_token_' + testStoreSlug, validToken);
      expect(service.isAuthenticated(testStoreSlug)).toBeTrue();
    });
  });

  describe('getToken', () => {
    it('should return stored token', () => {
      const token = 'test-token';
      localStorage.setItem('shopper_token_' + testStoreSlug, token);

      expect(service.getToken(testStoreSlug)).toBe(token);
    });

    it('should return null when no token exists', () => {
      expect(service.getToken(testStoreSlug)).toBeNull();
    });
  });

  describe('getStoredProfile', () => {
    it('should return stored profile', () => {
      const profile = { shopperId: '123', fullName: 'Test User', email: 'test@example.com' };
      localStorage.setItem('shopper_profile_' + testStoreSlug, JSON.stringify(profile));

      const stored = service.getStoredProfile(testStoreSlug);
      expect(stored?.fullName).toBe('Test User');
    });

    it('should return null when no profile exists', () => {
      expect(service.getStoredProfile(testStoreSlug)).toBeNull();
    });
  });

  describe('getCurrentProfile', () => {
    it('should return current profile from subject', () => {
      expect(service.getCurrentProfile()).toBeNull();

      const profile = {
        shopperId: '123',
        fullName: 'Test User',
        email: 'test@example.com',
        emailNotifications: true,
        memberSince: new Date()
      };

      service['currentProfileSubject'].next(profile);
      expect(service.getCurrentProfile()).toEqual(profile);
    });
  });

  describe('observables', () => {
    it('should emit current profile changes', () => {
      const profile = {
        shopperId: '123',
        fullName: 'Test User',
        email: 'test@example.com',
        emailNotifications: true,
        memberSince: new Date()
      };

      service.currentProfile$.subscribe(emittedProfile => {
        if (emittedProfile) {
          expect(emittedProfile.fullName).toBe('Test User');
        }
      });

      service['currentProfileSubject'].next(profile);
    });

    it('should emit auth status changes', () => {
      service.authStatus$.subscribe(status => {
        expect(typeof status).toBe('boolean');
      });

      service['authStatusSubject'].next(true);
    });
  });

  describe('error handling', () => {
    it('should handle registration error', () => {
      const request: ShopperRegisterRequest = {
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123',
        emailNotifications: true
      };

      service.register(testStoreSlug, request).subscribe({
        error: (error) => {
          expect(error.message).toBeDefined();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/auth/register`);
      req.flush(
        { message: 'Email already exists' },
        { status: 400, statusText: 'Bad Request' }
      );
    });

    it('should handle login error', () => {
      const request: ShopperLoginRequest = {
        email: 'john@example.com',
        password: 'wrongpassword',
        rememberMe: false
      };

      service.login(testStoreSlug, request).subscribe({
        error: (error) => {
          expect(error.message).toBeDefined();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/auth/login`);
      req.flush(
        { message: 'Invalid credentials' },
        { status: 401, statusText: 'Unauthorized' }
      );
    });
  });
});