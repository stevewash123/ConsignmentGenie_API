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
    storeSlug: mockStoreSlug,
    storeName: 'Test Store',
    description: 'Test Description',
    isActive: true,
    theme: { primaryColor: '#000000', secondaryColor: '#ffffff' },
    contact: { email: 'test@test.com', phone: '123-456-7890' },
    hours: { monday: '9-5', tuesday: '9-5', wednesday: '9-5', thursday: '9-5', friday: '9-5' },
    socialMedia: { facebook: '', instagram: '', twitter: '', website: '' }
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
        categories: ['Electronics', 'Clothing'],
        conditions: ['Good', 'Excellent'],
        priceRange: { min: 0, max: 1000 }
      }
    }
  };

  const mockCategoriesData = {
    success: true,
    data: {
      categories: ['Electronics', 'Clothing', 'Books'],
      categoryCounts: [
        { category: 'Electronics', count: 10 },
        { category: 'Clothing', count: 15 },
        { category: 'Books', count: 8 }
      ]
    }
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
    mockStoreService.getStoreInfo.and.returnValue(of({ success: true, data: mockStoreInfo }));
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
      expect(component.availableCategories.length).toBe(3);
      expect(mockCartService.setCurrentStore).toHaveBeenCalledWith(mockStoreSlug);
    }));

    it('should handle store loading error', fakeAsync(() => {
      mockStoreService.getStoreInfo.and.returnValue(throwError(() => ({ status: 404 })));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
      expect(component.error).toContain('Store not found');
    }));

    it('should handle catalog loading error', fakeAsync(() => {
      mockCatalogService.getCatalogItems.and.returnValue(throwError(() => ({ status: 500 })));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
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
      component.currentFilters.searchQuery = 'test query';

      component.clearSearch();

      expect(component.currentFilters.searchQuery).toBe('');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });
  });

  describe('Filtering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should filter by category', () => {
      component.filterByCategory('Electronics');

      expect(component.currentFilters.category).toBe('Electronics');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledWith(mockStoreSlug, jasmine.objectContaining({
        category: 'Electronics'
      }));
    });

    it('should filter by price range', () => {
      component.filterByPriceRange(10, 100);

      expect(component.currentFilters.minPrice).toBe(10);
      expect(component.currentFilters.maxPrice).toBe(100);
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledWith(mockStoreSlug, jasmine.objectContaining({
        minPrice: 10,
        maxPrice: 100
      }));
    });

    it('should filter by condition', () => {
      component.filterByCondition('Excellent');

      expect(component.currentFilters.condition).toBe('Excellent');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalled();
    });

    it('should clear all filters', () => {
      component.currentFilters = {
        searchQuery: 'test',
        category: 'Electronics',
        minPrice: 10,
        maxPrice: 100,
        condition: 'Good',
        sortBy: 'price',
        sortDirection: 'desc',
        page: 1,
        pageSize: 20
      };

      component.clearFilters();

      expect(component.currentFilters).toEqual({
        searchQuery: '',
        category: undefined,
        minPrice: undefined,
        maxPrice: undefined,
        condition: undefined,
        sortBy: 'title',
        sortDirection: 'asc',
        page: 1,
        pageSize: 20
      });
    });
  });

  describe('Sorting', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should sort items', () => {
      component.sortItems('price', 'desc');

      expect(component.currentFilters.sortBy).toBe('price');
      expect(component.currentFilters.sortDirection).toBe('desc');
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledWith(mockStoreSlug, jasmine.objectContaining({
        sortBy: 'price',
        sortDirection: 'desc'
      }));
    });
  });

  describe('Pagination', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should go to specific page', () => {
      component.goToPage(2);

      expect(component.currentFilters.page).toBe(2);
      expect(mockCatalogService.getCatalogItems).toHaveBeenCalledWith(mockStoreSlug, jasmine.objectContaining({
        page: 2
      }));
    });

    it('should go to next page', () => {
      component.totalPages = 5;
      component.currentFilters.page = 1;

      component.nextPage();

      expect(component.currentFilters.page).toBe(2);
    });

    it('should not go beyond last page', () => {
      component.totalPages = 5;
      component.currentFilters.page = 5;

      component.nextPage();

      expect(component.currentFilters.page).toBe(5);
    });

    it('should go to previous page', () => {
      component.currentFilters.page = 2;

      component.previousPage();

      expect(component.currentFilters.page).toBe(1);
    });

    it('should not go before first page', () => {
      component.currentFilters.page = 1;

      component.previousPage();

      expect(component.currentFilters.page).toBe(1);
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
      mockCartService.isItemInCart.and.returnValue(true);

      const result = component.isItemInCart('item-1');

      expect(result).toBe(true);
      expect(mockCartService.isItemInCart).toHaveBeenCalledWith('item-1');
    });

    it('should get item quantity in cart', () => {
      mockCartService.getItemQuantity.and.returnValue(2);

      const quantity = component.getItemQuantity('item-1');

      expect(quantity).toBe(2);
      expect(mockCartService.getItemQuantity).toHaveBeenCalledWith('item-1');
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should navigate to item detail', () => {
      component.viewItemDetail('item-1');

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/shop', mockStoreSlug, 'item', 'item-1']);
    });
  });

  describe('Template Rendering', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should display loading state', () => {
      component.loading = true;
      fixture.detectChanges();

      const loadingElement = fixture.debugElement.query(By.css('.loading'));
      expect(loadingElement).toBeTruthy();
    });

    it('should display error state', () => {
      component.loading = false;
      component.error = 'Test error message';
      fixture.detectChanges();

      const errorElement = fixture.debugElement.query(By.css('.error'));
      expect(errorElement).toBeTruthy();
      expect(errorElement.nativeElement.textContent).toContain('Test error message');
    });

    it('should display items when loaded', () => {
      component.loading = false;
      component.error = '';
      fixture.detectChanges();

      const itemElements = fixture.debugElement.queryAll(By.css('.item-card'));
      expect(itemElements.length).toBe(2);
    });

    it('should display no items message when empty', () => {
      component.items = [];
      component.loading = false;
      component.error = '';
      fixture.detectChanges();

      const noItemsElement = fixture.debugElement.query(By.css('.no-items'));
      expect(noItemsElement).toBeTruthy();
    });
  });
});