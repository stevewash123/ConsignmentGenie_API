import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { OwnerLayoutComponent } from './owner-layout.component';
import { TransactionService, TransactionQueryParams, PagedResult, UpdateTransactionRequest } from '../../services/transaction.service';
import { Transaction, CreateTransactionRequest } from '../../models/transaction.model';
import { ItemService, ItemFilters } from '../../services/item.service';
import { ProviderService } from '../../services/provider.service';
import { Item, ItemStatus } from '../../models/item.model';
import { Provider } from '../../models/provider.model';

@Component({
  selector: 'app-owner-sales',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, OwnerLayoutComponent],
  template: `
    <app-owner-layout>
      <div class="sales-page">
        <div class="page-header">
          <div class="header-content">
            <h1>Sales & Transactions</h1>
            <p>View and manage all sales transactions</p>
          </div>
          <div class="header-actions">
            <button class="btn-primary" (click)="showCreateModal = true">
              <span class="btn-icon">💰</span>
              Process Sale
            </button>
          </div>
        </div>

        <!-- Filters Section -->
        <div class="filters-section">
          <div class="filter-row">
            <div class="filter-group">
              <label for="startDate">Start Date</label>
              <input
                type="date"
                id="startDate"
                [(ngModel)]="filters.startDate"
                (change)="applyFilters()"
                class="filter-input">
            </div>
            <div class="filter-group">
              <label for="endDate">End Date</label>
              <input
                type="date"
                id="endDate"
                [(ngModel)]="filters.endDate"
                (change)="applyFilters()"
                class="filter-input">
            </div>
            <div class="filter-group">
              <label for="paymentMethod">Payment Method</label>
              <select
                id="paymentMethod"
                [(ngModel)]="filters.paymentMethod"
                (change)="applyFilters()"
                class="filter-select">
                <option value="">All Payment Methods</option>
                <option value="Cash">Cash</option>
                <option value="Card">Card</option>
                <option value="Online">Online</option>
              </select>
            </div>
            <button class="btn-secondary" (click)="clearFilters()">
              Clear Filters
            </button>
          </div>
        </div>

        <!-- Sales Summary Cards -->
        <div class="summary-cards" *ngIf="summary()">
          <div class="summary-card total">
            <h3>Total Sales</h3>
            <div class="summary-value">\${{ summary()!.totalSales | number:'1.2-2' }}</div>
            <div class="summary-detail">{{ summary()!.transactionCount }} transactions</div>
          </div>
          <div class="summary-card shop">
            <h3>Shop Revenue</h3>
            <div class="summary-value">\${{ summary()!.totalShopAmount | number:'1.2-2' }}</div>
            <div class="summary-detail">After commissions</div>
          </div>
          <div class="summary-card provider">
            <h3>Provider Payouts</h3>
            <div class="summary-value">\${{ summary()!.totalProviderAmount | number:'1.2-2' }}</div>
            <div class="summary-detail">Commissions owed</div>
          </div>
          <div class="summary-card average">
            <h3>Average Sale</h3>
            <div class="summary-value">\${{ summary()!.averageTransactionValue | number:'1.2-2' }}</div>
            <div class="summary-detail">Per transaction</div>
          </div>
        </div>

        <!-- Transactions Table -->
        <div class="transactions-section">
          <div class="section-header">
            <h2>Recent Transactions</h2>
            <div class="table-controls">
              <select [(ngModel)]="pageSize" (change)="updatePageSize()" class="page-size-select">
                <option value="10">10 per page</option>
                <option value="20">20 per page</option>
                <option value="50">50 per page</option>
              </select>
            </div>
          </div>

          <div class="table-container" *ngIf="pagedResult(); else loadingTransactions">
            <table class="transactions-table">
              <thead>
                <tr>
                  <th (click)="setSorting('saleDate')" class="sortable">
                    Date
                    <span class="sort-indicator" [class.active]="sortBy === 'saleDate'">
                      {{ sortDirection === 'asc' ? '↑' : '↓' }}
                    </span>
                  </th>
                  <th>Item</th>
                  <th>Provider</th>
                  <th (click)="setSorting('salePrice')" class="sortable">
                    Sale Price
                    <span class="sort-indicator" [class.active]="sortBy === 'salePrice'">
                      {{ sortDirection === 'asc' ? '↑' : '↓' }}
                    </span>
                  </th>
                  <th>Payment</th>
                  <th>Commission</th>
                  <th>Shop Amount</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let transaction of pagedResult()!.items" class="transaction-row">
                  <td class="date-cell">
                    {{ transaction.saleDate | date:'MMM d, y' }}
                  </td>
                  <td class="item-cell">
                    <div class="item-info">
                      <div class="item-name">{{ transaction.item.name }}</div>
                      <div class="item-description">{{ transaction.item.description }}</div>
                    </div>
                  </td>
                  <td class="provider-cell">
                    <div class="provider-info">
                      <div class="provider-name">{{ transaction.provider.name }}</div>
                      <div class="commission-rate">{{ transaction.providerSplitPercentage }}% commission</div>
                    </div>
                  </td>
                  <td class="price-cell">
                    <div class="sale-price">\${{ transaction.salePrice | number:'1.2-2' }}</div>
                    <div class="tax-amount" *ngIf="transaction.salesTaxAmount">
                      +\${{ transaction.salesTaxAmount | number:'1.2-2' }} tax
                    </div>
                  </td>
                  <td class="payment-cell">
                    <span class="payment-badge" [class]="'payment-' + transaction.paymentMethod.toLowerCase()">
                      {{ transaction.paymentMethod }}
                    </span>
                  </td>
                  <td class="commission-cell">
                    \${{ transaction.providerAmount | number:'1.2-2' }}
                  </td>
                  <td class="shop-amount-cell">
                    \${{ transaction.shopAmount | number:'1.2-2' }}
                  </td>
                  <td class="actions-cell">
                    <div class="action-buttons">
                      <button class="btn-icon" (click)="viewTransaction(transaction)" title="View Details">
                        👁️
                      </button>
                      <button class="btn-icon" (click)="editTransaction(transaction)" title="Edit">
                        ✏️
                      </button>
                      <button class="btn-icon danger" (click)="deleteTransaction(transaction)" title="Void Sale">
                        🗑️
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>

            <!-- Pagination -->
            <div class="pagination" *ngIf="pagedResult()!.totalPages > 1">
              <button
                class="page-btn"
                [disabled]="currentPage === 1"
                (click)="goToPage(currentPage - 1)">
                Previous
              </button>

              <div class="page-numbers">
                <button
                  *ngFor="let page of getPageNumbers()"
                  class="page-btn"
                  [class.active]="page === currentPage"
                  (click)="goToPage(page)">
                  {{ page }}
                </button>
              </div>

              <button
                class="page-btn"
                [disabled]="currentPage === pagedResult()!.totalPages"
                (click)="goToPage(currentPage + 1)">
                Next
              </button>
            </div>
          </div>

          <ng-template #loadingTransactions>
            <div class="loading-state">
              <div class="loading-spinner"></div>
              <p>Loading transactions...</p>
            </div>
          </ng-template>
        </div>

        <!-- Process Sale Modal -->
        <div class="modal-overlay" *ngIf="showCreateModal" (click)="closeModal($event)">
          <div class="modal-content" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h2>Process Sale</h2>
              <button class="close-btn" (click)="closeModal()">×</button>
            </div>

            <div class="modal-body">
              <form (ngSubmit)="onSubmitSale()" #saleForm="ngForm">
                <!-- Item Selection -->
                <div class="form-group">
                  <label for="itemId">Item *</label>
                  <select
                    id="itemId"
                    [(ngModel)]="saleForm_itemId"
                    name="itemId"
                    (change)="onItemSelected()"
                    class="form-control"
                    required>
                    <option value="">Select an item...</option>
                    <option *ngFor="let item of availableItems()" [value]="item.id">
                      {{ item.name }} - \${{ item.price | number:'1.2-2' }}
                    </option>
                  </select>
                </div>

                <!-- Provider Info (Auto-populated) -->
                <div class="form-group" *ngIf="selectedProvider()">
                  <label>Provider</label>
                  <div class="provider-info-display">
                    <div class="provider-name">{{ selectedProvider()!.name }}</div>
                    <div class="commission-rate">{{ selectedProvider()!.commissionRate }}% commission</div>
                  </div>
                </div>

                <!-- Sale Price -->
                <div class="form-group">
                  <label for="salePrice">Sale Price *</label>
                  <input
                    type="number"
                    id="salePrice"
                    [(ngModel)]="saleForm_salePrice"
                    name="salePrice"
                    (input)="calculateCommission()"
                    step="0.01"
                    min="0"
                    class="form-control"
                    required>
                </div>

                <!-- Sales Tax -->
                <div class="form-group">
                  <label for="salesTax">Sales Tax</label>
                  <input
                    type="number"
                    id="salesTax"
                    [(ngModel)]="saleForm_salesTax"
                    name="salesTax"
                    (input)="calculateCommission()"
                    step="0.01"
                    min="0"
                    class="form-control">
                </div>

                <!-- Sale Date -->
                <div class="form-group">
                  <label for="saleDate">Sale Date *</label>
                  <input
                    type="date"
                    id="saleDate"
                    [(ngModel)]="saleForm_saleDate"
                    name="saleDate"
                    class="form-control"
                    required>
                </div>

                <!-- Payment Method -->
                <div class="form-group">
                  <label for="paymentMethod">Payment Method *</label>
                  <select
                    id="paymentMethod"
                    [(ngModel)]="saleForm_paymentMethod"
                    name="paymentMethod"
                    class="form-control"
                    required>
                    <option value="">Select payment method...</option>
                    <option value="Cash">Cash</option>
                    <option value="Credit Card">Credit Card</option>
                    <option value="Debit Card">Debit Card</option>
                    <option value="Check">Check</option>
                    <option value="Other">Other</option>
                  </select>
                </div>

                <!-- Commission Split Display -->
                <div class="commission-split" *ngIf="selectedProvider() && saleForm_salePrice">
                  <h4>Commission Split</h4>
                  <div class="split-row">
                    <span>Provider ({{ selectedProvider()!.commissionRate }}%):</span>
                    <span class="amount">\${{ calculatedProviderAmount() | number:'1.2-2' }}</span>
                  </div>
                  <div class="split-row">
                    <span>Shop ({{ 100 - selectedProvider()!.commissionRate }}%):</span>
                    <span class="amount">\${{ calculatedShopAmount() | number:'1.2-2' }}</span>
                  </div>
                  <div class="split-row total">
                    <span>Total:</span>
                    <span class="amount">\${{ saleForm_salePrice | number:'1.2-2' }}</span>
                  </div>
                </div>

                <!-- Notes -->
                <div class="form-group">
                  <label for="notes">Notes</label>
                  <textarea
                    id="notes"
                    [(ngModel)]="saleForm_notes"
                    name="notes"
                    rows="3"
                    class="form-control"
                    placeholder="Optional notes about this sale..."></textarea>
                </div>

                <!-- Submit Buttons -->
                <div class="modal-actions">
                  <button type="button" class="btn-secondary" (click)="closeModal()">Cancel</button>
                  <button
                    type="submit"
                    class="btn-primary"
                    [disabled]="!saleForm.form.valid || isSubmitting()"
                    [class.loading]="isSubmitting()">
                    {{ isSubmitting() ? 'Processing...' : 'Process Sale' }}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>

        <!-- Transaction Detail Modal -->
        <div class="modal-overlay" *ngIf="showDetailModal && selectedTransactionForDetail()" (click)="closeDetailModal($event)">
          <div class="modal-content detail-modal" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h2>Transaction Details</h2>
              <button class="close-btn" (click)="closeDetailModal()">×</button>
            </div>

            <div class="modal-body">
              <div class="transaction-detail" *ngIf="selectedTransactionForDetail() as transaction">
                <!-- Header with key transaction info -->
                <div class="detail-header">
                  <div class="header-main">
                    <h3>{{ transaction.item.name }}</h3>
                    <div class="header-meta">
                      <span class="transaction-id">ID: {{ transaction.id }}</span>
                      <span class="sale-date">{{ transaction.saleDate | date:'MMM d, yyyy h:mm a' }}</span>
                    </div>
                  </div>
                  <div class="header-amount">
                    <div class="sale-total">\${{ transaction.salePrice | number:'1.2-2' }}</div>
                    <div class="payment-method">
                      <span class="payment-badge" [class]="'payment-' + transaction.paymentMethod.toLowerCase()">
                        {{ transaction.paymentMethod }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Two column layout for details -->
                <div class="detail-columns">
                  <!-- Left Column -->
                  <div class="detail-column">
                    <!-- Item Information -->
                    <div class="detail-section">
                      <h4>Item Details</h4>
                      <div class="detail-grid compact">
                        <div class="detail-row" *ngIf="transaction.item.description">
                          <label>Description:</label>
                          <span>{{ transaction.item.description }}</span>
                        </div>
                        <div class="detail-row">
                          <label>Original Price:</label>
                          <span class="amount">\${{ transaction.item.originalPrice | number:'1.2-2' }}</span>
                        </div>
                        <div class="detail-row" *ngIf="transaction.salesTaxAmount">
                          <label>Sales Tax:</label>
                          <span class="amount">\${{ transaction.salesTaxAmount | number:'1.2-2' }}</span>
                        </div>
                      </div>
                    </div>

                    <!-- Commission Split -->
                    <div class="detail-section">
                      <h4>Commission Split</h4>
                      <div class="commission-breakdown compact">
                        <div class="breakdown-row">
                          <span>Provider ({{ transaction.providerSplitPercentage }}%)</span>
                          <span class="amount provider-amount">\${{ transaction.providerAmount | number:'1.2-2' }}</span>
                        </div>
                        <div class="breakdown-row">
                          <span>Shop ({{ 100 - transaction.providerSplitPercentage }}%)</span>
                          <span class="amount shop-amount">\${{ transaction.shopAmount | number:'1.2-2' }}</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Right Column -->
                  <div class="detail-column">
                    <!-- Provider Information -->
                    <div class="detail-section">
                      <h4>Provider</h4>
                      <div class="detail-grid compact">
                        <div class="detail-row">
                          <label>Name:</label>
                          <span>{{ transaction.provider.name }}</span>
                        </div>
                        <div class="detail-row" *ngIf="transaction.provider.email">
                          <label>Email:</label>
                          <span>{{ transaction.provider.email }}</span>
                        </div>
                        <div class="detail-row">
                          <label>Commission Rate:</label>
                          <span>{{ transaction.providerSplitPercentage }}%</span>
                        </div>
                      </div>
                    </div>

                    <!-- Audit Information -->
                    <div class="detail-section">
                      <h4>Audit Trail</h4>
                      <div class="detail-grid compact">
                        <div class="detail-row">
                          <label>Created:</label>
                          <span>{{ transaction.createdAt | date:'MMM d, yyyy h:mm a' }}</span>
                        </div>
                        <div class="detail-row">
                          <label>Updated:</label>
                          <span>{{ transaction.updatedAt | date:'MMM d, yyyy h:mm a' }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Notes (full width if present) -->
                <div class="detail-section full-width" *ngIf="transaction.notes">
                  <h4>Notes</h4>
                  <div class="notes-content">
                    {{ transaction.notes }}
                  </div>
                </div>
              </div>

              <div class="modal-actions">
                <button type="button" class="btn-secondary" (click)="closeDetailModal()">Close</button>
                <button type="button" class="btn-primary" (click)="editTransactionFromDetail()">Edit Transaction</button>
              </div>
            </div>
          </div>
        </div>

        <!-- Edit Transaction Modal -->
        <div class="modal-overlay" *ngIf="showEditModal && selectedTransactionForEdit()" (click)="closeEditModal($event)">
          <div class="modal-content edit-modal-content" (click)="$event.stopPropagation()">
            <div class="modal-header compact">
              <h2>Edit Transaction</h2>
              <button class="close-btn" (click)="closeEditModal()">×</button>
            </div>

            <div class="modal-body compact">
              <div class="edit-transaction-info" *ngIf="selectedTransactionForEdit() as transaction">
                <!-- Compact Transaction Summary -->
                <div class="transaction-summary compact">
                  <div class="summary-row">
                    <strong>{{ transaction.item.name }}</strong>
                    <span class="provider-info">{{ transaction.provider.name }} ({{ transaction.providerSplitPercentage }}%)</span>
                  </div>
                </div>

                <form (ngSubmit)="onSubmitEditTransaction()" #editForm="ngForm">
                  <!-- Compact Form Layout -->
                  <div class="form-row-2">
                    <div class="form-group compact">
                      <label for="editSalePrice">Sale Price *</label>
                      <input
                        type="number"
                        id="editSalePrice"
                        [(ngModel)]="editForm_salePrice"
                        name="editSalePrice"
                        (input)="calculateEditCommission()"
                        step="0.01"
                        min="0"
                        class="form-control compact"
                        required>
                    </div>

                    <div class="form-group compact">
                      <label for="editSalesTax">Sales Tax</label>
                      <input
                        type="number"
                        id="editSalesTax"
                        [(ngModel)]="editForm_salesTax"
                        name="editSalesTax"
                        (input)="calculateEditCommission()"
                        step="0.01"
                        min="0"
                        class="form-control compact">
                    </div>
                  </div>

                  <div class="form-group compact">
                    <label for="editPaymentMethod">Payment Method *</label>
                    <select
                      id="editPaymentMethod"
                      [(ngModel)]="editForm_paymentMethod"
                      name="editPaymentMethod"
                      class="form-control compact"
                      required>
                      <option value="">Select payment method...</option>
                      <option value="Cash">Cash</option>
                      <option value="Credit Card">Credit Card</option>
                      <option value="Debit Card">Debit Card</option>
                      <option value="Check">Check</option>
                      <option value="Other">Other</option>
                    </select>
                  </div>

                  <!-- Inline Commission Split -->
                  <div class="commission-split-inline" *ngIf="editForm_salePrice">
                    <div class="split-summary">
                      <span class="split-item">Provider: <strong>\${{ calculatedProviderAmount() | number:'1.2-2' }}</strong></span>
                      <span class="split-item">Shop: <strong>\${{ calculatedShopAmount() | number:'1.2-2' }}</strong></span>
                      <span class="split-item total">Total: <strong>\${{ editForm_salePrice | number:'1.2-2' }}</strong></span>
                    </div>
                  </div>

                  <!-- Compact Notes -->
                  <div class="form-group compact">
                    <label for="editNotes">Notes</label>
                    <textarea
                      id="editNotes"
                      [(ngModel)]="editForm_notes"
                      name="editNotes"
                      rows="2"
                      class="form-control compact"
                      placeholder="Optional notes..."></textarea>
                  </div>

                  <!-- Submit Buttons -->
                  <div class="modal-actions compact">
                    <button type="button" class="btn-secondary" (click)="closeEditModal()">Cancel</button>
                    <button
                      type="submit"
                      class="btn-primary"
                      [disabled]="!editForm.form.valid || isSubmitting()"
                      [class.loading]="isSubmitting()">
                      {{ isSubmitting() ? 'Updating...' : 'Update Transaction' }}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>

        <!-- Styled Confirmation Dialog -->
        <div class="modal-overlay" *ngIf="showConfirmDialog" (click)="closeConfirmDialog()">
          <div class="modal-content confirm-dialog" (click)="$event.stopPropagation()">
            <div class="confirm-header" [class.destructive]="confirmDialog.isDestructive">
              <div class="confirm-icon">
                <span *ngIf="confirmDialog.isDestructive">⚠️</span>
                <span *ngIf="!confirmDialog.isDestructive">❓</span>
              </div>
              <h3>{{ confirmDialog.title }}</h3>
            </div>

            <div class="confirm-body">
              <p>{{ confirmDialog.message }}</p>
            </div>

            <div class="confirm-actions">
              <button type="button" class="btn-secondary" (click)="closeConfirmDialog()">
                {{ confirmDialog.cancelText }}
              </button>
              <button
                type="button"
                class="btn-primary"
                [class.btn-danger]="confirmDialog.isDestructive"
                (click)="executeConfirmAction()">
                {{ confirmDialog.confirmText }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </app-owner-layout>
  `,
  styles: [`
    .sales-page {
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

    .page-header h1 {
      color: #059669;
      margin-bottom: 0.5rem;
      font-size: 2rem;
    }

    .page-header p {
      color: #6b7280;
      margin: 0;
    }

    .btn-primary, .btn-secondary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      border: none;
      transition: all 0.2s;
    }

    .btn-primary {
      background: #059669;
      color: white;
    }

    .btn-primary:hover {
      background: #047857;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .btn-secondary:hover {
      background: #e5e7eb;
    }

    .filters-section {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      padding: 1.5rem;
      margin-bottom: 2rem;
    }

    .filter-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      align-items: end;
    }

    .filter-group {
      display: flex;
      flex-direction: column;
    }

    .filter-group label {
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
    }

    .filter-input, .filter-select {
      padding: 0.5rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
    }

    .filter-input:focus, .filter-select:focus {
      outline: none;
      border-color: #059669;
      box-shadow: 0 0 0 3px rgba(5, 150, 105, 0.1);
    }

    .summary-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .summary-card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      padding: 1.5rem;
      border-left: 4px solid #059669;
    }

    .summary-card.total { border-left-color: #059669; }
    .summary-card.shop { border-left-color: #0891b2; }
    .summary-card.provider { border-left-color: #7c3aed; }
    .summary-card.average { border-left-color: #f59e0b; }

    .summary-card h3 {
      color: #6b7280;
      font-size: 0.875rem;
      font-weight: 600;
      text-transform: uppercase;
      margin-bottom: 0.5rem;
      letter-spacing: 0.05em;
    }

    .summary-value {
      font-size: 2rem;
      font-weight: bold;
      color: #1f2937;
      margin-bottom: 0.25rem;
    }

    .summary-detail {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .transactions-section {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.1);
      overflow: hidden;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .section-header h2 {
      color: #1f2937;
      font-size: 1.25rem;
      margin: 0;
    }

    .page-size-select {
      padding: 0.5rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
    }

    .table-container {
      overflow-x: auto;
    }

    .transactions-table {
      width: 100%;
      border-collapse: collapse;
    }

    .transactions-table th {
      background: #f9fafb;
      padding: 1rem;
      text-align: left;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .transactions-table th.sortable {
      cursor: pointer;
      user-select: none;
    }

    .transactions-table th.sortable:hover {
      background: #f3f4f6;
    }

    .sort-indicator {
      opacity: 0.3;
      margin-left: 0.5rem;
    }

    .sort-indicator.active {
      opacity: 1;
      color: #059669;
    }

    .transaction-row {
      border-bottom: 1px solid #e5e7eb;
    }

    .transaction-row:hover {
      background: #f9fafb;
    }

    .transaction-row td {
      padding: 1rem;
      vertical-align: top;
    }

    .item-info, .provider-info {
      display: flex;
      flex-direction: column;
    }

    .item-name, .provider-name {
      font-weight: 600;
      color: #1f2937;
      margin-bottom: 0.25rem;
    }

    .item-description, .commission-rate {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .sale-price {
      font-weight: 600;
      color: #1f2937;
    }

    .tax-amount {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .payment-badge {
      display: inline-block;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .payment-cash { background: #dcfce7; color: #166534; }
    .payment-card { background: #dbeafe; color: #1e40af; }
    .payment-online { background: #fdf4ff; color: #7c2d12; }

    .commission-cell, .shop-amount-cell {
      font-weight: 600;
      color: #059669;
    }

    .action-buttons {
      display: flex;
      gap: 0.5rem;
    }

    .btn-icon {
      width: 32px;
      height: 32px;
      border: none;
      background: #f3f4f6;
      border-radius: 6px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .btn-icon:hover {
      background: #e5e7eb;
    }

    .btn-icon.danger:hover {
      background: #fee2e2;
      color: #dc2626;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      padding: 1.5rem;
      border-top: 1px solid #e5e7eb;
    }

    .page-btn {
      padding: 0.5rem 1rem;
      border: 1px solid #d1d5db;
      background: white;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .page-btn:hover:not(:disabled) {
      background: #f3f4f6;
    }

    .page-btn.active {
      background: #059669;
      color: white;
      border-color: #059669;
    }

    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-numbers {
      display: flex;
      gap: 0.5rem;
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 4rem;
      color: #6b7280;
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 3px solid #e5e7eb;
      border-top: 3px solid #059669;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 1rem;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    /* Modal Styles */
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
      padding: 1rem;
    }

    .modal-content {
      background: white;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      max-width: 600px;
      width: 100%;
      max-height: 90vh;
      overflow-y: auto;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .modal-header h2 {
      color: #1f2937;
      font-size: 1.5rem;
      margin: 0;
    }

    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #6b7280;
      padding: 0;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .close-btn:hover {
      background: #f3f4f6;
      color: #374151;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .form-control:focus {
      outline: none;
      border-color: #059669;
      box-shadow: 0 0 0 3px rgba(5, 150, 105, 0.1);
    }

    .form-control:invalid {
      border-color: #ef4444;
    }

    .provider-info-display {
      background: #f3f4f6;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      padding: 0.75rem;
    }

    .provider-name {
      font-weight: 600;
      color: #1f2937;
      margin-bottom: 0.25rem;
    }

    .commission-rate {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .commission-split {
      background: #f9fafb;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      padding: 1rem;
      margin-top: 1rem;
    }

    .commission-split h4 {
      color: #1f2937;
      font-size: 1rem;
      margin-bottom: 0.75rem;
    }

    .split-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
      color: #374151;
    }

    .split-row.total {
      border-top: 1px solid #d1d5db;
      padding-top: 0.5rem;
      margin-top: 0.5rem;
      font-weight: 600;
      color: #1f2937;
    }

    .split-row .amount {
      font-weight: 600;
      color: #059669;
    }

    .split-row.total .amount {
      color: #1f2937;
    }

    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      margin-top: 2rem;
      padding-top: 1.5rem;
      border-top: 1px solid #e5e7eb;
    }

    .btn-primary.loading {
      position: relative;
      color: transparent;
    }

    .btn-primary.loading:after {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 16px;
      height: 16px;
      border: 2px solid white;
      border-top: 2px solid transparent;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    /* Detail Modal Styles */
    .detail-modal {
      max-width: 900px;
    }

    .transaction-detail {
      max-height: 80vh;
      overflow-y: auto;
    }

    /* Header section with key info */
    .detail-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 1.5rem;
      background: linear-gradient(135deg, #059669 0%, #047857 100%);
      border-radius: 8px;
      margin-bottom: 1.5rem;
      color: white;
    }

    .header-main h3 {
      color: white;
      font-size: 1.5rem;
      font-weight: 700;
      margin-bottom: 0.5rem;
    }

    .header-meta {
      display: flex;
      gap: 1rem;
      opacity: 0.9;
    }

    .transaction-id {
      font-family: 'Courier New', monospace;
      font-size: 0.875rem;
    }

    .sale-date {
      font-size: 0.875rem;
    }

    .header-amount {
      text-align: right;
    }

    .sale-total {
      font-size: 2rem;
      font-weight: 700;
      color: white;
      margin-bottom: 0.5rem;
    }

    .payment-method .payment-badge {
      background: rgba(255, 255, 255, 0.2);
      color: white;
      border: 1px solid rgba(255, 255, 255, 0.3);
    }

    /* Two column layout */
    .detail-columns {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
      margin-bottom: 1.5rem;
    }

    .detail-column {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .detail-section {
      background: #f8fafc;
      border-radius: 8px;
      padding: 1.25rem;
      border: 1px solid #e2e8f0;
    }

    .detail-section.full-width {
      grid-column: 1 / -1;
      margin-top: 1rem;
    }

    .detail-section h4 {
      color: #1e293b;
      font-size: 1rem;
      font-weight: 600;
      margin-bottom: 1rem;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid #e2e8f0;
    }

    .detail-grid {
      display: grid;
      gap: 0.75rem;
    }

    .detail-grid.compact {
      gap: 0.5rem;
    }

    .detail-row {
      display: grid;
      grid-template-columns: 120px 1fr;
      gap: 0.75rem;
      align-items: center;
    }

    .detail-row label {
      font-weight: 600;
      color: #475569;
      font-size: 0.875rem;
    }

    .detail-row span {
      color: #1e293b;
      font-size: 0.875rem;
    }

    .detail-row .amount {
      font-weight: 600;
      color: #059669;
    }

    /* Commission breakdown */
    .commission-breakdown {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 1rem;
    }

    .commission-breakdown.compact {
      padding: 0.75rem;
    }

    .breakdown-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
      color: #475569;
      font-size: 0.875rem;
    }

    .breakdown-row:last-child {
      margin-bottom: 0;
    }

    .breakdown-row .amount {
      font-weight: 600;
    }

    .breakdown-row .provider-amount {
      color: #7c3aed;
    }

    .breakdown-row .shop-amount {
      color: #0891b2;
    }

    /* Notes section */
    .notes-content {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 1rem;
      color: #475569;
      line-height: 1.5;
      font-size: 0.875rem;
    }

    /* Edit Transaction Modal */
    .edit-modal-content {
      max-width: 600px;
      max-height: 90vh;
    }

    .modal-header.compact {
      padding: 1rem 1.5rem;
    }

    .modal-body.compact {
      padding: 0 1.5rem 1.5rem;
    }

    .transaction-summary.compact {
      background: #f1f5f9;
      border: 1px solid #cbd5e1;
      border-radius: 6px;
      padding: 0.75rem;
      margin-bottom: 1rem;
    }

    .summary-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .provider-info {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .form-group.compact {
      margin-bottom: 1rem;
    }

    .form-group.compact label {
      margin-bottom: 0.375rem;
      font-size: 0.875rem;
    }

    .form-control.compact {
      padding: 0.5rem;
      font-size: 0.875rem;
    }

    .form-row-2 {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .commission-split-inline {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      padding: 0.75rem;
      margin-bottom: 1rem;
    }

    .split-summary {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .split-item {
      font-size: 0.875rem;
      color: #475569;
    }

    .split-item.total {
      color: #059669;
      font-weight: 600;
    }

    .modal-actions.compact {
      padding-top: 1rem;
      margin-top: 0;
    }

    /* Confirmation Dialog */
    .confirm-dialog {
      max-width: 500px;
    }

    .confirm-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.5rem 1.5rem 1rem;
      border-bottom: 1px solid #e2e8f0;
    }

    .confirm-header.destructive {
      border-bottom-color: #fecaca;
      background: linear-gradient(135deg, #fef2f2 0%, #fff 100%);
    }

    .confirm-icon {
      font-size: 2rem;
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f1f5f9;
      border-radius: 50%;
    }

    .confirm-header.destructive .confirm-icon {
      background: #fee2e2;
    }

    .confirm-header h3 {
      color: #1e293b;
      font-size: 1.25rem;
      font-weight: 600;
      margin: 0;
    }

    .confirm-body {
      padding: 1rem 1.5rem;
    }

    .confirm-body p {
      color: #475569;
      line-height: 1.5;
      margin: 0;
    }

    .confirm-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      padding: 1rem 1.5rem 1.5rem;
    }

    .btn-danger {
      background: #dc2626;
      color: white;
      border: 1px solid #dc2626;
    }

    .btn-danger:hover {
      background: #b91c1c;
      border-color: #b91c1c;
    }

    @media (max-width: 768px) {
      .sales-page {
        padding: 1rem;
      }

      .page-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .filter-row {
        grid-template-columns: 1fr;
      }

      .summary-cards {
        grid-template-columns: 1fr;
      }

      .section-header {
        flex-direction: column;
        gap: 1rem;
        align-items: stretch;
      }

      .transactions-table {
        font-size: 0.875rem;
      }

      .transactions-table th,
      .transactions-table td {
        padding: 0.75rem 0.5rem;
      }

      .modal-overlay {
        padding: 0.5rem;
      }

      .modal-content {
        max-height: 95vh;
      }

      .modal-actions {
        flex-direction: column;
        gap: 0.75rem;
      }

      .modal-actions button {
        width: 100%;
      }

      .detail-modal {
        max-width: 95vw;
      }

      .detail-header {
        flex-direction: column;
        gap: 1rem;
        text-align: left;
      }

      .header-amount {
        text-align: left !important;
      }

      .sale-total {
        font-size: 1.5rem;
      }

      .header-meta {
        flex-direction: column;
        gap: 0.5rem;
      }

      .detail-columns {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .detail-row {
        grid-template-columns: 1fr;
        gap: 0.25rem;
      }

      .detail-row label {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #6b7280;
      }

      .breakdown-row {
        flex-direction: column;
        align-items: stretch;
        gap: 0.25rem;
      }

      .breakdown-row .amount {
        text-align: right;
        font-size: 1rem;
      }

      /* Mobile styles for compact edit modal */
      .edit-modal-content {
        max-width: 95vw;
        max-height: 95vh;
      }

      .form-row-2 {
        grid-template-columns: 1fr;
        gap: 0.75rem;
      }

      .summary-row {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.25rem;
      }

      .split-summary {
        flex-direction: column;
        align-items: stretch;
        gap: 0.5rem;
      }

      .split-item {
        display: flex;
        justify-content: space-between;
        padding: 0.25rem 0;
        border-bottom: 1px solid #e2e8f0;
      }

      .split-item:last-child {
        border-bottom: none;
        padding-top: 0.5rem;
        border-top: 2px solid #059669;
        margin-top: 0.25rem;
      }
    }
  `]
})
export class OwnerSalesComponent implements OnInit {
  pagedResult = signal<PagedResult<Transaction> | null>(null);
  summary = signal<any>(null);
  loading = signal(false);

