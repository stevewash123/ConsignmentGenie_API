import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="registration-page">
      <div class="container">
        <div class="registration-card">
          <h1>ConsignmentGenie</h1>

          <div class="selection-section">
            <h2>Join Consignment Genie</h2>
            <p>I want to...</p>

            <div class="role-selection">
              <a routerLink="/register/owner" class="role-card">
                <div class="role-icon">🏪</div>
                <h3>Open a Consignment Shop</h3>
                <p>→ Shop setup wizard</p>
                <button class="select-btn">Select</button>
              </a>

              <a routerLink="/register/provider" class="role-card">
                <div class="role-icon">📦</div>
                <h3>Consign Items at a Shop</h3>
                <p>→ Basic signup</p>
                <button class="select-btn">Select</button>
              </a>
            </div>

            <div class="login-link">
              <p>Already have an account? <a routerLink="/login">Log In</a></p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .registration-page {
      min-height: 100vh;
      background: linear-gradient(135deg, #047857 0%, #059669 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
    }

    .container {
      width: 100%;
      max-width: 800px;
    }

    .registration-card {
      background: white;
      border-radius: 16px;
      padding: 3rem;
      box-shadow: 0 20px 40px rgba(0,0,0,0.1);
      text-align: center;
    }

    h1 {
      color: #047857;
      font-size: 2.5rem;
      margin-bottom: 2rem;
      font-weight: 700;
    }

    h2 {
      color: #1f2937;
      font-size: 1.75rem;
      margin-bottom: 0.5rem;
    }

    .selection-section p {
      color: #6b7280;
      font-size: 1.1rem;
      margin-bottom: 2rem;
    }

    .role-selection {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
      margin-bottom: 2rem;
    }

    .role-card {
      background: #f9fafb;
      border: 2px solid #e5e7eb;
      border-radius: 12px;
      padding: 2rem;
      text-decoration: none;
      color: inherit;
      transition: all 0.2s;
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .role-card:hover {
      border-color: #047857;
      background: #f0fdf4;
      transform: translateY(-4px);
      box-shadow: 0 8px 25px rgba(4, 120, 87, 0.15);
    }

    .role-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    .role-card h3 {
      color: #047857;
      font-size: 1.5rem;
      margin-bottom: 0.5rem;
    }

    .role-card p {
      color: #6b7280;
      margin-bottom: 1.5rem;
      line-height: 1.5;
    }

    .select-btn {
      background: #047857;
      color: white;
      border: none;
      padding: 0.75rem 2rem;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }

    .select-btn:hover {
      background: #059669;
    }

    .login-link {
      padding-top: 1rem;
      border-top: 1px solid #e5e7eb;
    }

    .login-link a {
      color: #047857;
      text-decoration: none;
      font-weight: 600;
    }

    .login-link a:hover {
      text-decoration: underline;
    }

    @media (max-width: 768px) {
      .registration-card {
        padding: 2rem;
      }

      .role-selection {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      h1 {
        font-size: 2rem;
      }
    }
  `]
})
export class RegisterComponent {
}