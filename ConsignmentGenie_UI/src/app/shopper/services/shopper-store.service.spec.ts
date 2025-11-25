import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ShopperStoreService, ShopItemQueryParams } from './shopper-store.service';
import { environment } from '../../../environments/environment';

describe('ShopperStoreService', () => {
  let service: ShopperStoreService;
  let httpMock: HttpTestingController;
  const testStoreSlug = 'test-store';
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ShopperStoreService]
    });
    service = TestBed.inject(ShopperStoreService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getStoreInfo', () => {
    it('should get store information successfully', () => {
      const mockStore = {
        organizationId: 'org-123',
        name: 'Test Store',
        slug: testStoreSlug,
        description: 'A test store',
        logoUrl: 'logo.jpg',
        address: '123 Main St',
        phone: '555-0123',
        email: 'store@test.com',
        hours: { monday: '9-5', tuesday: '9-5' },
        isOpen: true
      };

      const mockResponse = {
        success: true,
        data: mockStore
      };

      service.getStoreInfo(testStoreSlug).subscribe(store => {
        expect(store).toEqual(mockStore);
        expect(store.name).toBe('Test Store');
        expect(store.isOpen).toBeTrue();
        expect(service.getCurrentStore()).toEqual(mockStore);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle store not found error', () => {
      service.getStoreInfo('non-existent').subscribe({
        error: (error) => {
          expect(error.message).toBeDefined();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/non-existent`);
      req.flush(
        { message: 'Store not found' },
        { status: 404, statusText: 'Not Found' }
      );
    });
  });

  describe('getCatalogItems', () => {
    it('should get catalog items without parameters', () => {
      const mockItems = {
        items: [
          {
            itemId: '1',
            title: 'Test Item',
            description: 'Test Description',
            price: 50.00,
            category: 'Electronics',
            condition: 'Good',
            primaryImageUrl: 'test.jpg',
            listedDate: new Date()
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 12,
        totalPages: 1
      };

      const mockResponse = {
        success: true,
        data: mockItems
      };

      service.getCatalogItems(testStoreSlug).subscribe(result => {
        expect(result).toEqual(mockItems);
        expect(result.items.length).toBe(1);
        expect(result.items[0].title).toBe('Test Item');
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/items`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0);
      req.flush(mockResponse);
    });

    it('should get catalog items with all query parameters', () => {
      const queryParams: ShopItemQueryParams = {
        category: 'Electronics',
        minPrice: 10,
        maxPrice: 100,
        condition: 'Good',
        size: 'M',
        sortBy: 'price',
        sortDirection: 'asc',
        page: 2,
        pageSize: 20
      };

      const mockResponse = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          page: 2,
          pageSize: 20,
          totalPages: 0
        }
      };

      service.getCatalogItems(testStoreSlug, queryParams).subscribe(result => {
        expect(result.page).toBe(2);
        expect(result.pageSize).toBe(20);
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/api/shop/${testStoreSlug}/items` &&
               request.params.get('category') === 'Electronics' &&
               request.params.get('minPrice') === '10' &&
               request.params.get('maxPrice') === '100' &&
               request.params.get('condition') === 'Good' &&
               request.params.get('size') === 'M' &&
               request.params.get('sortBy') === 'price' &&
               request.params.get('sortDirection') === 'asc' &&
               request.params.get('page') === '2' &&
               request.params.get('pageSize') === '20';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getItemDetail', () => {
    it('should get item detail successfully', () => {
      const itemId = 'item-123';
      const mockItemDetail = {
        itemId: itemId,
        title: 'Detailed Item',
        description: 'Detailed description',
        price: 75.00,
        category: 'Electronics',
        condition: 'Excellent',
        size: 'L',
        primaryImageUrl: 'primary.jpg',
        listedDate: new Date(),
        imageUrls: ['image1.jpg', 'image2.jpg'],
        brand: 'TestBrand',
        color: 'Blue',
        materials: 'Cotton',
        measurements: '30x20x10',
        isAvailable: true
      };

      const mockResponse = {
        success: true,
        data: mockItemDetail
      };

      service.getItemDetail(testStoreSlug, itemId).subscribe(item => {
        expect(item).toEqual(mockItemDetail);
        expect(item.itemId).toBe(itemId);
        expect(item.isAvailable).toBeTrue();
        expect(item.imageUrls.length).toBe(2);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/items/${itemId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getCategories', () => {
    it('should get categories successfully', () => {
      const mockCategories = [
        { id: '1', name: 'Electronics', itemCount: 25 },
        { id: '2', name: 'Clothing', itemCount: 45 },
        { id: '3', name: 'Books', itemCount: 12 }
      ];

      const mockResponse = {
        success: true,
        data: mockCategories
      };

      service.getCategories(testStoreSlug).subscribe(categories => {
        expect(categories).toEqual(mockCategories);
        expect(categories.length).toBe(3);
        expect(categories[0].name).toBe('Electronics');
        expect(categories[0].itemCount).toBe(25);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/categories`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle empty categories response', () => {
      const mockResponse = {
        success: true,
        data: null
      };

      service.getCategories(testStoreSlug).subscribe(categories => {
        expect(categories).toEqual([]);
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/categories`);
      req.flush(mockResponse);
    });
  });

  describe('searchItems', () => {
    it('should search items with query only', () => {
      const searchQuery = 'laptop';
      const mockSearchResult = {
        items: [
          {
            itemId: '1',
            title: 'Gaming Laptop',
            description: 'High performance laptop',
            price: 800.00,
            category: 'Electronics',
            condition: 'Good',
            primaryImageUrl: 'laptop.jpg',
            listedDate: new Date()
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 12,
        totalPages: 1
      };

      const mockResponse = {
        success: true,
        data: mockSearchResult
      };

      service.searchItems(testStoreSlug, searchQuery).subscribe(result => {
        expect(result).toEqual(mockSearchResult);
        expect(result.items.length).toBe(1);
        expect(result.items[0].title).toBe('Gaming Laptop');
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/api/shop/${testStoreSlug}/search` &&
               request.params.get('q') === searchQuery;
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should search items with query and filters', () => {
      const searchQuery = 'shirt';
      const queryParams: ShopItemQueryParams = {
        category: 'Clothing',
        minPrice: 20,
        maxPrice: 80,
        condition: 'Excellent',
        size: 'M',
        sortBy: 'price',
        sortDirection: 'asc',
        page: 1,
        pageSize: 20
      };

      const mockResponse = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 20,
          totalPages: 0
        }
      };

      service.searchItems(testStoreSlug, searchQuery, queryParams).subscribe(result => {
        expect(result.page).toBe(1);
        expect(result.pageSize).toBe(20);
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/api/shop/${testStoreSlug}/search` &&
               request.params.get('q') === searchQuery &&
               request.params.get('category') === 'Clothing' &&
               request.params.get('minPrice') === '20' &&
               request.params.get('maxPrice') === '80' &&
               request.params.get('condition') === 'Excellent' &&
               request.params.get('size') === 'M' &&
               request.params.get('sortBy') === 'price' &&
               request.params.get('sortDirection') === 'asc' &&
               request.params.get('page') === '1' &&
               request.params.get('pageSize') === '20';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('store management', () => {
    it('should get current store', () => {
      expect(service.getCurrentStore()).toBeNull();

      const mockStore = {
        organizationId: 'org-123',
        name: 'Test Store',
        slug: testStoreSlug,
        isOpen: true
      };

      service['currentStoreSubject'].next(mockStore);
      expect(service.getCurrentStore()).toEqual(mockStore);
    });

    it('should clear current store', () => {
      const mockStore = {
        organizationId: 'org-123',
        name: 'Test Store',
        slug: testStoreSlug,
        isOpen: true
      };

      service['currentStoreSubject'].next(mockStore);
      expect(service.getCurrentStore()).toEqual(mockStore);

      service.clearCurrentStore();
      expect(service.getCurrentStore()).toBeNull();
    });

    it('should emit store changes through observable', () => {
      const mockStore = {
        organizationId: 'org-123',
        name: 'Test Store',
        slug: testStoreSlug,
        isOpen: true
      };

      service.currentStore$.subscribe(store => {
        if (store) {
          expect(store.name).toBe('Test Store');
        }
      });

      service['currentStoreSubject'].next(mockStore);
    });
  });

  describe('error handling', () => {
    it('should handle catalog error', () => {
      service.getCatalogItems(testStoreSlug).subscribe({
        error: (error) => {
          expect(error.message).toBe('Catalog unavailable');
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/items`);
      req.flush(
        { error: { message: 'Catalog unavailable' } },
        { status: 500, statusText: 'Internal Server Error' }
      );
    });

    it('should handle item detail error', () => {
      const itemId = 'non-existent';

      service.getItemDetail(testStoreSlug, itemId).subscribe({
        error: (error) => {
          expect(error.message).toBe('Item not found');
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}/items/${itemId}`);
      req.flush(
        { error: { message: 'Item not found' } },
        { status: 404, statusText: 'Not Found' }
      );
    });

    it('should handle search error', () => {
      service.searchItems(testStoreSlug, 'test').subscribe({
        error: (error) => {
          expect(error.message).toBe('Search service unavailable');
        }
      });

      const req = httpMock.expectOne(request =>
        request.url === `${apiUrl}/api/shop/${testStoreSlug}/search`
      );
      req.flush(
        { error: { message: 'Search service unavailable' } },
        { status: 503, statusText: 'Service Unavailable' }
      );
    });

    it('should handle error with errors array', () => {
      service.getStoreInfo(testStoreSlug).subscribe({
        error: (error) => {
          expect(error.message).toBe('Validation error');
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}`);
      req.flush(
        { error: { errors: ['Validation error', 'Second error'] } },
        { status: 400, statusText: 'Bad Request' }
      );
    });

    it('should handle generic error message', () => {
      service.getStoreInfo(testStoreSlug).subscribe({
        error: (error) => {
          expect(error.message).toBe('Network error');
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/api/shop/${testStoreSlug}`);
      req.flush({ message: 'Network error' }, {
        status: 0,
        statusText: 'Unknown Error'
      });
    });
  });
});