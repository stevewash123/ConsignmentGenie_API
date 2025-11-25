import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ShopperCatalogService, CatalogRequest, SearchRequest } from './shopper-catalog.service';
import { environment } from '../../../environments/environment';

describe('ShopperCatalogService', () => {
  let service: ShopperCatalogService;
  let httpMock: HttpTestingController;
  const testStoreSlug = 'test-store';
  const apiUrl = `${environment.apiUrl}/api/shop`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ShopperCatalogService]
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

  describe('getCatalogItems', () => {
    it('should get catalog items without parameters', () => {
      const mockCatalog = {
        success: true,
        data: {
          items: [
            {
              itemId: '1',
              title: 'Test Item',
              description: 'Test Description',
              price: 50.00,
              condition: 'Good',
              primaryImageUrl: 'test.jpg',
              images: []
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 12,
          totalPages: 1,
          filters: {
            sortBy: 'date',
            sortDirection: 'desc'
          }
        }
      };

      service.getCatalogItems(testStoreSlug).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.items.length).toBe(1);
        expect(response.data?.items[0].title).toBe('Test Item');
      });

      const req = httpMock.expectOne(`${apiUrl}/${testStoreSlug}/items`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0);
      req.flush(mockCatalog);
    });

    it('should get catalog items with all filter parameters', () => {
      const request: CatalogRequest = {
        category: 'Electronics',
        minPrice: 10,
        maxPrice: 100,
        condition: 'Good',
        size: 'M',
        sortBy: 'price',
        sortDirection: 'asc',
        page: 2,
        pageSize: 24
      };

      const mockCatalog = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          page: 2,
          pageSize: 24,
          totalPages: 0,
          filters: request
        }
      };

      service.getCatalogItems(testStoreSlug, request).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.page).toBe(2);
        expect(response.data?.pageSize).toBe(24);
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/${testStoreSlug}/items` &&
               request.params.get('category') === 'Electronics' &&
               request.params.get('minPrice') === '10' &&
               request.params.get('maxPrice') === '100' &&
               request.params.get('condition') === 'Good' &&
               request.params.get('size') === 'M' &&
               request.params.get('sortBy') === 'price' &&
               request.params.get('sortDirection') === 'asc' &&
               request.params.get('page') === '2' &&
               request.params.get('pageSize') === '24';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockCatalog);
    });

    it('should handle partial filter parameters', () => {
      const request: CatalogRequest = {
        category: 'Clothing',
        minPrice: 0,
        maxPrice: 50
      };

      const mockCatalog = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 12,
          totalPages: 0,
          filters: request
        }
      };

      service.getCatalogItems(testStoreSlug, request).subscribe(response => {
        expect(response.success).toBeTrue();
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/${testStoreSlug}/items` &&
               request.params.get('category') === 'Clothing' &&
               request.params.get('minPrice') === '0' &&
               request.params.get('maxPrice') === '50' &&
               request.params.get('condition') === null &&
               request.params.get('size') === null;
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockCatalog);
    });
  });

  describe('getItemDetail', () => {
    it('should get item detail', () => {
      const itemId = 'item-123';
      const mockItemDetail = {
        success: true,
        data: {
          itemId: itemId,
          title: 'Detailed Item',
          description: 'Detailed description',
          price: 75.00,
          category: 'Electronics',
          brand: 'TestBrand',
          size: 'L',
          color: 'Blue',
          condition: 'Excellent',
          materials: 'Cotton',
          measurements: '30x20x10',
          images: [
            {
              imageId: 'img1',
              imageUrl: 'image1.jpg',
              displayOrder: 1,
              isPrimary: true
            }
          ],
          isAvailable: true,
          listedDate: '2023-01-01'
        }
      };

      service.getItemDetail(testStoreSlug, itemId).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.itemId).toBe(itemId);
        expect(response.data?.title).toBe('Detailed Item');
        expect(response.data?.isAvailable).toBeTrue();
        expect(response.data?.images.length).toBe(1);
      });

      const req = httpMock.expectOne(`${apiUrl}/${testStoreSlug}/items/${itemId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockItemDetail);
    });
  });

  describe('getCategories', () => {
    it('should get categories', () => {
      const mockCategories = {
        success: true,
        data: [
          { name: 'Electronics', itemCount: 25 },
          { name: 'Clothing', itemCount: 45 },
          { name: 'Books', itemCount: 12 }
        ]
      };

      service.getCategories(testStoreSlug).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.length).toBe(3);
        expect(response.data?.[0].name).toBe('Electronics');
        expect(response.data?.[0].itemCount).toBe(25);
        expect(response.data?.[1].name).toBe('Clothing');
        expect(response.data?.[2].name).toBe('Books');
      });

      const req = httpMock.expectOne(`${apiUrl}/${testStoreSlug}/categories`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCategories);
    });
  });

  describe('searchItems', () => {
    it('should search items with search query only', () => {
      const searchRequest: SearchRequest = {
        searchQuery: 'laptop'
      };

      const mockSearchResult = {
        success: true,
        data: {
          items: [
            {
              itemId: '1',
              title: 'Gaming Laptop',
              description: 'High performance laptop',
              price: 800.00,
              condition: 'Good',
              primaryImageUrl: 'laptop.jpg',
              images: []
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 12,
          totalPages: 1,
          searchQuery: 'laptop',
          filters: {
            sortBy: 'relevance',
            sortDirection: 'desc'
          }
        }
      };

      service.searchItems(testStoreSlug, searchRequest).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.items.length).toBe(1);
        expect(response.data?.items[0].title).toBe('Gaming Laptop');
        expect(response.data?.searchQuery).toBe('laptop');
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/${testStoreSlug}/search` &&
               request.params.get('q') === 'laptop' &&
               request.params.get('category') === null;
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockSearchResult);
    });

    it('should search items with query and filters', () => {
      const searchRequest: SearchRequest = {
        searchQuery: 'shirt',
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

      const mockSearchResult = {
        success: true,
        data: {
          items: [
            {
              itemId: '2',
              title: 'Cotton Shirt',
              description: 'Comfortable cotton shirt',
              price: 35.00,
              category: 'Clothing',
              size: 'M',
              condition: 'Excellent',
              primaryImageUrl: 'shirt.jpg',
              images: []
            }
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
          searchQuery: 'shirt',
          filters: searchRequest
        }
      };

      service.searchItems(testStoreSlug, searchRequest).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.items.length).toBe(1);
        expect(response.data?.items[0].category).toBe('Clothing');
        expect(response.data?.searchQuery).toBe('shirt');
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/${testStoreSlug}/search` &&
               request.params.get('q') === 'shirt' &&
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
      req.flush(mockSearchResult);
    });

    it('should handle search with undefined numeric values', () => {
      const searchRequest: SearchRequest = {
        searchQuery: 'test',
        minPrice: undefined,
        maxPrice: undefined,
        page: undefined,
        pageSize: undefined
      };

      const mockSearchResult = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 12,
          totalPages: 0,
          searchQuery: 'test',
          filters: {}
        }
      };

      service.searchItems(testStoreSlug, searchRequest).subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data?.searchQuery).toBe('test');
      });

      const req = httpMock.expectOne(request => {
        return request.url === `${apiUrl}/${testStoreSlug}/search` &&
               request.params.get('q') === 'test' &&
               request.params.get('minPrice') === null &&
               request.params.get('maxPrice') === null &&
               request.params.get('page') === null &&
               request.params.get('pageSize') === null;
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockSearchResult);
    });
  });

  describe('error handling', () => {
    it('should handle catalog error', () => {
      service.getCatalogItems(testStoreSlug).subscribe({
        error: (error) => {
          expect(error).toBeDefined();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/${testStoreSlug}/items`);
      req.flush(
        { message: 'Store not found' },
        { status: 404, statusText: 'Not Found' }
      );
    });

    it('should handle item detail error', () => {
      const itemId = 'non-existent';

      service.getItemDetail(testStoreSlug, itemId).subscribe({
        error: (error) => {
          expect(error).toBeDefined();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/${testStoreSlug}/items/${itemId}`);
      req.flush(
        { message: 'Item not found' },
        { status: 404, statusText: 'Not Found' }
      );
    });

    it('should handle search error', () => {
      const searchRequest: SearchRequest = {
        searchQuery: 'test'
      };

      service.searchItems(testStoreSlug, searchRequest).subscribe({
        error: (error) => {
          expect(error).toBeDefined();
        }
      });

      const req = httpMock.expectOne(request =>
        request.url === `${apiUrl}/${testStoreSlug}/search`
      );
      req.flush(
        { message: 'Search service unavailable' },
        { status: 500, statusText: 'Internal Server Error' }
      );
    });
  });
});