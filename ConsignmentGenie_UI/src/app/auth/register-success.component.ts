import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-register-success',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="success-page">
      <div class="container">
        <div class="success-card">
          <div class="success-icon">✅</div>

          <h1>Account Created!</h1>

          <div class="success-content">
            <p class="greeting">Thanks for registering, {{ getFirstName() }}!</p>

            <div class="status-message" *ngIf="registrationType() === 'owner'">
              <p>Your account is pending approval from our team.</p>
            </div>

            <div class="status-message" *ngIf="registrationType() === 'provider'">
              <p>Your account is pending approval from {{ shopName() }}.</p>
            </div>

            <p class="email-info">
              We've sent a confirmation to {{ userEmail() }}.
              You'll receive another email when your account is approved.
            </p>

            <div class="next-steps" *ngIf="registrationType() === 'owner'">
              <h3>What happens next:</h3>
              <ul>
                <li>Our team will review your shop registration</li>
                <li>You'll receive an approval email within 1-2 business days</li>
                <li>Once approved, you'll get your unique store code for providers</li>
                <li>You can then start adding inventory and managing your shop</li>
              </ul>
            </div>

            <div class="next-steps" *ngIf="registrationType() === 'provider'">
              <h3>What happens next:</h3>
              <ul>
                <li>The shop owner will review your request</li>
                <li>You'll receive an email when approved</li>
                <li>Once approved, you can access your Provider Portal</li>
                <li>You'll be able to track your items and earnings</li>
              </ul>
            </div>
          </div>

          <div class="actions">
            <a routerLink="/" class="home-btn">Back to Home</a>
            <a routerLink="/login" class="login-btn">Try to Log In</a>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .success-page {
      min-height: 100vh;
      background: linear-gradient(135deg, #047857 0%, #059669 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
    }

    .container {
      width: 100%;
      max-width: 600px;
    }

    .success-card {
      background: white;
      border-radius: 16px;
      padding: 3rem;
      box-shadow: 0 20px 40px rgba(0,0,0,0.1);
      text-align: center;
    }

    .success-icon {
      font-size: 4rem;
      margin-bottom: 1.5rem;
    }

    h1 {
      color: #047857;
      font-size: 2.25rem;
      margin-bottom: 2rem;
      font-weight: 700;
    }

    .success-content {
      text-align: left;
      margin-bottom: 2rem;
    }

    .greeting {
      font-size: 1.25rem;
      color: #1f2937;
      font-weight: 600;
      margin-bottom: 1rem;
    }

    .status-message {
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 1rem;
    }

    .status-message p {
      color: #047857;
      font-weight: 600;
      margin: 0;
    }

    .email-info {
      color: #6b7280;
      margin-bottom: 1.5rem;
    }

    .next-steps {
      background: #f9fafb;
      border-radius: 8px;
      padding: 1.5rem;
      margin-bottom: 1rem;
    }

    .next-steps h3 {
      color: #1f2937;
      margin-bottom: 1rem;
      font-size: 1.1rem;
    }

    .next-steps ul {
      margin: 0;
      padding-left: 1.5rem;
      color: #4b5563;
    }

    .next-steps li {
      margin-bottom: 0.5rem;
      line-height: 1.5;
    }

    .actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .home-btn, .login-btn {
      padding: 0.75rem 2rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      transition: all 0.2s;
    }

    .home-btn {
      background: #f3f4f6;
      color: #374151;
      border: 1px solid #d1d5db;
    }

    .home-btn:hover {
      background: #e5e7eb;
    }

    .login-btn {
      background: #047857;
      color: white;
    }

    .login-btn:hover {
      background: #059669;
    }

    @media (max-width: 768px) {
      .success-card {
        padding: 2rem;
      }

      h1 {
        font-size: 1.75rem;
      }

      .actions {
        flex-direction: column;
      }

      .home-btn, .login-btn {
        width: 100%;
        text-align: center;
      }
    }
  `]
})
export class RegisterSuccessComponent implements OnInit {
  registrationType = signal<string>('');
  shopName = signal<string>('');
  userEmail = signal<string>('');
  fullName = signal<string>('');

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.registrationType.set(params['type'] || '');
      this.shopName.set(params['shopName'] || '');
      this.userEmail.set(params['email'] || '');
      this.fullName.set(params['fullName'] || '');
    });
  }

  getFirstName(): string {
    const fullName = this.fullName();
    if (fullName) {
      return fullName.split(' ')[0];
    }

    const email = this.userEmail();
    if (email) {
      return email.split('@')[0];
    }

    return 'there';
  }
}