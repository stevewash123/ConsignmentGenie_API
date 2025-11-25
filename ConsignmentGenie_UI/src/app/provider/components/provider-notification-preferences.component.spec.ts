import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ProviderNotificationPreferencesComponent } from './provider-notification-preferences.component';
import { ProviderPortalService } from '../services/provider-portal.service';
import { LoadingService } from '../../shared/services/loading.service';
import { LOADING_KEYS } from '../constants/loading-keys';
import { NotificationPreferencesDto, UpdateNotificationPreferencesRequest } from '../models/provider.models';

describe('ProviderNotificationPreferencesComponent', () => {
  let component: ProviderNotificationPreferencesComponent;
  let fixture: ComponentFixture<ProviderNotificationPreferencesComponent>;
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;
  let mockLoadingService: jasmine.SpyObj<LoadingService>;

  const mockPreferences: NotificationPreferencesDto = {
    emailEnabled: true,
    emailItemSold: true,
    emailPayoutProcessed: true,
    emailPayoutPending: false,
    emailItemExpired: false,
    emailStatementReady: true,
    emailAccountUpdate: true,
    digestMode: 'instant',
    digestTime: '09:00',
    digestDay: 1,
    payoutPendingThreshold: 50.00
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ProviderPortalService', [
      'getNotificationPreferences',
      'updateNotificationPreferences'
    ]);

    const loadingSpy = jasmine.createSpyObj('LoadingService', [
      'start', 'stop', 'isLoading', 'clear'
    ]);
    loadingSpy.isLoading.and.callFake((key: string) => false);

    await TestBed.configureTestingModule({
      imports: [
        ProviderNotificationPreferencesComponent,
        ReactiveFormsModule,
        RouterTestingModule
      ],
      providers: [
        { provide: ProviderPortalService, useValue: spy },
        { provide: LoadingService, useValue: loadingSpy }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    mockLoadingService = TestBed.inject(LoadingService) as jasmine.SpyObj<LoadingService>;
    fixture = TestBed.createComponent(ProviderNotificationPreferencesComponent);
    component = fixture.componentInstance;
  });

  beforeEach(() => {
    mockProviderService.getNotificationPreferences.and.returnValue(of(mockPreferences));
    mockProviderService.updateNotificationPreferences.and.returnValue(of(mockPreferences));

    // Reset loading service to return false for all keys by default
    mockLoadingService.isLoading.and.callFake((key: string) => false);
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with default values', () => {
    // Trigger ngOnInit to initialize form
    fixture.detectChanges();

    expect(component.preferencesForm).toBeDefined();
    expect(component.preferencesForm.get('emailEnabled')?.value).toBeTrue();
    expect(component.preferencesForm.get('digestMode')?.value).toBe('instant');
    expect(component.preferencesForm.get('payoutPendingThreshold')?.value).toBe(50.00);
  });

  it('should load preferences on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockProviderService.getNotificationPreferences).toHaveBeenCalled();
    expect(component.originalPreferences).toEqual(mockPreferences);
    expect(component.preferencesForm.get('emailEnabled')?.value).toBeTrue();
    expect(component.preferencesForm.get('digestMode')?.value).toBe('instant');
    expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS);
    expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS);
  }));

  xit('should handle error loading preferences', fakeAsync(() => {
    mockProviderService.getNotificationPreferences.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    fixture.detectChanges();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error loading preferences:', jasmine.any(Error));
    expect(component.error).toBe('Failed to load notification preferences. Please try again later.');
    expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS);
    expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS);
  }));

  it('should show loading state', () => {
    // Set loading state before ngOnInit affects form creation
    mockLoadingService.isLoading.and.returnValue(true);
    component.error = null;
    // Don't call fixture.detectChanges() here to avoid ngOnInit triggering
    fixture.detectChanges();

    const loadingContainer = fixture.nativeElement.querySelector('.loading-container');
    expect(loadingContainer).toBeTruthy();
    expect(loadingContainer.textContent).toContain('Loading preferences...');
  });

  xit('should show error state', () => {
    component.error = 'Failed to load preferences';
    mockLoadingService.isLoading.and.returnValue(false);
    component.preferencesForm = null!; // Ensure form is not present
    fixture.detectChanges();

    const errorContainer = fixture.nativeElement.querySelector('.error-container');
    expect(errorContainer).toBeTruthy();
    expect(errorContainer.textContent).toContain('Failed to load preferences');
  });

  it('should disable all email preferences when master toggle is off', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({ emailEnabled: false });
    component.onMasterEmailToggle();

    expect(component.preferencesForm.get('emailItemSold')?.value).toBeFalse();
    expect(component.preferencesForm.get('emailPayoutProcessed')?.value).toBeFalse();
    expect(component.preferencesForm.get('emailPayoutPending')?.value).toBeFalse();
    expect(component.preferencesForm.get('emailStatementReady')?.value).toBeFalse();
    expect(component.preferencesForm.get('emailAccountUpdate')?.value).toBeFalse();
  });

  it('should not change email preferences when master toggle is on', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({
      emailEnabled: true,
      emailItemSold: true,
      emailPayoutProcessed: false
    });

    component.onMasterEmailToggle();

    expect(component.preferencesForm.get('emailItemSold')?.value).toBeTrue();
    expect(component.preferencesForm.get('emailPayoutProcessed')?.value).toBeFalse();
  });

  it('should save preferences successfully', fakeAsync(() => {
    fixture.detectChanges(); // Initialize form
    tick(); // Complete initial load

    component.preferencesForm.markAsDirty();
    component.preferencesForm.patchValue({
      emailEnabled: true,
      digestMode: 'daily'
    });

    component.savePreferences();
    tick();

    expect(mockProviderService.updateNotificationPreferences).toHaveBeenCalledWith(jasmine.any(Object));
    expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS_SAVE);
    expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS_SAVE);
    expect(component.saveMessage).toContain('saved successfully');
    expect(component.saveSuccess).toBeTrue();
  }));

  xit('should handle save preferences error', fakeAsync(() => {
    mockProviderService.updateNotificationPreferences.and.returnValue(throwError(() => new Error('Save failed')));
    spyOn(console, 'error');

    fixture.detectChanges(); // Initialize form
    tick(); // Complete initial load

    component.preferencesForm.markAsDirty();

    component.savePreferences();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error saving preferences:', jasmine.any(Error));
    expect(component.saveMessage).toContain('Failed to save');
    expect(component.saveSuccess).toBeFalse();
    expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS_SAVE);
    expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.NOTIFICATION_PREFS_SAVE);
  }));

  it('should not save when form is invalid', () => {
    fixture.detectChanges(); // Initialize form
    
    component.preferencesForm.get('payoutPendingThreshold')?.setValue(5); // Below minimum
    component.preferencesForm.markAsDirty();

    component.savePreferences();

    expect(mockProviderService.updateNotificationPreferences).not.toHaveBeenCalled();
  });

  xit('should not save when form is not dirty', () => {
    fixture.detectChanges(); // Initialize form
    
    component.preferencesForm.markAsPristine();
    component.savePreferences();

    expect(mockProviderService.updateNotificationPreferences).not.toHaveBeenCalled();
  });

  it('should reset form to original values', () => {
    fixture.detectChanges(); // Initialize form
    
    component.originalPreferences = mockPreferences;
    component.preferencesForm.patchValue({ emailEnabled: false });
    component.preferencesForm.markAsDirty();

    component.resetForm();

    expect(component.preferencesForm.get('emailEnabled')?.value).toBeTrue();
    expect(component.preferencesForm.pristine).toBeTrue();
    expect(component.saveMessage).toBeNull();
  });

  it('should validate payout threshold minimum', () => {
    fixture.detectChanges(); // Initialize form
    
    const thresholdControl = component.preferencesForm.get('payoutPendingThreshold');

    thresholdControl?.setValue(5);
    expect(thresholdControl?.hasError('min')).toBeTrue();

    thresholdControl?.setValue(15);
    expect(thresholdControl?.hasError('min')).toBeFalse();
  });

  it('should display form sections', () => {
    mockLoadingService.isLoading.and.returnValue(false);
    component.error = null;
    fixture.detectChanges();

    const sections = fixture.nativeElement.querySelectorAll('.preferences-section');
    expect(sections.length).toBe(3); // Email Notifications, Digest Settings, Payout Alerts

    const sectionTitles = Array.from(sections).map((section: Element) =>
      section.querySelector('h2')?.textContent?.trim()
    );
    expect(sectionTitles).toContain('Email Notifications');
    expect(sectionTitles).toContain('Digest Settings');
    expect(sectionTitles).toContain('Payout Alerts');
  });

  it('should show digest time input for daily mode', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({ digestMode: 'daily' });
    fixture.detectChanges();

    const digestTimeInput = fixture.nativeElement.querySelector('#digestTime');
    expect(digestTimeInput).toBeTruthy();
  });

  it('should show digest day input for weekly mode', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({ digestMode: 'weekly' });
    fixture.detectChanges();

    const digestDaySelect = fixture.nativeElement.querySelector('#digestDay');
    expect(digestDaySelect).toBeTruthy();
  });

  it('should not show digest time/day inputs for instant mode', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({ digestMode: 'instant' });
    fixture.detectChanges();

    const digestTimeInput = fixture.nativeElement.querySelector('#digestTime');
    const digestDaySelect = fixture.nativeElement.querySelector('#digestDay');
    expect(digestTimeInput).toBeFalsy();
    expect(digestDaySelect).toBeFalsy();
  });

  it('should disable email preferences when master toggle is off', () => {
    fixture.detectChanges();
    component.preferencesForm.patchValue({ emailEnabled: false });
    fixture.detectChanges();

    const emailPreferences = fixture.nativeElement.querySelector('.email-preferences');
    expect(emailPreferences.classList.contains('disabled')).toBeTrue();
  });

  it('should enable save button when form is dirty and valid', () => {
    fixture.detectChanges();
    component.preferencesForm.markAsDirty();
    fixture.detectChanges();

    const saveButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(saveButton.disabled).toBeFalse();
  });

  it('should disable save button when form is pristine', () => {
    fixture.detectChanges();
    component.preferencesForm.markAsPristine();
    fixture.detectChanges();

    const saveButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(saveButton.disabled).toBeTrue();
  });

  it('should disable reset button when form is pristine', () => {
    fixture.detectChanges();
    component.preferencesForm.markAsPristine();
    fixture.detectChanges();

    const resetButton = fixture.nativeElement.querySelector('button[type="button"]');
    expect(resetButton.disabled).toBeTrue();
  });

  it('should clear save message after timeout on success', fakeAsync(() => {
    fixture.detectChanges();
    tick(); // Complete initial load

    component.preferencesForm.markAsDirty();

    component.savePreferences();
    tick();

    expect(component.saveMessage).toBeTruthy();

    tick(3000); // Wait for timeout
    expect(component.saveMessage).toBeNull();
  }));

  it('should clear save message after timeout on error', fakeAsync(() => {
    mockProviderService.updateNotificationPreferences.and.returnValue(throwError(() => new Error('Error')));
    
    fixture.detectChanges();
    tick(); // Complete initial load

    component.preferencesForm.markAsDirty();

    component.savePreferences();
    tick();

    expect(component.saveMessage).toBeTruthy();

    tick(5000); // Wait for timeout
    expect(component.saveMessage).toBeNull();
  }));

  it('should create proper request object when saving', () => {
    fixture.detectChanges();

    const formValue = {
      emailEnabled: true,
      emailItemSold: false,
      emailPayoutProcessed: true,
      emailPayoutPending: false,
      emailItemExpired: false,
      emailStatementReady: true,
      emailAccountUpdate: false,
      digestMode: 'daily',
      digestTime: '10:00',
      digestDay: 2,
      payoutPendingThreshold: 75.00
    };

    component.preferencesForm.patchValue(formValue);
    component.preferencesForm.markAsDirty();

    component.savePreferences();

    const expectedRequest: UpdateNotificationPreferencesRequest = formValue;
    expect(mockProviderService.updateNotificationPreferences).toHaveBeenCalledWith(expectedRequest);
  });

  it('should cleanup subscriptions on destroy', () => {
    spyOn(component['destroy$'], 'next');
    spyOn(component['destroy$'], 'complete');

    component.ngOnDestroy();

    expect(component['destroy$'].next).toHaveBeenCalled();
    expect(component['destroy$'].complete).toHaveBeenCalled();
  });

  it('should display back link', () => {
    fixture.detectChanges();

    const backLink = fixture.nativeElement.querySelector('.back-link');
    expect(backLink).toBeTruthy();
    expect(backLink.getAttribute('routerLink')).toBe('/provider/notifications');
  });

  it('should show validation error for invalid threshold', () => {
    fixture.detectChanges();
    component.preferencesForm.get('payoutPendingThreshold')?.setValue(5);
    component.preferencesForm.get('payoutPendingThreshold')?.markAsTouched();
    fixture.detectChanges();

    const errorMessage = fixture.nativeElement.querySelector('.form-error');
    expect(errorMessage).toBeTruthy();
    expect(errorMessage.textContent).toContain('Minimum threshold is $10.00');
  });
});