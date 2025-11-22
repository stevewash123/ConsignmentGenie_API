import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppHeaderComponent } from './app-header.component';
import { AppFooterComponent } from './app-footer.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, AppHeaderComponent, AppFooterComponent],
  template: `
    <div class="app-layout">
      <app-header></app-header>

      <main class="main-content">
        <ng-content></ng-content>
      </main>

      <app-footer></app-footer>
    </div>
  `,
  styles: [`
    .app-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
      background: #f9fafb;
    }
  `]
})
export class AppLayoutComponent {
}