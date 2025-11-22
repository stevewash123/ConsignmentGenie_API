import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { ProductListComponent } from './product-list.component';
import { PublicStoreService } from '../../services/public-store.service';
import { ShoppingCartService } from '../../../customer/services/shopping-cart.service';

describe('ProductListComponent', () => {
  let component: ProductListComponent;
  let fixture: ComponentFixture<ProductListComponent>;
  let mockStoreService: jasmine.SpyObj<PublicStoreService>;
  let mockCartService: jasmine.SpyObj<ShoppingCartService>;
  let mockActivatedRoute: any;

  const mockProducts = [
    {
      id: '1',
      title: 'Test Product 1',
      description: 'Description 1',
      price: 29.99,
      originalPrice: 39.99,
      category: 'Electronics',
      condition: 'Excellent',
      isAvailable: true,
      photos: [{ id: '1', url: 'product1.jpg', altText: 'Product 1', isPrimary: true, order: 1 }],
      providerName: 'Provider 1',
      providerId: 'provider-1',
      tags: ['electronics', 'sale'],
      createdAt: new Date().toISOString(),
      status: 'available',
      organizationId: 'org-1'
    }
  ];

  const mockPagedResult = {
    items: mockProducts,
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
    organizationId: 'org-1'
  };

  const mockCategories = [
    { id: '1', name: 'Electronics', slug: 'electronics', itemCount: 25 },
    { id: '2', name: 'Books', slug: 'books', itemCount: 50 }
  ];

  beforeEach(async () => {
    const storeServiceSpy = jasmine.createSpyObj('PublicStoreService', ['searchItems', 'getCategories']);
    const cartServiceSpy = jasmine.createSpyObj('ShoppingCartService', ['addToCart', 'isItemInCart']);

    mockActivatedRoute = {
      params: of({ orgSlug: 'test-store' }),
      queryParams: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ProductListComponent, FormsModule],
      providers: [
        { provide: PublicStoreService, useValue: storeServiceSpy },
        { provide: ShoppingCartService, useValue: cartServiceSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockStoreService = TestBed.inject(PublicStoreService) as jasmine.SpyObj<PublicStoreService>;
    mockCartService = TestBed.inject(ShoppingCartService) as jasmine.SpyObj<ShoppingCartService>;

    mockStoreService.searchItems.and.returnValue(of(mockPagedResult));
    mockStoreService.getCategories.and.returnValue(of(mockCategories));
    mockCartService.addToCart.and.returnValue(of(mockProducts[0] as any));
    mockCartService.isItemInCart.and.returnValue(false);

    fixture = TestBed.createComponent(ProductListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with org slug from route params', () => {
    expect(component['orgSlug']()).toBe('test-store');
  });

  it('should load products and categories on init', () => {
    expect(mockStoreService.searchItems).toHaveBeenCalled();
    expect(mockStoreService.getCategories).toHaveBeenCalledWith('test-store');
    expect(component['products']()).toEqual(mockProducts);
    expect(component['categories']()).toEqual(mockCategories);
  });

  describe('filtering', () => {
    it('should apply filters and reset to page 1', () => {
      component['selectedCategory'] = 'electronics';
      component['minPrice'] = 20;
      component['maxPrice'] = 100;

      spyOn(component as any, 'updateUrl');
      component['applyFilters']();

      expect(component['currentPage']()).toBe(1);
      expect(component['updateUrl']).toHaveBeenCalled();
    });

    it('should clear all filters', () => {
      component['selectedCategory'] = 'electronics';
      component['minPrice'] = 10;
      component['maxPrice'] = 100;
      component['sortBy'] = 'price';
      spyOn(component as any, 'applyFilters');

      component['clearAllFilters']();

      expect(component['selectedCategory']).toBe('');
      expect(component['minPrice']).toBeNull();
      expect(component['maxPrice']).toBeNull();
      expect(component['sortBy']).toBe('created');
      expect(component['applyFilters']).toHaveBeenCalled();
    });

    it('should detect active filters', () => {
      expect(component['hasActiveFilters']()).toBeFalsy();

      component['selectedCategory'] = 'electronics';
      expect(component['hasActiveFilters']()).toBeTruthy();
    });
  });

  describe('view mode', () => {
    it('should set view mode and save to localStorage', () => {
      spyOn(localStorage, 'setItem');

      component['setViewMode']('list');

      expect(component['viewMode']()).toBe('list');
      expect(localStorage.setItem).toHaveBeenCalledWith('cg_view_mode', 'list');
    });
  });

  describe('pagination', () => {
    beforeEach(() => {
      component['totalPages'].set(5);
      component['currentPage'].set(3);
    });

    it('should go to specific page', () => {
      spyOn(component as any, 'updateUrl');

      component['goToPage'](4);

      expect(component['currentPage']()).toBe(4);
      expect(component['updateUrl']).toHaveBeenCalled();
    });

    it('should generate correct page numbers', () => {
      const pages = component['getPageNumbers']();
      expect(pages).toEqual([1, 2, 3, 4, 5]);
    });
  });

  describe('shopping cart integration', () => {
    it('should add item to cart successfully', () => {
      const product = mockProducts[0];

      component['addToCart'](product);

      expect(mockCartService.addToCart).toHaveBeenCalledWith(product.id, 1);
    });

    it('should not add unavailable item to cart', () => {
      const unavailableProduct = { ...mockProducts[0], isAvailable: false };

      component['addToCart'](unavailableProduct);

      expect(mockCartService.addToCart).not.toHaveBeenCalled();
    });
  });

  describe('computed properties', () => {
    it('should compute page title correctly', () => {
      component['categories'].set(mockCategories);

      // No category selected
      expect(component['pageTitle']()).toBe('All Products');

      // Category selected
      component['selectedCategory'] = 'electronics';
      expect(component['pageTitle']()).toBe('Electronics Products');
    });
  });

  describe('template rendering', () => {
    it('should display products when loaded', () => {
      component['products'].set(mockProducts);
      component['loading'].set(false);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const productCards = compiled.querySelectorAll('.product-card');
      expect(productCards.length).toBe(1);
    });

    it('should display loading state', () => {
      component['loading'].set(true);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      expect(compiled.querySelector('.loading-state')).toBeTruthy();
      expect(compiled.textContent).toContain('Loading products...');
    });

    it('should display empty state when no products', () => {
      component['products'].set([]);
      component['loading'].set(false);
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      expect(compiled.querySelector('.empty-state')).toBeTruthy();
      expect(compiled.textContent).toContain('No products found');
    });
  });
});