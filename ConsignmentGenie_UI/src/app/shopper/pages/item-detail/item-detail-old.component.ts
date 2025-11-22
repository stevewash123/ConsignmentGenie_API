import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ShopperStoreService, StoreInfoDto } from '../../services/shopper-store.service';
import {
  ShopperCatalogService,
  ShopperItemDetail,
  ShopperItemImage
} from '../../services/shopper-catalog.service';

@Component({
  selector: 'app-item-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="item-detail">
      <div class="container">
        <!-- Breadcrumb -->
        <nav class="breadcrumb">
          <a [routerLink]="['/shop', storeSlug]">Home</a>
          <span>/</span>
          <a [routerLink]="['/shop', storeSlug, 'catalog']">Catalog</a>
          <span>/</span>
          <span>Item Details</span>
        </nav>

        <!-- Item Details Content -->
        <div class="item-content">
          <div class="item-images">
            <div class="main-image">
              <img src="/assets/placeholder-item.jpg" alt="Item Image" />
            </div>
          </div>

          <div class="item-info">
            <h1>Item Detail</h1>
            <p class="price">$0.00</p>
            <p class="description">This feature is planned for Phase 2 - Shopper Portal implementation.</p>

            <div class="actions">
              <button class="btn btn-primary" disabled>Add to Cart</button>
              <button class="btn btn-outline" disabled>Add to Favorites</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .item-detail {
      padding: 2rem 0;
      min-height: 60vh;
    }

    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .breadcrumb {
      margin-bottom: 2rem;
      color: #6c757d;
    }

    .breadcrumb a {
      color: #007bff;
      text-decoration: none;
    }

    .item-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 3rem;
      align-items: start;
    }

    .main-image img {
      width: 100%;
      height: 400px;
      object-fit: cover;
      border-radius: 8px;
    }

    .item-info h1 {
      font-size: 2rem;
      margin-bottom: 1rem;
      color: #343a40;
    }

    .price {
      font-size: 1.5rem;
      font-weight: bold;
      color: #007bff;
      margin-bottom: 1rem;
    }

    .description {
      color: #6c757d;
      margin-bottom: 2rem;
    }

    .actions {
      display: flex;
      gap: 1rem;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
    }

    .btn-outline {
      background-color: transparent;
      color: #007bff;
      border: 1px solid #007bff;
    }

    @media (max-width: 768px) {
      .item-content {
        grid-template-columns: 1fr;
        gap: 2rem;
      }
    }
  `]
})
export class ItemDetailComponent implements OnInit, OnDestroy {
  storeSlug = '';
  itemId = '';

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      this.storeSlug = params.get('storeSlug') || '';
      this.itemId = params.get('itemId') || '';
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}