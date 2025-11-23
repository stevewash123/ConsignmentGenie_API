import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { ProviderNotificationsComponent } from './provider-notifications.component';
import { ProviderPortalService } from '../services/provider-portal.service';
import { NotificationDto, PagedResult } from '../models/provider.models';

describe('ProviderNotificationsComponent', () => {
  let component: ProviderNotificationsComponent;
  let fixture: ComponentFixture<ProviderNotificationsComponent>;
  let mockProviderService: jasmine.SpyObj<ProviderPortalService>;

  const mockNotifications: NotificationDto[] = [
    {
      notificationId: '1',
      type: 'ItemSold',
      title: 'Item Sold',
      message: 'Your item has been sold!',
      isRead: false,
      createdAt: new Date('2024-01-15'),
      timeAgo: '2 hours ago',
      metadata: {
        itemSku: 'SKU-001',
        salePrice: 50.00,
        earningsAmount: 25.00
      }
    },
    {
      notificationId: '2',
      type: 'PayoutProcessed',
      title: 'Payout Processed',
      message: 'Your payout has been processed',
      isRead: true,
      createdAt: new Date('2024-01-14'),
      timeAgo: '1 day ago',
      metadata: {
        payoutAmount: 100.00
      }
    }
  ];

  const mockPagedResult: PagedResult<NotificationDto> = {
    items: mockNotifications,
    totalCount: 2,
    page: 1,
    pageSize: 10,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ProviderPortalService', [
      'getNotifications',
      'markNotificationAsRead',
      'markAllNotificationsAsRead',
      'deleteNotification'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        ProviderNotificationsComponent,
        FormsModule,
        RouterTestingModule
      ],
      providers: [
        { provide: ProviderPortalService, useValue: spy }
      ]
    }).compileComponents();

    mockProviderService = TestBed.inject(ProviderPortalService) as jasmine.SpyObj<ProviderPortalService>;
    fixture = TestBed.createComponent(ProviderNotificationsComponent);
    component = fixture.componentInstance;
  });

  beforeEach(() => {
    mockProviderService.getNotifications.and.returnValue(of(mockPagedResult));
    mockProviderService.markNotificationAsRead.and.returnValue(of(void 0));
    mockProviderService.markAllNotificationsAsRead.and.returnValue(of(void 0));
    mockProviderService.deleteNotification.and.returnValue(of(void 0));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load notifications on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockProviderService.getNotifications).toHaveBeenCalledWith({
      unreadOnly: false,
      type: undefined,
      page: 1,
      pageSize: 10
    });
    expect(component.notifications).toEqual(mockNotifications);
    expect(component.pagedResult).toEqual(mockPagedResult);
    expect(component.loading).toBeFalse();
  }));

  it('should display notifications', () => {
    component.notifications = mockNotifications;
    component.loading = false;
    fixture.detectChanges();

    const notificationItems = fixture.nativeElement.querySelectorAll('.notification-item');
    expect(notificationItems.length).toBe(2);

    expect(notificationItems[0].textContent).toContain('Item Sold');
    expect(notificationItems[0].textContent).toContain('Your item has been sold!');
    expect(notificationItems[1].textContent).toContain('Payout Processed');
  });

  it('should show unread notification as unread', () => {
    component.notifications = mockNotifications;
    component.loading = false;
    fixture.detectChanges();

    const unreadNotification = fixture.nativeElement.querySelector('.notification-item.unread');
    expect(unreadNotification).toBeTruthy();
  });

  it('should handle loading state', () => {
    component.loading = true;
    fixture.detectChanges();

    const loadingContainer = fixture.nativeElement.querySelector('.loading-container');
    expect(loadingContainer).toBeTruthy();
    expect(loadingContainer.textContent).toContain('Loading notifications...');
  });

  it('should handle empty state', () => {
    component.notifications = [];
    component.loading = false;
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No notifications');
  });

  it('should toggle unread only filter', () => {
    expect(component.showUnreadOnly).toBeFalse();

    component.toggleUnreadOnly();

    expect(component.showUnreadOnly).toBeTrue();
    expect(component.currentPage).toBe(1);
    expect(mockProviderService.getNotifications).toHaveBeenCalledWith({
      unreadOnly: true,
      type: undefined,
      page: 1,
      pageSize: 10
    });
  });

  it('should filter by notification type', () => {
    component.selectedType = 'ItemSold';
    component.loadNotifications();

    expect(mockProviderService.getNotifications).toHaveBeenCalledWith({
      unreadOnly: false,
      type: 'ItemSold',
      page: 1,
      pageSize: 10
    });
  });

  it('should mark notification as read', fakeAsync(() => {
    const notification = mockNotifications[0];
    const mockEvent = new Event('click');
    spyOn(mockEvent, 'stopPropagation');

    component.markAsRead(notification, mockEvent);
    tick();

    expect(mockEvent.stopPropagation).toHaveBeenCalled();
    expect(mockProviderService.markNotificationAsRead).toHaveBeenCalledWith('1');
    expect(notification.isRead).toBeTrue();
  }));

  it('should mark all notifications as read', fakeAsync(() => {
    component.notifications = [...mockNotifications];
    component.notifications[0].isRead = false;
    component.notifications[1].isRead = false;

    component.markAllAsRead();
    tick();

    expect(mockProviderService.markAllNotificationsAsRead).toHaveBeenCalled();
    expect(component.notifications.every(n => n.isRead)).toBeTrue();
  }));

  it('should delete notification after confirmation', fakeAsync(() => {
    const notification = mockNotifications[0];
    const mockEvent = new Event('click');
    spyOn(mockEvent, 'stopPropagation');
    spyOn(window, 'confirm').and.returnValue(true);
    component.notifications = [...mockNotifications];

    component.deleteNotification(notification, mockEvent);
    tick();

    expect(mockEvent.stopPropagation).toHaveBeenCalled();
    expect(window.confirm).toHaveBeenCalledWith('Are you sure you want to delete this notification?');
    expect(mockProviderService.deleteNotification).toHaveBeenCalledWith('1');
    expect(component.notifications.length).toBe(1);
    expect(component.notifications[0].notificationId).toBe('2');
  }));

  it('should not delete notification if not confirmed', () => {
    const notification = mockNotifications[0];
    const mockEvent = new Event('click');
    spyOn(window, 'confirm').and.returnValue(false);
    component.notifications = [...mockNotifications];

    component.deleteNotification(notification, mockEvent);

    expect(mockProviderService.deleteNotification).not.toHaveBeenCalled();
    expect(component.notifications.length).toBe(2);
  });

  it('should handle notification click', () => {
    const notification = { ...mockNotifications[0], isRead: false };
    spyOn(component, 'markAsRead');

    component.handleNotificationClick(notification);

    expect(component.markAsRead).toHaveBeenCalledWith(notification, jasmine.any(Event));
  });

  it('should not mark read notification as read again on click', () => {
    const notification = { ...mockNotifications[1], isRead: true };
    spyOn(component, 'markAsRead');

    component.handleNotificationClick(notification);

    expect(component.markAsRead).not.toHaveBeenCalled();
  });

  it('should navigate to correct page', () => {
    component.pagedResult = mockPagedResult;

    component.goToPage(1);

    expect(component.currentPage).toBe(1);
    expect(mockProviderService.getNotifications).toHaveBeenCalled();
  });

  it('should not navigate to invalid page', () => {
    component.pagedResult = { ...mockPagedResult, totalPages: 1 };
    component.currentPage = 1;

    component.goToPage(2); // Invalid page

    expect(component.currentPage).toBe(1); // Should not change
  });

  it('should return correct notification icon', () => {
    expect(component.getNotificationIcon('ItemSold')).toBe('💰');
    expect(component.getNotificationIcon('PayoutProcessed')).toBe('💳');
    expect(component.getNotificationIcon('PayoutPending')).toBe('⏳');
    expect(component.getNotificationIcon('StatementReady')).toBe('📄');
    expect(component.getNotificationIcon('Unknown')).toBe('🔔');
  });

  it('should return correct notification icon class', () => {
    expect(component.getNotificationIconClass('ItemSold')).toBe('item-sold');
    expect(component.getNotificationIconClass('PayoutProcessed')).toBe('payout-processed');
    expect(component.getNotificationIconClass('PayoutPending')).toBe('payout-pending');
    expect(component.getNotificationIconClass('StatementReady')).toBe('statement-ready');
    expect(component.getNotificationIconClass('Unknown')).toBe('default');
  });

  it('should calculate hasUnreadNotifications correctly', () => {
    component.notifications = [];
    expect(component.hasUnreadNotifications).toBeFalse();

    component.notifications = [{ ...mockNotifications[1], isRead: true }];
    expect(component.hasUnreadNotifications).toBeFalse();

    component.notifications = [{ ...mockNotifications[0], isRead: false }];
    expect(component.hasUnreadNotifications).toBeTrue();
  });

  it('should handle error loading notifications', fakeAsync(() => {
    mockProviderService.getNotifications.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');

    component.loadNotifications();
    tick();

    expect(console.error).toHaveBeenCalledWith('Error loading notifications:', jasmine.any(Error));
    expect(component.loading).toBeFalse();
  }));

  it('should auto-refresh every 30 seconds', fakeAsync(() => {
    fixture.detectChanges();
    tick(); // Initial load

    expect(mockProviderService.getNotifications).toHaveBeenCalledTimes(1);

    // Advance time by 30 seconds
    tick(30000);
    expect(mockProviderService.getNotifications).toHaveBeenCalledTimes(2);
  }));

  it('should cleanup subscriptions on destroy', () => {
    spyOn(component['destroy$'], 'next');
    spyOn(component['destroy$'], 'complete');

    component.ngOnDestroy();

    expect(component['destroy$'].next).toHaveBeenCalled();
    expect(component['destroy$'].complete).toHaveBeenCalled();
  });
});