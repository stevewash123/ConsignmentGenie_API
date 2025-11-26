import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <footer class="app-footer">
      <div class="footer-container">
        <div class="footer-content">
          <div class="footer-section">
            <h3>ConsignmentGenie</h3>
            <p>Modern consignment management for the digital age.</p>
          </div>

          <div class="footer-section">
            <h4>Resources</h4>
            <ul>
              <li><a href="#" target="_blank">Documentation</a></li>
              <li><a href="#" target="_blank">API Reference</a></li>
              <li><a href="#" target="_blank">Help Center</a></li>
              <li><a href="#" target="_blank">Status Page</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Support</h4>
            <ul>
              <li><a href="mailto:support@consignmentgenie.com">Contact Support</a></li>
              <li><a href="#" target="_blank">Feature Requests</a></li>
              <li><a href="#" target="_blank">Bug Reports</a></li>
              <li><a href="#" target="_blank">Community Forum</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Legal</h4>
            <ul>
              <li><a href="#" target="_blank">Privacy Policy</a></li>
              <li><a href="#" target="_blank">Terms of Service</a></li>
              <li><a href="#" target="_blank">Security</a></li>
              <li><a href="#" target="_blank">Compliance</a></li>
            </ul>
          </div>
        </div>

        <div class="footer-bottom">
          <div class="copyright">
            <p>&copy; {{ currentYear }} ConsignmentGenie. All rights reserved.</p>
          </div>
          <div class="version">
            <p>Version 1.0.0 | <a href="#" target="_blank">Release Notes</a></p>
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .app-footer {
      background: #1f2937;
      color: #d1d5db;
      border-top: 1px solid #374151;
      margin-top: auto;
    }

    .footer-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 2rem;
    }

    .footer-content {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr 1fr;
      gap: 2rem;
      margin-bottom: 2rem;
    }

    .footer-section h3 {
      color: #60a5fa;
      margin-bottom: 1rem;
      font-size: 1.25rem;
      font-weight: bold;
    }

    .footer-section h4 {
      color: white;
      margin-bottom: 1rem;
      font-size: 1rem;
      font-weight: 600;
    }

    .footer-section p {
      margin-bottom: 0;
      line-height: 1.5;
      color: #9ca3af;
    }

    .footer-section ul {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .footer-section li {
      margin-bottom: 0.5rem;
    }

    .footer-section a {
      color: #d1d5db;
      text-decoration: none;
      font-size: 0.875rem;
      transition: color 0.2s;
    }

    .footer-section a:hover {
      color: #60a5fa;
    }

    .footer-bottom {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 2rem;
      border-top: 1px solid #374151;
    }

    .copyright p,
    .version p {
      margin: 0;
      font-size: 0.875rem;
      color: #9ca3af;
    }

    .version a {
      color: #60a5fa;
      text-decoration: none;
    }

    .version a:hover {
      text-decoration: underline;
    }

    @media (max-width: 768px) {
      .footer-content {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .footer-bottom {
        flex-direction: column;
        gap: 1rem;
        text-align: center;
      }

      .footer-container {
        padding: 1.5rem 1rem;
      }
    }

    @media (max-width: 1024px) {
      .footer-content {
        grid-template-columns: 1fr 1fr;
      }
    }
  `]
})
export class AppFooterComponent {
  currentYear = new Date().getFullYear();
}