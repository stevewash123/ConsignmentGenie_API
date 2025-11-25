import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../services/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockHttpClient: jasmine.SpyObj<HttpClient>;
  let mockAuthService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const httpSpy = jasmine.createSpyObj('HttpClient', ['post']);
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['loadStoredAuth']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: HttpClient, useValue: httpSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    mockHttpClient = TestBed.inject(HttpClient) as jasmine.SpyObj<HttpClient>;
    mockAuthService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty credentials', () => {
    expect(component.credentials.email).toBe('');
    expect(component.credentials.password).toBe('');
  });

  it('should initialize with loading false', () => {
    expect(component.isLoading()).toBeFalsy();
  });

  it('should initialize with password hidden', () => {
    expect(component.showPassword()).toBeFalsy();
  });

  it('should initialize with no error message', () => {
    expect(component.errorMessage()).toBe('');
  });

  describe('togglePassword', () => {
    it('should toggle password visibility', () => {
      expect(component.showPassword()).toBeFalsy();

      component.togglePassword();
      expect(component.showPassword()).toBeTruthy();

      component.togglePassword();
      expect(component.showPassword()).toBeFalsy();
    });
  });

  describe('useTestAccount', () => {
    it('should set credentials for test account', () => {
      const testEmail = 'test@example.com';

      component.useTestAccount(testEmail);

      expect(component.credentials.email).toBe(testEmail);
      expect(component.credentials.password).toBe('password123');
      expect(component.errorMessage()).toBe('');
    });
  });

  describe('onSubmit', () => {
    it('should return early if email is missing', async () => {
      component.credentials.email = '';
      component.credentials.password = 'password';

      await component.onSubmit();

      expect(mockHttpClient.post).not.toHaveBeenCalled();
    });

    it('should return early if password is missing', async () => {
      component.credentials.email = 'test@example.com';
      component.credentials.password = '';

      await component.onSubmit();

      expect(mockHttpClient.post).not.toHaveBeenCalled();
    });

    it('should handle successful login with owner role', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'owner@test.com',
          role: 1,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString(),
          approvalStatus: 1
        }
      };

      component.credentials.email = 'owner@test.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      await component.onSubmit();

      expect(component.isLoading()).toBeFalsy();
      expect(mockAuthService.loadStoredAuth).toHaveBeenCalled();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should handle successful login with customer role', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'customer@test.com',
          role: 3,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString()
        }
      };

      component.credentials.email = 'customer@test.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      await component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should handle admin login', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'admin@microsaasbuilders.com',
          role: 0,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString()
        }
      };

      component.credentials.email = 'admin@microsaasbuilders.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      await component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });

    it('should handle pending approval status', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'pending@test.com',
          role: 1,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString(),
          approvalStatus: 0
        }
      };

      component.credentials.email = 'pending@test.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      await component.onSubmit();

      expect(component.errorMessage()).toContain('pending admin approval');
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should handle rejected approval status', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'rejected@test.com',
          role: 1,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString(),
          approvalStatus: 2
        }
      };

      component.credentials.email = 'rejected@test.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      await component.onSubmit();

      expect(component.errorMessage()).toContain('has been rejected');
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should handle 401 error', async () => {
      const error = { status: 401 };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'wrongpassword';

      mockHttpClient.post.and.returnValue(throwError(error));

      await component.onSubmit();

      expect(component.errorMessage()).toContain('Invalid email or password');
      expect(component.isLoading()).toBeFalsy();
    });

    it('should handle network error (status 0)', async () => {
      const error = { status: 0 };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(throwError(error));

      await component.onSubmit();

      expect(component.errorMessage()).toContain('Unable to connect to server');
      expect(component.isLoading()).toBeFalsy();
    });

    it('should handle generic error', async () => {
      const error = { status: 500 };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(throwError(error));

      await component.onSubmit();

      expect(component.errorMessage()).toContain('Login failed');
      expect(component.isLoading()).toBeFalsy();
    });

    it('should set loading state during login', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          userId: '123',
          email: 'test@example.com',
          role: 1,
          organizationId: 'org123',
          organizationName: 'Test Org',
          expiresAt: new Date().toISOString()
        }
      };

      component.credentials.email = 'test@example.com';
      component.credentials.password = 'password123';

      mockHttpClient.post.and.returnValue(of(mockResponse));

      const loginPromise = component.onSubmit();
      expect(component.isLoading()).toBeTruthy();

      await loginPromise;
      expect(component.isLoading()).toBeFalsy();
    });
  });

  describe('normalizeRole', () => {
    it('should convert numeric role to string', () => {
      expect(component['normalizeRole'](0)).toBe('Admin');
      expect(component['normalizeRole'](1)).toBe('Owner');
      expect(component['normalizeRole'](2)).toBe('Provider');
      expect(component['normalizeRole'](3)).toBe('Customer');
    });

    it('should return Owner for unknown numeric role', () => {
      expect(component['normalizeRole'](999)).toBe('Owner');
    });

    it('should return string role as-is', () => {
      expect(component['normalizeRole']('CustomRole')).toBe('CustomRole');
    });

    it('should handle string numbers', () => {
      expect(component['normalizeRole']('0')).toBe('Admin');
      expect(component['normalizeRole']('1')).toBe('Owner');
      expect(component['normalizeRole']('2')).toBe('Provider');
      expect(component['normalizeRole']('3')).toBe('Customer');
    });
  });

  describe('redirectBasedOnRole', () => {
    it('should redirect admin email to admin dashboard', () => {
      component['redirectBasedOnRole']('0', 'admin@microsaasbuilders.com');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });

    it('should redirect owner to owner dashboard', () => {
      component['redirectBasedOnRole']('1', 'owner@example.com');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    // it('should redirect manager to owner dashboard', () => {
    //   component['redirectBasedOnRole']('2', 'manager@example.com');
    //   expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    // });

    it('should redirect provider to customer dashboard', () => {
      component['redirectBasedOnRole']('2', 'provider@example.com');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should redirect customer to customer dashboard', () => {
      component['redirectBasedOnRole']('3', 'customer@example.com');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should default to owner dashboard for unknown role', () => {
      component['redirectBasedOnRole']('999', 'unknown@example.com');
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });
  });
});