import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OwnerHeaderComponent } from './owner-header.component';
import { OwnerFooterComponent } from './owner-footer.component';

@Component({
  selector: 'app-owner-layout',
  standalone: true,
  imports: [CommonModule, OwnerHeaderComponent, OwnerFooterComponent],
  template: `
    <div class="owner-layout">
      <app-owner-header></app-owner-header>

      <main class="main-content">
        <ng-content></ng-content>
      </main>

      <app-owner-footer></app-owner-footer>
    </div>
  `,
  styles: [`
    .owner-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      background: #f0fdf4;
    }

    .main-content {
      flex: 1;
      background: #f0fdf4;
    }
  `]
})
export class OwnerLayoutComponent {
}