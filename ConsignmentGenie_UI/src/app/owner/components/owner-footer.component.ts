import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuggestionBoxModalComponent, SuggestionFormData } from './suggestion-box-modal.component';

interface UserData {
  userId: string;
  email: string;
  role: number;
  organizationId: string;
  organizationName: string;
}

@Component({
  selector: 'app-owner-footer',
  standalone: true,
  imports: [CommonModule, SuggestionBoxModalComponent],
  template: `
    <footer class="owner-footer">
      <div class="footer-container">
        <div class="footer-content">
          <div class="footer-section">
            <h3>Consignment Genie</h3>
            <p>Complete consignment management for your business.</p>
          </div>

          <div class="footer-section">
            <h4>Business Tools</h4>
            <ul>
              <li><a href="#" target="_blank">Inventory Management</a></li>
              <li><a href="#" target="_blank">Sales Analytics</a></li>
              <li><a href="#" target="_blank">Provider Relations</a></li>
              <li><a href="#" target="_blank">Financial Reports</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Integrations</h4>
            <ul>
              <li><a href="#" target="_blank">QuickBooks Setup</a></li>
              <li><a href="#" target="_blank">Square POS</a></li>
              <li><a href="#" target="_blank">Payment Processing</a></li>
              <li><a href="#" target="_blank">Accounting Export</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Support</h4>
            <ul>
              <li><a href="mailto:support@consignmentgenie.com">Contact Support</a></li>
              <li><a href="#" target="_blank">User Guide</a></li>
              <li><a href="#" target="_blank">Video Tutorials</a></li>
              <li><a href="#" target="_blank">Best Practices</a></li>
              <li><button class="suggestion-link" (click)="openSuggestionBox()">💡 Suggestion Box</button></li>
            </ul>
          </div>
        </div>

        <div class="footer-bottom">
          <div class="copyright">
            <p>&copy; {{ currentYear }} {{ currentUser()?.organizationName || 'ConsignmentGenie' }} Shop Management</p>
          </div>
          <div class="version">
            <p>Business v1.0.0 | <a href="#" target="_blank">Release Notes</a></p>
          </div>
        </div>
      </div>
    </footer>

    <app-suggestion-box-modal
      [isVisible]="showSuggestionModal()"
      (close)="closeSuggestionBox()"
      (submit)="onSuggestionSubmit($event)">
    </app-suggestion-box-modal>
  `,
  styles: [`
    .owner-footer {
      background: #047857;
      color: #d1fae5;
      border-top: 1px solid #059669;
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
      color: #fbbf24;
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
      color: #a7f3d0;
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
      color: #d1fae5;
      text-decoration: none;
      font-size: 0.875rem;
      transition: color 0.2s;
    }

    .footer-section a:hover {
      color: #fbbf24;
    }

    .footer-bottom {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 2rem;
      border-top: 1px solid #059669;
    }

    .copyright p,
    .version p {
      margin: 0;
      font-size: 0.875rem;
      color: #a7f3d0;
    }

    .version a {
      color: #fbbf24;
      text-decoration: none;
    }

    .version a:hover {
      text-decoration: underline;
    }

    .suggestion-link {
      background: none;
      border: none;
      color: #d1fae5;
      text-decoration: none;
      font-size: 0.875rem;
      transition: color 0.2s;
      cursor: pointer;
      padding: 0;
      margin: 0;
      text-align: left;
    }

    .suggestion-link:hover {
      color: #fbbf24;
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
export class OwnerFooterComponent implements OnInit {
  currentYear = new Date().getFullYear();
  currentUser = signal<UserData | null>(null);
  showSuggestionModal = signal<boolean>(false);

  ngOnInit() {
    this.loadUserData();
  }

  private loadUserData() {
    const userData = localStorage.getItem('user_data');
    if (userData) {
      try {
        this.currentUser.set(JSON.parse(userData));
      } catch (error) {
        console.error('Failed to parse user data from localStorage', error);
        this.currentUser.set(null);
      }
    }
  }

  openSuggestionBox() {
    this.showSuggestionModal.set(true);
  }

  closeSuggestionBox() {
    this.showSuggestionModal.set(false);
  }

  onSuggestionSubmit(suggestionData: SuggestionFormData) {
    console.log('Suggestion submitted:', suggestionData);

    // For now, just log to console and show a simple alert
    // TODO: Integrate with backend API when notification service is ready
    alert(`Thank you for your ${suggestionData.type.toLowerCase()} suggestion! We'll review it and get back to you.`);

    this.closeSuggestionBox();
  }
}