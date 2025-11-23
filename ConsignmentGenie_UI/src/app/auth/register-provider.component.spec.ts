import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RegisterProviderComponent } from './register-provider.component';
import { AuthService } from '../services/auth.service';

describe('RegisterProviderComponent', () => {
  let component: RegisterProviderComponent;
  let fixture: ComponentFixture<RegisterProviderComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['validateStoreCode', 'registerProvider']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [RegisterProviderComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterProviderComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Store Code Validation', () => {
    it('should initialize with store code form', () => {
      expect(component.storeCodeForm).toBeDefined();
      expect(component.storeCodeValidated).toBeFalse();
    });

    it('should require 4-digit store code', () => {
      const storeCodeControl = component.storeCodeForm.get('storeCode');

      storeCodeControl?.setValue('123');
      expect(storeCodeControl?.invalid).toBeTrue();
      expect(storeCodeControl?.errors?.['pattern']).toBeTruthy();

      storeCodeControl?.setValue('12345');
      expect(storeCodeControl?.invalid).toBeTrue();
      expect(storeCodeControl?.errors?.['pattern']).toBeTruthy();

      storeCodeControl?.setValue('abcd');
      expect(storeCodeControl?.invalid).toBeTrue();
      expect(storeCodeControl?.errors?.['pattern']).toBeTruthy();

      storeCodeControl?.setValue('1234');
      expect(storeCodeControl?.valid).toBeTrue();
    });

    it('should validate store code successfully', async () => {
      const mockValidation = {
        isValid: true,
        shopName: 'Test Shop'
      };

      authService.validateStoreCode.and.returnValue(Promise.resolve(mockValidation));
      component.storeCodeForm.get('storeCode')?.setValue('1234');

      await component.validateStoreCode();

      expect(authService.validateStoreCode).toHaveBeenCalledWith('1234');
      expect(component.storeCodeValidated).toBeTrue();
      expect(component.shopName).toBe('Test Shop');
      expect(component.storeCodeError).toBe('');
    });

    it('should handle invalid store code', async () => {
      const mockValidation = {
        isValid: false,
        errorMessage: 'Invalid store code'
      };

      authService.validateStoreCode.and.returnValue(Promise.resolve(mockValidation));
      component.storeCodeForm.get('storeCode')?.setValue('9999');

      await component.validateStoreCode();

      expect(component.storeCodeValidated).toBeFalse();
      expect(component.storeCodeError).toBe('Invalid store code');
    });

    it('should handle store code validation error', async () => {
      authService.validateStoreCode.and.returnValue(Promise.reject(new Error('Network error')));
      component.storeCodeForm.get('storeCode')?.setValue('1234');

      await component.validateStoreCode();

      expect(component.storeCodeValidated).toBeFalse();
      expect(component.storeCodeError).toBe('Unable to validate store code. Please try again.');
    });

    it('should not validate if form is invalid', async () => {
      component.storeCodeForm.get('storeCode')?.setValue('');

      await component.validateStoreCode();

      expect(authService.validateStoreCode).not.toHaveBeenCalled();
    });
  });

  describe('Registration Form', () => {
    beforeEach(() => {
      // Set up validated store code state
      component.storeCodeValidated = true;
      component.shopName = 'Test Shop';
      component.storeCodeForm.get('storeCode')?.setValue('1234');
    });

    it('should initialize registration form with required fields', () => {
      expect(component.registrationForm).toBeDefined();

      const fullNameControl = component.registrationForm.get('fullName');
      expect(fullNameControl?.hasError('required')).toBeTrue();

      const emailControl = component.registrationForm.get('email');
      expect(emailControl?.hasError('required')).toBeTrue();

      const passwordControl = component.registrationForm.get('password');
      expect(passwordControl?.hasError('required')).toBeTrue();
    });

    it('should validate email format', () => {
      const emailControl = component.registrationForm.get('email');

      emailControl?.setValue('invalid-email');
      expect(emailControl?.hasError('email')).toBeTrue();

      emailControl?.setValue('valid@email.com');
      expect(emailControl?.hasError('email')).toBeFalse();
    });

    it('should validate password minimum length', () => {
      const passwordControl = component.registrationForm.get('password');

      passwordControl?.setValue('123');
      expect(passwordControl?.hasError('minlength')).toBeTrue();

      passwordControl?.setValue('12345678');
      expect(passwordControl?.hasError('minlength')).toBeFalse();
    });

    it('should validate phone number pattern', () => {
      const phoneControl = component.registrationForm.get('phone');

      phoneControl?.setValue('invalid-phone');
      expect(phoneControl?.hasError('pattern')).toBeTrue();

      phoneControl?.setValue('1234567890');
      expect(phoneControl?.hasError('pattern')).toBeFalse();

      phoneControl?.setValue('+1234567890');
      expect(phoneControl?.hasError('pattern')).toBeFalse();
    });

    it('should validate full name minimum length', () => {
      const fullNameControl = component.registrationForm.get('fullName');

      fullNameControl?.setValue('A');
      expect(fullNameControl?.hasError('minlength')).toBeTrue();

      fullNameControl?.setValue('John Doe');
      expect(fullNameControl?.hasError('minlength')).toBeFalse();
    });
  });

  describe('Registration Submission', () => {
    beforeEach(() => {
      // Set up validated store code and valid form
      component.storeCodeValidated = true;
      component.shopName = 'Test Shop';
      component.storeCodeForm.get('storeCode')?.setValue('1234');

      component.registrationForm.patchValue({
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123',
        phone: '1234567890',
        preferredPaymentMethod: 'Venmo',
        paymentDetails: '@johndoe'
      });
    });

    it('should submit registration successfully', async () => {
      const mockResult = { success: true };
      authService.registerProvider.and.returnValue(Promise.resolve(mockResult));

      await component.onSubmit();

      expect(authService.registerProvider).toHaveBeenCalledWith({
        storeCode: '1234',
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123',
        phone: '1234567890',
        preferredPaymentMethod: 'Venmo',
        paymentDetails: '@johndoe'
      });

      expect(router.navigate).toHaveBeenCalledWith(['/register/success'], {
        queryParams: {
          type: 'provider',
          shopName: 'Test Shop',
          email: 'john@example.com',
          fullName: 'John Doe'
        }
      });
    });

    it('should handle registration failure', async () => {
      const mockResult = {
        success: false,
        message: 'Registration failed',
        errors: ['Email already exists']
      };
      authService.registerProvider.and.returnValue(Promise.resolve(mockResult));

      await component.onSubmit();

      expect(component.registrationError).toBe('Registration failed');
      expect(component.registrationErrors).toEqual(['Email already exists']);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should handle registration exception', async () => {
      authService.registerProvider.and.returnValue(Promise.reject(new Error('Network error')));

      await component.onSubmit();

      expect(component.registrationError).toBe('Network error');
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should not submit if form is invalid', async () => {
      component.registrationForm.get('email')?.setValue('invalid-email');

      await component.onSubmit();

      expect(authService.registerProvider).not.toHaveBeenCalled();
    });

    it('should handle optional fields correctly', async () => {
      component.registrationForm.patchValue({
        phone: '',
        preferredPaymentMethod: '',
        paymentDetails: ''
      });

      const mockResult = { success: true };
      authService.registerProvider.and.returnValue(Promise.resolve(mockResult));

      await component.onSubmit();

      expect(authService.registerProvider).toHaveBeenCalledWith({
        storeCode: '1234',
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123',
        phone: undefined,
        preferredPaymentMethod: undefined,
        paymentDetails: undefined
      });
    });
  });

  describe('Store Code Reset', () => {
    it('should reset all form data and state', () => {
      // Set up some state
      component.storeCodeValidated = true;
      component.shopName = 'Test Shop';
      component.storeCodeError = 'Some error';
      component.registrationError = 'Registration error';
      component.registrationErrors = ['Error 1'];
      component.storeCodeForm.get('storeCode')?.setValue('1234');
      component.registrationForm.get('fullName')?.setValue('John Doe');

      component.resetStoreCode();

      expect(component.storeCodeValidated).toBeFalse();
      expect(component.shopName).toBe('');
      expect(component.storeCodeError).toBe('');
      expect(component.registrationError).toBe('');
      expect(component.registrationErrors).toEqual([]);
      expect(component.storeCodeForm.get('storeCode')?.value).toBeNull();
      expect(component.registrationForm.get('fullName')?.value).toBeNull();
    });
  });

  describe('Payment Method Helpers', () => {
    beforeEach(() => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Venmo');
    });

    it('should return correct placeholder for Venmo', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Venmo');
      expect(component.getPaymentPlaceholder()).toBe('@username');
    });

    it('should return correct placeholder for PayPal', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('PayPal');
      expect(component.getPaymentPlaceholder()).toBe('email@example.com');
    });

    it('should return correct placeholder for Zelle', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Zelle');
      expect(component.getPaymentPlaceholder()).toBe('email@example.com or phone number');
    });

    it('should return correct placeholder for Bank Transfer', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Bank Transfer');
      expect(component.getPaymentPlaceholder()).toBe('Account details');
    });

    it('should return correct placeholder for Check', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Check');
      expect(component.getPaymentPlaceholder()).toBe('Mailing address');
    });

    it('should return default placeholder for unknown method', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Unknown');
      expect(component.getPaymentPlaceholder()).toBe('Payment details');
    });

    it('should return correct help text for Venmo', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Venmo');
      expect(component.getPaymentHelpText()).toBe('Enter your Venmo username (e.g., @john-doe)');
    });

    it('should return correct help text for PayPal', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('PayPal');
      expect(component.getPaymentHelpText()).toBe('Enter the email address associated with your PayPal account');
    });

    it('should return correct help text for Zelle', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Zelle');
      expect(component.getPaymentHelpText()).toBe('Enter email or phone number registered with Zelle');
    });

    it('should return correct help text for Bank Transfer', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Bank Transfer');
      expect(component.getPaymentHelpText()).toBe('Bank account information will be collected securely later');
    });

    it('should return correct help text for Check', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Check');
      expect(component.getPaymentHelpText()).toBe('Enter your mailing address for check delivery');
    });

    it('should return correct help text for Cash', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Cash');
      expect(component.getPaymentHelpText()).toBe('No additional details needed for cash pickup');
    });

    it('should return default help text for unknown method', () => {
      component.registrationForm.get('preferredPaymentMethod')?.setValue('Unknown');
      expect(component.getPaymentHelpText()).toBe('Enter relevant payment information');
    });
  });

  describe('Component State', () => {
    it('should handle loading states correctly', () => {
      expect(component.isValidatingStoreCode).toBeFalse();
      expect(component.isSubmitting).toBeFalse();
    });

    it('should reset errors when validation starts', async () => {
      component.storeCodeError = 'Previous error';
      component.storeCodeForm.get('storeCode')?.setValue('1234');

      authService.validateStoreCode.and.returnValue(new Promise(resolve => {
        setTimeout(() => resolve({ isValid: true, shopName: 'Test Shop' }), 100);
      }));

      const validationPromise = component.validateStoreCode();
      expect(component.storeCodeError).toBe('');
      await validationPromise;
    });

    it('should reset registration errors when submission starts', async () => {
      // Setup valid state
      component.storeCodeValidated = true;
      component.storeCodeForm.get('storeCode')?.setValue('1234');
      component.registrationForm.patchValue({
        fullName: 'John Doe',
        email: 'john@example.com',
        password: 'password123'
      });

      component.registrationError = 'Previous error';
      component.registrationErrors = ['Previous errors'];

      authService.registerProvider.and.returnValue(new Promise(resolve => {
        setTimeout(() => resolve({ success: true }), 100);
      }));

      const submitPromise = component.onSubmit();
      expect(component.registrationError).toBe('');
      expect(component.registrationErrors).toEqual([]);
      await submitPromise;
    });
  });
});