import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { UnauthorizedComponent } from './unauthorized.component';

describe('UnauthorizedComponent', () => {
  let component: UnauthorizedComponent;
  let fixture: ComponentFixture<UnauthorizedComponent>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [UnauthorizedComponent],
      providers: [
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UnauthorizedComponent);
    component = fixture.componentInstance;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Clear localStorage before each test
    localStorage.clear();

    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('goToDashboard', () => {
    it('should navigate to owner dashboard for owner role', () => {
      const userData = { role: 1 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should navigate to owner dashboard for manager role', () => {
      const userData = { role: 2 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should navigate to owner dashboard for staff role', () => {
      const userData = { role: 3 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should navigate to owner dashboard for cashier role', () => {
      const userData = { role: 4 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should navigate to owner dashboard for accountant role', () => {
      const userData = { role: 5 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/owner/dashboard']);
    });

    it('should navigate to provider dashboard for provider role', () => {
      const userData = { role: 6 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/provider/dashboard']);
    });

    it('should navigate to customer dashboard for customer role', () => {
      const userData = { role: 7 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/customer/dashboard']);
    });

    it('should navigate to login for unknown role', () => {
      const userData = { role: 999 };
      localStorage.setItem('user_data', JSON.stringify(userData));

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should navigate to login when user_data is not found', () => {
      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should navigate to login when user_data is invalid JSON', () => {
      localStorage.setItem('user_data', 'invalid-json');

      component.goToDashboard();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle JSON parsing error gracefully', () => {
      localStorage.setItem('user_data', '{"role":}'); // Invalid JSON

      expect(() => component.goToDashboard()).not.toThrow();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('logout', () => {
    it('should remove auth tokens and navigate to login', () => {
      localStorage.setItem('auth_token', 'test-token');
      localStorage.setItem('user_data', JSON.stringify({ role: 1 }));

      component.logout();

      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('user_data')).toBeNull();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should navigate to login even when no tokens exist', () => {
      component.logout();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('template rendering', () => {
    it('should render error icon', () => {
      const errorIcon = fixture.nativeElement.querySelector('.error-icon');
      expect(errorIcon).toBeTruthy();
      expect(errorIcon.textContent).toContain('🚫');
    });

    it('should render "Access Denied" title', () => {
      const title = fixture.nativeElement.querySelector('h1');
      expect(title).toBeTruthy();
      expect(title.textContent).toContain('Access Denied');
    });

    it('should render permission message', () => {
      const message = fixture.nativeElement.querySelector('p');
      expect(message).toBeTruthy();
      expect(message.textContent).toContain("You don't have permission to access this page");
    });

    it('should render help text', () => {
      const helpText = fixture.nativeElement.querySelector('.help-text');
      expect(helpText).toBeTruthy();
      expect(helpText.textContent).toContain('Please contact your administrator');
    });

    it('should render dashboard button', () => {
      const dashboardBtn = fixture.nativeElement.querySelector('.btn-primary');
      expect(dashboardBtn).toBeTruthy();
      expect(dashboardBtn.textContent.trim()).toBe('Go to Dashboard');
    });

    it('should render logout button', () => {
      const logoutBtn = fixture.nativeElement.querySelector('.btn-secondary');
      expect(logoutBtn).toBeTruthy();
      expect(logoutBtn.textContent.trim()).toBe('Logout');
    });

    it('should call goToDashboard when dashboard button is clicked', () => {
      spyOn(component, 'goToDashboard');
      const dashboardBtn = fixture.nativeElement.querySelector('.btn-primary');

      dashboardBtn.click();

      expect(component.goToDashboard).toHaveBeenCalled();
    });

    it('should call logout when logout button is clicked', () => {
      spyOn(component, 'logout');
      const logoutBtn = fixture.nativeElement.querySelector('.btn-secondary');

      logoutBtn.click();

      expect(component.logout).toHaveBeenCalled();
    });
  });
});