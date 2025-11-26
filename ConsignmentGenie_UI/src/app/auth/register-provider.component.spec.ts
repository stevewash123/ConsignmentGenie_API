import { ComponentFixture, TestBed, fakeAsync, tick, flushMicrotasks } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { RegisterProviderComponent } from './register-provider.component';
import { AuthService } from '../services/auth.service';

describe('RegisterProviderComponent', () => {
  let component: RegisterProviderComponent;
  let fixture: ComponentFixture<RegisterProviderComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['registerProvider', 'validateStoreCode']);

    await TestBed.configureTestingModule({
      imports: [ RegisterProviderComponent, ReactiveFormsModule, RouterTestingModule ],
      providers: [
        { provide: AuthService, useValue: mockAuthService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterProviderComponent);
    component = fixture.componentInstance;

    // Get the router and spy on navigate
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    spyOn(mockRouter, 'navigate').and.returnValue(Promise.resolve(true));

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with invalid form', () => {
    expect(component.registrationForm.valid).toBeFalsy();
  });

  it('should navigate on successful registration', fakeAsync(() => {
    // Setup mock to return successful observable
    mockAuthService.registerProvider.and.returnValue(of({ success: true }));

    // Fill form with valid data
    component.registrationForm.patchValue({
      email: 'test@example.com',
      password: 'Password123!',
      fullName: 'Test User',
      phone: '' // Use a valid phone number format
    });

    // Mark all form fields as valid by removing validation errors
    component.registrationForm.markAllAsTouched();
    component.registrationForm.updateValueAndValidity();

    // Set store code as validated and shop name
    component.storeCodeValidated = true;
    component.shopName = 'Test Shop';
    component.storeCodeForm.patchValue({ storeCode: 'ABC123' });

    // Verify form is valid before submitting
    expect(component.registrationForm.valid).toBe(true);

    // Submit and wait for async operation to complete
    component.onSubmit();
    tick(); // Wait for microtasks to complete

    // Verify navigation was called
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/register/success'], {
      queryParams: {
        type: 'provider',
        shopName: 'Test Shop',
        email: 'test@example.com',
        fullName: 'Test User'
      }
    });
  }));

  it('should display error on registration failure', fakeAsync(() => {
    const errorResponse = {
      success: false,
      message: 'Email already exists',
      errors: ['Email is already in use']
    };

    mockAuthService.registerProvider.and.returnValue(of(errorResponse));

    component.registrationForm.patchValue({
      email: 'test@example.com',
      password: 'Password123!',
      fullName: 'Test User',
      phone: ''
    });

    // Mark form as valid and touched
    component.registrationForm.markAllAsTouched();
    component.registrationForm.updateValueAndValidity();

    component.storeCodeValidated = true;
    component.shopName = 'Test Shop';
    component.storeCodeForm.patchValue({ storeCode: 'ABC123' });

    // Verify form is valid before submitting
    expect(component.registrationForm.valid).toBe(true);

    component.onSubmit();
    tick(); // Wait for microtasks to complete
    fixture.detectChanges(); // Update the DOM

    expect(component.registrationError).toBe('Email already exists');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  }));

  it('should show generic error when exception occurs', fakeAsync(() => {
    mockAuthService.registerProvider.and.returnValue(
      throwError(() => new Error('Network error'))
    );

    component.registrationForm.patchValue({
      email: 'test@example.com',
      password: 'Password123!',
      fullName: 'Test User',
      phone: ''
    });

    // Mark form as valid and touched
    component.registrationForm.markAllAsTouched();
    component.registrationForm.updateValueAndValidity();

    component.storeCodeValidated = true;
    component.shopName = 'Test Shop';
    component.storeCodeForm.patchValue({ storeCode: 'ABC123' });

    // Verify form is valid before submitting
    expect(component.registrationForm.valid).toBe(true);

    // Call onSubmit - the component handles the promise rejection internally
    try {
      component.onSubmit();
      tick(); // Wait for microtasks to complete
    } catch (error) {
      // Ignore the error since the component handles it
    }

    fixture.detectChanges(); // Update the DOM

    expect(component.registrationError).toBe('Network error');
  }));

  it('should validate required fields', () => {
    const form = component.registrationForm;

    expect(form.get('email')?.hasError('required')).toBeTruthy();
    expect(form.get('password')?.hasError('required')).toBeTruthy();
    expect(form.get('fullName')?.hasError('required')).toBeTruthy();

    const storeCodeForm = component.storeCodeForm;
    expect(storeCodeForm.get('storeCode')?.hasError('required')).toBeTruthy();
  });

  it('should validate email format', () => {
    const emailControl = component.registrationForm.get('email');

    emailControl?.setValue('invalid-email');
    expect(emailControl?.hasError('email')).toBeTruthy();

    emailControl?.setValue('valid@email.com');
    expect(emailControl?.hasError('email')).toBeFalsy();
  });

  it('should validate password minimum length', () => {
    const passwordControl = component.registrationForm.get('password');

    passwordControl?.setValue('short');
    expect(passwordControl?.hasError('minlength')).toBeTruthy();

    passwordControl?.setValue('LongEnough123!');
    expect(passwordControl?.hasError('minlength')).toBeFalsy();
  });
});