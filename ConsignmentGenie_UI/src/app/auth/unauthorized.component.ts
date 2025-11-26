import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="unauthorized-container">
      <div class="unauthorized-card">
        <div class="error-icon">🚫</div>
        <h1>Access Denied</h1>
        <p>You don't have permission to access this page.</p>
        <p class="help-text">Please contact your administrator if you believe this is an error.</p>

        <div class="action-buttons">
          <button class="btn-primary" (click)="goToDashboard()">
            Go to Dashboard
          </button>
          <button class="btn-secondary" (click)="logout()">
            Logout
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .unauthorized-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #dc2626 0%, #991b1b 100%);
      padding: 2rem;
    }

    .unauthorized-card {
      background: white;
      border-radius: 16px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
      padding: 3rem;
      max-width: 480px;
      width: 100%;
      text-align: center;
    }

    .error-icon {
      font-size: 4rem;
      margin-bottom: 1.5rem;
    }

    h1 {
      font-size: 2rem;
      font-weight: bold;
      color: #dc2626;
      margin-bottom: 1rem;
    }

    p {
      color: #6b7280;
      margin-bottom: 1rem;
      font-size: 1.1rem;
    }

    .help-text {
      font-size: 0.875rem;
      margin-bottom: 2rem;
    }

    .action-buttons {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .btn-primary, .btn-secondary {
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      border: none;
      font-size: 1rem;
    }

    .btn-primary {
      background: #3b82f6;
      color: white;
    }

    .btn-primary:hover {
      background: #2563eb;
    }

    .btn-secondary {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .btn-secondary:hover {
      background: #e5e7eb;
    }

    @media (max-width: 640px) {
      .unauthorized-card {
        padding: 2rem;
      }

      .action-buttons {
        flex-direction: column;
      }
    }
  `]
})
export class UnauthorizedComponent {
  constructor(private router: Router) {}

  goToDashboard() {
    // Get user role and redirect to appropriate dashboard
    const userDataStr = localStorage.getItem('user_data');
    if (userDataStr) {
      try {
        const userData = JSON.parse(userDataStr);
        switch (userData.role) {
          case 1: // Owner
          case 2: // Manager
          case 3: // Staff
          case 4: // Cashier
          case 5: // Accountant
            this.router.navigate(['/owner/dashboard']);
            break;
          case 6: // Provider
            this.router.navigate(['/provider/dashboard']);
            break;
          case 7: // Customer
            this.router.navigate(['/customer/dashboard']);
            break;
          default:
            this.router.navigate(['/login']);
        }
      } catch (error) {
        this.router.navigate(['/login']);
      }
    } else {
      this.router.navigate(['/login']);
    }
  }

  logout() {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_data');
    this.router.navigate(['/login']);
  }
}