  // Filters
  filters = {
    startDate: '',
    endDate: '',
    paymentMethod: ''
  };

  // Pagination
  currentPage = 1;
  pageSize = 20;

  // Sorting
  sortBy = 'saleDate';
  sortDirection = 'desc';

  // Modal state
  showCreateModal = false;
  showDetailModal = false;
  showEditModal = false;
  showConfirmDialog = false;
  isSubmitting = signal(false);
  selectedTransactionForDetail = signal<Transaction | null>(null);
  selectedTransactionForEdit = signal<Transaction | null>(null);

  // Confirmation dialog state
  confirmDialog = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmAction: () => {},
    isDestructive: false
  };

  // Available data for form
  availableItems = signal<Item[]>([]);
  providers = signal<Provider[]>([]);
  selectedProvider = signal<Provider | null>(null);

  // Form data
  saleForm_itemId: number | null = null;
  saleForm_salePrice: number | null = null;
  saleForm_salesTax: number | null = null;
  saleForm_saleDate: string = '';
  saleForm_paymentMethod: string = '';
  saleForm_notes: string = '';

  // Edit form data
  editForm_salePrice: number | null = null;
  editForm_salesTax: number | null = null;
  editForm_paymentMethod: string = '';
  editForm_notes: string = '';

  // Calculated amounts
  calculatedProviderAmount = signal(0);
  calculatedShopAmount = signal(0);

  private toastr = inject(ToastrService);

  constructor(
    private transactionService: TransactionService,
    private itemService: ItemService,
    private providerService: ProviderService
  ) {}

  ngOnInit() {
    this.loadTransactions();
    this.loadSummary();
    this.loadAvailableItems();
    this.loadProviders();
    this.initializeSaleForm();
  }

  private buildQueryParams(): TransactionQueryParams {
    return {
      startDate: this.filters.startDate ? new Date(this.filters.startDate) : undefined,
      endDate: this.filters.endDate ? new Date(this.filters.endDate) : undefined,
      paymentMethod: this.filters.paymentMethod || undefined,
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  loadTransactions() {
    this.loading.set(true);

    this.transactionService.getTransactions(this.buildQueryParams()).subscribe({
      next: (result) => {
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load transactions:', error);
        this.loading.set(false);
      }
    });
  }

  loadSummary() {
    const queryParams = {
      startDate: this.filters.startDate ? new Date(this.filters.startDate) : undefined,
      endDate: this.filters.endDate ? new Date(this.filters.endDate) : undefined
    };

    this.transactionService.getSalesMetrics(queryParams).subscribe({
      next: (metrics) => {
        this.summary.set(metrics);
      },
      error: (error) => {
        console.error('Failed to load sales metrics:', error);
      }
    });
  }

  applyFilters() {
    this.currentPage = 1;
    this.loadTransactions();
    this.loadSummary();
  }

  clearFilters() {
    this.filters = {
      startDate: '',
      endDate: '',
      paymentMethod: ''
    };
    this.applyFilters();
  }

  setSorting(column: string) {
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'desc';
    }
    this.loadTransactions();
  }

  updatePageSize() {
    this.currentPage = 1;
    this.loadTransactions();
  }

  goToPage(page: number) {
    if (page >= 1 && page <= (this.pagedResult()?.totalPages || 1)) {
      this.currentPage = page;
      this.loadTransactions();
    }
  }

  getPageNumbers(): number[] {
    const totalPages = this.pagedResult()?.totalPages || 1;
    const pages: number[] = [];
    const maxPages = 5;

    let start = Math.max(1, this.currentPage - Math.floor(maxPages / 2));
    let end = Math.min(totalPages, start + maxPages - 1);

    if (end - start < maxPages - 1) {
      start = Math.max(1, end - maxPages + 1);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  viewTransaction(transaction: Transaction) {
    this.selectedTransactionForDetail.set(transaction);
    this.showDetailModal = true;
  }

  editTransaction(transaction: Transaction) {
    this.selectedTransactionForEdit.set(transaction);
    this.initializeEditForm(transaction);
    this.showEditModal = true;
  }

  deleteTransaction(transaction: Transaction) {
    this.showConfirmDialog = true;
    this.confirmDialog = {
      title: 'Void Sale',
      message: `Are you sure you want to void the sale of "${transaction.item.name}"? This action cannot be undone.`,
      confirmText: 'Void Sale',
      cancelText: 'Cancel',
      isDestructive: true,
      confirmAction: () => {
        this.transactionService.deleteTransaction(transaction.id).subscribe({
          next: () => {
            this.loadTransactions();
            this.loadSummary();
            this.showNotification('Sale voided successfully', 'success', 'Transaction Voided');
          },
          error: (error) => {
            console.error('Failed to delete transaction:', error);
            this.showNotification('Failed to void the sale. Please try again.', 'error', 'Delete Failed');
          }
        });
      }
    };
  }

  // Modal and Form Management
  initializeSaleForm() {
    const today = new Date();
    this.saleForm_saleDate = today.toISOString().split('T')[0];
    this.saleForm_itemId = null;
    this.saleForm_salePrice = null;
    this.saleForm_salesTax = null;
    this.saleForm_paymentMethod = '';
    this.saleForm_notes = '';
    this.selectedProvider.set(null);
    this.calculatedProviderAmount.set(0);
    this.calculatedShopAmount.set(0);
  }

  loadAvailableItems() {
    const filters: ItemFilters = { status: ItemStatus.Available };
    this.itemService.getItems(filters).subscribe({
      next: (items) => {
        this.availableItems.set(items);
      },
      error: (error) => {
        console.error('Failed to load available items:', error);
      }
    });
  }

  loadProviders() {
    this.providerService.getProviders().subscribe({
      next: (providers) => {
        this.providers.set(providers.filter(p => p.isActive));
      },
      error: (error) => {
        console.error('Failed to load providers:', error);
      }
    });
  }

  onItemSelected() {
    const itemId = this.saleForm_itemId;
    if (!itemId) {
      this.selectedProvider.set(null);
      this.saleForm_salePrice = null;
      this.calculateCommission();
      return;
    }

    // Find the selected item
    const selectedItem = this.availableItems().find(item => item.id === itemId);
    if (selectedItem) {
      // Set the sale price to the item's listed price
      this.saleForm_salePrice = selectedItem.price;

      // Find and set the provider
      const provider = this.providers().find(p => p.id === selectedItem.providerId);
      this.selectedProvider.set(provider || null);

      // Recalculate commission split
      this.calculateCommission();
    }
  }

  calculateCommission() {
    const salePrice = this.saleForm_salePrice || 0;
    const provider = this.selectedProvider();

    if (provider && salePrice > 0) {
      const providerAmount = (salePrice * provider.commissionRate) / 100;
      const shopAmount = salePrice - providerAmount;

      this.calculatedProviderAmount.set(providerAmount);
      this.calculatedShopAmount.set(shopAmount);
    } else {
      this.calculatedProviderAmount.set(0);
      this.calculatedShopAmount.set(0);
    }
  }

  closeModal(event?: Event) {
    this.showCreateModal = false;
    this.initializeSaleForm();
  }

  closeDetailModal(event?: Event) {
    this.showDetailModal = false;
    this.selectedTransactionForDetail.set(null);
  }

  editTransactionFromDetail() {
    const transaction = this.selectedTransactionForDetail();
    if (transaction) {
      this.closeDetailModal();
      this.editTransaction(transaction);
    }
  }

  onSubmitSale() {
    if (!this.saleForm_itemId || !this.saleForm_salePrice || !this.saleForm_paymentMethod || !this.saleForm_saleDate) {
      alert('Please fill in all required fields.');
      return;
    }

    this.isSubmitting.set(true);

    const request: CreateTransactionRequest = {
      itemId: this.saleForm_itemId.toString(),
      salePrice: this.saleForm_salePrice,
      salesTaxAmount: this.saleForm_salesTax || undefined,
      paymentMethod: this.saleForm_paymentMethod,
      notes: this.saleForm_notes || undefined,
      saleDate: new Date(this.saleForm_saleDate)
    };

    this.transactionService.createTransaction(request).subscribe({
      next: (transaction) => {
        this.isSubmitting.set(false);
        this.showCreateModal = false;
        this.initializeSaleForm();

        // Reload data to reflect the new sale
        this.loadTransactions();
        this.loadSummary();
        this.loadAvailableItems(); // Refresh to remove the sold item

        this.showNotification(`Transaction ${transaction.id} created successfully!`, 'success', 'Sale Processed');
      },
      error: (error) => {
        this.isSubmitting.set(false);
        console.error('Failed to process sale:', error);
        this.showNotification('Failed to process the sale. Please try again.', 'error', 'Process Sale Failed');
      }
    });
  }

  // Edit Transaction Methods
  initializeEditForm(transaction: Transaction) {
    this.editForm_salePrice = transaction.salePrice;
    this.editForm_salesTax = transaction.salesTaxAmount || null;
    this.editForm_paymentMethod = transaction.paymentMethod;
    this.editForm_notes = transaction.notes || '';

    // Set up calculation with existing commission rate
    const provider = this.providers().find(p => p.name === transaction.provider.name);
    this.selectedProvider.set(provider || null);
    this.calculateEditCommission();
  }

  calculateEditCommission() {
    const salePrice = this.editForm_salePrice || 0;
    const transaction = this.selectedTransactionForEdit();

    if (transaction && salePrice > 0) {
      const providerAmount = (salePrice * transaction.providerSplitPercentage) / 100;
      const shopAmount = salePrice - providerAmount;

      this.calculatedProviderAmount.set(providerAmount);
      this.calculatedShopAmount.set(shopAmount);
    } else {
      this.calculatedProviderAmount.set(0);
      this.calculatedShopAmount.set(0);
    }
  }

  closeEditModal(event?: Event) {
    this.showEditModal = false;
    this.selectedTransactionForEdit.set(null);
    this.clearEditForm();
  }

  clearEditForm() {
    this.editForm_salePrice = null;
    this.editForm_salesTax = null;
    this.editForm_paymentMethod = '';
    this.editForm_notes = '';
    this.calculatedProviderAmount.set(0);
    this.calculatedShopAmount.set(0);
  }

  onSubmitEditTransaction() {
    const transaction = this.selectedTransactionForEdit();
    if (!transaction || !this.editForm_salePrice || !this.editForm_paymentMethod) {
      this.showNotification('Please fill in all required fields.', 'error', 'Validation Error');
      return;
    }

    this.showConfirmDialog = true;
    this.confirmDialog = {
      title: 'Update Transaction',
      message: `Are you sure you want to update this transaction? The sale price will change from $${transaction.salePrice.toFixed(2)} to $${this.editForm_salePrice.toFixed(2)}.`,
      confirmText: 'Update Transaction',
      cancelText: 'Cancel',
      isDestructive: false,
      confirmAction: () => {
        this.submitEditTransaction();
      }
    };
  }

  submitEditTransaction() {
    const transaction = this.selectedTransactionForEdit();
    if (!transaction) return;

    this.isSubmitting.set(true);

    const updateRequest = {
      salePrice: this.editForm_salePrice!,
      salesTaxAmount: this.editForm_salesTax || undefined,
      paymentMethod: this.editForm_paymentMethod,
      notes: this.editForm_notes || undefined
    };

    this.transactionService.updateTransaction(transaction.id, updateRequest).subscribe({
      next: (updatedTransaction) => {
        this.isSubmitting.set(false);
        this.showEditModal = false;
        this.clearEditForm();

        // Reload data to reflect the updated transaction
        this.loadTransactions();
        this.loadSummary();

        this.showNotification(`Transaction updated successfully!`, 'success', 'Update Complete');
      },
      error: (error) => {
        this.isSubmitting.set(false);
        console.error('Failed to update transaction:', error);
        this.showNotification('Failed to update the transaction. Please try again.', 'error', 'Update Failed');
      }
    });
  }

  // Confirmation Dialog Methods
  closeConfirmDialog() {
    this.showConfirmDialog = false;
  }

  executeConfirmAction() {
    this.confirmDialog.confirmAction();
    this.closeConfirmDialog();
  }

  // Notification helper using toastr
  showNotification(message: string, type: 'success' | 'error' | 'info' | 'warning' = 'success', title?: string) {
    const options = {
      timeOut: 4000,
      positionClass: 'toast-top-right',
      progressBar: true
    };

    switch (type) {
      case 'success':
        this.toastr.success(message, title || 'Success', options);
        break;
      case 'error':
        this.toastr.error(message, title || 'Error', { ...options, timeOut: 6000 });
        break;
      case 'info':
        this.toastr.info(message, title || 'Info', options);
        break;
      case 'warning':
        this.toastr.warning(message, title || 'Warning', options);
        break;
    }
  }
}