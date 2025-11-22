import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CustomerAuthService } from './customer-auth.service';
import { CustomerLoginRequest, CustomerRegistrationRequest, CustomerProfile } from '../../shared/models/customer.models';

describe('CustomerAuthService', () => {
  let service: CustomerAuthService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'https://localhost:7042';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CustomerAuthService]
    });
    service = TestBed.inject(CustomerAuthService);
    httpMock = TestBed.inject(HttpTestingController);

    // Clear localStorage
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
    it('should login successfully and store auth data', () => {
      const loginRequest: CustomerLoginRequest = {
        email: 'test@example.com',
        password: 'password123',
        orgSlug: 'test-store',
        rememberMe: false
      };

      const mockResponse = {
        token: 'mock-jwt-token',
        refreshToken: 'mock-refresh-token',
        customer: {
          id: '1',
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User'
        },
        expiresIn: 86400
      };

      let result: any;
      service.login(loginRequest).subscribe(response => {
        result = response;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginRequest);
      req.flush(mockResponse);

      expect(result).toEqual(mockResponse);
      expect(localStorage.getItem('cg_customer_token')).toBe('mock-jwt-token');
      expect(localStorage.getItem('cg_customer_refresh_token')).toBe('mock-refresh-token');
      expect(service.isAuthenticated()).toBe(true);
    });

    it('should handle login error', () => {
      const loginRequest: CustomerLoginRequest = {
        email: 'test@example.com',
        password: 'wrongpassword',
        orgSlug: 'test-store',
        rememberMe: false
      };

      let error: any;
      service.login(loginRequest).subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/login`);
      req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      expect(error).toBeTruthy();
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('register', () => {
    it('should register successfully', () => {
      const registerRequest: CustomerRegistrationRequest = {
        email: 'newuser@example.com',
        password: 'password123',
        firstName: 'New',
        lastName: 'User',
        orgSlug: 'test-store',
        phoneNumber: '1234567890'
      };

      const mockResponse = {
        token: 'mock-jwt-token',
        refreshToken: 'mock-refresh-token',
        customer: {
          id: '2',
          email: 'newuser@example.com',
          firstName: 'New',
          lastName: 'User'
        },
        expiresIn: 86400
      };

      let result: any;
      service.register(registerRequest).subscribe(response => {
        result = response;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(registerRequest);
      req.flush(mockResponse);

      expect(result).toEqual(mockResponse);
      expect(service.isAuthenticated()).toBe(true);
    });

    it('should handle registration error for existing email', () => {
      const registerRequest: CustomerRegistrationRequest = {
        email: 'existing@example.com',
        password: 'password123',
        firstName: 'Test',
        lastName: 'User',
        orgSlug: 'test-store'
      };

      let error: any;
      service.register(registerRequest).subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/register`);
      req.flush({ message: 'Email already exists' }, { status: 409, statusText: 'Conflict' });

      expect(error).toBeTruthy();
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('getProfile', () => {
    it('should get customer profile successfully', () => {
      const mockProfile: CustomerProfile = {
        id: '1',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        phoneNumber: '1234567890',
        isEmailVerified: true,
        fullName: 'Test User',
        orderCount: 0,
        totalSpent: 0,
        wishlistCount: 0,
        memberSince: new Date().toISOString()
      };

      let result: CustomerProfile | undefined;
      service.getProfile().subscribe(profile => {
        result = profile;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockProfile);

      expect(result).toEqual(mockProfile);
    });

    it('should handle profile fetch error', () => {
      let error: any;
      service.getProfile().subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/profile`);
      req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

      expect(error).toBeTruthy();
    });
  });

  describe('updateProfile', () => {
    it('should update customer profile successfully', () => {
      const updateData = {
        firstName: 'Updated',
        lastName: 'Name',
        phoneNumber: '9876543210'
      };

      const mockUpdatedProfile: CustomerProfile = {
        id: '1',
        email: 'test@example.com',
        firstName: 'Updated',
        lastName: 'Name',
        phoneNumber: '9876543210',
        isEmailVerified: true,
        fullName: 'Updated Name',
        orderCount: 0,
        totalSpent: 0,
        wishlistCount: 0,
        memberSince: new Date().toISOString()
      };

      let result: CustomerProfile | undefined;
      service.updateProfile(updateData).subscribe(profile => {
        result = profile;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/customers/profile`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateData);
      req.flush(mockUpdatedProfile);

      expect(result).toEqual(mockUpdatedProfile);
    });
  });

  describe('logout', () => {
    it('should logout and clear stored data', () => {
      // Setup authenticated state
      localStorage.setItem('cg_customer_token', 'test-token');
      localStorage.setItem('cg_customer_refresh_token', 'refresh-token');
      localStorage.setItem('cg_customer_profile', JSON.stringify({ id: '1', email: 'test@example.com' }));

      service.logout();

      expect(localStorage.getItem('cg_customer_token')).toBeNull();
      expect(localStorage.getItem('cg_customer_refresh_token')).toBeNull();
      expect(localStorage.getItem('cg_customer_profile')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentCustomer()).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('should return true when token exists and is valid', () => {
      // Mock a valid token (not expired)
      const futureDate = new Date(Date.now() + 3600000); // 1 hour from now
      localStorage.setItem('cg_customer_token', 'valid-token');
      localStorage.setItem('cg_customer_token_expiry', futureDate.toISOString());

      expect(service.isAuthenticated()).toBe(true);
    });

    it('should return false when token is expired', () => {
      // Mock an expired token
      const pastDate = new Date(Date.now() - 3600000); // 1 hour ago
      localStorage.setItem('cg_customer_token', 'expired-token');
      localStorage.setItem('cg_customer_token_expiry', pastDate.toISOString());

      expect(service.isAuthenticated()).toBe(false);
    });

    it('should return false when no token exists', () => {
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('getToken', () => {
    it('should return token when available', () => {
      localStorage.setItem('cg_customer_token', 'test-token');
      expect(service['getToken']()).toBe('test-token');
    });

    it('should return null when no token', () => {
      expect(service['getToken']()).toBeNull();
    });
  });

  describe('currentCustomer signal', () => {
    it('should update when profile is loaded from localStorage', () => {
      const mockCustomer = { id: '1', email: 'test@example.com', firstName: 'Test', lastName: 'User', isEmailVerified: true, fullName: 'Test User' };
      localStorage.setItem('cg_customer_profile', JSON.stringify(mockCustomer));

      // Trigger the service initialization
      service = TestBed.inject(CustomerAuthService);

      expect(service.currentCustomer()).toEqual(mockCustomer);
    });

    it('should be null when no customer data', () => {
      expect(service.currentCustomer()).toBeNull();
    });
  });
});