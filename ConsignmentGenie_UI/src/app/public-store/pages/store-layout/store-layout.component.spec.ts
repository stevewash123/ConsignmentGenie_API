import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { StoreLayoutComponent } from './store-layout.component';
import { PublicStoreService } from '../../services/public-store.service';
import { ShoppingCartService } from '../../../customer/services/shopping-cart.service';
import { CustomerAuthService } from '../../../customer/services/customer-auth.service';

describe('StoreLayoutComponent', () => {
  let component: StoreLayoutComponent;
  let fixture: ComponentFixture<StoreLayoutComponent>;
  let mockStoreService: jasmine.SpyObj<PublicStoreService>;
  let mockCartService: jasmine.SpyObj<ShoppingCartService>;
  let mockAuthService: jasmine.SpyObj<CustomerAuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockStoreInfo = {
    organizationId: 'org-1',
    storeName: 'Test Store',
    storeUrl: 'test-store',
    logoUrl: 'logo.jpg',
    theme: 'default',
    contactInfo: {
      email: 'contact@teststore.com',
      phone: '555-0123',
      address: '123 Main St'
    },
    storeDescription: 'A test consignment store',
    storeTagline: 'Quality items at great prices',
    allowWishlist: true,
    allowCustomerReviews: true,
    requireCustomerRegistration: false,
    currency: 'USD',
    isPublicStoreEnabled: true
  };

  const mockCategories = [
    { id: '1', name: 'Electronics', slug: 'electronics', itemCount: 25 },
    { id: '2', name: 'Books', slug: 'books', itemCount: 50 }
  ];

  beforeEach(async () => {
    const storeServiceSpy = jasmine.createSpyObj('PublicStoreService', ['getStoreInfo', 'getCategories', 'searchItems']);
    const cartServiceSpy = jasmine.createSpyObj('ShoppingCartService', ['getCart', 'removeFromCart']);
    const authServiceSpy = jasmine.createSpyObj('CustomerAuthService', ['logout'], {
      isAuthenticated: jasmine.createSpy().and.returnValue(false),
      currentCustomer: jasmine.createSpy().and.returnValue(null)
    });
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      params: of({ orgSlug: 'test-store' }),
      queryParams: of({})
    };

    await TestBed.configureTestingModule({
      imports: [StoreLayoutComponent],
      providers: [
        { provide: PublicStoreService, useValue: storeServiceSpy },
        { provide: ShoppingCartService, useValue: cartServiceSpy },
        { provide: CustomerAuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockStoreService = TestBed.inject(PublicStoreService) as jasmine.SpyObj<PublicStoreService>;
    mockCartService = TestBed.inject(ShoppingCartService) as jasmine.SpyObj<ShoppingCartService>;
    mockAuthService = TestBed.inject(CustomerAuthService) as jasmine.SpyObj<CustomerAuthService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    mockStoreService.getStoreInfo.and.returnValue(of(mockStoreInfo));
    mockStoreService.getCategories.and.returnValue(of(mockCategories));
    mockStoreService.searchItems.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false, organizationId: 'org-1' }));
    mockCartService.getCart.and.returnValue(of([]));

    fixture = TestBed.createComponent(StoreLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with org slug from route params', () => {
    expect(component['orgSlug']()).toBe('test-store');
  });

  it('should load store info and categories on init', () => {
    expect(mockStoreService.getStoreInfo).toHaveBeenCalledWith('test-store');
    expect(mockStoreService.getCategories).toHaveBeenCalledWith('test-store');
    expect(component['storeInfo']()).toEqual(mockStoreInfo);
    expect(component['categories']()).toEqual(mockCategories);
  });

  it('should load cart on init', () => {
    expect(mockCartService.getCart).toHaveBeenCalled();
  });

  describe('search functionality', () => {
    it('should perform search when search term is provided', () => {
      component['searchTerm'] = 'laptop';
      component['performSearch']();

      expect(mockStoreService.searchItems).toHaveBeenCalled();
    });

    it('should not search with empty search term', () => {
      mockStoreService.searchItems.calls.reset();
      component['searchTerm'] = '';
      component['performSearch']();

      expect(mockStoreService.searchItems).not.toHaveBeenCalled();
    });

    it('should handle search on enter key', () => {
      spyOn(component, 'performSearch' as any);
      const event = new KeyboardEvent('keyup', { key: 'Enter' });

      component['onSearchKeyUp'](event);

      expect(component['performSearch']).toHaveBeenCalled();
    });
  });

  describe('mobile menu', () => {
    it('should toggle mobile menu', () => {
      expect(component['showMobileMenu']()).toBeFalsy();

      component['toggleMobileMenu']();
      expect(component['showMobileMenu']()).toBeTruthy();

      component['toggleMobileMenu']();
      expect(component['showMobileMenu']()).toBeFalsy();
    });
  });

  describe('cart sidebar', () => {
    it('should toggle cart sidebar', () => {
      expect(component['showCartSidebar']()).toBeFalsy();

      component['toggleCartSidebar']();
      expect(component['showCartSidebar']()).toBeTruthy();

      component['toggleCartSidebar']();
      expect(component['showCartSidebar']()).toBeFalsy();
    });

    it('should remove item from cart', () => {
      mockCartService.removeFromCart.and.returnValue(of(undefined));

      component['removeFromCart']('item-1');

      expect(mockCartService.removeFromCart).toHaveBeenCalledWith('item-1');
    });
  });

  describe('user menu', () => {
    it('should toggle user dropdown', () => {
      expect(component['showUserDropdown']()).toBeFalsy();

      component['toggleUserDropdown']();
      expect(component['showUserDropdown']()).toBeTruthy();

      component['toggleUserDropdown']();
      expect(component['showUserDropdown']()).toBeFalsy();
    });

    it('should logout user', () => {
      spyOn(component, 'loadCart' as any);
      component['logout']();

      expect(mockAuthService.logout).toHaveBeenCalled();
      expect(component['showUserDropdown']()).toBeFalsy();
    });
  });

  describe('authentication state', () => {
    it('should check if user is authenticated', () => {
      expect(component['isAuthenticated']()).toBeFalsy();
    });

    it('should get current customer', () => {
      expect(component['currentCustomer']()).toBeNull();
    });
  });

  describe('navigation helpers', () => {
    it('should navigate to category', () => {
      component['navigateToCategory']('electronics');

      expect(mockRouter.navigate).toHaveBeenCalledWith(
        ['/store', 'test-store', 'products'],
        { queryParams: { category: 'electronics' } }
      );
      expect(component['showMobileMenu']()).toBeFalsy();
    });

    it('should navigate to home', () => {
      component['navigateToHome']();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/store', 'test-store']);
      expect(component['showMobileMenu']()).toBeFalsy();
    });
  });

  describe('template rendering', () => {
    it('should display store logo when logoUrl is available', () => {
      component['storeInfo'].set(mockStoreInfo);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const logoImage = compiled.querySelector('.logo-image');
      expect(logoImage).toBeTruthy();
    });

    it('should display store name as text when no logo', () => {
      const storeInfoWithoutLogo = { ...mockStoreInfo, logoUrl: undefined };
      component['storeInfo'].set(storeInfoWithoutLogo);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const logoText = compiled.querySelector('.logo-text');
      expect(logoText).toBeTruthy();
    });

    it('should display navigation categories', () => {
      component['categories'].set(mockCategories);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const categoryLinks = compiled.querySelectorAll('.dropdown-link');
      expect(categoryLinks.length).toBeGreaterThan(0);
    });
  });

  describe('error handling', () => {
    it('should handle store info load error', () => {
      mockStoreService.getStoreInfo.and.returnValue(throwError(() => new Error('Store not found')));
      spyOn(console, 'error');

      (component as any)['loadStoreInfo']();

      expect(console.error).toHaveBeenCalled();
    });

    it('should handle categories load error', () => {
      mockStoreService.getCategories.and.returnValue(throwError(() => new Error('Categories error')));
      spyOn(console, 'error');

      (component as any)['loadCategories']();

      expect(console.error).toHaveBeenCalled();
    });

    it('should handle cart load error', () => {
      mockCartService.getCart.and.returnValue(throwError(() => new Error('Cart error')));
      spyOn(console, 'error');

      (component as any)['loadCart']();

      expect(console.error).toHaveBeenCalled();
    });
  });
});