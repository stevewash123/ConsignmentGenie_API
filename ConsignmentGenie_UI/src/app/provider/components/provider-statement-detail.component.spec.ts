import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ProviderStatementDetailComponent } from './provider-statement-detail.component';
import { ProviderPortalService } from '../services/provider-portal.service';
import { StatementDto, StatementSaleLineDto, StatementPayoutLineDto } from '../models/provider.models';

describe('ProviderStatementDetailComponent', () => {
  let component: ProviderStatementDetailComponent;
  let fixture: ComponentFixture<ProviderStatementDetailComponent>;
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;
  let mockRouter: jasmine.SpyObj<Router>;

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
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        ProviderStatementDetailComponent,
        RouterTestingModule
      ],
      providers: [
        { provide: ProviderPortalService, useValue: providerSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(new Map([['id', 'stmt-1']]))
          }
        }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture = TestBed.createComponent(ProviderStatementDetailComponent);
    component = fixture.componentInstance;
  });

  beforeEach(() => {
    mockProviderService.getStatement.and.returnValue(of(mockStatement));
    mockProviderService.downloadStatementPdf.and.returnValue(of(new Blob(['pdf content'], { type: 'application/pdf' })));
    mockProviderService.regenerateStatement.and.returnValue(of(mockStatement));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load statement on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockProviderService.getStatement).toHaveBeenCalledWith('stmt-1');
    expect(component.statement).toEqual(mockStatement);
    expect(component.loading).toBeFalse();
  }));

  it('should display statement details', () => {
    component.statement = mockStatement;
    component.loading = false;
    fixture.detectChanges();

    const content = fixture.nativeElement.textContent;
    expect(content).toContain('STMT-2024-01');
    expect(content).toContain('January 2024');
    expect(content).toContain('John Doe');
    expect(content).toContain('Antique Shop');
    expect(content).toContain('$175.00');
  });

  it('should show loading state', () => {
    component.loading = true;
    fixture.detectChanges();

    const loadingContainer = fixture.nativeElement.querySelector('.loading-container');
    expect(loadingContainer).toBeTruthy();
    expect(loadingContainer.textContent).toContain('Loading statement...');
  });

  it('should show error state', () => {
    component.error = 'Failed to load statement';
    component.loading = false;
    fixture.detectChanges();

    const errorContainer = fixture.nativeElement.querySelector('.error-container');
    expect(errorContainer).toBeTruthy();
    expect(errorContainer.textContent).toContain('Failed to load statement');
  });

  it('should display sales table', () => {
    component.statement = mockStatement;
    component.loading = false;
    fixture.detectChanges();

    const salesRows = fixture.nativeElement.querySelectorAll('.sales-table .table-row');
    expect(salesRows.length).toBe(2);

    expect(salesRows[0].textContent).toContain('Vintage Lamp');
    expect(salesRows[0].textContent).toContain('VL-001');
    expect(salesRows[1].textContent).toContain('Antique Vase');
  });

  it('should display payouts table', () => {
    component.statement = mockStatement;
    component.loading = false;
    fixture.detectChanges();

    const payoutRows = fixture.nativeElement.querySelectorAll('.payouts-table .table-row');
    expect(payoutRows.length).toBe(1);

    expect(payoutRows[0].textContent).toContain('$95.00');
    expect(payoutRows[0].textContent).toContain('Bank Transfer');
  });

  it('should download PDF successfully', fakeAsync(() => {
    spyOn(component as any, 'downloadFile');

    component.downloadPdf();
    tick();

    expect(mockProviderService.downloadStatementPdf).toHaveBeenCalledWith('stmt-1');
    expect(component.downloadingPdf).toBeFalse();
    expect((component as any).downloadFile).toHaveBeenCalled();
  }));

  it('should handle PDF download error', fakeAsync(() => {
    mockProviderService.downloadStatementPdf.and.returnValue(throwError(() => new Error('Download failed')));
    spyOn(window, 'alert');
    spyOn(console, 'error');

    component.statement = mockStatement;
    component.downloadPdf();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error downloading PDF:', jasmine.any(Error));
    expect(window.alert).toHaveBeenCalledWith('Failed to download PDF. Please try again.');
    expect(component.downloadingPdf).toBeFalse();
  }));

  it('should regenerate statement successfully', fakeAsync(() => {
    component.statement = mockStatement;

    component.regenerateStatement();
    tick();

    expect(mockProviderService.regenerateStatement).toHaveBeenCalledWith('stmt-1');
    expect(component.regenerating).toBeFalse();
    expect(component.statement).toEqual(mockStatement);
  }));

  it('should handle regenerate statement error', fakeAsync(() => {
    mockProviderService.regenerateStatement.and.returnValue(throwError(() => new Error('Regenerate failed')));
    spyOn(window, 'alert');
    spyOn(console, 'error');

    component.statement = mockStatement;
    component.regenerateStatement();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error regenerating statement:', jasmine.any(Error));
    expect(window.alert).toHaveBeenCalledWith('Failed to regenerate statement. Please try again.');
    expect(component.regenerating).toBeFalse();
  }));

  it('should have back link to statements in template', () => {
    component.statement = mockStatement;
    component.loading = false;
    fixture.detectChanges();

    const backLink = fixture.nativeElement.querySelector('.back-link');
    expect(backLink).toBeTruthy();
    expect(backLink.getAttribute('routerLink')).toBe('/provider/statements');
  });

  it('should format dates correctly', () => {
    const date = new Date('2024-01-15T10:30:00Z');
    const formatted = component.formatDate(date);

    expect(formatted).toBe('Jan 15, 2024');
  });

  it('should format date range correctly', () => {
    const startDate = '2024-01-01';
    const endDate = '2024-01-31';
    const formatted = component.formatDateRange(startDate, endDate);

    expect(formatted).toBe('Jan 1 - Jan 31, 2024');
  });

  it('should handle error loading statement', fakeAsync(() => {
    mockProviderService.getStatement.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    component.loadStatement();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error loading statement:', jasmine.any(Error));
    expect(component.error).toBe('Failed to load statement. Please try again later.');
    expect(component.loading).toBeFalse();
  }));

  it('should retry loading statement', fakeAsync(() => {
    component.error = 'Some error';

    component.loadStatement();
    tick();

    expect(component.error).toBeNull();
    expect(component.loading).toBeFalse(); // Should be false after successful load
  }));

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

  it('should not download PDF when already downloading', () => {
    component.statement = mockStatement;
    component.downloadingPdf = true;

    component.downloadPdf();

    expect(mockProviderService.downloadStatementPdf).not.toHaveBeenCalled();
  });

  it('should not regenerate when already regenerating', () => {
    component.statement = mockStatement;
    component.regenerating = true;

    component.regenerateStatement();

    expect(mockProviderService.regenerateStatement).not.toHaveBeenCalled();
  });

  it('should show empty message when no sales', () => {
    component.statement = { ...mockStatement, sales: [] };
    component.loading = false;
    fixture.detectChanges();

    const noSalesMessage = fixture.nativeElement.querySelector('.no-data');
    expect(noSalesMessage).toBeTruthy();
    expect(noSalesMessage.textContent).toContain('No sales in this period');
  });

  it('should show empty message when no payouts', () => {
    component.statement = { ...mockStatement, payouts: [] };
    component.loading = false;
    fixture.detectChanges();

    const noPayoutsMessage = fixture.nativeElement.querySelector('.no-data');
    expect(noPayoutsMessage).toBeTruthy();
    expect(noPayoutsMessage.textContent).toContain('No payouts in this period');
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

  it('should handle route parameter changes', fakeAsync(() => {
    // Component should load statement when route params change through ngOnInit
    fixture.detectChanges();
    tick();

    expect(mockProviderService.getStatement).toHaveBeenCalledWith('stmt-1');
    expect(component.statement).toEqual(mockStatement);
  }));
});