import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminHeaderComponent } from './admin-header.component';
import { AdminFooterComponent } from './admin-footer.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, AdminHeaderComponent, AdminFooterComponent],
  template: `
    <div class="admin-layout">
      <app-admin-header></app-admin-header>

      <main class="main-content">
        <ng-content></ng-content>
      </main>

      <app-admin-footer></app-admin-footer>
    </div>
  `,
  styles: [`
    .admin-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background: #f8fafc;
    }

    .main-content {
      flex: 1;
      background: #f8fafc;
    }
  `]
})
export class AdminLayoutComponent {
}