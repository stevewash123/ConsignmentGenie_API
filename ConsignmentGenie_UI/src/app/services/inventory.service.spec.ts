import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InventoryService } from './inventory.service';
import { environment } from '../../environments/environment';
import { ItemListDto, PagedResult, ApiResponse } from '../models/inventory.model';

describe('InventoryService', () => {
  let service: InventoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [InventoryService]
    });
    service = TestBed.inject(InventoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get items with pagination', () => {
    const mockResponse: PagedResult<ItemListDto> = {
      items: [
        {
          id: 'item-1',
          sku: 'TEST001',
          title: 'Test Item',
          description: 'Test Description',
          category: 'Clothing',
          condition: 'Good' as any,
          price: 50.00,
          status: 'Available' as any,
          primaryImageUrl: 'test.jpg',
          providerId: 'prov-1',
          providerName: 'Test Provider',
          commissionRate: 50,
          createdAt: new Date(),
          updatedAt: new Date()
        }
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
      organizationId: 'org-1'
    };

    service.getItems({ page: 1, pageSize: 10 }).subscribe(response => {
      expect(response).toEqual(mockResponse);
      expect(response.items.length).toBe(1);
      expect(response.items[0].title).toBe('Test Item');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/items?page=1&pageSize=10`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should get single item', () => {
    const itemId = 'item-1';
    const mockResponse: ApiResponse<any> = {
      success: true,
      data: {
        id: itemId,
        sku: 'TEST001',
        title: 'Test Item',
        category: 'Clothing'
      }
    };

    service.getItem(itemId).subscribe(response => {
      expect(response.success).toBe(true);
      expect(response.data.id).toBe(itemId);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/items/${itemId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should generate SKU', () => {
    const prefix = 'TEST';
    const mockResponse: ApiResponse<string> = {
      success: true,
      data: 'TEST001'
    };

    service.generateSku(prefix).subscribe(response => {
      expect(response.success).toBe(true);
      expect(response.data).toBe('TEST001');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/items/generate-sku/${prefix}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});