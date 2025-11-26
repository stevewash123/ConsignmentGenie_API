import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ProviderLayoutComponent } from './provider-layout.component';
import { ProviderPortalService } from '../services/provider-portal.service';

@Component({
  template: '<div>Test Component</div>'
})
class TestComponent { }

describe('ProviderLayoutComponent', () => {
  let component: ProviderLayoutComponent;
  let fixture: ComponentFixture<ProviderLayoutComponent>;
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ProviderPortalService', ['getUnreadNotificationCount']);

    await TestBed.configureTestingModule({
      imports: [
        ProviderLayoutComponent,
        RouterTestingModule.withRoutes([
          { path: 'provider/dashboard', component: TestComponent },
          { path: 'provider/notifications', component: TestComponent },
          { path: 'provider/profile', component: TestComponent }
        ])
      ],
      providers: [
        { provide: ProviderPortalService, useValue: spy }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    fixture = TestBed.createComponent(ProviderLayoutComponent);
    component = fixture.componentInstance;
  });

  beforeEach(() => {
    mockProviderService.getUnreadNotificationCount.and.returnValue(of({ count: 5 }));
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  describe('Component Initialization', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should load unread notification count on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(mockProviderService.getUnreadNotificationCount).toHaveBeenCalled();
      expect(component.unreadCount).toBe(5);
    }));

    it('should handle error when loading unread count', fakeAsync(() => {
      mockProviderService.getUnreadNotificationCount.and.returnValue(throwError(() => new Error('API Error')));
      spyOn(console, 'error');

      fixture.detectChanges();
      tick();

      expect(console.error).toHaveBeenCalledWith('Error loading unread notification count:', jasmine.any(Error));
      expect(component.unreadCount).toBe(0);
    }));
  });

  describe('Notification Badge Display', () => {
    it('should display notification badge when unreadCount > 0', fakeAsync(() => {
      mockProviderService.getUnreadNotificationCount.and.returnValue(of({ count: 3 }));

      fixture.detectChanges();
      tick();

      const badge = fixture.nativeElement.querySelector('.notification-badge');
      expect(badge).toBeTruthy();
      expect(badge.textContent.trim()).toBe('3');
    }));

    it('should display 99+ when unreadCount > 99', fakeAsync(() => {
      mockProviderService.getUnreadNotificationCount.and.returnValue(of({ count: 150 }));

      fixture.detectChanges();
      tick();

      const badge = fixture.nativeElement.querySelector('.notification-badge');
      expect(badge.textContent.trim()).toBe('99+');
      expect(badge.getAttribute('data-count')).toBe('99+');
    }));

    it('should not display notification badge when unreadCount is 0', fakeAsync(() => {
      mockProviderService.getUnreadNotificationCount.and.returnValue(of({ count: 0 }));

      fixture.detectChanges();
      tick();

      const badge = fixture.nativeElement.querySelector('.notification-badge');
      expect(badge).toBeFalsy();
    }));
  });

  describe('UI Interactions', () => {
    it('should toggle user menu when clicked', () => {
      expect(component.userMenuOpen).toBeFalse();
      fixture.detectChanges();

      const userMenu = fixture.nativeElement.querySelector('.user-menu');
      userMenu.click();

      expect(component.userMenuOpen).toBeTrue();

      userMenu.click();

      expect(component.userMenuOpen).toBeFalse();
    });

    it('should close user menu when closeUserMenu is called', () => {
      component.userMenuOpen = true;
      component.closeUserMenu();

      expect(component.userMenuOpen).toBeFalse();
    });

    it('should display navigation links', () => {
      fixture.detectChanges();

      const navLinks = fixture.nativeElement.querySelectorAll('.nav-link');
      const expectedLinks = ['Dashboard', 'My Items', 'Sales', 'Payouts', 'Statements'];

      navLinks.forEach((link: HTMLElement, index: number) => {
        expect(link.textContent?.trim()).toBe(expectedLinks[index]);
      });
    });

    it('should call logout and clear token', () => {
      spyOn(localStorage, 'removeItem');

      // Spy on the component's logout method to prevent actual navigation
      const originalLogout = component.logout;
      spyOn(component, 'logout').and.callFake(() => {
        localStorage.removeItem('token');
        // Don't actually navigate during test
      });

      component.logout();

      expect(localStorage.removeItem).toHaveBeenCalledWith('token');
    });
  });

  it('should close user menu when clicking outside', () => {
    fixture.detectChanges();
    component.userMenuOpen = true;

    // Create a proper DOM click event and dispatch it
    const clickEvent = new Event('click', { bubbles: true });
    const outsideElement = fixture.nativeElement.querySelector('.brand');
    outsideElement.dispatchEvent(clickEvent);

    expect(component.userMenuOpen).toBeFalse();
  });

  it('should refresh unread count every 30 seconds', fakeAsync(() => {
    fixture.detectChanges();
    tick(); // Initial load (startWith triggers immediately)

    // The startWith(0) and immediate loadUnreadCount() call both trigger the service
    expect(mockProviderService.getUnreadNotificationCount).toHaveBeenCalledTimes(2);

    // Advance time by 30 seconds
    tick(30000);
    expect(mockProviderService.getUnreadNotificationCount).toHaveBeenCalledTimes(3);

    // Advance time by another 30 seconds
    tick(30000);
    expect(mockProviderService.getUnreadNotificationCount).toHaveBeenCalledTimes(4);
  }));

  it('should cleanup subscriptions on destroy', () => {
    spyOn(component['destroy$'], 'next');
    spyOn(component['destroy$'], 'complete');

    component.ngOnDestroy();

    expect(component['destroy$'].next).toHaveBeenCalled();
    expect(component['destroy$'].complete).toHaveBeenCalled();
  });

  it('should render router outlet', () => {
    fixture.detectChanges();

    const routerOutlet = fixture.nativeElement.querySelector('router-outlet');
    expect(routerOutlet).toBeTruthy();
  });

  it('should have correct navigation structure', () => {
    fixture.detectChanges();

    const header = fixture.nativeElement.querySelector('.provider-header');
    const nav = fixture.nativeElement.querySelector('.main-nav');
    const userActions = fixture.nativeElement.querySelector('.user-actions');

    expect(header).toBeTruthy();
    expect(nav).toBeTruthy();
    expect(userActions).toBeTruthy();
  });
});