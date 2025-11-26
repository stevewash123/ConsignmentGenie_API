import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OwnerInvitationService } from '../../services/owner-invitation.service';
import {
  OwnerInvitationListDto,
  OwnerInvitationQueryParams,
  InvitationStatus,
  OwnerInvitationMetricsDto
} from '../../models/owner-invitation.model';
import { PagedResult } from '../../shared/models/api.models';

@Component({
  selector: 'app-owner-invitations-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="invitations-container">
      <!-- Metrics Cards -->
      <div class="metrics-grid">
        <div class="metric-card">
          <div class="metric-icon">📧</div>
          <div class="metric-content">
            <div class="metric-value">{{ metrics()?.totalInvitations || 0 }}</div>
            <div class="metric-label">Total Invitations</div>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-icon">⏳</div>
          <div class="metric-content">
            <div class="metric-value">{{ metrics()?.pendingInvitations || 0 }}</div>
            <div class="metric-label">Pending</div>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-icon">✅</div>
          <div class="metric-content">
            <div class="metric-value">{{ metrics()?.acceptedInvitations || 0 }}</div>
            <div class="metric-label">Accepted</div>
          </div>
        </div>

        <div class="metric-card">
          <div class="metric-icon">📈</div>
          <div class="metric-content">
            <div class="metric-value">{{ metrics()?.acceptanceRate || 0 }}%</div>
            <div class="metric-label">Acceptance Rate</div>
          </div>
        </div>
      </div>

      <!-- Filters and Search -->
      <div class="filters-bar">
        <div class="search-section">
          <input
            type="text"
            placeholder="Search by name or email..."
            [(ngModel)]="searchTerm"
            (input)="onSearchChange()"
            class="search-input"
          />
          <select [(ngModel)]="statusFilter" (change)="applyFilters()" class="status-filter">
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Accepted">Accepted</option>
            <option value="Expired">Expired</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <div class="actions-section">
          <button class="refresh-btn" (click)="loadInvitations()" [disabled]="isLoading()">
            <span class="refresh-icon">🔄</span>
            Refresh
          </button>
        </div>
      </div>

      <!-- Loading State -->
      @if (isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Loading invitations...</p>
        </div>
      }

      <!-- Error State -->
      @else if (errorMessage()) {
        <div class="error-message">
          <span class="error-icon">⚠️</span>
          {{ errorMessage() }}
          <button class="retry-btn" (click)="loadInvitations()">Try Again</button>
        </div>
      }

      <!-- Empty State -->
      @else if (invitations().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">📨</div>
          <h3>No Invitations Found</h3>
          <p>{{ searchTerm || statusFilter ? 'No invitations match your search criteria.' : 'No owner invitations have been sent yet.' }}</p>
          @if (searchTerm || statusFilter) {
            <button class="clear-filters-btn" (click)="clearFilters()">Clear Filters</button>
          }
        </div>
      }

      <!-- Invitations Table -->
      @else {
        <div class="table-container">
          <table class="invitations-table">
            <thead>
              <tr>
                <th>Owner Details</th>
                <th>Status</th>
                <th>Invited By</th>
                <th>Dates</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (invitation of invitations(); track invitation.id) {
                <tr [class.expired]="invitation.isExpired">
                  <td>
                    <div class="owner-details">
                      <div class="name">{{ invitation.name }}</div>
                      <div class="email">{{ invitation.email }}</div>
                    </div>
                  </td>
                  <td>
                    <span class="status-badge" [class]="'status-' + invitation.status.toLowerCase()">
                      {{ invitation.status }}
                    </span>
                    @if (invitation.isExpired) {
                      <div class="expired-label">Expired</div>
                    }
                  </td>
                  <td class="invited-by">{{ invitation.invitedByName }}</td>
                  <td>
                    <div class="dates">
                      <div class="sent-date">Sent: {{ formatDate(invitation.createdAt) }}</div>
                      <div class="expires-date">Expires: {{ formatDate(invitation.expiresAt) }}</div>
                    </div>
                  </td>
                  <td class="actions">
                    @if (invitation.status === 'Pending' && !invitation.isExpired) {
                      <button
                        class="action-btn resend-btn"
                        (click)="resendInvitation(invitation.id, invitation.name)"
                        [disabled]="processingInvitations().has(invitation.id)"
                        title="Resend invitation"
                      >
                        @if (processingInvitations().has(invitation.id)) {
                          <span class="spinner-sm"></span>
                        } @else {
                          📧
                        }
                      </button>

                      <button
                        class="action-btn cancel-btn"
                        (click)="cancelInvitation(invitation.id, invitation.name)"
                        [disabled]="processingInvitations().has(invitation.id)"
                        title="Cancel invitation"
                      >
                        @if (processingInvitations().has(invitation.id)) {
                          <span class="spinner-sm"></span>
                        } @else {
                          ❌
                        }
                      </button>
                    } @else {
                      <span class="no-actions">—</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <div class="pagination">
            <button
              class="page-btn"
              [disabled]="currentPage() === 1"
              (click)="goToPage(currentPage() - 1)"
            >
              Previous
            </button>

            @for (page of getPageNumbers(); track page) {
              <button
                class="page-btn"
                [class.active]="page === currentPage()"
                (click)="goToPage(page)"
              >
                {{ page }}
              </button>
            }

            <button
              class="page-btn"
              [disabled]="currentPage() === totalPages()"
              (click)="goToPage(currentPage() + 1)"
            >
              Next
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .invitations-container {
      margin-top: 2rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }

    .metric-card {
      background: white;
      border-radius: 8px;
      padding: 1.5rem;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      border: 1px solid #e5e7eb;
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .metric-icon {
      font-size: 2rem;
      opacity: 0.8;
    }

    .metric-value {
      font-size: 2rem;
      font-weight: bold;
      color: #1f2937;
      line-height: 1;
    }

    .metric-label {
      color: #6b7280;
      font-size: 0.875rem;
      font-weight: 500;
    }

    .filters-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .search-section {
      display: flex;
      gap: 1rem;
      flex: 1;
      max-width: 500px;
    }

    .search-input, .status-filter {
      padding: 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
    }

    .search-input {
      flex: 1;
      min-width: 200px;
    }

    .status-filter {
      width: 150px;
    }

    .actions-section {
      display: flex;
      gap: 1rem;
    }

    .refresh-btn {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .refresh-btn:hover:not(:disabled) {
      background: #e5e7eb;
    }

    .refresh-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .loading-state, .empty-state {
      text-align: center;
      padding: 3rem 1rem;
      color: #6b7280;
    }

    .loading-state .spinner {
      width: 3rem;
      height: 3rem;
      border: 3px solid #f3f4f6;
      border-top: 3px solid #3b82f6;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 1rem;
    }

    .empty-state .empty-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
      opacity: 0.5;
    }

    .empty-state h3 {
      margin: 0 0 0.5rem 0;
      color: #374151;
    }

    .clear-filters-btn {
      margin-top: 1rem;
      padding: 0.5rem 1rem;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }

    .error-message {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 2rem;
      background: #fee2e2;
      color: #dc2626;
      border-radius: 8px;
      margin: 2rem 0;
    }

    .retry-btn {
      padding: 0.5rem 1rem;
      background: #dc2626;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }

    .table-container {
      background: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      border: 1px solid #e5e7eb;
    }

    .invitations-table {
      width: 100%;
      border-collapse: collapse;
    }

    .invitations-table th,
    .invitations-table td {
      padding: 1rem;
      text-align: left;
      border-bottom: 1px solid #e5e7eb;
    }

    .invitations-table th {
      background: #f9fafb;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
    }

    .owner-details .name {
      font-weight: 600;
      color: #1f2937;
      margin-bottom: 0.25rem;
    }

    .owner-details .email {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .status-badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .status-pending { background: #fef3c7; color: #92400e; }
    .status-accepted { background: #dcfce7; color: #166534; }
    .status-expired { background: #fee2e2; color: #dc2626; }
    .status-cancelled { background: #f3f4f6; color: #6b7280; }

    .expired-label {
      font-size: 0.75rem;
      color: #dc2626;
      margin-top: 0.25rem;
      font-weight: 500;
    }

    .invited-by {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .dates {
      font-size: 0.875rem;
    }

    .sent-date {
      color: #6b7280;
      margin-bottom: 0.25rem;
    }

    .expires-date {
      color: #dc2626;
      font-weight: 500;
    }

    .actions {
      display: flex;
      gap: 0.5rem;
      align-items: center;
    }

    .action-btn {
      width: 2rem;
      height: 2rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .resend-btn {
      background: #dbeafe;
      color: #1e40af;
    }

    .resend-btn:hover:not(:disabled) {
      background: #bfdbfe;
    }

    .cancel-btn {
      background: #fee2e2;
      color: #dc2626;
    }

    .cancel-btn:hover:not(:disabled) {
      background: #fecaca;
    }

    .action-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .no-actions {
      color: #9ca3af;
      font-size: 1rem;
    }

    .spinner-sm {
      width: 1rem;
      height: 1rem;
      border: 2px solid transparent;
      border-top: 2px solid currentColor;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    .pagination {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
      padding: 2rem 1rem;
    }

    .page-btn {
      padding: 0.5rem 1rem;
      border: 1px solid #d1d5db;
      background: white;
      cursor: pointer;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .page-btn:hover:not(:disabled) {
      background: #f3f4f6;
    }

    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-btn.active {
      background: #3b82f6;
      color: white;
      border-color: #3b82f6;
    }

    .expired {
      opacity: 0.7;
      background: #fefefe;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .metrics-grid {
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      }

      .filters-bar {
        flex-direction: column;
        align-items: stretch;
      }

      .search-section {
        max-width: none;
      }

      .invitations-table {
        font-size: 0.875rem;
      }

      .invitations-table th,
      .invitations-table td {
        padding: 0.75rem 0.5rem;
      }
    }
  `]
})
export class OwnerInvitationsListComponent implements OnInit {
  invitations = signal<OwnerInvitationListDto[]>([]);
  metrics = signal<OwnerInvitationMetricsDto | null>(null);
  isLoading = signal(false);
  errorMessage = signal('');
  processingInvitations = signal(new Set<string>());

  // Pagination
  currentPage = signal(1);
  pageSize = signal(10);
  totalPages = signal(0);
  totalCount = signal(0);

  // Filters
  searchTerm = '';
  statusFilter = '';
  private searchTimeout?: number;

  constructor(private ownerInvitationService: OwnerInvitationService) {}

  ngOnInit() {
    this.loadInvitations();
    this.loadMetrics();
  }

  loadInvitations() {
    this.isLoading.set(true);
    this.errorMessage.set('');

    const queryParams: OwnerInvitationQueryParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      search: this.searchTerm || undefined,
      status: this.statusFilter || undefined,
      sortBy: 'CreatedAt',
      sortDirection: 'desc'
    };

    this.ownerInvitationService.getInvitations(queryParams).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success && response.data) {
          this.invitations.set(response.data.items || []);
          this.totalCount.set(response.data.totalCount);
          this.totalPages.set(response.data.totalPages);
        } else {
          this.errorMessage.set('Failed to load invitations');
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.error?.message || 'Failed to load invitations');
      }
    });
  }

  loadMetrics() {
    this.ownerInvitationService.getMetrics().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.metrics.set(response.data);
        }
      },
      error: (error) => {
        console.error('Failed to load metrics:', error);
      }
    });
  }

  onSearchChange() {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadInvitations();
    }, 300);
  }

  applyFilters() {
    this.currentPage.set(1);
    this.loadInvitations();
  }

  clearFilters() {
    this.searchTerm = '';
    this.statusFilter = '';
    this.currentPage.set(1);
    this.loadInvitations();
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadInvitations();
    }
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  resendInvitation(invitationId: string, ownerName: string) {
    if (confirm(`Resend invitation to ${ownerName}?`)) {
      this.processingInvitations.update(set => new Set(set).add(invitationId));

      this.ownerInvitationService.resendInvitation(invitationId).subscribe({
        next: (response) => {
          this.processingInvitations.update(set => {
            const newSet = new Set(set);
            newSet.delete(invitationId);
            return newSet;
          });

          if (response.success) {
            this.loadInvitations();
          }
        },
        error: (error) => {
          this.processingInvitations.update(set => {
            const newSet = new Set(set);
            newSet.delete(invitationId);
            return newSet;
          });
          alert(`Failed to resend invitation: ${error.error?.message || 'Unknown error'}`);
        }
      });
    }
  }

  cancelInvitation(invitationId: string, ownerName: string) {
    if (confirm(`Cancel invitation for ${ownerName}? This action cannot be undone.`)) {
      this.processingInvitations.update(set => new Set(set).add(invitationId));

      this.ownerInvitationService.cancelInvitation(invitationId).subscribe({
        next: (response) => {
          this.processingInvitations.update(set => {
            const newSet = new Set(set);
            newSet.delete(invitationId);
            return newSet;
          });

          if (response.success) {
            this.loadInvitations();
            this.loadMetrics();
          }
        },
        error: (error) => {
          this.processingInvitations.update(set => {
            const newSet = new Set(set);
            newSet.delete(invitationId);
            return newSet;
          });
          alert(`Failed to cancel invitation: ${error.error?.message || 'Unknown error'}`);
        }
      });
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }
}