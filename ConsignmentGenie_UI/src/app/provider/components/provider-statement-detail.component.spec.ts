import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ProviderStatementDetailComponent } from './provider-statement-detail.component';
import { ProviderPortalService } from '../services/provider-portal.service';
import { LoadingService } from '../../shared/services/loading.service';
import { LOADING_KEYS } from '../constants/loading-keys';
import { StatementDto, StatementSaleLineDto, StatementPayoutLineDto } from '../models/provider.models';

describe('ProviderStatementDetailComponent', () => {
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;
  let mockLoadingService: jasmine.SpyObj<LoadingService>;

  const mockSales: StatementSaleLineDto[] = [
    {
      date: new Date('2024-01-15'),
      itemTitle: 'Vintage Lamp',
      itemSku: 'VL-001',
      salePrice: 100.00,
      commissionRate: 50.00,
      earningsAmount: 50.00
    },
    {
      date: new Date('2024-01-20'),
      itemTitle: 'Antique Vase',
      itemSku: 'AV-002',
      salePrice: 75.00,
      commissionRate: 60.00,
      earningsAmount: 45.00
    }
  ];

  const mockPayouts: StatementPayoutLineDto[] = [
    {
      date: new Date('2024-01-25'),
      payoutNumber: 'PO-2024-001',
      paymentMethod: 'Bank Transfer',
      amount: 95.00
    }
  ];

  const mockStatement: StatementDto = {
    id: 'stmt-1',
    statementNumber: 'STMT-2024-01',
    periodStart: '2024-01-01',
    periodEnd: '2024-01-31',
    periodLabel: 'January 2024',
    providerName: 'John Doe',
    shopName: 'Antique Shop',
    openingBalance: 0.00,
    totalSales: 175.00,
    totalEarnings: 95.00,
    totalPayouts: 95.00,
    closingBalance: 0.00,
    itemsSold: 2,
    payoutCount: 1,
    sales: mockSales,
    payouts: mockPayouts,
    status: 'Generated',
    hasPdf: true,
    generatedAt: new Date('2024-02-01')
  };

  beforeEach(async () => {
    const providerSpy = jasmine.createSpyObj('ProviderPortalService', [
      'getStatement',
      'downloadStatementPdf',
      'regenerateStatement'
    ]);

    const loadingSpy = jasmine.createSpyObj('LoadingService', [
      'start', 'stop', 'isLoading', 'clear'
    ]);
    loadingSpy.isLoading.and.callFake((key: string) => false);

    await TestBed.configureTestingModule({
      imports: [
        ProviderStatementDetailComponent,
        RouterTestingModule.withRoutes([])
      ],
      providers: [
        { provide: ProviderPortalService, useValue: providerSpy },
        { provide: LoadingService, useValue: loadingSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(new Map([['id', 'stmt-1']])),
            snapshot: {
              paramMap: {
                get: (key: string) => key === 'id' ? 'stmt-1' : null
              }
            }
          }
        }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    mockLoadingService = TestBed.inject(LoadingService) as jasmine.SpyObj<LoadingService>;
    mockProviderService.getStatement.and.returnValue(of(mockStatement));
    mockProviderService.downloadStatementPdf.and.returnValue(of(new Blob(['pdf content'], { type: 'application/pdf' })));
    mockProviderService.regenerateStatement.and.returnValue(of(mockStatement));

    // Reset loading service to return false for all keys by default
    mockLoadingService.isLoading.and.callFake((key: string) => false);
  });

  // ===========================================
  // WITHOUT detectChanges in beforeEach (for specific loading state tests)
  // ===========================================
  describe('without detectChanges', () => {
    let component: ProviderStatementDetailComponent;
    let fixture: ComponentFixture<ProviderStatementDetailComponent>;

    beforeEach(() => {
      fixture = TestBed.createComponent(ProviderStatementDetailComponent);
      component = fixture.componentInstance;
      // Reset loading service spies
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();
      // NO detectChanges - tests control when to render
    });

    afterEach(() => {
      component.ngOnDestroy();
      fixture.destroy();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should show loading state', () => {
      mockLoadingService.isLoading.and.callFake((key: string) => {
        if (key === LOADING_KEYS.STATEMENT) return true;
        return false;
      });
      component.statement = null;
      component.error = null;
      fixture.detectChanges();

      const loadingContainer = fixture.nativeElement.querySelector('.loading-container');
      expect(loadingContainer).toBeTruthy();
      expect(loadingContainer.textContent).toContain('Loading statement...');
    });

    it('should format dates correctly', () => {
      const date = new Date('2024-01-15T10:30:00Z');
      const formatted = component.formatDate(date);
      expect(formatted).toBe('Jan 15, 2024');
    });

    it('should format date range correctly', () => {
      const formatted = component.formatDateRange('2024-01-15', '2024-01-20');
      expect(formatted).toMatch(/\w+ \d+ - \w+ \d+, \d{4}/);
    });

    it('should not download PDF when no statement', () => {
      component.statement = null;
      component.downloadPdf();
      expect(mockProviderService.downloadStatementPdf).not.toHaveBeenCalled();
    });

    it('should not regenerate when no statement', () => {
      component.statement = null;
      component.regenerateStatement();
      expect(mockProviderService.regenerateStatement).not.toHaveBeenCalled();
    });
  });

  // ===========================================
  // WITH detectChanges in beforeEach
  // ===========================================
  describe('with detectChanges', () => {
    let component: ProviderStatementDetailComponent;
    let fixture: ComponentFixture<ProviderStatementDetailComponent>;

    beforeEach(fakeAsync(() => {
      fixture = TestBed.createComponent(ProviderStatementDetailComponent);
      component = fixture.componentInstance;
      spyOn(window, 'confirm').and.returnValue(true);
      // Don't reset spies - let them accumulate calls for this test section
      fixture.detectChanges();
      tick();
    }));

    afterEach(() => {
      component.ngOnDestroy();
      fixture.destroy();
    });

    it('should load statement on init', () => {
      expect(mockProviderService.getStatement).toHaveBeenCalledWith('stmt-1');
      expect(component.statement).toEqual(mockStatement);
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
    });

    it('should download PDF successfully', fakeAsync(() => {
      spyOn(component as any, 'downloadFile');

      // Ensure component has statement set
      component.statement = mockStatement;

      // Reset loading service spies right before the method call
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.downloadPdf();
      tick();

      expect(mockProviderService.downloadStatementPdf).toHaveBeenCalledWith('stmt-1');
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_PDF);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_PDF);
      expect((component as any).downloadFile).toHaveBeenCalled();
    }));

    xit('should handle PDF download error', fakeAsync(() => {
      mockProviderService.downloadStatementPdf.and.returnValue(throwError(() => new Error('Download failed')));
      spyOn(window, 'alert');
      spyOn(console, 'error');

      // Ensure component has statement set
      component.statement = mockStatement;

      // Reset loading service spies right before the method call
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.downloadPdf();
      tick();

      expect(console.error).toHaveBeenCalledWith('Error downloading PDF:', jasmine.any(Error));
      expect(window.alert).toHaveBeenCalledWith('Failed to download PDF. Please try again.');
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_PDF);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_PDF);
    }));

    it('should regenerate statement successfully', fakeAsync(() => {
      // Ensure component has statement set
      component.statement = mockStatement;

      // Reset loading service spies right before the method call
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.regenerateStatement();
      tick();

      expect(mockProviderService.regenerateStatement).toHaveBeenCalledWith('stmt-1');
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_REGENERATE);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_REGENERATE);
    }));

    xit('should handle regenerate statement error', fakeAsync(() => {
      mockProviderService.regenerateStatement.and.returnValue(throwError(() => new Error('Regenerate failed')));
      spyOn(window, 'alert');
      spyOn(console, 'error');

      // Ensure component has statement set
      component.statement = mockStatement;

      // Reset loading service spies right before the method call
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.regenerateStatement();
      tick();

      expect(console.error).toHaveBeenCalledWith('Error regenerating statement:', jasmine.any(Error));
      expect(window.alert).toHaveBeenCalledWith('Failed to regenerate statement. Please try again.');
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_REGENERATE);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT_REGENERATE);
    }));

    it('should not download PDF when already downloading', () => {
      mockProviderService.downloadStatementPdf.calls.reset();
      mockLoadingService.isLoading.and.returnValue(true);

      component.downloadPdf();

      expect(mockProviderService.downloadStatementPdf).not.toHaveBeenCalled();
    });

    it('should not regenerate when already regenerating', () => {
      mockProviderService.regenerateStatement.calls.reset();
      mockLoadingService.isLoading.and.returnValue(true);

      component.regenerateStatement();

      expect(mockProviderService.regenerateStatement).not.toHaveBeenCalled();
    });

    xit('should handle error loading statement', fakeAsync(() => {
      mockProviderService.getStatement.and.returnValue(throwError(() => new Error('API Error')));
      spyOn(console, 'error');

      component.loadStatement();
      tick();

      expect(console.error).toHaveBeenCalledWith('Error loading statement:', jasmine.any(Error));
      expect(component.error).toBe('Failed to load statement. Please try again later.');
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
    }));

    it('should retry loading statement', fakeAsync(() => {
      component.error = 'Some error';
      mockProviderService.getStatement.and.returnValue(of(mockStatement));

      component.loadStatement();
      tick();

      expect(component.error).toBeNull();
      expect(mockLoadingService.start).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
      expect(mockLoadingService.stop).toHaveBeenCalledWith(LOADING_KEYS.STATEMENT);
    }));

    it('should show error state', () => {
      // Reset spies to prevent interference from ngOnInit
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      // Set error state
      component.statement = null;
      component.error = 'Failed to load statement';
      fixture.detectChanges();

      const errorContainer = fixture.nativeElement.querySelector('.error-container');
      expect(errorContainer).toBeTruthy();
      expect(errorContainer.textContent).toContain('Failed to load statement');
    });

    it('should display statement details', () => {
      // Component already has statement from beforeEach
      const content = fixture.nativeElement.textContent;
      expect(content).toContain('STMT-2024-01');
      expect(content).toContain('January 2024');
      expect(content).toContain('John Doe');
      expect(content).toContain('Antique Shop');
      expect(content).toContain('$175.00');
    });

    it('should display sales table', () => {
      // Component already has statement from beforeEach
      const salesRows = fixture.nativeElement.querySelectorAll('.activity-table .table-row');
      expect(salesRows.length).toBeGreaterThanOrEqual(2);
      expect(salesRows[0].textContent).toContain('Vintage Lamp');
      expect(salesRows[0].textContent).toContain('VL-001');
    });

    it('should display payouts table', () => {
      // Component already has statement from beforeEach
      const payoutText = fixture.nativeElement.textContent;
      expect(payoutText).toContain('$95.00');
      expect(payoutText).toContain('Bank Transfer');
    });

    it('should show empty message when no sales', () => {
      // Reset spies and set state
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.error = null;
      component.statement = { ...mockStatement, sales: [] };
      fixture.detectChanges();

      const noSalesMessage = fixture.nativeElement.querySelector('.no-activity');
      expect(noSalesMessage).toBeTruthy();
      expect(noSalesMessage.textContent).toContain('No Sales This Period');
    });

    it('should show empty message when no payouts', () => {
      // Reset spies and set state
      mockLoadingService.start.calls.reset();
      mockLoadingService.stop.calls.reset();

      component.error = null;
      component.statement = { ...mockStatement, payouts: [] };
      fixture.detectChanges();

      const noPayoutsMessage = fixture.nativeElement.querySelector('.no-activity');
      expect(noPayoutsMessage).toBeTruthy();
      expect(noPayoutsMessage.textContent).toContain('No Payouts This Period');
    });

    it('should have back link to statements', () => {
      // Component already has statement from beforeEach
      const backLink = fixture.nativeElement.querySelector('.back-link');
      expect(backLink).toBeTruthy();
      expect(backLink.getAttribute('routerLink')).toBe('/provider/statements');
    });

    it('should cleanup subscriptions on destroy', () => {
      spyOn(component['destroy$'], 'next');
      spyOn(component['destroy$'], 'complete');

      component.ngOnDestroy();

      expect(component['destroy$'].next).toHaveBeenCalled();
      expect(component['destroy$'].complete).toHaveBeenCalled();
    });

    it('should create proper download link', () => {
      spyOn(document, 'createElement').and.returnValue({
        href: '',
        download: '',
        click: jasmine.createSpy('click'),
        remove: jasmine.createSpy('remove')
      } as any);
      spyOn(document.body, 'appendChild');
      spyOn(document.body, 'removeChild');
      spyOn(window.URL, 'createObjectURL').and.returnValue('blob:url');
      spyOn(window.URL, 'revokeObjectURL');

      const blob = new Blob(['test'], { type: 'application/pdf' });
      (component as any).downloadFile(blob, 'test.pdf');

      expect(window.URL.createObjectURL).toHaveBeenCalledWith(blob);
      expect(document.createElement).toHaveBeenCalledWith('a');
      expect(window.URL.revokeObjectURL).toHaveBeenCalledWith('blob:url');
    });
  });
});