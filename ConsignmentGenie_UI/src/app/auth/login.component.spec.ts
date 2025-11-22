import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { LoginComponent } from './login.component';
import { environment } from '../../environments/environment';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let httpTestingController: HttpTestingController;
  let router: jasmine.SpyObj<Router>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['loadStoredAuth']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, HttpClientTestingModule, FormsModule],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpTestingController = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty credentials', () => {
    expect(component.credentials.email).toBe('');
    expect(component.credentials.password).toBe('');
    expect(component.isLoading()).toBe(false);
    expect(component.showPassword()).toBe(false);
    expect(component.errorMessage()).toBe('');
  });

  it('should toggle password visibility', () => {
    expect(component.showPassword()).toBe(false);
    component.togglePassword();
    expect(component.showPassword()).toBe(true);
    component.togglePassword();
    expect(component.showPassword()).toBe(false);
  });

  it('should set test account credentials', () => {
    const testEmail = 'admin@demoshop.com';
    component.useTestAccount(testEmail);

    expect(component.credentials.email).toBe(testEmail);
    expect(component.credentials.password).toBe('password123');
    expect(component.errorMessage()).toBe('');
  });

  describe('successful login', () => {
    it('should handle successful login response and store auth data correctly', async () => {
      const loginResponse = {
        success: true,
        data: {
          token: 'test-jwt-token',
          userId: 'user-123',
          email: 'test@example.com',
          role: 1,
          organizationId: 'org-456',
          organizationName: 'Test Shop',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
        },
        message: 'Login successful',
        errors: null
      };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();
      expect(component.isLoading()).toBe(true);

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        email: 'test@example.com',
        password: 'password123'
      });

      req.flush(loginResponse);
      await loginPromise;

      // Verify localStorage is set correctly
      expect(localStorage.getItem('token')).toBe('test-jwt-token');
      expect(localStorage.getItem('tokenExpiry')).toBe(loginResponse.data.expiresAt);

      const storedUser = JSON.parse(localStorage.getItem('user')!);
      expect(storedUser.userId).toBe('user-123');
      expect(storedUser.email).toBe('test@example.com');
      expect(storedUser.organizationId).toBe('org-456');
      expect(storedUser.organizationName).toBe('Test Shop');
      expect(storedUser.businessName).toBe('Test Shop');

      // Verify old auth keys are cleared
      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('user_data')).toBeNull();

      // Verify AuthService is called to refresh state
      expect(authService.loadStoredAuth).toHaveBeenCalled();
      expect(component.isLoading()).toBe(false);
    });

    it('should handle login without expiresAt and set default expiry', async () => {
      const loginResponse = {
        success: true,
        data: {
          token: 'test-jwt-token',
          userId: 'user-123',
          email: 'test@example.com',
          role: 1,
          organizationId: 'org-456',
          organizationName: 'Test Shop'
          // No expiresAt field
        },
        message: 'Login successful',
        errors: null
      };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush(loginResponse);
      await loginPromise;

      // Should have set a default expiry (24 hours from now)
      const tokenExpiry = localStorage.getItem('tokenExpiry');
      expect(tokenExpiry).toBeTruthy();

      const expiryDate = new Date(tokenExpiry!);
      const now = new Date();
      const diffHours = (expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60);
      expect(diffHours).toBeCloseTo(24, 1); // Within 1 hour of 24 hours
    });

    it('should redirect admin to admin dashboard', async () => {
      const loginResponse = {
        success: true,
        data: {
          token: 'test-jwt-token',
          userId: 'admin-123',
          email: 'admin@demoshop.com',
          role: 1,
          organizationId: 'org-456',
          organizationName: 'Test Shop',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
        },
        message: 'Login successful',
        errors: null
      };

      component.credentials.email = 'admin@demoshop.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush(loginResponse);
      await loginPromise;

      expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });

    it('should redirect owner/manager to owner dashboard', async () => {
      const loginResponse = {
        success: true,
        data: {
          token: 'test-jwt-token',
          userId: 'owner-123',
          email: 'owner@demoshop.com',
          role: 1, // Owner role
          organizationId: 'org-456',
          organizationName: 'Test Shop',
          expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
        },
        message: 'Login successful',
        errors: null
      };

      component.credentials.email = 'owner@demoshop.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush(loginResponse);
      await loginPromise;

      expect(router.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });
  });

  describe('login error handling', () => {
    it('should handle 401 unauthorized error', async () => {
      component.credentials.email = 'wrong@example.com';
      component.credentials.password = 'wrongpassword';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

      await loginPromise;

      expect(component.errorMessage()).toBe('Invalid email or password. Please try again.');
      expect(component.isLoading()).toBe(false);
    });

    it('should handle connection error', async () => {
      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush({ error: 'Connection refused' }, { status: 0, statusText: 'Unknown Error' });

      await loginPromise;

      expect(component.errorMessage()).toBe('Unable to connect to server. Please check your connection.');
      expect(component.isLoading()).toBe(false);
    });

    it('should handle general server error', async () => {
      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      const loginPromise = component.onSubmit();

      const req = httpTestingController.expectOne(`${environment.apiUrl}/api/auth/login`);
      req.flush({ error: 'Internal Server Error' }, { status: 500, statusText: 'Internal Server Error' });

      await loginPromise;

      expect(component.errorMessage()).toBe('Login failed. Please try again later.');
      expect(component.isLoading()).toBe(false);
    });

    it('should not submit when credentials are missing', async () => {
      component.credentials.email = '';
      component.credentials.password = '';

      await component.onSubmit();

      // No HTTP request should be made
      httpTestingController.expectNone(`${environment.apiUrl}/api/auth/login`);
      expect(component.isLoading()).toBe(false);
    });
  });

  describe('role normalization', () => {
    it('should normalize numeric roles correctly', () => {
      const normalized1 = component['normalizeRole'](1);
      expect(normalized1).toBe('Owner');

      const normalized6 = component['normalizeRole'](6);
      expect(normalized6).toBe('Provider');

      const normalized7 = component['normalizeRole'](7);
      expect(normalized7).toBe('Customer');
    });

    it('should handle string roles', () => {
      const normalizedString = component['normalizeRole']('Manager');
      expect(normalizedString).toBe('Manager');
    });

    it('should default to Owner for unknown roles', () => {
      const normalizedUnknown = component['normalizeRole'](999);
      expect(normalizedUnknown).toBe('Owner');
    });
  });
});