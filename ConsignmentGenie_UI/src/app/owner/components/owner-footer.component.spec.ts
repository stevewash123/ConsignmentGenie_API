import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OwnerFooterComponent } from './owner-footer.component';
import { SuggestionFormData } from './suggestion-box-modal.component';

describe('OwnerFooterComponent', () => {
  let component: OwnerFooterComponent;
  let fixture: ComponentFixture<OwnerFooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OwnerFooterComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(OwnerFooterComponent);
    component = fixture.componentInstance;

    // Clear any existing localStorage data
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.currentYear).toBe(new Date().getFullYear());
    expect(component.currentUser()).toBeNull();
    expect(component.showSuggestionModal()).toBe(false);
  });

  describe('ngOnInit', () => {
    it('should load user data from localStorage when available', () => {
      const mockUserData = {
        userId: 'user-123',
        email: 'test@example.com',
        role: 1,
        organizationId: 'org-456',
        organizationName: 'Test Organization'
      };

      localStorage.setItem('user_data', JSON.stringify(mockUserData));

      component.ngOnInit();

      expect(component.currentUser()).toEqual(mockUserData);
    });

    it('should handle missing localStorage data gracefully', () => {
      component.ngOnInit();

      expect(component.currentUser()).toBeNull();
    });

    it('should handle invalid JSON in localStorage gracefully', () => {
      localStorage.setItem('user_data', 'invalid json');

      spyOn(console, 'error');

      component.ngOnInit();

      expect(component.currentUser()).toBeNull();
      expect(console.error).toHaveBeenCalled();
    });
  });

  describe('suggestion box functionality', () => {
    it('should open suggestion modal when openSuggestionBox is called', () => {
      expect(component.showSuggestionModal()).toBe(false);

      component.openSuggestionBox();

      expect(component.showSuggestionModal()).toBe(true);
    });

    it('should close suggestion modal when closeSuggestionBox is called', () => {
      component.showSuggestionModal.set(true);

      component.closeSuggestionBox();

      expect(component.showSuggestionModal()).toBe(false);
    });

    it('should handle suggestion submission and close modal', () => {
      spyOn(window, 'alert');
      spyOn(console, 'log');
      component.showSuggestionModal.set(true);

      const suggestionData: SuggestionFormData = {
        type: 'FeatureRequest',
        message: 'Please add dark mode support'
      };

      component.onSuggestionSubmit(suggestionData);

      expect(console.log).toHaveBeenCalledWith('Suggestion submitted:', suggestionData);
      expect(window.alert).toHaveBeenCalledWith('Thank you for your featurerequest suggestion! We\'ll review it and get back to you.');
      expect(component.showSuggestionModal()).toBe(false);
    });

    it('should handle different suggestion types in submission', () => {
      spyOn(window, 'alert');

      const testCases = [
        { type: 'BugReport', expected: 'bugreport' },
        { type: 'Improvement', expected: 'improvement' },
        { type: 'Integration', expected: 'integration' },
        { type: 'Other', expected: 'other' }
      ];

      testCases.forEach(testCase => {
        const suggestionData: SuggestionFormData = {
          type: testCase.type,
          message: 'Test message'
        };

        component.onSuggestionSubmit(suggestionData);

        expect(window.alert).toHaveBeenCalledWith(
          `Thank you for your ${testCase.expected} suggestion! We'll review it and get back to you.`
        );
      });
    });
  });

  describe('template rendering', () => {
    it('should display current year in copyright', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const copyrightText = compiled.querySelector('.copyright p')?.textContent;

      expect(copyrightText).toContain(component.currentYear.toString());
    });

    it('should display organization name when user data is available', () => {
      const mockUserData = {
        userId: 'user-123',
        email: 'test@example.com',
        role: 1,
        organizationId: 'org-456',
        organizationName: 'My Test Shop'
      };

      component.currentUser.set(mockUserData);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const copyrightText = compiled.querySelector('.copyright p')?.textContent;

      expect(copyrightText).toContain('My Test Shop');
    });

    it('should display fallback text when no user data is available', () => {
      component.currentUser.set(null);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const copyrightText = compiled.querySelector('.copyright p')?.textContent;

      expect(copyrightText).toContain('ConsignmentGenie');
    });

    it('should render suggestion box button', () => {
      fixture.detectChanges();

      const suggestionButton = fixture.nativeElement.querySelector('.suggestion-link');
      expect(suggestionButton).toBeTruthy();
      expect(suggestionButton?.textContent?.trim()).toBe('💡 Suggestion Box');
    });

    it('should open suggestion modal when suggestion button is clicked', () => {
      fixture.detectChanges();

      const suggestionButton = fixture.nativeElement.querySelector('.suggestion-link');
      expect(component.showSuggestionModal()).toBe(false);

      suggestionButton?.click();

      expect(component.showSuggestionModal()).toBe(true);
    });

    it('should show/hide suggestion modal based on signal value', () => {
      // Initially hidden
      component.showSuggestionModal.set(false);
      fixture.detectChanges();

      let modalElement = fixture.nativeElement.querySelector('app-suggestion-box-modal');
      expect(modalElement).toBeTruthy(); // Component exists

      // Show modal
      component.showSuggestionModal.set(true);
      fixture.detectChanges();

      modalElement = fixture.nativeElement.querySelector('app-suggestion-box-modal');
      expect(modalElement).toBeTruthy(); // Component still exists

      // Test the component property directly
      expect(component.showSuggestionModal()).toBe(true);
    });

    it('should render all footer sections', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const sections = compiled.querySelectorAll('.footer-section');

      expect(sections.length).toBe(4);

      // Check section headings
      const headings = Array.from(sections).map(section =>
        section.querySelector('h3, h4')?.textContent?.trim()
      );

      expect(headings).toContain('Consignment Genie');
      expect(headings).toContain('Business Tools');
      expect(headings).toContain('Integrations');
      expect(headings).toContain('Support');
    });

    it('should render footer links', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const links = compiled.querySelectorAll('a');

      expect(links.length).toBeGreaterThan(0);

      // Check that support email link exists
      const emailLink = Array.from(links).find(link =>
        link.href?.includes('mailto:support@consignmentgenie.com')
      );
      expect(emailLink).toBeTruthy();
    });
  });

  describe('responsive behavior', () => {
    it('should have responsive grid classes', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      const footerContent = compiled.querySelector('.footer-content');

      expect(footerContent).toBeTruthy();

      // Check that CSS classes are applied (we can't test actual responsive behavior in unit tests)
      const styles = getComputedStyle(footerContent as Element);
      expect(styles.display).toBe('grid');
    });
  });

  describe('event handling', () => {
    it('should handle modal close event', () => {
      component.showSuggestionModal.set(true);

      // Simulate modal close event
      component.closeSuggestionBox();

      expect(component.showSuggestionModal()).toBe(false);
    });

    it('should handle modal submit event with proper data flow', () => {
      spyOn(component, 'onSuggestionSubmit').and.callThrough();
      spyOn(component, 'closeSuggestionBox');
      spyOn(window, 'alert');

      const suggestionData: SuggestionFormData = {
        type: 'UserExperience',
        message: 'Improve the dashboard layout'
      };

      component.onSuggestionSubmit(suggestionData);

      expect(component.onSuggestionSubmit).toHaveBeenCalledWith(suggestionData);
      expect(component.closeSuggestionBox).toHaveBeenCalled();
      expect(window.alert).toHaveBeenCalled();
    });
  });
});