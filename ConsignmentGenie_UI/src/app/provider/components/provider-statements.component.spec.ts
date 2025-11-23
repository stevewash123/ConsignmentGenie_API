import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ProviderStatementsComponent } from './provider-statements.component';
import { ProviderPortalService } from '../services/provider-portal.service';
import { StatementListDto } from '../models/provider.models';

@Component({
  template: '<div>Statement Detail</div>'
})
class MockStatementDetailComponent { }

describe('ProviderStatementsComponent', () => {
  let component: ProviderStatementsComponent;
  let fixture: ComponentFixture<ProviderStatementsComponent>;
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;

  const mockStatements: StatementListDto[] = [
    {
      statementId: '1',
      statementNumber: 'STMT-2024-01',
      periodStart: new Date('2024-01-01'),
      periodEnd: new Date('2024-01-31'),
      periodLabel: 'January 2024',
      totalEarnings: 150.00,
      closingBalance: 150.00,
      itemsSold: 5,
      status: 'Generated',
      hasPdf: true,
      generatedAt: new Date('2024-02-01')
    },
    {
      statementId: '2',
      statementNumber: 'STMT-2023-12',
      periodStart: new Date('2023-12-01'),
      periodEnd: new Date('2023-12-31'),
      periodLabel: 'December 2023',
      totalEarnings: 200.00,
      closingBalance: 75.00,
      itemsSold: 8,
      status: 'Viewed',
      hasPdf: true,
      generatedAt: new Date('2024-01-01')
    }
  ];

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ProviderPortalService', [
      'getStatements',
      'downloadStatementPdf'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        ProviderStatementsComponent,
        RouterTestingModule.withRoutes([
          { path: 'provider/statements/:id', component: MockStatementDetailComponent }
        ])
      ],
      providers: [
        { provide: ProviderPortalService, useValue: spy }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    fixture = TestBed.createComponent(ProviderStatementsComponent);
    component = fixture.componentInstance;
  });

  beforeEach(() => {
    mockProviderService.getStatements.and.returnValue(of(mockStatements));
    mockProviderService.downloadStatementPdf.and.returnValue(of(new Blob(['pdf content'], { type: 'application/pdf' })));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load statements on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockProviderService.getStatements).toHaveBeenCalled();
    expect(component.statements.length).toBe(2);
    expect(component.loading).toBeFalse();
  }));

  it('should sort statements by period start date descending', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(component.statements[0].periodStart).toEqual(new Date('2024-01-01')); // Most recent first
    expect(component.statements[1].periodStart).toEqual(new Date('2023-12-01'));
  }));

  it('should display statements', () => {
    component.statements = mockStatements;
    component.loading = false;
    fixture.detectChanges();

    const statementCards = fixture.nativeElement.querySelectorAll('.statement-card');
    expect(statementCards.length).toBe(2);

    expect(statementCards[0].textContent).toContain('January 2024');
    expect(statementCards[0].textContent).toContain('STMT-2024-01');
    expect(statementCards[0].textContent).toContain('$150.00');
  });

  it('should show loading state', () => {
    component.loading = true;
    fixture.detectChanges();

    const loadingContainer = fixture.nativeElement.querySelector('.loading-container');
    expect(loadingContainer).toBeTruthy();
    expect(loadingContainer.textContent).toContain('Loading statements...');
  });

  it('should show error state', () => {
    component.error = 'Failed to load statements';
    component.loading = false;
    fixture.detectChanges();

    const errorContainer = fixture.nativeElement.querySelector('.error-container');
    expect(errorContainer).toBeTruthy();
    expect(errorContainer.textContent).toContain('Failed to load statements');
  });

  it('should show empty state when no statements', () => {
    component.statements = [];
    component.loading = false;
    component.error = null;
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No statements available');
  });

  it('should download PDF successfully', fakeAsync(() => {
    spyOn(component as any, 'downloadFile');
    const statement = mockStatements[0];

    component.downloadPdf(statement);
    tick();

    expect(mockProviderService.downloadStatementPdf).toHaveBeenCalledWith('1');
    expect(component.downloadingPdf).toBeNull();
    expect((component as any).downloadFile).toHaveBeenCalledWith(jasmine.any(Blob), 'STMT-2024-01.pdf');
  }));

  it('should handle PDF download error', fakeAsync(() => {
    mockProviderService.downloadStatementPdf.and.returnValue(throwError(() => new Error('Download failed')));
    spyOn(window, 'alert');
    spyOn(console, 'error');
    const statement = mockStatements[0];

    component.downloadPdf(statement);
    tick();

    expect(console.error).toHaveBeenCalledWith('Error downloading PDF:', jasmine.any(Error));
    expect(window.alert).toHaveBeenCalledWith('Failed to download PDF. Please try again.');
    expect(component.downloadingPdf).toBeNull();
  }));

  it('should not download PDF if already downloading', () => {
    component.downloadingPdf = '1';
    const statement = mockStatements[0];

    component.downloadPdf(statement);

    expect(mockProviderService.downloadStatementPdf).not.toHaveBeenCalled();
  });

  it('should not download PDF if statement has no PDF', () => {
    const statement = { ...mockStatements[0], hasPdf: false };

    component.downloadPdf(statement);

    expect(mockProviderService.downloadStatementPdf).not.toHaveBeenCalled();
  });

  it('should identify new statements correctly', () => {
    const newStatement = {
      ...mockStatements[0],
      generatedAt: new Date() // Today
    };
    const oldStatement = {
      ...mockStatements[1],
      generatedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000) // 5 days ago
    };

    expect(component.isNewStatement(newStatement)).toBeTrue();
    expect(component.isNewStatement(oldStatement)).toBeFalse();
  });

  it('should return correct status class', () => {
    expect(component.getStatusClass('Generated')).toBe('generated');
    expect(component.getStatusClass('Viewed')).toBe('viewed');
    expect(component.getStatusClass('PENDING')).toBe('pending');
  });

  it('should format dates correctly', () => {
    const date = new Date('2024-01-15');
    const formatted = component.formatDate(date);

    expect(formatted).toBe('Jan 15, 2024');
  });

  it('should calculate total earnings', () => {
    component.statements = mockStatements;

    const total = component.getTotalEarnings();

    expect(total).toBe(350.00); // 150 + 200
  });

  it('should get current balance from most recent statement', () => {
    component.statements = mockStatements;

    const currentBalance = component.getCurrentBalance();

    expect(currentBalance).toBe(150.00); // From first (most recent) statement
  });

  it('should return 0 for current balance when no statements', () => {
    component.statements = [];

    const currentBalance = component.getCurrentBalance();

    expect(currentBalance).toBe(0);
  });

  it('should calculate total items sold', () => {
    component.statements = mockStatements;

    const totalItems = component.getTotalItemsSold();

    expect(totalItems).toBe(13); // 5 + 8
  });

  it('should track by statement ID', () => {
    const result = component.trackByStatementId(0, mockStatements[0]);

    expect(result).toBe('1');
  });

  it('should handle error loading statements', fakeAsync(() => {
    mockProviderService.getStatements.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    component.loadStatements();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error loading statements:', jasmine.any(Error));
    expect(component.error).toBe('Failed to load statements. Please try again later.');
    expect(component.loading).toBeFalse();
  }));

  it('should retry loading statements', fakeAsync(() => {
    component.error = 'Some error';

    component.loadStatements();
    tick();

    expect(component.error).toBeNull();
    expect(component.loading).toBeFalse(); // Should be false after successful load
  }));

  it('should show quick stats when statements exist', () => {
    component.statements = mockStatements;
    fixture.detectChanges();

    const quickStats = fixture.nativeElement.querySelector('.quick-stats');
    expect(quickStats).toBeTruthy();

    const statCards = fixture.nativeElement.querySelectorAll('.stat-card');
    expect(statCards.length).toBe(4); // Total Statements, Total Earnings, Current Balance, Items Sold
  });

  it('should not show quick stats when no statements', () => {
    component.statements = [];
    fixture.detectChanges();

    const quickStats = fixture.nativeElement.querySelector('.quick-stats');
    expect(quickStats).toBeFalsy();
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