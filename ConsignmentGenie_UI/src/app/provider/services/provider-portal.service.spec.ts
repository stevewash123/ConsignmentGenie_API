import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ProviderPortalService } from './provider-portal.service';
import { environment } from '../../../environments/environment';

describe('ProviderPortalService', () => {
  let service: ProviderPortalService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/provider`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ProviderPortalService]
    });
    service = TestBed.inject(ProviderPortalService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Dashboard', () => {
    it('should get dashboard data', () => {
      const mockDashboard = {
        shopName: 'Test Shop',
        providerName: 'Test Provider',
        totalItems: 50,
        availableItems: 30,
        soldItems: 15,
        inventoryValue: 1500.00,
        pendingBalance: 250.00,
        totalEarningsAllTime: 1500.00,
        earningsThisMonth: 250.00,
        recentSales: []
      };

      service.getDashboard().subscribe(dashboard => {
        expect(dashboard).toEqual(jasmine.objectContaining({
          shopName: 'Test Shop',
          totalItems: 50
        }));
      });

      const req = httpMock.expectOne(`${apiUrl}/dashboard`);
      expect(req.request.method).toBe('GET');
      req.flush(mockDashboard);
    });
  });

  describe('Items', () => {
    it('should get my items without query', () => {
      const mockResponse = {
        items: [],
        totalCount: 0,
        pageSize: 10,
        page: 1,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false
      };

      service.getMyItems().subscribe(response => {
        expect(response.totalCount).toBe(0);
      });

      const req = httpMock.expectOne(`${apiUrl}/items`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should get specific item', () => {
      const itemId = '123';
      const mockItem = {
        itemId: itemId,
        sku: 'TEST-001',
        title: 'Test Item',
        description: 'Test Description',
        primaryImageUrl: 'test.jpg',
        price: 100.00,
        myEarnings: 70.00,
        category: 'Electronics',
        status: 'Active',
        receivedDate: new Date(),
        imageUrls: [],
        notes: 'Test notes'
      };

      service.getMyItem(itemId).subscribe(item => {
        expect(item.title).toBe('Test Item');
      });

      const req = httpMock.expectOne(`${apiUrl}/items/${itemId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockItem);
    });
  });

  describe('Sales', () => {
    it('should get my sales', () => {
      const mockResponse = {
        items: [],
        totalCount: 0,
        pageSize: 10,
        page: 1,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false
      };

      service.getMySales().subscribe(response => {
        expect(response.totalCount).toBe(0);
      });

      const req = httpMock.expectOne(`${apiUrl}/sales`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('Profile', () => {
    it('should get profile', () => {
      const mockProfile = {
        providerId: '123',
        fullName: 'Test Provider',
        email: 'test@example.com',
        phone: '555-0123',
        commissionRate: 70.00,
        emailNotifications: true,
        memberSince: new Date(),
        organizationName: 'Test Business'
      };

      service.getProfile().subscribe(profile => {
        expect(profile.fullName).toBe('Test Provider');
      });

      const req = httpMock.expectOne(`${apiUrl}/profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockProfile);
    });

    it('should update profile', () => {
      const updateRequest = {
        fullName: 'Updated Provider',
        phone: '555-9999',
        emailNotifications: false
      };

      const mockUpdatedProfile = {
        providerId: '123',
        fullName: 'Updated Provider',
        email: 'test@example.com',
        phone: '555-9999',
        commissionRate: 70.00,
        emailNotifications: false,
        memberSince: new Date(),
        organizationName: 'Test Business'
      };

      service.updateProfile(updateRequest).subscribe(profile => {
        expect(profile.fullName).toBe('Updated Provider');
      });

      const req = httpMock.expectOne(`${apiUrl}/profile`);
      expect(req.request.method).toBe('PUT');
      req.flush(mockUpdatedProfile);
    });
  });

  describe('Notifications', () => {
    it('should get notifications', () => {
      const mockResponse = {
        items: [],
        totalCount: 0,
        pageSize: 10,
        page: 1,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false
      };

      service.getNotifications().subscribe(response => {
        expect(response.totalCount).toBe(0);
      });

      const req = httpMock.expectOne(`${apiUrl}/notifications`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should get unread count', () => {
      const mockCount = { count: 5 };

      service.getUnreadNotificationCount().subscribe(result => {
        expect(result.count).toBe(5);
      });

      const req = httpMock.expectOne(`${apiUrl}/notifications/unread-count`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCount);
    });

    it('should mark notification as read', () => {
      const notificationId = '123';

      service.markNotificationAsRead(notificationId).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/notifications/${notificationId}/read`);
      expect(req.request.method).toBe('PUT');
      req.flush({});
    });

    it('should get notification preferences', () => {
      const mockPreferences = {
        emailEnabled: true,
        emailItemSold: true,
        emailPayoutProcessed: true,
        emailPayoutPending: false,
        emailItemExpired: false,
        emailStatementReady: true,
        emailAccountUpdate: false,
        digestMode: 'instant',
        digestTime: '09:00',
        digestDay: 1,
        payoutPendingThreshold: 50.00
      };

      service.getNotificationPreferences().subscribe(prefs => {
        expect(prefs.emailEnabled).toBeTrue();
      });

      const req = httpMock.expectOne(`${apiUrl}/notifications/preferences`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPreferences);
    });
  });

  describe('Statements', () => {
    it('should get statements', () => {
      const mockStatements = [
        {
          statementId: '1',
          statementNumber: 'ST-001',
          periodStart: new Date(),
          periodEnd: new Date(),
          periodLabel: 'December 2023',
          itemsSold: 5,
          totalEarnings: 1000.00,
          closingBalance: 1000.00,
          status: 'Generated',
          hasPdf: true,
          generatedAt: new Date()
        }
      ];

      service.getStatements().subscribe(statements => {
        expect(statements.length).toBe(1);
      });

      const req = httpMock.expectOne(`${apiUrl}/statements`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStatements);
    });

    it('should get specific statement', () => {
      const statementId = '123';
      const mockStatement = {
        id: statementId,
        statementNumber: 'ST-001',
        periodStart: '2023-12-01',
        periodEnd: '2023-12-31',
        periodLabel: 'December 2023',
        providerName: 'Test Provider',
        shopName: 'Test Shop',
        openingBalance: 0,
        totalSales: 1500.00,
        totalEarnings: 1000.00,
        totalPayouts: 0,
        closingBalance: 1000.00,
        itemsSold: 5,
        payoutCount: 0,
        sales: [],
        payouts: [],
        status: 'Generated',
        hasPdf: true,
        generatedAt: new Date()
      };

      service.getStatement(statementId).subscribe(statement => {
        expect(statement.id).toBe(statementId);
      });

      const req = httpMock.expectOne(`${apiUrl}/statements/${statementId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStatement);
    });
  });
});