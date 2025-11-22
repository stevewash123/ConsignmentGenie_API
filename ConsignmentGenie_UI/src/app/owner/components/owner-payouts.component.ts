import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { PayoutService, PayoutStatus } from '../../services/payout.service';
import {
  PayoutListDto,
  PayoutDto,
  PayoutSearchRequest,
  CreatePayoutRequest,
  PendingPayoutData,
  UpdatePayoutRequest
} from '../../models/payout.model';
import { ProviderService } from '../../services/provider.service';
import { Provider } from '../../models/provider.model';

@Component({
  selector: 'app-owner-payouts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <h1 class="page-title">Payouts Management</h1>
        <div class="header-actions">
          <button
            (click)="showCreatePayoutModal = true"
            class="btn btn-primary"
            [disabled]="pendingPayouts().length === 0"
          >
            Create Payout
          </button>
          <button (click)="refreshData()" class="btn btn-secondary">
            Refresh
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <div class="filter-row">
          <select [(ngModel)]="selectedProviderId" (change)="loadPayouts()" class="form-select">
            <option value="">All Providers</option>
            <option *ngFor="let provider of providers()" [value]="provider.id">
              {{provider.name}}
            </option>
          </select>

          <select [(ngModel)]="selectedStatus" (change)="loadPayouts()" class="form-select">
            <option value="">All Status</option>
            <option value="Paid">Paid</option>
          </select>

          <input
            type="date"
            [(ngModel)]="dateFrom"
            (change)="loadPayouts()"
            class="form-control"
            placeholder="From Date"
          />

          <input
            type="date"
            [(ngModel)]="dateTo"
            (change)="loadPayouts()"
            class="form-control"
            placeholder="To Date"
          />
        </div>
      </div>

      <!-- Pending Payouts Section -->
      <div class="pending-section" *ngIf="pendingPayouts().length > 0">
        <h2>Pending Payouts</h2>
        <div class="pending-grid">
          <div *ngFor="let pending of pendingPayouts()" class="pending-card">
            <div class="card-header">
              <h3>{{pending.providerName}}</h3>
              <span class="amount">\${{pending.pendingAmount.toFixed(2)}}</span>
            </div>
            <div class="card-body">
              <p><strong>Transactions:</strong> {{pending.transactionCount}}</p>
              <p><strong>Period:</strong> {{formatDate(pending.earliestSale)}} - {{formatDate(pending.latestSale)}}</p>
              <button
                (click)="createPayoutForProvider(pending)"
                class="btn btn-sm btn-success"
              >
                Create Payout
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Payouts Table -->
      <div class="table-section">
        <div class="table-header">
          <h2>Payout History</h2>
          <div class="table-info">
            {{totalPayouts()}} total payouts
          </div>
        </div>

        <div *ngIf="loading()" class="loading">Loading payouts...</div>

        <div *ngIf="!loading() && payouts().length === 0" class="empty-state">
          No payouts found.
        </div>

        <table *ngIf="!loading() && payouts().length > 0" class="payouts-table">
          <thead>
            <tr>
              <th (click)="sort('payoutNumber')" class="sortable">
                Payout #
                <span class="sort-indicator" [ngClass]="getSortClass('payoutNumber')"></span>
              </th>
              <th (click)="sort('provider')" class="sortable">
                Provider
                <span class="sort-indicator" [ngClass]="getSortClass('provider')"></span>
              </th>
              <th (click)="sort('payoutDate')" class="sortable">
                Payout Date
                <span class="sort-indicator" [ngClass]="getSortClass('payoutDate')"></span>
              </th>
              <th (click)="sort('amount')" class="sortable">
                Amount
                <span class="sort-indicator" [ngClass]="getSortClass('amount')"></span>
              </th>
              <th>Payment Method</th>
              <th>Period</th>
              <th>Transactions</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let payout of payouts()">
              <td class="payout-number">{{payout.payoutNumber}}</td>
              <td class="provider-name">{{payout.provider.name}}</td>
              <td>{{formatDate(payout.payoutDate)}}</td>
              <td class="amount">\${{payout.amount.toFixed(2)}}</td>
              <td>{{payout.paymentMethod}}</td>
              <td class="period">
                {{formatDate(payout.periodStart)}} - {{formatDate(payout.periodEnd)}}
              </td>
              <td class="text-center">{{payout.transactionCount}}</td>
              <td>
                <span class="status-badge" [ngClass]="'status-' + payout.status.toLowerCase()">
                  {{payout.status}}
                </span>
              </td>
              <td class="actions">
                <button
                  (click)="viewPayout(payout.id)"
                  class="btn btn-sm btn-outline-primary"
                  title="View Details"
                >
                  View
                </button>
                <button
                  (click)="editPayout(payout)"
                  class="btn btn-sm btn-outline-secondary"
                  title="Edit"
                >
                  Edit
                </button>
                <button
                  (click)="exportPayout(payout.id, 'csv')"
                  class="btn btn-sm btn-outline-success"
                  title="Export CSV"
                >
                  CSV
                </button>
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Pagination -->
        <div class="pagination-section" *ngIf="totalPages() > 1">
          <div class="pagination-info">
            Page {{currentPage()}} of {{totalPages()}} ({{totalPayouts()}} total)
          </div>
          <div class="pagination-controls">
            <button
              (click)="goToPage(currentPage() - 1)"
              [disabled]="currentPage() === 1"
              class="btn btn-sm btn-outline-secondary"
            >
              Previous
            </button>
            <button
              *ngFor="let page of visiblePages()"
              (click)="goToPage(page)"
              [ngClass]="{'active': page === currentPage()}"
              class="btn btn-sm btn-outline-secondary page-btn"
            >
              {{page}}
            </button>
            <button
              (click)="goToPage(currentPage() + 1)"
              [disabled]="currentPage() === totalPages()"
              class="btn btn-sm btn-outline-secondary"
            >
              Next
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Payout Modal -->
    <div *ngIf="showCreatePayoutModal" class="modal-overlay" (click)="closeModal($event)">
      <div class="modal-content">
        <div class="modal-header">
          <h3>Create New Payout</h3>
          <button (click)="showCreatePayoutModal = false" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <form (ngSubmit)="createPayout()">
            <div class="form-group">
              <label>Provider</label>
              <select [(ngModel)]="newPayout.providerId" name="providerId" required class="form-control">
                <option value="">Select Provider</option>
                <option *ngFor="let pending of pendingPayouts()" [value]="pending.providerId">
                  {{pending.providerName}} - \${{pending.pendingAmount.toFixed(2)}}
                </option>
              </select>
            </div>

            <div class="form-group">
              <label>Payout Date</label>
              <input type="date" [(ngModel)]="newPayout.payoutDate" name="payoutDate" required class="form-control">
            </div>

            <div class="form-group">
              <label>Payment Method</label>
              <select [(ngModel)]="newPayout.paymentMethod" name="paymentMethod" required class="form-control">
                <option value="">Select Method</option>
                <option value="Venmo">Venmo</option>
                <option value="Zelle">Zelle</option>
                <option value="PayPal">PayPal</option>
                <option value="Check">Check</option>
                <option value="Cash">Cash</option>
                <option value="Bank Transfer">Bank Transfer</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div class="form-group">
              <label>Payment Reference (Optional)</label>
              <input type="text" [(ngModel)]="newPayout.paymentReference" name="paymentReference"
                     class="form-control" placeholder="Transaction ID, Check #, etc.">
            </div>

            <div class="form-group">
              <label>Notes (Optional)</label>
              <textarea [(ngModel)]="newPayout.notes" name="notes" class="form-control" rows="3"></textarea>
            </div>

            <div class="form-actions">
              <button type="button" (click)="showCreatePayoutModal = false" class="btn btn-secondary">Cancel</button>
              <button type="submit" class="btn btn-primary">Create Payout</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- View Payout Modal -->
    <div *ngIf="showViewModal && selectedPayout()" class="modal-overlay" (click)="closeModal($event)">
      <div class="modal-content large">
        <div class="modal-header">
          <h3>Payout Details - {{selectedPayout()?.payoutNumber}}</h3>
          <button (click)="showViewModal = false" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <div class="payout-details">
            <div class="details-grid">
              <div class="detail-item">
                <label>Provider:</label>
                <span>{{selectedPayout()?.provider.name}}</span>
              </div>
              <div class="detail-item">
                <label>Amount:</label>
                <span class="amount">\${{selectedPayout()?.amount.toFixed(2)}}</span>
              </div>
              <div class="detail-item">
                <label>Payout Date:</label>
                <span>{{formatDate(selectedPayout()?.payoutDate!)}}</span>
              </div>
              <div class="detail-item">
                <label>Payment Method:</label>
                <span>{{selectedPayout()?.paymentMethod}}</span>
              </div>
              <div class="detail-item">
                <label>Period:</label>
                <span>{{formatDate(selectedPayout()?.periodStart!)}} - {{formatDate(selectedPayout()?.periodEnd!)}}</span>
              </div>
              <div class="detail-item">
                <label>Status:</label>
                <span class="status-badge" [ngClass]="'status-' + selectedPayout()?.status.toLowerCase()">
                  {{selectedPayout()?.status}}
                </span>
              </div>
            </div>

            <div *ngIf="selectedPayout()?.paymentReference" class="detail-item">
              <label>Payment Reference:</label>
              <span>{{selectedPayout()?.paymentReference}}</span>
            </div>

            <div *ngIf="selectedPayout()?.notes" class="detail-item">
              <label>Notes:</label>
              <p>{{selectedPayout()?.notes}}</p>
            </div>

            <div class="transactions-section">
              <h4>Transactions ({{selectedPayout()?.transactions.length}})</h4>
              <table class="transactions-table">
                <thead>
                  <tr>
                    <th>Item</th>
                    <th>Sale Date</th>
                    <th>Sale Price</th>
                    <th>Provider Amount</th>
                    <th>Shop Amount</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let transaction of selectedPayout()?.transactions">
                    <td>{{transaction.itemName}}</td>
                    <td>{{formatDate(transaction.saleDate)}}</td>
                    <td>\${{transaction.salePrice.toFixed(2)}}</td>
                    <td>\${{transaction.providerAmount.toFixed(2)}}</td>
                    <td>\${{transaction.shopAmount.toFixed(2)}}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-container {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 2px solid #e5e7eb;
    }

    .page-title {
      font-size: 2rem;
      font-weight: 600;
      color: #1f2937;
      margin: 0;
    }

    .header-actions {
      display: flex;
      gap: 1rem;
    }

    .filters-section {
      background: white;
      padding: 1.5rem;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      margin-bottom: 2rem;
    }

    .filter-row {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr 1fr;
      gap: 1rem;
    }

    .pending-section {
      margin-bottom: 2rem;
    }

    .pending-section h2 {
      color: #1f2937;
      margin-bottom: 1rem;
    }

    .pending-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1rem;
    }

    .pending-card {
      background: #fef3c7;
      border: 1px solid #fbbf24;
      border-radius: 8px;
      padding: 1rem;
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .card-header h3 {
      margin: 0;
      font-size: 1.1rem;
      color: #92400e;
    }

    .card-header .amount {
      font-weight: bold;
      color: #92400e;
      font-size: 1.2rem;
    }

    .table-section {
      background: white;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .table-header {
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .table-header h2 {
      margin: 0;
      color: #1f2937;
    }

    .table-info {
      color: #6b7280;
      font-size: 0.9rem;
    }

    .payouts-table {
      width: 100%;
      border-collapse: collapse;
    }

    .payouts-table th {
      background: #f9fafb;
      padding: 0.75rem;
      text-align: left;
      font-weight: 600;
      color: #374151;
      border-bottom: 1px solid #e5e7eb;
    }

    .payouts-table th.sortable {
      cursor: pointer;
      user-select: none;
      position: relative;
    }

    .payouts-table th.sortable:hover {
      background: #f3f4f6;
    }

    .sort-indicator {
      position: absolute;
      right: 5px;
      opacity: 0.5;
    }

    .sort-indicator.asc::after {
      content: '▲';
    }

    .sort-indicator.desc::after {
      content: '▼';
    }

    .payouts-table td {
      padding: 0.75rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .payouts-table tr:hover {
      background: #f9fafb;
    }

    .payout-number {
      font-weight: 600;
      color: #1f2937;
    }

    .provider-name {
      font-weight: 500;
    }

    .amount {
      font-weight: 600;
      color: #059669;
    }

    .period {
      font-size: 0.9rem;
      color: #6b7280;
    }

    .text-center {
      text-align: center;
    }

    .status-badge {
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.8rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .status-paid {
      background: #d1fae5;
      color: #065f46;
    }

    .status-pending {
      background: #fef3c7;
      color: #92400e;
    }

    .actions {
      display: flex;
      gap: 0.5rem;
    }

    .pagination-section {
      padding: 1rem 1.5rem;
      border-top: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .pagination-controls {
      display: flex;
      gap: 0.5rem;
    }

    .page-btn.active {
      background: #3b82f6;
      color: white;
    }

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .modal-content {
      background: white;
      border-radius: 8px;
      max-width: 500px;
      width: 90%;
      max-height: 90vh;
      overflow-y: auto;
    }

    .modal-content.large {
      max-width: 800px;
    }

    .modal-header {
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .modal-header h3 {
      margin: 0;
      color: #1f2937;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #6b7280;
    }

    .close-btn:hover {
      color: #374151;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.25rem;
      font-weight: 500;
      color: #374151;
    }

    .form-control, .form-select {
      width: 100%;
      padding: 0.5rem;
      border: 1px solid #d1d5db;
      border-radius: 4px;
      font-size: 0.9rem;
    }

    .form-control:focus, .form-select:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      margin-top: 2rem;
    }

    .details-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1rem;
      margin-bottom: 2rem;
    }

    .detail-item {
      margin-bottom: 1rem;
    }

    .detail-item label {
      font-weight: 600;
      color: #374151;
      display: block;
      margin-bottom: 0.25rem;
    }

    .transactions-section h4 {
      color: #1f2937;
      margin-bottom: 1rem;
    }

    .transactions-table {
      width: 100%;
      border-collapse: collapse;
    }

    .transactions-table th,
    .transactions-table td {
      padding: 0.5rem;
      text-align: left;
      border-bottom: 1px solid #e5e7eb;
    }

    .transactions-table th {
      background: #f9fafb;
      font-weight: 600;
      color: #374151;
    }

    .btn {
      padding: 0.5rem 1rem;
      border-radius: 4px;
      font-weight: 500;
      text-decoration: none;
      display: inline-block;
      cursor: pointer;
      border: 1px solid transparent;
      transition: all 0.2s;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
      border-color: #3b82f6;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2563eb;
    }

    .btn-secondary {
      background: #6b7280;
      color: white;
      border-color: #6b7280;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #4b5563;
    }

    .btn-success {
      background: #059669;
      color: white;
      border-color: #059669;
    }

    .btn-success:hover:not(:disabled) {
      background: #047857;
    }

    .btn-outline-primary {
      background: transparent;
      color: #3b82f6;
      border-color: #3b82f6;
    }

    .btn-outline-primary:hover {
      background: #3b82f6;
      color: white;
    }

    .btn-outline-secondary {
      background: transparent;
      color: #6b7280;
      border-color: #6b7280;
    }

    .btn-outline-secondary:hover {
      background: #6b7280;
      color: white;
    }

    .btn-outline-success {
      background: transparent;
      color: #059669;
      border-color: #059669;
    }

    .btn-outline-success:hover {
      background: #059669;
      color: white;
    }

    .btn-sm {
      padding: 0.25rem 0.5rem;
      font-size: 0.8rem;
    }

    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .loading {
      text-align: center;
      padding: 2rem;
      color: #6b7280;
    }

    .empty-state {
      text-align: center;
      padding: 3rem;
      color: #6b7280;
    }

    @media (max-width: 768px) {
      .page-container {
        padding: 1rem;
      }

      .filter-row {
        grid-template-columns: 1fr;
      }

      .pending-grid {
        grid-template-columns: 1fr;
      }

      .details-grid {
        grid-template-columns: 1fr;
      }

      .payouts-table {
        font-size: 0.8rem;
      }

      .actions {
        flex-direction: column;
        gap: 0.25rem;
      }
    }
  `]
})
export class OwnerPayoutsComponent implements OnInit {
  // State signals
  payouts = signal<PayoutListDto[]>([]);
  pendingPayouts = signal<PendingPayoutData[]>([]);
  providers = signal<Provider[]>([]);
  loading = signal(true);
  selectedPayout = signal<PayoutDto | null>(null);

  // Pagination
  currentPage = signal(1);
  totalPages = signal(1);
  totalPayouts = signal(0);
  pageSize = 10;

  // Filters
  selectedProviderId = '';
  selectedStatus = '';
  dateFrom = '';
  dateTo = '';
  sortBy = 'payoutDate';
  sortDirection = 'desc';

  // Modals
  showCreatePayoutModal = false;
  showViewModal = false;

  // Form data
  newPayout: Partial<CreatePayoutRequest> = {
    providerId: '',
    payoutDate: new Date(),
    paymentMethod: '',
    paymentReference: '',
    notes: '',
    transactionIds: []
  };

  // Computed values
  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, start + 4);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  constructor(
    private payoutService: PayoutService,
    private providerService: ProviderService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadProviders();
    this.loadPendingPayouts();
    this.loadPayouts();
  }

  async loadProviders() {
    try {
      const providers = await this.providerService.getProviders().toPromise();
      this.providers.set(providers || []);
    } catch (error) {
      console.error('Error loading providers:', error);
      this.toastr.error('Failed to load providers');
    }
  }

  async loadPendingPayouts() {
    try {
      const pending = await this.payoutService.getPendingPayouts().toPromise();
      this.pendingPayouts.set(pending || []);
    } catch (error) {
      console.error('Error loading pending payouts:', error);
      this.toastr.error('Failed to load pending payouts');
    }
  }

  async loadPayouts() {
    this.loading.set(true);

    try {
      const request: PayoutSearchRequest = {
        page: this.currentPage(),
        pageSize: this.pageSize,
        sortBy: this.sortBy,
        sortDirection: this.sortDirection
      };

      if (this.selectedProviderId) request.providerId = this.selectedProviderId;
      if (this.selectedStatus) request.status = this.selectedStatus as PayoutStatus;
      if (this.dateFrom) request.payoutDateFrom = new Date(this.dateFrom);
      if (this.dateTo) request.payoutDateTo = new Date(this.dateTo);

      const response = await this.payoutService.getPayouts(request).toPromise();

      if (response) {
        this.payouts.set(response.data);
        this.totalPages.set(response.totalPages);
        this.totalPayouts.set(response.totalCount);
      }
    } catch (error) {
      console.error('Error loading payouts:', error);
      this.toastr.error('Failed to load payouts');
    } finally {
      this.loading.set(false);
    }
  }

  createPayoutForProvider(pending: PendingPayoutData) {
    this.newPayout = {
      providerId: pending.providerId,
      payoutDate: new Date(),
      paymentMethod: '',
      paymentReference: '',
      periodStart: pending.earliestSale,
      periodEnd: pending.latestSale,
      notes: '',
      transactionIds: pending.transactions.map(t => t.transactionId)
    };
    this.showCreatePayoutModal = true;
  }

  async createPayout() {
    try {
      if (!this.newPayout.providerId || !this.newPayout.paymentMethod || !this.newPayout.transactionIds?.length) {
        this.toastr.error('Please fill in all required fields');
        return;
      }

      const request: CreatePayoutRequest = {
        providerId: this.newPayout.providerId!,
        payoutDate: this.newPayout.payoutDate!,
        paymentMethod: this.newPayout.paymentMethod!,
        paymentReference: this.newPayout.paymentReference,
        periodStart: this.newPayout.periodStart!,
        periodEnd: this.newPayout.periodEnd!,
        notes: this.newPayout.notes,
        transactionIds: this.newPayout.transactionIds!
      };

      await this.payoutService.createPayout(request).toPromise();

      this.toastr.success('Payout created successfully');
      this.showCreatePayoutModal = false;
      this.refreshData();
    } catch (error) {
      console.error('Error creating payout:', error);
      this.toastr.error('Failed to create payout');
    }
  }

  async viewPayout(id: string) {
    try {
      const payout = await this.payoutService.getPayoutById(id).toPromise();
      this.selectedPayout.set(payout!);
      this.showViewModal = true;
    } catch (error) {
      console.error('Error loading payout details:', error);
      this.toastr.error('Failed to load payout details');
    }
  }

  editPayout(payout: PayoutListDto) {
    // TODO: Implement edit functionality
    this.toastr.info('Edit functionality coming soon');
  }

  async exportPayout(id: string, format: 'csv' | 'pdf') {
    try {
      const blob = format === 'csv'
        ? await this.payoutService.exportPayoutToCsv(id).toPromise()
        : await this.payoutService.exportPayoutToPdf(id).toPromise();

      if (blob) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `payout-${id}.${format}`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.toastr.success(`Payout exported as ${format.toUpperCase()}`);
      }
    } catch (error) {
      console.error(`Error exporting payout as ${format}:`, error);
      this.toastr.error(`Failed to export payout as ${format.toUpperCase()}`);
    }
  }

  refreshData() {
    this.loadPendingPayouts();
    this.loadPayouts();
  }

  sort(column: string) {
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'asc';
    }
    this.loadPayouts();
  }

  getSortClass(column: string): string {
    if (this.sortBy === column) {
      return this.sortDirection;
    }
    return '';
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadPayouts();
    }
  }

  closeModal(event: Event) {
    if (event.target === event.currentTarget) {
      this.showCreatePayoutModal = false;
      this.showViewModal = false;
    }
  }

  formatDate(date: Date | string | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString();
  }
}