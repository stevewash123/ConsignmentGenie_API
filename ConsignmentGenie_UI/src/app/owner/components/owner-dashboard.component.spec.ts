import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { OwnerDashboardComponent } from './owner-dashboard.component';
import { AuthService } from '../../services/auth.service';
import { ProviderService } from '../../services/provider.service';
import { TransactionService } from '../../services/transaction.service';
import { PayoutService, PayoutStatus } from '../../services/payout.service';

describe('OwnerDashboardComponent', () => {
  let component: OwnerDashboardComponent;
  let fixture: ComponentFixture<OwnerDashboardComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let providerService: jasmine.SpyObj<ProviderService>;
  let transactionService: jasmine.SpyObj<TransactionService>;
  let payoutService: jasmine.SpyObj<PayoutService>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['getCurrentUser']);
    const providerServiceSpy = jasmine.createSpyObj('ProviderService', ['getProviders']);
    const transactionServiceSpy = jasmine.createSpyObj('TransactionService', ['getSalesMetrics']);
    const payoutServiceSpy = jasmine.createSpyObj('PayoutService', ['getPayoutReports']);

    await TestBed.configureTestingModule({
      imports: [OwnerDashboardComponent, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ProviderService, useValue: providerServiceSpy },
        { provide: TransactionService, useValue: transactionServiceSpy },
        { provide: PayoutService, useValue: payoutServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OwnerDashboardComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    providerService = TestBed.inject(ProviderService) as jasmine.SpyObj<ProviderService>;
    transactionService = TestBed.inject(TransactionService) as jasmine.SpyObj<TransactionService>;
    payoutService = TestBed.inject(PayoutService) as jasmine.SpyObj<PayoutService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with null summary and zero provider count', () => {
    expect(component.summary()).toBeNull();
    expect(component.activeProviderCount()).toBe(0);
  });

  describe('getCurrentUser', () => {
    it('should return current user from AuthService', () => {
      const mockUser = {
        userId: 'user-123',
        email: 'test@example.com',
        organizationName: 'Test Shop',
        organizationId: 'org-456'
      };

      authService.getCurrentUser.and.returnValue(mockUser);

      const user = component.getCurrentUser();
      expect(user).toEqual(mockUser);
      expect(authService.getCurrentUser).toHaveBeenCalled();
    });

    it('should return null when no user is authenticated', () => {
      authService.getCurrentUser.and.returnValue(null);

      const user = component.getCurrentUser();
      expect(user).toBeNull();
    });
  });

  describe('loadDashboardData', () => {
    beforeEach(() => {
      // Set up default mock responses
      providerService.getProviders.and.returnValue(of([
        { id: '1', displayName: 'Provider 1', status: 1, isActive: true },
        { id: '2', displayName: 'Provider 2', status: 1, isActive: true },
        { id: '3', displayName: 'Provider 3', status: 2, isActive: false }
      ]));

      transactionService.getSalesMetrics.and.returnValue(of({
        totalSales: 5000,
        totalProviderAmount: 2500,
        totalShopAmount: 2500,
        transactionCount: 25,
        averageTransactionValue: 200,
        totalTax: 400,
        topProviders: [],
        paymentMethodBreakdown: [],
        periodStart: new Date('2024-01-01'),
        periodEnd: new Date('2024-01-31')
      }));

      payoutService.getPayoutReports.and.returnValue(of([
        { id: '1', payoutAmount: 500, status: PayoutStatus.Pending },
        { id: '2', payoutAmount: 300, status: PayoutStatus.Pending }
      ]));
    });

    it('should load dashboard data and update summary', async () => {
      component.ngOnInit();

      // Wait for promises to resolve
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(providerService.getProviders).toHaveBeenCalled();
      expect(transactionService.getSalesMetrics).toHaveBeenCalledWith({
        startDate: jasmine.any(Date),
        endDate: jasmine.any(Date)
      });
      expect(payoutService.getPayoutReports).toHaveBeenCalledWith(undefined, PayoutStatus.Pending);

      const summary = component.summary();
      expect(summary).toBeTruthy();
      expect(summary!.activeProviders).toBe(2); // Only active providers
      expect(summary!.recentSales).toBe(5000);
      expect(summary!.recentSalesCount).toBe(25);
      expect(summary!.pendingPayouts).toBe(800); // 500 + 300
      expect(summary!.pendingPayoutCount).toBe(2);

      expect(component.activeProviderCount()).toBe(2);
    });

    it('should handle empty provider list', async () => {
      providerService.getProviders.and.returnValue(of([]));

      component.ngOnInit();
      await new Promise(resolve => setTimeout(resolve, 0));

      const summary = component.summary();
      expect(summary!.activeProviders).toBe(0);
      expect(component.activeProviderCount()).toBe(0);
    });

    it('should handle null sales metrics response', async () => {
      transactionService.getSalesMetrics.and.returnValue(of(null as any));

      component.ngOnInit();
      await new Promise(resolve => setTimeout(resolve, 0));

      const summary = component.summary();
      expect(summary!.recentSales).toBe(0);
      expect(summary!.recentSalesCount).toBe(0);
    });

    it('should handle empty pending payouts', async () => {
      payoutService.getPayoutReports.and.returnValue(of([]));

      component.ngOnInit();
      await new Promise(resolve => setTimeout(resolve, 0));

      const summary = component.summary();
      expect(summary!.pendingPayouts).toBe(0);
      expect(summary!.pendingPayoutCount).toBe(0);
    });

    it('should handle API errors gracefully', async () => {
      providerService.getProviders.and.returnValue(of(null as any));
      transactionService.getSalesMetrics.and.returnValue(of(null as any));
      payoutService.getPayoutReports.and.returnValue(of(null as any));

      spyOn(console, 'error');

      component.ngOnInit();
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(console.error).toHaveBeenCalled();

      // Should still create a summary with default values
      const summary = component.summary();
      expect(summary).toBeTruthy();
      expect(summary!.activeProviders).toBe(0);
      expect(summary!.recentSales).toBe(0);
    });

    it('should request metrics for last 30 days', () => {
      component.ngOnInit();

      const salesMetricsCall = transactionService.getSalesMetrics.calls.mostRecent();
      const params = salesMetricsCall.args[0];

      expect(params.startDate).toBeInstanceOf(Date);
      expect(params.endDate).toBeInstanceOf(Date);

      // Check that the date range is approximately 30 days
      const daysDiff = (params.endDate.getTime() - params.startDate.getTime()) / (1000 * 60 * 60 * 24);
      expect(daysDiff).toBeCloseTo(30, 1);
    });
  });

  describe('template integration', () => {
    it('should display organization name from getCurrentUser', () => {
      const mockUser = {
        userId: 'user-123',
        email: 'test@example.com',
        organizationName: 'My Test Shop',
        organizationId: 'org-456'
      };

      authService.getCurrentUser.and.returnValue(mockUser);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('My Test Shop Dashboard');
    });

    it('should display fallback text when no user', () => {
      authService.getCurrentUser.and.returnValue(null);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Shop Dashboard');
      expect(compiled.textContent).toContain('Welcome back, Owner!');
    });

    it('should show loading state initially', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Loading dashboard data...');
    });
  });
});