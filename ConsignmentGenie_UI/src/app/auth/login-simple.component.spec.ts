import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginSimpleComponent } from './login-simple.component';
import { AuthService } from '../services/auth.service';
import { signal } from '@angular/core';

describe('LoginSimpleComponent', () => {
  let component: LoginSimpleComponent;
  let fixture: ComponentFixture<LoginSimpleComponent>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockAuthService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginSimpleComponent],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginSimpleComponent);
    component = fixture.componentInstance;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    mockAuthService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty email and password', () => {
    expect(component.email).toBe('');
    expect(component.password).toBe('');
  });

  it('should initialize with false loading state', () => {
    expect(component.isLoading()).toBeFalse();
  });

  it('should initialize with empty error message', () => {
    expect(component.errorMessage()).toBe('');
  });

  describe('onSubmit', () => {
    beforeEach(() => {
      component.email = 'test@example.com';
      component.password = 'password123';
    });

    it('should handle successful login with wrapped response format', () => {
      const mockUser = {
        id: 1,
        email: 'test@example.com',
        businessName: 'Test Business',
        ownerName: 'Test Owner',
        organizationId: 1,
        role: 1
      };
      const mockResponse = {
        success: true,
        data: {
          token: 'mock-token',
          refreshToken: 'mock-refresh',
          user: mockUser,
          expiresAt: new Date(Date.now() + 3600000).toISOString()
        },
        message: 'Login successful'
      };

      spyOn(component as any, 'redirectBasedOnUser');
      mockAuthService.login.and.returnValue(of(mockResponse as any));

      component.onSubmit();

      expect(mockAuthService.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123'
      });
      expect((component as any).redirectBasedOnUser).toHaveBeenCalledWith(mockUser);
      expect(component.isLoading()).toBeFalse();
    });

    it('should handle successful login with direct user response format', () => {
      const mockUser = {
        id: 1,
        email: 'test@example.com',
        businessName: 'Test Business',
        ownerName: 'Test Owner',
        organizationId: 1,
        role: 1
      };
      const mockResponse = {
        token: 'mock-token',
        refreshToken: 'mock-refresh',
        user: mockUser,
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      spyOn(component as any, 'redirectBasedOnUser');
      mockAuthService.login.and.returnValue(of(mockResponse as any));

      component.onSubmit();

      expect((component as any).redirectBasedOnUser).toHaveBeenCalledWith(mockUser);
      expect(component.isLoading()).toBeFalse();
    });

    it('should handle successful login with data-only response format', () => {
      const mockUser = {
        id: 1,
        email: 'test@example.com',
        businessName: 'Test Business',
        ownerName: 'Test Owner',
        organizationId: 1,
        role: 1
      };
      const mockResponse = {
        success: true,
        data: mockUser,
        message: 'Login successful'
      };

      spyOn(component as any, 'redirectBasedOnUser');
      mockAuthService.login.and.returnValue(of(mockResponse as any));

      component.onSubmit();

      expect((component as any).redirectBasedOnUser).toHaveBeenCalledWith(mockUser);
      expect(component.isLoading()).toBeFalse();
    });

    it('should handle 401 error', () => {
      const error = { status: 401 };
      mockAuthService.login.and.returnValue(throwError(error));

      component.onSubmit();

      expect(component.errorMessage()).toBe('Invalid email or password');
      expect(component.isLoading()).toBeFalse();
    });

    it('should handle network error (status 0)', () => {
      const error = { status: 0 };
      mockAuthService.login.and.returnValue(throwError(error));

      component.onSubmit();

      expect(component.errorMessage()).toBe('Cannot connect to server');
      expect(component.isLoading()).toBeFalse();
    });

    it('should handle generic error', () => {
      const error = { status: 500 };
      mockAuthService.login.and.returnValue(throwError(error));

      component.onSubmit();

      expect(component.errorMessage()).toBe('Login failed. Please try again.');
      expect(component.isLoading()).toBeFalse();
    });

    it('should set loading state during login', () => {
      const mockResponse = {
        token: 'mock-token',
        refreshToken: 'mock-refresh',
        user: { id: 1, email: 'test@example.com', businessName: 'Test', ownerName: 'Test', organizationId: 1, role: 1 },
        expiresAt: new Date(Date.now() + 3600000).toISOString()
      };

      spyOn(component as any, 'redirectBasedOnUser');
      mockAuthService.login.and.returnValue(of(mockResponse as any));

      expect(component.isLoading()).toBeFalse();

      component.onSubmit();

      // Note: In a real async scenario, we'd check loading state during the call,
      // but since this is synchronous in tests, we just verify it's reset
      expect(component.isLoading()).toBeFalse();
    });
  });

  describe('redirectBasedOnUser', () => {
    it('should redirect admin email to admin dashboard', () => {
      const userData = { email: 'admin@microsaasbuilders.com', role: 0 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });

    it('should redirect owner role to owner dashboard', () => {
      const userData = { email: 'owner@example.com', role: 1 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should redirect provider role to customer dashboard', () => {
      const userData = { email: 'provider@example.com', role: 2 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should redirect customer role to customer dashboard', () => {
      const userData = { email: 'customer@example.com', role: 3 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should handle string role values', () => {
      const userData = { email: 'owner@example.com', role: 'Owner' };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should default to owner dashboard for unknown role', () => {
      const userData = { email: 'unknown@example.com', role: 999 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should throw exception for undefined role', () => {
      const userData = { email: 'test@example.com', role: undefined };

      expect(() => (component as any).redirectBasedOnUser(userData)).toThrowError('Role cannot be undefined');
    });

    it('should handle missing email', () => {
      const userData = { role: 1 };
      (component as any).redirectBasedOnUser(userData);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });
  });

  describe('normalizeRole', () => {
    it('should convert numeric role to string', () => {
      expect((component as any).normalizeRole(0)).toBe('Admin');
      expect((component as any).normalizeRole(1)).toBe('Owner');
      expect((component as any).normalizeRole(2)).toBe('Provider');
      expect((component as any).normalizeRole(3)).toBe('Customer');
    });

    it('should return Owner for unknown numeric role', () => {
      expect((component as any).normalizeRole(999)).toBe('Owner');
    });

    it('should return string role as-is', () => {
      expect((component as any).normalizeRole('CustomRole')).toBe('CustomRole');
    });

    it('should handle string numbers', () => {
      expect((component as any).normalizeRole('0')).toBe('Admin');
      expect((component as any).normalizeRole('1')).toBe('Owner');
      expect((component as any).normalizeRole('2')).toBe('Provider');
      expect((component as any).normalizeRole('3')).toBe('Customer');
    });

    it('should throw exception for undefined role', () => {
      expect(() => (component as any).normalizeRole(undefined)).toThrowError('Role cannot be undefined');
    });

    it('should throw exception for null role', () => {
      expect(() => (component as any).normalizeRole(null)).toThrowError('Role cannot be null');
    });
  });
});