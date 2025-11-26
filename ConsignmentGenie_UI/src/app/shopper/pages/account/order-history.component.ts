import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';

interface OrderItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
  imageUrl?: string;
}

interface Order {
  id: string;
  orderNumber: string;
  date: Date;
  status: 'pending' | 'processing' | 'shipped' | 'delivered' | 'cancelled';
  items: OrderItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  shippingAddress: {
    address: string;
    apartment?: string;
    city: string;
    state: string;
    zipCode: string;
  };
  trackingNumber?: string;
}

@Component({
  selector: 'app-order-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="order-history-container">
      <div class="container">
        <div class="page-header">
          <h1>Order History</h1>
          <p class="store-name" *ngIf="storeInfo">{{ storeInfo.name }}</p>
        </div>

        <div class="orders-content" *ngIf="orders.length > 0; else noOrdersTemplate">
          <div class="orders-list">
            <div class="order-card" *ngFor="let order of paginatedOrders">
              <div class="order-header">
                <div class="order-info">
                  <h3>Order #{{ order.orderNumber }}</h3>
                  <p class="order-date">{{ order.date | date:'mediumDate' }}</p>
                </div>
                <div class="order-status">
                  <span class="status-badge" [class]="'status-' + order.status">
                    {{ getStatusLabel(order.status) }}
                  </span>
                </div>
              </div>

              <div class="order-items">
                <div class="order-item" *ngFor="let item of order.items">
                  <img
                    [src]="item.imageUrl || '/assets/placeholder-item.jpg'"
                    [alt]="item.name"
                    class="item-image"
                    (error)="onImageError($event)">
                  <div class="item-details">
                    <h4>{{ item.name }}</h4>
                    <p>Qty: {{ item.quantity }} × \${{ item.price | number:'1.2-2' }}</p>
                  </div>
                  <div class="item-total">
                    \${{ (item.price * item.quantity) | number:'1.2-2' }}
                  </div>
                </div>
              </div>

              <div class="order-summary">
                <div class="summary-row">
                  <span>Subtotal:</span>
                  <span>\${{ order.subtotal | number:'1.2-2' }}</span>
                </div>
                <div class="summary-row" *ngIf="order.shipping > 0">
                  <span>Shipping:</span>
                  <span>\${{ order.shipping | number:'1.2-2' }}</span>
                </div>
                <div class="summary-row" *ngIf="order.shipping === 0">
                  <span>Shipping:</span>
                  <span>FREE</span>
                </div>
                <div class="summary-row">
                  <span>Tax:</span>
                  <span>\${{ order.tax | number:'1.2-2' }}</span>
                </div>
                <div class="summary-row total">
                  <span>Total:</span>
                  <span>\${{ order.total | number:'1.2-2' }}</span>
                </div>
              </div>

              <div class="order-details">
                <div class="shipping-info">
                  <h5>Shipping Address</h5>
                  <p>{{ order.shippingAddress.address }}</p>
                  <p *ngIf="order.shippingAddress.apartment">{{ order.shippingAddress.apartment }}</p>
                  <p>{{ order.shippingAddress.city }}, {{ order.shippingAddress.state }} {{ order.shippingAddress.zipCode }}</p>
                </div>

                <div class="tracking-info" *ngIf="order.trackingNumber">
                  <h5>Tracking Information</h5>
                  <p>Tracking #: <strong>{{ order.trackingNumber }}</strong></p>
                  <a href="#" class="track-link" (click)="trackOrder(order, $event)">Track Package</a>
                </div>
              </div>

              <div class="order-actions">
                <button class="btn btn-outline-primary btn-sm" (click)="viewOrderDetails(order)">
                  View Details
                </button>
                <button
                  class="btn btn-outline-secondary btn-sm"
                  *ngIf="canReorder(order)"
                  (click)="reorderItems(order)">
                  Reorder
                </button>
                <button
                  class="btn btn-outline-danger btn-sm"
                  *ngIf="canCancel(order)"
                  (click)="cancelOrder(order)">
                  Cancel Order
                </button>
              </div>
            </div>
          </div>

          <div class="pagination" *ngIf="totalPages > 1">
            <button
              class="btn btn-outline-secondary"
              [disabled]="currentPage === 1"
              (click)="goToPage(currentPage - 1)">
              Previous
            </button>

            <span class="page-info">
              Page {{ currentPage }} of {{ totalPages }}
            </span>

            <button
              class="btn btn-outline-secondary"
              [disabled]="currentPage === totalPages"
              (click)="goToPage(currentPage + 1)">
              Next
            </button>
          </div>
        </div>

        <ng-template #noOrdersTemplate>
          <div class="no-orders">
            <div class="empty-icon">📋</div>
            <h2>No Orders Yet</h2>
            <p>You haven't placed any orders yet. Start shopping to see your order history here.</p>
            <button class="btn btn-primary" [routerLink]="['/shop', storeSlug]">
              Start Shopping
            </button>
          </div>
        </ng-template>

        <div class="help-section">
          <div class="help-card">
            <h3>Need Help?</h3>
            <p>If you have questions about your orders or need assistance, we're here to help.</p>
            <div class="help-actions">
              <button class="btn btn-outline-primary" (click)="contactSupport()">
                Contact Support
              </button>
              <a href="#" class="help-link">Return Policy</a>
              <a href="#" class="help-link">Shipping Info</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .order-history-container {
      min-height: 80vh;
      padding: 2rem 0;
      background-color: #f8f9fa;
    }

    .container {
      max-width: 1000px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .page-header {
      text-align: center;
      margin-bottom: 3rem;
    }

    .page-header h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #343a40;
      margin-bottom: 0.5rem;
    }

    .store-name {
      font-size: 1.1rem;
      color: #007bff;
      margin: 0;
    }

    .orders-list {
      margin-bottom: 2rem;
    }

    .order-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      margin-bottom: 1.5rem;
      overflow: hidden;
    }

    .order-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      background-color: #f8f9fa;
      border-bottom: 1px solid #dee2e6;
    }

    .order-info h3 {
      color: #343a40;
      margin-bottom: 0.25rem;
      font-size: 1.25rem;
    }

    .order-date {
      color: #6c757d;
      margin: 0;
      font-size: 0.9rem;
    }

    .status-badge {
      padding: 0.375rem 0.75rem;
      border-radius: 1rem;
      font-size: 0.8rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .status-pending {
      background-color: #fff3cd;
      color: #856404;
      border: 1px solid #ffeaa7;
    }

    .status-processing {
      background-color: #cce5ff;
      color: #004085;
      border: 1px solid #99d1ff;
    }

    .status-shipped {
      background-color: #d4edda;
      color: #155724;
      border: 1px solid #c3e6cb;
    }

    .status-delivered {
      background-color: #d1ecf1;
      color: #0c5460;
      border: 1px solid #b8daff;
    }

    .status-cancelled {
      background-color: #f8d7da;
      color: #721c24;
      border: 1px solid #f5c6cb;
    }

    .order-items {
      padding: 1rem 1.5rem;
    }

    .order-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem 0;
      border-bottom: 1px solid #f1f1f1;
    }

    .order-item:last-child {
      border-bottom: none;
    }

    .item-image {
      width: 60px;
      height: 60px;
      object-fit: cover;
      border-radius: 0.375rem;
    }

    .item-details {
      flex: 1;
    }

    .item-details h4 {
      color: #343a40;
      margin-bottom: 0.25rem;
      font-size: 1rem;
    }

    .item-details p {
      color: #6c757d;
      margin: 0;
      font-size: 0.875rem;
    }

    .item-total {
      font-weight: bold;
      color: #28a745;
      font-size: 1rem;
    }

    .order-summary {
      padding: 1rem 1.5rem;
      background-color: #f8f9fa;
      border-top: 1px solid #dee2e6;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      margin-bottom: 0.5rem;
      font-size: 0.9rem;
    }

    .summary-row.total {
      font-weight: bold;
      font-size: 1rem;
      color: #343a40;
      border-top: 1px solid #dee2e6;
      padding-top: 0.5rem;
      margin-top: 0.5rem;
    }

    .order-details {
      padding: 1rem 1.5rem;
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
    }

    .shipping-info h5, .tracking-info h5 {
      color: #343a40;
      margin-bottom: 0.75rem;
      font-size: 0.9rem;
      font-weight: 600;
    }

    .shipping-info p, .tracking-info p {
      color: #6c757d;
      margin: 0.25rem 0;
      font-size: 0.875rem;
    }

    .track-link {
      color: #007bff;
      text-decoration: none;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .track-link:hover {
      text-decoration: underline;
    }

    .order-actions {
      padding: 1rem 1.5rem;
      border-top: 1px solid #dee2e6;
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .btn {
      padding: 0.5rem 1rem;
      border: 1px solid transparent;
      border-radius: 0.375rem;
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      display: inline-block;
      text-align: center;
    }

    .btn-outline-primary {
      color: #007bff;
      border-color: #007bff;
      background-color: transparent;
    }

    .btn-outline-primary:hover {
      color: white;
      background-color: #007bff;
    }

    .btn-outline-secondary {
      color: #6c757d;
      border-color: #6c757d;
      background-color: transparent;
    }

    .btn-outline-secondary:hover:not(:disabled) {
      color: white;
      background-color: #6c757d;
    }

    .btn-outline-danger {
      color: #dc3545;
      border-color: #dc3545;
      background-color: transparent;
    }

    .btn-outline-danger:hover {
      color: white;
      background-color: #dc3545;
    }

    .btn-primary {
      background-color: #007bff;
      border-color: #007bff;
      color: white;
    }

    .btn-primary:hover {
      background-color: #0056b3;
      border-color: #004085;
    }

    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.8rem;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      margin-top: 2rem;
    }

    .page-info {
      color: #6c757d;
      font-weight: 500;
    }

    .no-orders {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 4rem 2rem;
      text-align: center;
      margin-bottom: 2rem;
    }

    .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .no-orders h2 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .no-orders p {
      color: #6c757d;
      margin-bottom: 2rem;
      max-width: 400px;
      margin-left: auto;
      margin-right: auto;
    }

    .help-section {
      margin-top: 3rem;
    }

    .help-card {
      background: white;
      border-radius: 0.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 2rem;
      text-align: center;
    }

    .help-card h3 {
      color: #343a40;
      margin-bottom: 1rem;
    }

    .help-card p {
      color: #6c757d;
      margin-bottom: 1.5rem;
    }

    .help-actions {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1.5rem;
      flex-wrap: wrap;
    }

    .help-link {
      color: #007bff;
      text-decoration: none;
      font-size: 0.9rem;
    }

    .help-link:hover {
      text-decoration: underline;
    }

    @media (max-width: 768px) {
      .order-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .order-details {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .order-actions {
        flex-direction: column;
        align-items: stretch;
      }

      .help-actions {
        flex-direction: column;
        gap: 1rem;
      }

      .pagination {
        flex-direction: column;
        gap: 0.5rem;
      }
    }

    @media (max-width: 480px) {
      .page-header h1 {
        font-size: 2rem;
      }

      .order-item {
        flex-direction: column;
        text-align: center;
        gap: 0.75rem;
      }

      .item-details {
        text-align: center;
      }

      .no-orders {
        padding: 2rem 1rem;
      }

      .help-card {
        padding: 1.5rem;
      }
    }
  `]
})
export class OrderHistoryComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug = '';

  // Sample order data for Phase 1 MVP
  orders: Order[] = [
    {
      id: '1',
      orderNumber: '1001',
      date: new Date('2024-01-15'),
      status: 'delivered',
      items: [
        {
          id: '1',
          name: 'Vintage Leather Jacket',
          price: 125.00,
          quantity: 1,
          imageUrl: ''
        },
        {
          id: '2',
          name: 'Classic Handbag',
          price: 85.00,
          quantity: 1,
          imageUrl: ''
        }
      ],
      subtotal: 210.00,
      tax: 16.80,
      shipping: 0.00,
      total: 226.80,
      shippingAddress: {
        address: '123 Main St',
        city: 'Springfield',
        state: 'IL',
        zipCode: '62701'
      },
      trackingNumber: 'TRK1234567890'
    },
    {
      id: '2',
      orderNumber: '1002',
      date: new Date('2024-01-10'),
      status: 'shipped',
      items: [
        {
          id: '3',
          name: 'Antique Wooden Table',
          price: 450.00,
          quantity: 1,
          imageUrl: ''
        }
      ],
      subtotal: 450.00,
      tax: 36.00,
      shipping: 0.00,
      total: 486.00,
      shippingAddress: {
        address: '123 Main St',
        city: 'Springfield',
        state: 'IL',
        zipCode: '62701'
      },
      trackingNumber: 'TRK0987654321'
    },
    {
      id: '3',
      orderNumber: '1003',
      date: new Date('2024-01-05'),
      status: 'processing',
      items: [
        {
          id: '4',
          name: 'Modern Art Print',
          price: 75.00,
          quantity: 2,
          imageUrl: ''
        }
      ],
      subtotal: 150.00,
      tax: 12.00,
      shipping: 15.00,
      total: 177.00,
      shippingAddress: {
        address: '123 Main St',
        city: 'Springfield',
        state: 'IL',
        zipCode: '62701'
      }
    }
  ];

  paginatedOrders: Order[] = [];
  currentPage = 1;
  ordersPerPage = 5;
  totalPages = 1;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private storeService: ShopperStoreService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
    });

    this.storeService.currentStore$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(store => {
      this.storeInfo = store;
    });

    this.orders.sort((a, b) => b.date.getTime() - a.date.getTime());
    this.totalPages = Math.ceil(this.orders.length / this.ordersPerPage);
    this.updatePagination();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'pending': return 'Pending';
      case 'processing': return 'Processing';
      case 'shipped': return 'Shipped';
      case 'delivered': return 'Delivered';
      case 'cancelled': return 'Cancelled';
      default: return status;
    }
  }

  canReorder(order: Order): boolean {
    return order.status === 'delivered';
  }

  canCancel(order: Order): boolean {
    return order.status === 'pending' || order.status === 'processing';
  }

  viewOrderDetails(order: Order): void {
    // TODO: Navigate to detailed order view or show modal
    alert(`Viewing details for order #${order.orderNumber}`);
  }

  reorderItems(order: Order): void {
    // TODO: Add order items to cart
    alert(`Adding items from order #${order.orderNumber} to cart`);
  }

  cancelOrder(order: Order): void {
    if (confirm(`Are you sure you want to cancel order #${order.orderNumber}?`)) {
      // TODO: Call API to cancel order
      order.status = 'cancelled';
      alert(`Order #${order.orderNumber} has been cancelled`);
    }
  }

  trackOrder(order: Order, event: Event): void {
    event.preventDefault();
    if (order.trackingNumber) {
      // TODO: Open tracking page or modal
      alert(`Tracking package: ${order.trackingNumber}`);
    }
  }

  contactSupport(): void {
    // TODO: Open contact support modal or redirect to support page
    alert('Support contact functionality will be implemented in a future update.');
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagination();
    }
  }

  onImageError(event: any): void {
    event.target.src = '/assets/placeholder-item.jpg';
  }

  private updatePagination(): void {
    const startIndex = (this.currentPage - 1) * this.ordersPerPage;
    const endIndex = startIndex + this.ordersPerPage;
    this.paginatedOrders = this.orders.slice(startIndex, endIndex);
  }
}