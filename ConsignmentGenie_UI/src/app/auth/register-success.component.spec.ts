import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { RegisterSuccessComponent } from './register-success.component';

describe('RegisterSuccessComponent', () => {
  let component: RegisterSuccessComponent;
  let fixture: ComponentFixture<RegisterSuccessComponent>;
  let activatedRoute: ActivatedRoute;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterSuccessComponent, RouterTestingModule],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({
              type: 'owner',
              shopName: 'Test Shop',
              email: 'test@example.com',
              fullName: 'John Doe'
            })
          }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterSuccessComponent);
    component = fixture.componentInstance;
    activatedRoute = TestBed.inject(ActivatedRoute);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Query Parameter Handling', () => {
    it('should initialize signals from query parameters', () => {
      fixture.detectChanges();

      expect(component.registrationType()).toBe('owner');
      expect(component.shopName()).toBe('Test Shop');
      expect(component.userEmail()).toBe('test@example.com');
      expect(component.fullName()).toBe('John Doe');
    });

    it('should handle missing query parameters', () => {
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [RegisterSuccessComponent, RouterTestingModule],
        providers: [
          {
            provide: ActivatedRoute,
            useValue: {
              queryParams: of({})
            }
          }
        ]
      });

      fixture = TestBed.createComponent(RegisterSuccessComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      expect(component.registrationType()).toBe('');
      expect(component.shopName()).toBe('');
      expect(component.userEmail()).toBe('');
      expect(component.fullName()).toBe('');
    });

    it('should handle provider registration type', () => {
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [RegisterSuccessComponent, RouterTestingModule],
        providers: [
          {
            provide: ActivatedRoute,
            useValue: {
              queryParams: of({
                type: 'provider',
                shopName: 'Provider Shop',
                email: 'provider@example.com',
                fullName: 'Jane Provider'
              })
            }
          }
        ]
      });

      fixture = TestBed.createComponent(RegisterSuccessComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();

      expect(component.registrationType()).toBe('provider');
      expect(component.shopName()).toBe('Provider Shop');
      expect(component.userEmail()).toBe('provider@example.com');
      expect(component.fullName()).toBe('Jane Provider');
    });
  });

  describe('getFirstName Method', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should extract first name from full name', () => {
      component.fullName.set('John Michael Doe');
      expect(component.getFirstName()).toBe('John');
    });

    it('should handle single name', () => {
      component.fullName.set('John');
      expect(component.getFirstName()).toBe('John');
    });

    it('should extract name from email when full name is not available', () => {
      component.fullName.set('');
      component.userEmail.set('john.doe@example.com');
      expect(component.getFirstName()).toBe('john.doe');
    });

    it('should return "there" when neither full name nor email are available', () => {
      component.fullName.set('');
      component.userEmail.set('');
      expect(component.getFirstName()).toBe('there');
    });

    it('should prefer full name over email', () => {
      component.fullName.set('John Doe');
      component.userEmail.set('different@example.com');
      expect(component.getFirstName()).toBe('John');
    });

    it('should handle empty full name string', () => {
      component.fullName.set('   ');
      component.userEmail.set('test@example.com');
      expect(component.getFirstName()).toBe('test');
    });
  });

  describe('Template Rendering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should display success message', () => {
      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.querySelector('h1').textContent).toContain('Account Created!');
    });

    it('should show owner-specific content when type is owner', () => {
      component.registrationType.set('owner');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).toContain('Your account is pending approval from our team');
      expect(compiled.textContent).toContain('Our team will review your shop registration');
      expect(compiled.textContent).toContain('you\'ll get your unique store code for providers');
    });

    it('should show provider-specific content when type is provider', () => {
      component.registrationType.set('provider');
      component.shopName.set('Test Shop');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).toContain('Your account is pending approval from Test Shop');
      expect(compiled.textContent).toContain('The shop owner will review your request');
      expect(compiled.textContent).toContain('you can access your Provider Portal');
    });

    it('should display user email in confirmation message', () => {
      component.userEmail.set('test@example.com');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).toContain('We\'ve sent a confirmation to test@example.com');
    });

    it('should display personalized greeting', () => {
      component.fullName.set('John Doe');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).toContain('Thanks for registering, John!');
    });

    it('should show navigation buttons', () => {
      const compiled = fixture.debugElement.nativeElement;
      const homeBtn = compiled.querySelector('a[routerLink="/"]');
      const loginBtn = compiled.querySelector('a[routerLink="/login"]');

      expect(homeBtn).toBeTruthy();
      expect(homeBtn.textContent).toContain('Back to Home');
      expect(loginBtn).toBeTruthy();
      expect(loginBtn.textContent).toContain('Try to Log In');
    });

    it('should not show owner-specific content for provider', () => {
      component.registrationType.set('provider');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).not.toContain('Our team will review your shop registration');
      expect(compiled.textContent).not.toContain('you\'ll get your unique store code for providers');
    });

    it('should not show provider-specific content for owner', () => {
      component.registrationType.set('owner');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.textContent).not.toContain('The shop owner will review your request');
      expect(compiled.textContent).not.toContain('you can access your Provider Portal');
    });

    it('should handle unknown registration type gracefully', () => {
      component.registrationType.set('unknown');
      fixture.detectChanges();

      const compiled = fixture.debugElement.nativeElement;
      // Should not show type-specific content, but basic success message should still appear
      expect(compiled.querySelector('h1').textContent).toContain('Account Created!');
      expect(compiled.textContent).not.toContain('Our team will review');
      expect(compiled.textContent).not.toContain('The shop owner will review');
    });
  });

  describe('Component Structure', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should have proper CSS classes applied', () => {
      const compiled = fixture.debugElement.nativeElement;
      expect(compiled.querySelector('.success-page')).toBeTruthy();
      expect(compiled.querySelector('.container')).toBeTruthy();
      expect(compiled.querySelector('.success-card')).toBeTruthy();
      expect(compiled.querySelector('.success-icon')).toBeTruthy();
      expect(compiled.querySelector('.actions')).toBeTruthy();
    });

    it('should display success icon', () => {
      const compiled = fixture.debugElement.nativeElement;
      const icon = compiled.querySelector('.success-icon');
      expect(icon.textContent.trim()).toBe('✅');
    });

    it('should have proper button styling', () => {
      const compiled = fixture.debugElement.nativeElement;
      const homeBtn = compiled.querySelector('.home-btn');
      const loginBtn = compiled.querySelector('.login-btn');

      expect(homeBtn).toBeTruthy();
      expect(loginBtn).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should have proper heading structure', () => {
      const compiled = fixture.debugElement.nativeElement;
      const h1 = compiled.querySelector('h1');
      const h3 = compiled.querySelector('h3');

      expect(h1).toBeTruthy();
      expect(h3).toBeTruthy();
    });

    it('should have descriptive link text', () => {
      const compiled = fixture.debugElement.nativeElement;
      const homeBtn = compiled.querySelector('a[routerLink="/"]');
      const loginBtn = compiled.querySelector('a[routerLink="/login"]');

      expect(homeBtn.textContent.trim()).toBe('Back to Home');
      expect(loginBtn.textContent.trim()).toBe('Try to Log In');
    });
  });

  describe('Responsive Design Elements', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should include responsive container classes', () => {
      const compiled = fixture.debugElement.nativeElement;
      const container = compiled.querySelector('.container');

      // Test that responsive classes are applied through component styles
      expect(container).toBeTruthy();
    });

    it('should have flexible action buttons layout', () => {
      const compiled = fixture.debugElement.nativeElement;
      const actionsDiv = compiled.querySelector('.actions');

      expect(actionsDiv).toBeTruthy();
      // The CSS should handle responsive behavior
    });
  });
});