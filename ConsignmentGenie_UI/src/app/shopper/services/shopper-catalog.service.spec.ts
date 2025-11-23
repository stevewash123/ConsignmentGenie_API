import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ShopperCatalogService } from './shopper-catalog.service';
import { environment } from '../../../environments/environment';

describe('ShopperCatalogService', () => {
  let service: ShopperCatalogService;
  let httpMock: HttpTestingController;
  const mockStoreSlug = 'test-store';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(ShopperCatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Catalog Items', () => {
    it('should get catalog items with default parameters', () => {
      const mockResponse = {
        success: true,
        data: {
          items: [
            {
              itemId: 'item-1',
              title: 'Test Item',
              price: 50.00,
              category: 'Electronics',
              condition: 'Good'
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1
        }
      };

      service.getCatalogItems(mockStoreSlug).subscribe(response => {
        expect(response.success).toBe(true);
        expect(response.data.items.length).toBe(1);
        expect(response.data.items[0].itemId).toBe('item-1');
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/items?page=1&pageSize=20&sortBy=title&sortDirection=asc`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should get catalog items with custom parameters', () => {
      const request = {
        category: 'Electronics',
        minPrice: 10,
        maxPrice: 100,
        searchQuery: 'test',
        condition: 'Good',
        sortBy: 'price',
        sortDirection: 'desc' as const,
        page: 2,
        pageSize: 10
      };

      service.getCatalogItems(mockStoreSlug, request).subscribe();

      const req = httpMock.expectOne((req) => {
        return req.url.includes('/api/shop/test-store/items') &&
               req.params.get('category') === 'Electronics' &&
               req.params.get('minPrice') === '10' &&
               req.params.get('maxPrice') === '100' &&
               req.params.get('searchQuery') === 'test' &&
               req.params.get('condition') === 'Good' &&
               req.params.get('sortBy') === 'price' &&
               req.params.get('sortDirection') === 'desc' &&
               req.params.get('page') === '2' &&
               req.params.get('pageSize') === '10';
      });
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: { items: [], totalCount: 0 } });
    });
  });

  describe('Item Detail', () => {
    it('should get item detail', () => {
      const itemId = 'item-1';
      const mockResponse = {
        success: true,
        data: {
          itemId: 'item-1',
          title: 'Test Item',
          description: 'Test Description',
          price: 50.00,
          category: 'Electronics',
          condition: 'Good',
          images: [
            {
              imageId: 'img-1',
              url: 'test-image.jpg',
              isPrimary: true,
              sortOrder: 1
            }
          ]
        }
      };

      service.getItemDetail(mockStoreSlug, itemId).subscribe(response => {
        expect(response.success).toBe(true);
        expect(response.data.itemId).toBe('item-1');
        expect(response.data.images.length).toBe(1);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/items/${itemId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('Categories', () => {
    it('should get categories', () => {
      const mockResponse = {
        success: true,
        data: [
          { name: 'Electronics', itemCount: 10 },
          { name: 'Clothing', itemCount: 15 },
          { name: 'Books', itemCount: 8 }
        ]
      };

      service.getCategories(mockStoreSlug).subscribe(response => {
        expect(response.success).toBe(true);
        expect(response.data!.length).toBe(3);
        expect(response.data![0].name).toBe('Electronics');
        expect(response.data![0].itemCount).toBe(10);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/categories`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('Search', () => {
    it('should search items', () => {
      const searchRequest = { searchQuery: 'test search' };
      const mockResponse = {
        success: true,
        data: {
          items: [
            {
              itemId: 'item-1',
              title: 'Test Item',
              price: 50.00,
              category: 'Electronics',
              condition: 'Good',
              images: []
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
          searchQuery: 'test search',
          filters: {}
        }
      };

      service.searchItems(mockStoreSlug, searchRequest).subscribe(response => {
        expect(response.success).toBe(true);
        expect(response.data!.searchQuery).toBe('test search');
        expect(response.data!.items.length).toBe(1);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/search?searchQuery=${encodeURIComponent(searchRequest.searchQuery)}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should search items with page size', () => {
      const searchRequest = { searchQuery: 'test', pageSize: 5 };

      service.searchItems(mockStoreSlug, searchRequest).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/search?searchQuery=${searchRequest.searchQuery}&pageSize=${searchRequest.pageSize}`);
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: { items: [], totalCount: 0, page: 1, pageSize: 5, totalPages: 0, searchQuery: 'test', filters: {} } });
    });
  });

  describe('Error Handling', () => {
    it('should handle HTTP errors gracefully', () => {
      service.getCatalogItems(mockStoreSlug).subscribe({
        next: () => fail('Expected error'),
        error: (error) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne((req) => req.url.includes('/api/shop/test-store/items'));
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle network errors', () => {
      service.getItemDetail(mockStoreSlug, 'item-1').subscribe({
        next: () => fail('Expected error'),
        error: (error) => {
          expect(error.status).toBe(0);
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/api/shop/test-store/items/item-1`);
      req.error(new ErrorEvent('Network error'));
    });
  });

  describe('URL Building', () => {
    it('should build correct URLs for different endpoints', () => {
      // Test catalog items endpoint
      service.getCatalogItems('my-store').subscribe();
      let req = httpMock.expectOne((r) => r.url.includes('/api/shop/my-store/items'));
      req.flush({ success: true, data: { items: [] } });

      // Test item detail endpoint
      service.getItemDetail('my-store', 'test-item').subscribe();
      req = httpMock.expectOne(`${environment.apiUrl}/api/shop/my-store/items/test-item`);
      req.flush({ success: true, data: {} });

      // Test categories endpoint
      service.getCategories('my-store').subscribe();
      req = httpMock.expectOne(`${environment.apiUrl}/api/shop/my-store/categories`);
      req.flush({ success: true, data: [] });

      // Test search endpoint
      service.searchItems('my-store', { searchQuery: 'search-term' }).subscribe();
      req = httpMock.expectOne(`${environment.apiUrl}/api/shop/my-store/search?searchQuery=search-term`);
      req.flush({ success: true, data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, searchQuery: 'search-term', filters: {} } });
    });
  });
});