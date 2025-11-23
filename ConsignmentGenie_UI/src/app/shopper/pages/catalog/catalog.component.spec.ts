import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';

import { CatalogComponent } from './catalog.component';
import { ShopperCatalogService } from '../../services/shopper-catalog.service';
import { ShopperCartService } from '../../services/shopper-cart.service';
import { ShopperStoreService } from '../../services/shopper-store.service';

describe('CatalogComponent', () => {
  let component: CatalogComponent;
  let fixture: ComponentFixture<CatalogComponent>;
  let mockCatalogService: jasmine.SpyObj<ShopperCatalogService>;
  let mockCartService: jasmine.SpyObj<ShopperCartService>;
  let mockStoreService: jasmine.SpyObj<ShopperStoreService>;
  let mockActivatedRoute: jasmine.SpyObj<ActivatedRoute>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockStoreSlug = 'test-store';
  const mockStoreInfo = {
    organizationId: 'org-123',
    name: 'Test Store',
    slug: mockStoreSlug,
    description: 'Test Description',
    isOpen: true,
    logoUrl: 'logo.png',
    address: '123 Test St',
    phone: '123-456-7890',
    email: 'test@test.com',
    hours: { monday: '9-5', tuesday: '9-5', wednesday: '9-5', thursday: '9-5', friday: '9-5' }
  };

  const mockCatalogData = {
    success: true,
    data: {
      items: [
        {
          itemId: 'item-1',
          title: 'Test Item 1',
          description: 'Test Description 1',
          price: 50.00,
          category: 'Electronics',
          brand: 'TestBrand',
          condition: 'Good',
          primaryImageUrl: 'test1.jpg',
          images: []
        },
        {
          itemId: 'item-2',
          title: 'Test Item 2',
          description: 'Test Description 2',
          price: 75.00,
          category: 'Clothing',
          brand: 'TestBrand2',
          condition: 'Excellent',
          primaryImageUrl: 'test2.jpg',
          images: []
        }
      ],
      totalCount: 2,
      page: 1,
      pageSize: 20,
      totalPages: 1,
      filters: {
        category: 'Electronics',
        minPrice: 0,
        maxPrice: 1000,
        condition: 'Good',
        size: 'M',
        sortBy: 'price',
        sortDirection: 'asc'
      }
    }
  };

  const mockCategoriesData = {
    success: true,
    data: [
      { name: 'Electronics', itemCount: 10 },
      { name: 'Clothing', itemCount: 15 },
      { name: 'Books', itemCount: 8 }
    ]
  };

  beforeEach(async () => {
    const catalogServiceSpy = jasmine.createSpyObj('ShopperCatalogService', ['getCatalogItems', 'getCategories', 'searchItems']);
    const cartServiceSpy = jasmine.createSpyObj('ShopperCartService', ['setCurrentStore', 'addItem', 'isItemInCart', 'getItemQuantity']);
    const storeServiceSpy = jasmine.createSpyObj('ShopperStoreService', ['getStoreInfo']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      paramMap: of(new Map([['storeSlug', mockStoreSlug]])),
      queryParamMap: of(new Map())
    } as any;

    await TestBed.configureTestingModule({
      imports: [CatalogComponent, NoopAnimationsModule],
      providers: [
        { provide: ShopperCatalogService, useValue: catalogServiceSpy },
        { provide: ShopperCartService, useValue: cartServiceSpy },
        { provide: ShopperStoreService, useValue: storeServiceSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    mockCatalogService = TestBed.inject(ShopperCatalogService) as jasmine.SpyObj<ShopperCatalogService>;
    mockCartService = TestBed.inject(ShopperCartService) as jasmine.SpyObj<ShopperCartService>;
    mockStoreService = TestBed.inject(ShopperStoreService) as jasmine.SpyObj<ShopperStoreService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Default mock implementations
    mockStoreService.getStoreInfo.and.returnValue(of(mockStoreInfo));
    mockCatalogService.getCatalogItems.and.returnValue(of(mockCatalogData));
    mockCatalogService.getCategories.and.returnValue(of(mockCategoriesData));
    mockCartService.isItemInCart.and.returnValue(false);
    mockCartService.getItemQuantity.and.returnValue(0);
    mockCartService.addItem.and.returnValue(true);

    fixture = TestBed.createComponent(CatalogComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load store info and catalog data on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(component.storeSlug).toBe(mockStoreSlug);
      expect(component.storeInfo).toEqual(mockStoreInfo);
      expect(component.items.length).toBe(2);
      expect(component.categories.length).toBe(3);
      expect(mockCartService.setCurrentStore).toHaveBeenCalledWith(mockStoreSlug);
    }));

    it('should handle store loading error', fakeAsync(() => {
      mockStoreService.getStoreInfo.and.returnValue(throwError(() => ({ status: 404 })));

      fixture.detectChanges();
      tick();

      expect(component.isLoading).toBe(false);
      expect(component.error).toContain('Store not found');
    }));

    it('should handle catalog loading error', fakeAsync(() => {
      mockCatalogService.getCatalogItems.and.returnValue(throwError(() => ({ status: 500 })));

      fixture.detectChanges();
      tick();

      expect(component.isLoading).toBe(false);
      expect(component.error).toContain('Failed to load catalog');
    }));
  });

  describe('Search Functionality', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should perform search with debounce', fakeAsync(() => {
      const searchInput = fixture.debugElement.query(By.css('input[type="search"]'));

      searchInput.nativeElement.value = 'test query';
      searchInput.nativeElement.dispatchEvent(new Event('input'));

      tick(299); // Less than debounce time
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledTimes(1); // Initial load only

      tick(1); // Complete debounce time
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledTimes(2); // Initial + search
    }));

    it('should clear search', () => {
      component.searchTerm = 'test query';

      component.clearFilters();

      expect(component.searchTerm).toBe('');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });
  });

  describe('Filtering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should filter by category', () => {
      component.selectedCategory = 'Electronics';
      component.onCategoryChange();

      expect(component.selectedCategory).toBe('Electronics');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledWith(mockStoreSlug, jasmine.objectContaining({
        category: 'Electronics'
      }));
    });

    it('should filter by price range', () => {
      // The actual component doesn't have price filtering implemented yet
      // This test would need the component to have minPrice/maxPrice properties
      pending('Price range filtering not yet implemented');
    });

    it('should filter by condition', () => {
      // The actual component doesn't have condition filtering implemented yet
      // This test would need the component to have a condition property and filterByCondition method
      pending('Condition filtering not yet implemented');
    });

    it('should clear all filters', () => {
      // Set up some filter state
      component.searchTerm = 'test';
      component.selectedCategory = 'Electronics';
      component.sortBy = 'price';
      component.currentPage = 3;

      component.clearFilters();

      expect(component.searchTerm).toBe('');
      expect(component.selectedCategory).toBe('');
      expect(component.sortBy).toBe('newest');
      expect(component.sortDirection).toBe('desc');
      expect(component.currentPage).toBe(1);
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });
  });

  describe('Sorting', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should change sort order', () => {
      component.sortBy = 'price-high';

      component.onSortChange();

      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });
  });

  describe('Pagination', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should go to specific page', () => {
      component.totalPages = 5;

      component.goToPage(2);

      expect(component.currentPage).toBe(2);
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });

    it('should go to next page', () => {
      component.totalPages = 5;
      component.currentPage = 1;

      component.goToPage(component.currentPage + 1);

      expect(component.currentPage).toBe(2);
    });

    it('should not go beyond last page', () => {
      component.totalPages = 5;
      component.currentPage = 5;

      component.goToPage(component.currentPage + 1);

      expect(component.currentPage).toBe(5);
    });

    it('should go to previous page', () => {
      component.currentPage = 2;

      component.goToPage(component.currentPage - 1);

      expect(component.currentPage).toBe(1);
    });

    it('should not go before first page', () => {
      component.currentPage = 1;

      component.goToPage(component.currentPage - 1);

      expect(component.currentPage).toBe(1);
    });
  });

  describe('Cart Integration', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should add item to cart', () => {
      const testItem = mockCatalogData.data.items[0];

      component.addToCart(testItem);

      expect(mockCartService.addItem).toHaveBeenCalledWith(testItem, 1);
    });

    it('should check if item is in cart', () => {
      // The component doesn't have this method - it relies on the cart service directly
      pending('isItemInCart method not implemented in component');
    });

    it('should get item quantity in cart', () => {
      // The component doesn't have this method - it relies on the cart service directly
      pending('getItemQuantity method not implemented in component');
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should navigate to item detail', () => {
      const testItem = mockCatalogData.data.items[0];

      component.viewItemDetail(testItem);

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/shop', mockStoreSlug, 'items', testItem.itemId]);
    });
  });

  describe('Template Rendering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should display loading state', () => {
      component.isLoading = true;
      fixture.detectChanges();

      const loadingElement = fixture.debugElement.query(By.css('.loading'));
      expect(loadingElement).toBeTruthy();
    });

    it('should display error state', () => {
      component.isLoading = false;
      component.error = 'Test error message';
      fixture.detectChanges();

      const errorElement = fixture.debugElement.query(By.css('.error'));
      expect(errorElement).toBeTruthy();
      expect(errorElement.nativeElement.textContent).toContain('Test error message');
    });

    it('should display items when loaded', () => {
      component.isLoading = false;
      component.error = '';
      fixture.detectChanges();

      const itemElements = fixture.debugElement.queryAll(By.css('.item-card'));
      expect(itemElements.length).toBe(2);
    });

    it('should display no items message when empty', () => {
      component.items = [];
      component.isLoading = false;
      component.error = '';
      fixture.detectChanges();

      const noItemsElement = fixture.debugElement.query(By.css('.no-items'));
      expect(noItemsElement).toBeTruthy();
    });
  });
});