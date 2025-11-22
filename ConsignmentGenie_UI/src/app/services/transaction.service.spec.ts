import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TransactionService, SalesMetrics, MetricsQueryParams } from './transaction.service';
import { PagedResult } from '../shared/models/api.models';
import { environment } from '../../environments/environment';

interface TransactionDto {
  id: string;
  saleDate: string;
  salePrice: number;
  paymentMethod: string;
  source: string;
  providerAmount: number;
  shopAmount: number;
}

describe('TransactionService', () => {
  let service: TransactionService;
  let httpTestingController: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/transactions`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TransactionService]
    });
    service = TestBed.inject(TransactionService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getTransactions', () => {
    it('should fetch transactions with query parameters', () => {
      const mockResponse: PagedResult<TransactionDto> = {
        items: [
          {
            id: '1',
            saleDate: '2024-01-01',
            salePrice: 100,
            paymentMethod: 'Card',
            source: 'InStore',
            providerAmount: 50,
            shopAmount: 50
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
        organizationId: 'org-123'
      };

      const queryParams = {
        startDate: new Date('2024-01-01'),
        endDate: new Date('2024-01-31'),
        page: 1,
        pageSize: 20,
        sortBy: 'saleDate',
        sortDirection: 'desc'
      };

      service.getTransactions(queryParams).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpTestingController.expectOne(request =>
        request.url === apiUrl &&
        request.params.get('startDate') === '2024-01-01T00:00:00.000Z' &&
        request.params.get('endDate') === '2024-01-31T00:00:00.000Z' &&
        request.params.get('page') === '1' &&
        request.params.get('pageSize') === '20' &&
        request.params.get('sortBy') === 'saleDate' &&
        request.params.get('sortDirection') === 'desc'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle empty query parameters', () => {
      const mockResponse: PagedResult<TransactionDto> = {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false,
        organizationId: 'org-123'
      };

      service.getTransactions({}).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpTestingController.expectOne(apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should include providerId and paymentMethod filters when provided', () => {
      const queryParams = {
        providerId: 'provider-123',
        paymentMethod: 'Cash',
        source: 'Square'
      };

      service.getTransactions(queryParams).subscribe();

      const req = httpTestingController.expectOne(request =>
        request.url === apiUrl &&
        request.params.get('providerId') === 'provider-123' &&
        request.params.get('paymentMethod') === 'Cash' &&
        request.params.get('source') === 'Square'
      );
      expect(req.request.method).toBe('GET');
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
    });
  });

  describe('getSalesMetrics', () => {
    it('should fetch sales metrics with query parameters', () => {
      const mockMetrics: SalesMetrics = {
        totalSales: 1000,
        totalProviderAmount: 500,
        totalShopAmount: 500,
        transactionCount: 10,
        averageTransactionValue: 100,
        totalTax: 80,
        topProviders: [],
        paymentMethodBreakdown: [],
        periodStart: new Date('2024-01-01'),
        periodEnd: new Date('2024-01-31')
      };

      const queryParams: MetricsQueryParams = {
        startDate: new Date('2024-01-01'),
        endDate: new Date('2024-01-31'),
        providerId: 'provider-123'
      };

      service.getSalesMetrics(queryParams).subscribe(response => {
        expect(response).toEqual(mockMetrics);
      });

      const req = httpTestingController.expectOne(request =>
        request.url === `${apiUrl}/metrics` &&
        request.params.get('startDate') === '2024-01-01T00:00:00.000Z' &&
        request.params.get('endDate') === '2024-01-31T00:00:00.000Z' &&
        request.params.get('providerId') === 'provider-123'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockMetrics);
    });

    it('should handle empty metrics query parameters', () => {
      const mockMetrics: SalesMetrics = {
        totalSales: 0,
        totalProviderAmount: 0,
        totalShopAmount: 0,
        transactionCount: 0,
        averageTransactionValue: 0,
        totalTax: 0,
        topProviders: [],
        paymentMethodBreakdown: [],
        periodStart: null,
        periodEnd: null
      };

      service.getSalesMetrics({}).subscribe(response => {
        expect(response).toEqual(mockMetrics);
      });

      const req = httpTestingController.expectOne(`${apiUrl}/metrics`);
      expect(req.request.method).toBe('GET');
      req.flush(mockMetrics);
    });
  });

  describe('getTransactionById', () => {
    it('should fetch single transaction by id', () => {
      const mockTransaction = {
        id: 'transaction-123',
        saleDate: '2024-01-01',
        salePrice: 150,
        paymentMethod: 'Card',
        source: 'InStore',
        providerAmount: 75,
        shopAmount: 75,
        notes: 'Test transaction'
      };

      service.getTransactionById('transaction-123').subscribe(response => {
        expect(response).toEqual(mockTransaction);
      });

      const req = httpTestingController.expectOne(`${apiUrl}/transaction-123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockTransaction);
    });
  });

  describe('createTransaction', () => {
    it('should create new transaction', () => {
      const createRequest = {
        itemId: 'item-123',
        salePrice: 200,
        paymentMethod: 'Cash',
        source: 'InStore',
        notes: 'New sale'
      };

      const mockResponse = {
        id: 'new-transaction-123',
        ...createRequest,
        saleDate: '2024-01-01',
        providerAmount: 100,
        shopAmount: 100
      };

      service.createTransaction(createRequest).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpTestingController.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createRequest);
      req.flush(mockResponse);
    });
  });

  describe('updateTransaction', () => {
    it('should update transaction', () => {
      const updateRequest = {
        paymentMethod: 'Card',
        notes: 'Updated notes'
      };

      const mockResponse = {
        id: 'transaction-123',
        saleDate: '2024-01-01',
        salePrice: 150,
        paymentMethod: 'Card',
        notes: 'Updated notes',
        providerAmount: 75,
        shopAmount: 75
      };

      service.updateTransaction('transaction-123', updateRequest).subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpTestingController.expectOne(`${apiUrl}/transaction-123`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(mockResponse);
    });
  });

  describe('deleteTransaction', () => {
    it('should delete transaction', () => {
      service.deleteTransaction('transaction-123').subscribe(response => {
        expect(response).toBe(true);
      });

      const req = httpTestingController.expectOne(`${apiUrl}/transaction-123`);
      expect(req.request.method).toBe('DELETE');
      req.flush(true);
    });
  });

  describe('error handling', () => {
    it('should handle HTTP errors in getTransactions', () => {
      let errorResponse: any;

      service.getTransactions({}).subscribe({
        next: () => fail('Expected error'),
        error: (error) => errorResponse = error
      });

      const req = httpTestingController.expectOne(apiUrl);
      req.flush({ error: 'Server Error' }, { status: 500, statusText: 'Internal Server Error' });

      expect(errorResponse.status).toBe(500);
    });

    it('should handle HTTP errors in getSalesMetrics', () => {
      let errorResponse: any;

      service.getSalesMetrics({}).subscribe({
        next: () => fail('Expected error'),
        error: (error) => errorResponse = error
      });

      const req = httpTestingController.expectOne(`${apiUrl}/metrics`);
      req.flush({ error: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

      expect(errorResponse.status).toBe(401);
    });
  });

  describe('query parameter building', () => {
    it('should skip null/undefined parameters', () => {
      const queryParams = {
        startDate: new Date('2024-01-01'),
        endDate: null,
        providerId: undefined,
        paymentMethod: 'Cash'
      };

      service.getTransactions(queryParams).subscribe();

      const req = httpTestingController.expectOne(request =>
        request.url === apiUrl &&
        request.params.get('startDate') === '2024-01-01T00:00:00.000Z' &&
        !request.params.has('endDate') &&
        !request.params.has('providerId') &&
        request.params.get('paymentMethod') === 'Cash'
      );
      req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
    });
  });
});