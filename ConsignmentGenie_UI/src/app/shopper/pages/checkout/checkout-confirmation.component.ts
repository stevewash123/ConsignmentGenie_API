import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-checkout-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="checkout-confirmation">
      <div class="container">
        <div class="confirmation-content">
          <!-- Success Icon -->
          <div class="success-icon">
            <div class="checkmark">✓</div>
          </div>

          <!-- Thank You Message -->
          <h1 class="confirmation-title">Order Confirmed!</h1>
          <p class="confirmation-message">
            Thank you for your order. We'll send you a confirmation email shortly.
          </p>

          <!-- Order Details -->
          <div class="order-summary">
            <h3>Order Summary</h3>
            <div class="order-info">
              <div class="order-row">
                <span class="label">Order Number:</span>
                <span class="value">{{ orderNumber || '#1234' }}</span>
              </div>
              <div class="order-row">
                <span class="label">Total:</span>
                <span class="value">\${{ orderTotal || '0.00' }}</span>
              </div>
              <div class="order-row">
                <span class="label">Status:</span>
                <span class="value status-pending">Processing</span>
              </div>
            </div>
          </div>

          <!-- Next Steps -->
          <div class="next-steps">
            <h3>What's Next?</h3>
            <ul>
              <li>You'll receive a confirmation email with your order details</li>
              <li>We'll notify you when your order is ready for pickup</li>
              <li>Bring a valid ID when picking up your order</li>
            </ul>
          </div>

          <!-- Action Buttons -->
          <div class="actions">
            <a [routerLink]="['/shop', storeSlug]" class="btn btn-primary">
              Continue Shopping
            </a>
            <a [routerLink]="['/shop', storeSlug, 'account', 'orders']" class="btn btn-outline">
              View Orders
            </a>
          </div>

          <!-- Phase Notice -->
          <div class="phase-notice">
            <p>
              <em>Note: Full checkout and order management features will be implemented in Phase 2.</em>
            </p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .checkout-confirmation {
      min-height: 80vh;
      display: flex;
      align-items: center;
      padding: 2rem 0;
    }

    .container {
      max-width: 600px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .confirmation-content {
      text-align: center;
      background: white;
      padding: 3rem 2rem;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .success-icon {
      margin-bottom: 2rem;
    }

    .checkmark {
      display: inline-block;
      width: 80px;
      height: 80px;
      background: linear-gradient(135deg, #28a745, #20c997);
      border-radius: 50%;
      color: white;
      font-size: 3rem;
      font-weight: bold;
      line-height: 80px;
      animation: successPulse 0.6s ease-out;
    }

    @keyframes successPulse {
      0% {
        transform: scale(0.8);
        opacity: 0;
      }
      50% {
        transform: scale(1.1);
      }
      100% {
        transform: scale(1);
        opacity: 1;
      }
    }

    .confirmation-title {
      font-size: 2rem;
      color: #343a40;
      margin-bottom: 1rem;
    }

    .confirmation-message {
      color: #6c757d;
      font-size: 1.1rem;
      margin-bottom: 2rem;
    }

    .order-summary {
      background: #f8f9fa;
      padding: 1.5rem;
      border-radius: 6px;
      margin-bottom: 2rem;
      text-align: left;
    }

    .order-summary h3 {
      margin-bottom: 1rem;
      color: #343a40;
    }

    .order-row {
      display: flex;
      justify-content: space-between;
      margin-bottom: 0.5rem;
    }

    .order-row:last-child {
      margin-bottom: 0;
      padding-top: 0.5rem;
      border-top: 1px solid #dee2e6;
      font-weight: 500;
    }

    .label {
      color: #6c757d;
    }

    .value {
      color: #343a40;
      font-weight: 500;
    }

    .status-pending {
      color: #ffc107;
      font-weight: 600;
    }

    .next-steps {
      text-align: left;
      margin-bottom: 2rem;
    }

    .next-steps h3 {
      margin-bottom: 1rem;
      color: #343a40;
      text-align: center;
    }

    .next-steps ul {
      color: #6c757d;
      line-height: 1.6;
    }

    .next-steps li {
      margin-bottom: 0.5rem;
    }

    .actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
      margin-bottom: 2rem;
    }

    .btn {
      display: inline-block;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      text-decoration: none;
      font-weight: 500;
      transition: all 0.2s;
      cursor: pointer;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
    }

    .btn-primary:hover {
      background-color: #0056b3;
      color: white;
      text-decoration: none;
    }

    .btn-outline {
      background-color: transparent;
      color: #007bff;
      border: 1px solid #007bff;
    }

    .btn-outline:hover {
      background-color: #007bff;
      color: white;
      text-decoration: none;
    }

    .phase-notice {
      padding-top: 1.5rem;
      border-top: 1px solid #dee2e6;
    }

    .phase-notice p {
      color: #6c757d;
      font-size: 0.9rem;
      margin: 0;
    }

    @media (max-width: 768px) {
      .confirmation-content {
        padding: 2rem 1rem;
      }

      .actions {
        flex-direction: column;
        align-items: center;
      }

      .btn {
        width: 200px;
        text-align: center;
      }
    }
  `]
})
export class CheckoutConfirmationComponent implements OnInit, OnDestroy {
  storeSlug = '';
  orderNumber = '';
  orderTotal = '0.00';

  private destroy$ = new Subject<void>();

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
    });

    // TODO: Get actual order details from query params or service in Phase 2
    this.loadMockOrderData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadMockOrderData(): void {
    // Mock data for demonstration
    this.orderNumber = '#' + Math.floor(Math.random() * 9000 + 1000);
    this.orderTotal = '0.00';
  }
}