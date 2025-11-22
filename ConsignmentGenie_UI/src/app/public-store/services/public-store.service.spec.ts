import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicStoreService } from './public-store.service';
import { PublicItem, PagedResult } from '../../shared/models/api.models';

describe('PublicStoreService', () => {
  let service: PublicStoreService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'https://localhost:7042';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PublicStoreService]
    });
    service = TestBed.inject(PublicStoreService);
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
      const mockStoreInfo = {
        orgSlug: 'test-store',
        displayName: 'Test Store',
        description: 'A test consignment store',
        logoUrl: 'logo.jpg',
        bannerUrl: 'banner.jpg',
        contactInfo: {
          email: 'contact@teststore.com',
          phone: '555-0123',
          address: '123 Main St'
        },
        isActive: true
      };

      let result: any;
      service.getStoreInfo('test-store').subscribe(info => {
        result = info;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStoreInfo);

      expect(result).toEqual(mockStoreInfo);
    });

    it('should handle store not found error', () => {
      let error: any;
      service.getStoreInfo('nonexistent-store').subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/nonexistent-store`);
      req.flush({ message: 'Store not found' }, { status: 404, statusText: 'Not Found' });

      expect(error).toBeTruthy();
    });
  });

  describe('searchItems', () => {
    it('should search items with all parameters', () => {
      const searchRequest = {
        orgSlug: 'test-store',
        page: 1,
        pageSize: 20,
        category: 'electronics',
        minPrice: 10,
        maxPrice: 100,
        sortBy: 'price',
        sortOrder: 'asc',
        searchTerm: 'laptop'
      };

      const mockResponse: PagedResult<PublicItem> = {
        items: [
          {
            id: '1',
            title: 'Test Laptop',
            description: 'A test laptop',
            price: 599.99,
            originalPrice: 799.99,
            category: 'Electronics',
            condition: 'Excellent',
            isAvailable: true,
            photos: [{ id: '1', url: 'laptop.jpg', altText: 'Laptop', isPrimary: true, order: 1 }],
            providerName: 'Tech Provider',
            providerId: 'provider-1',
            tags: ['laptop', 'electronics', 'refurbished'],
            createdAt: new Date().toISOString(),
            status: 'available',
            organizationId: 'org-1'
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
        organizationId: 'org-1'
      };

      let result: PagedResult<PublicItem> | undefined;
      service.searchItems(searchRequest).subscribe(response => {
        result = response;
      });

      const expectedUrl = `${mockApiUrl}/api/public-store/test-store/items?page=1&pageSize=20&category=electronics&minPrice=10&maxPrice=100&sortBy=price&sortOrder=asc&searchTerm=laptop`;
      const req = httpMock.expectOne(expectedUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);

      expect(result).toEqual(mockResponse);
    });

    it('should search items with minimal parameters', () => {
      const searchRequest = {
        orgSlug: 'test-store',
        page: 1,
        pageSize: 20,
        sortBy: 'created',
        sortOrder: 'desc'
      };

      const mockResponse: PagedResult<PublicItem> = {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false,
        organizationId: 'org-1'
      };

      let result: PagedResult<PublicItem> | undefined;
      service.searchItems(searchRequest).subscribe(response => {
        result = response;
      });

      const expectedUrl = `${mockApiUrl}/api/public-store/test-store/items?page=1&pageSize=20&sortBy=created&sortOrder=desc`;
      const req = httpMock.expectOne(expectedUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);

      expect(result).toEqual(mockResponse);
    });

    it('should handle search error', () => {
      const searchRequest = {
        orgSlug: 'test-store',
        page: 1,
        pageSize: 20,
        sortBy: 'created',
        sortOrder: 'desc'
      };

      let error: any;
      service.searchItems(searchRequest).subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store/items?page=1&pageSize=20&sortBy=created&sortOrder=desc`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });

      expect(error).toBeTruthy();
    });
  });

  describe('getCategories', () => {
    it('should get categories successfully', () => {
      const mockCategories = [
        { id: '1', name: 'Electronics', slug: 'electronics', itemCount: 25 },
        { id: '2', name: 'Books', slug: 'books', itemCount: 150 },
        { id: '3', name: 'Clothing', slug: 'clothing', itemCount: 75 }
      ];

      let result: any[] | undefined;
      service.getCategories('test-store').subscribe(categories => {
        result = categories;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store/categories`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCategories);

      expect(result).toEqual(mockCategories);
    });

    it('should handle categories fetch error', () => {
      let error: any;
      service.getCategories('test-store').subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store/categories`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });

      expect(error).toBeTruthy();
    });
  });

  describe('getFeaturedItems', () => {
    it('should get featured items successfully', () => {
      const mockFeaturedItems: PublicItem[] = [
        {
          id: '1',
          title: 'Featured Item 1',
          description: 'A featured item',
          price: 49.99,
          category: 'Featured',
          condition: 'Excellent',
          isAvailable: true,
          photos: [{ id: '1', url: 'featured1.jpg', altText: 'Featured item', isPrimary: true, order: 1 }],
          providerName: 'Featured Provider',
          providerId: 'provider-1',
          tags: ['featured'],
          createdAt: new Date().toISOString(),
          status: 'available',
          organizationId: 'org-1'
        }
      ];

      let result: PublicItem[] | undefined;
      service.getFeaturedItems('test-store').subscribe(items => {
        result = items;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store/featured`);
      expect(req.request.method).toBe('GET');
      req.flush(mockFeaturedItems);

      expect(result).toEqual(mockFeaturedItems);
    });

    it('should handle featured items fetch error', () => {
      let error: any;
      service.getFeaturedItems('test-store').subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/public-store/test-store/featured`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });

      expect(error).toBeTruthy();
    });
  });

  describe('URL parameter construction', () => {
    it('should build URL with only required parameters', () => {
      const searchRequest = {
        orgSlug: 'test-store',
        page: 1,
        pageSize: 10,
        sortBy: 'created',
        sortOrder: 'desc'
      };

      service.searchItems(searchRequest).subscribe();

      const expectedUrl = `${mockApiUrl}/api/public-store/test-store/items?page=1&pageSize=10&sortBy=created&sortOrder=desc`;
      httpMock.expectOne(expectedUrl);
    });

    it('should exclude undefined optional parameters from URL', () => {
      const searchRequest = {
        orgSlug: 'test-store',
        page: 1,
        pageSize: 10,
        category: undefined,
        minPrice: undefined,
        maxPrice: undefined,
        sortBy: 'created',
        sortOrder: 'desc'
      };

      service.searchItems(searchRequest).subscribe();

      const expectedUrl = `${mockApiUrl}/api/public-store/test-store/items?page=1&pageSize=10&sortBy=created&sortOrder=desc`;
      httpMock.expectOne(expectedUrl);
    });
  });
});