import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, switchMap } from 'rxjs/operators';
import { StoreHeaderComponent } from './store-header.component';
import { ShopperStoreService, StoreInfoDto } from '../services/shopper-store.service';

@Component({
  selector: 'app-store-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, StoreHeaderComponent],
  template: `
    <div class="store-layout">
      <app-store-header
        [storeInfo]="storeInfo"
        [storeSlug]="storeSlug">
      </app-store-header>

      <main class="store-content">
        <router-outlet></router-outlet>
      </main>

      <footer class="store-footer">
        <div class="container" *ngIf="storeInfo">
          <div class="footer-content">
            <div class="store-details">
              <h4>{{ storeInfo.name }}</h4>
              <p *ngIf="storeInfo.address">{{ storeInfo.address }}</p>
              <p *ngIf="storeInfo.phone">{{ storeInfo.phone }}</p>
              <p *ngIf="storeInfo.email">{{ storeInfo.email }}</p>
            </div>

            <div class="store-hours" *ngIf="storeInfo.hours">
              <h5>Store Hours</h5>
              <!-- TODO: Display store hours when implemented -->
            </div>
          </div>

          <div class="footer-bottom">
            <p>&copy; {{ currentYear }} {{ storeInfo.name }}. All rights reserved.</p>
            <p class="powered-by">Powered by ConsignmentGenie</p>
          </div>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    .store-layout {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .store-content {
      flex: 1;
      padding: 1rem 0;
    }

    .store-footer {
      background-color: #f8f9fa;
      border-top: 1px solid #dee2e6;
      padding: 2rem 0 1rem;
      margin-top: 2rem;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .footer-content {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 2rem;
      margin-bottom: 2rem;
    }

    .store-details h4 {
      margin: 0 0 1rem 0;
      color: #343a40;
    }

    .store-details p {
      margin: 0.5rem 0;
      color: #6c757d;
    }

    .store-hours h5 {
      margin: 0 0 1rem 0;
      color: #343a40;
    }

    .footer-bottom {
      border-top: 1px solid #dee2e6;
      padding-top: 1rem;
      text-align: center;
      color: #6c757d;
      font-size: 0.875rem;
    }

    .footer-bottom p {
      margin: 0.25rem 0;
    }

    .powered-by {
      color: #adb5bd !important;
    }

    @media (max-width: 768px) {
      .footer-content {
        grid-template-columns: 1fr;
        text-align: center;
      }
    }
  `]
})
export class StoreLayoutComponent implements OnInit, OnDestroy {
  storeInfo: StoreInfoDto | null = null;
  storeSlug: string = '';
  currentYear = new Date().getFullYear();

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private storeService: ShopperStoreService
  ) {}

  ngOnInit(): void {
    // Get store slug from route parameters and load store info
    this.route.paramMap.pipe(
      takeUntil(this.destroy$),
      switchMap(params => {
        this.storeSlug = params.get('storeSlug') || '';
        if (!this.storeSlug) {
          throw new Error('No store slug provided');
        }
        return this.storeService.getStoreInfo(this.storeSlug);
      })
    ).subscribe({
      next: (storeInfo) => {
        this.storeInfo = storeInfo;
        this.updatePageTitle(storeInfo.name);
      },
      error: (error) => {
        console.error('Failed to load store information:', error);
        this.router.navigate(['/']);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.storeService.clearCurrentStore();
  }

  private updatePageTitle(storeName: string): void {
    // Update page title to include store name
    const currentTitle = document.title;
    if (!currentTitle.includes(storeName)) {
      document.title = `${storeName} - ${currentTitle}`;
    }
  }
}