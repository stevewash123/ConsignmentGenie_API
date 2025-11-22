import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { CustomerAuthService } from '../../services/customer-auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: jasmine.SpyObj<CustomerAuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('CustomerAuthService', ['login']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      queryParams: of({ store: 'test-store' }),
      snapshot: { queryParams: { returnUrl: '/customer/dashboard' } }
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        { provide: CustomerAuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockAuthService = TestBed.inject(CustomerAuthService) as jasmine.SpyObj<CustomerAuthService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with proper validation', () => {
    expect(component['loginForm']).toBeDefined();
    expect(component['loginForm'].get('email')?.hasError('required')).toBeTruthy();
    expect(component['loginForm'].get('password')?.hasError('required')).toBeTruthy();
    expect(component['loginForm'].get('orgSlug')?.hasError('required')).toBeTruthy();
    expect(component['loginForm'].get('rememberMe')?.value).toBeFalsy();
  });

  it('should pre-populate store slug from query params', () => {
    expect(component['loginForm'].get('orgSlug')?.value).toBe('test-store');
  });

  it('should validate email format', () => {
    const emailControl = component['loginForm'].get('email');
    emailControl?.setValue('invalid-email');
    expect(emailControl?.hasError('email')).toBeTruthy();

    emailControl?.setValue('valid@example.com');
    expect(emailControl?.hasError('email')).toBeFalsy();
  });

  it('should toggle password visibility', () => {
    expect(component['showPassword']()).toBeFalsy();
    component['togglePassword']();
    expect(component['showPassword']()).toBeTruthy();
    component['togglePassword']();
    expect(component['showPassword']()).toBeFalsy();
  });

  it('should not submit invalid form', () => {
    component['onSubmit']();

    expect(component['loginForm'].get('email')?.touched).toBeTruthy();
    expect(component['loginForm'].get('password')?.touched).toBeTruthy();
    expect(component['loginForm'].get('orgSlug')?.touched).toBeTruthy();
    expect(mockAuthService.login).not.toHaveBeenCalled();
  });

  it('should submit valid form successfully', () => {
    const mockResponse = {
      token: 'mock-token',
      customer: { id: '1', email: 'test@example.com' }
    };
    mockAuthService.login.and.returnValue(of(mockResponse));

    // Fill form with valid data
    component['loginForm'].patchValue({
      email: 'test@example.com',
      password: 'password123',
      orgSlug: 'test-store',
      rememberMe: true
    });

    component['onSubmit']();

    expect(component['loading']()).toBeTruthy();
    expect(mockAuthService.login).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123',
      orgSlug: 'test-store',
      rememberMe: true
    });

    // Simulate async completion
    fixture.detectChanges();

    expect(component['loading']()).toBeFalsy();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    expect(component['errorMessage']()).toBe('');
  });

  it('should handle login error', () => {
    const errorResponse = { message: 'Invalid credentials' };
    mockAuthService.login.and.returnValue(throwError(() => errorResponse));

    // Fill form with valid data
    component['loginForm'].patchValue({
      email: 'test@example.com',
      password: 'wrongpassword',
      orgSlug: 'test-store'
    });

    component['onSubmit']();

    expect(component['loading']()).toBeTruthy();

    // Simulate async completion
    fixture.detectChanges();

    expect(component['loading']()).toBeFalsy();
    expect(component['errorMessage']()).toBe('Invalid credentials');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should use default error message when none provided', () => {
    mockAuthService.login.and.returnValue(throwError(() => ({})));

    component['loginForm'].patchValue({
      email: 'test@example.com',
      password: 'wrongpassword',
      orgSlug: 'test-store'
    });

    component['onSubmit']();
    fixture.detectChanges();

    expect(component['errorMessage']()).toBe('Invalid email, password, or store name. Please try again.');
  });

  it('should navigate to returnUrl if provided', () => {
    mockActivatedRoute.snapshot.queryParams.returnUrl = '/customer/orders';
    const mockResponse = { token: 'mock-token', customer: { id: '1' } };
    mockAuthService.login.and.returnValue(of(mockResponse));

    component['loginForm'].patchValue({
      email: 'test@example.com',
      password: 'password123',
      orgSlug: 'test-store'
    });

    component['onSubmit']();
    fixture.detectChanges();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/orders']);
  });

  describe('Template rendering', () => {
    it('should display form fields', () => {
      const compiled = fixture.nativeElement;

      expect(compiled.querySelector('input[formControlName="email"]')).toBeTruthy();
      expect(compiled.querySelector('input[formControlName="password"]')).toBeTruthy();
      expect(compiled.querySelector('input[formControlName="orgSlug"]')).toBeTruthy();
      expect(compiled.querySelector('input[formControlName="rememberMe"]')).toBeTruthy();
    });

    it('should display validation errors when fields are touched and invalid', () => {
      const emailInput = component['loginForm'].get('email');
      emailInput?.markAsTouched();
      emailInput?.setValue('');

      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const errorMessage = compiled.querySelector('.error-message');
      expect(errorMessage?.textContent).toContain('Email is required');
    });

    it('should display email format validation error', () => {
      const emailInput = component['loginForm'].get('email');
      emailInput?.markAsTouched();
      emailInput?.setValue('invalid-email');

      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const errorMessage = compiled.querySelector('.error-message');
      expect(errorMessage?.textContent).toContain('Please enter a valid email address');
    });

    it('should display password toggle button', () => {
      const compiled = fixture.nativeElement;
      const toggleButton = compiled.querySelector('.password-toggle');
      expect(toggleButton).toBeTruthy();
      expect(toggleButton.textContent?.trim()).toBe('Show');
    });

    it('should change password toggle text when clicked', () => {
      const compiled = fixture.nativeElement;
      const toggleButton = compiled.querySelector('.password-toggle');

      toggleButton?.click();
      fixture.detectChanges();

      expect(toggleButton.textContent?.trim()).toBe('Hide');
    });

    it('should disable submit button when form is invalid', () => {
      const compiled = fixture.nativeElement;
      const submitButton = compiled.querySelector('.submit-button');

      expect(submitButton?.disabled).toBeTruthy();
    });

    it('should enable submit button when form is valid', () => {
      component['loginForm'].patchValue({
        email: 'test@example.com',
        password: 'password123',
        orgSlug: 'test-store'
      });

      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const submitButton = compiled.querySelector('.submit-button');

      expect(submitButton?.disabled).toBeFalsy();
    });

    it('should show loading state during login', () => {
      component['loading'].set(true);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const loadingSpinner = compiled.querySelector('.loading-spinner');
      const submitText = compiled.querySelector('.submit-button').textContent;

      expect(loadingSpinner).toBeTruthy();
      expect(submitText).toContain('Signing in...');
    });

    it('should display error message when login fails', () => {
      component['errorMessage'].set('Login failed');
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const errorAlert = compiled.querySelector('.alert-error');

      expect(errorAlert).toBeTruthy();
      expect(errorAlert?.textContent?.trim()).toBe('Login failed');
    });

    it('should display marketing features section', () => {
      const compiled = fixture.nativeElement;

      expect(compiled.querySelector('.branding-section')).toBeTruthy();
      expect(compiled.querySelector('.feature')).toBeTruthy();
      expect(compiled.textContent).toContain('Shop with Confidence');
      expect(compiled.textContent).toContain('Easy Shopping');
      expect(compiled.textContent).toContain('Order Tracking');
      expect(compiled.textContent).toContain('Wishlist');
    });
  });
});