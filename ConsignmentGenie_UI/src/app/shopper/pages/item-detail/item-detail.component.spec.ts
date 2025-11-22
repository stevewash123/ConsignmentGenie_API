import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';

import { ItemDetailComponent } from './item-detail.component';
import { ShopperCatalogService } from '../../services/shopper-catalog.service';
import { ShopperCartService } from '../../services/shopper-cart.service';
import { ShopperStoreService } from '../../services/shopper-store.service';

describe('ItemDetailComponent', () => {
  let component: ItemDetailComponent;
  let fixture: ComponentFixture<ItemDetailComponent>;
  let mockCatalogService: jasmine.SpyObj<ShopperCatalogService>;
  let mockCartService: jasmine.SpyObj<ShopperCartService>;
  let mockStoreService: jasmine.SpyObj<ShopperStoreService>;
  let mockActivatedRoute: jasmine.SpyObj<ActivatedRoute>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockStoreSlug = 'test-store';
  const mockItemId = 'item-1';
  const mockStoreInfo = {
    storeSlug: mockStoreSlug,
    storeName: 'Test Store',
    description: 'Test Description',
    isActive: true,
    theme: { primaryColor: '#000000', secondaryColor: '#ffffff' },
    contact: { email: 'test@test.com', phone: '123-456-7890' },
    hours: { monday: '9-5', tuesday: '9-5', wednesday: '9-5', thursday: '9-5', friday: '9-5' },
    socialMedia: { facebook: '', instagram: '', twitter: '', website: '' }
  };

  const mockItemDetail = {
    success: true,
    data: {
      itemId: mockItemId,
      title: 'Test Item',
      description: 'This is a detailed description of the test item.',
      price: 50.00,
      category: 'Electronics',
      brand: 'TestBrand',
      size: 'M',
      color: 'Blue',
      condition: 'Good',
      primaryImageUrl: 'primary-image.jpg',
      images: [
        {
          imageId: 'img-1',
          url: 'image1.jpg',
          isPrimary: true,
          sortOrder: 1
        },
        {
          imageId: 'img-2',
          url: 'image2.jpg',
          isPrimary: false,
          sortOrder: 2
        },
        {
          imageId: 'img-3',
          url: 'image3.jpg',
          isPrimary: false,
          sortOrder: 3
        }
      ]
    }
  };

  beforeEach(async () => {
    const catalogServiceSpy = jasmine.createSpyObj('ShopperCatalogService', ['getItemDetail']);
    const cartServiceSpy = jasmine.createSpyObj('ShopperCartService', ['setCurrentStore', 'addItem', 'isItemInCart', 'getItemQuantity']);
    const storeServiceSpy = jasmine.createSpyObj('ShopperStoreService', ['getStoreInfo']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    const paramMap = new Map([
      ['storeSlug', mockStoreSlug],
      ['itemId', mockItemId]
    ]);

    mockActivatedRoute = {
      paramMap: of(paramMap)
    } as any;

    await TestBed.configureTestingModule({
      imports: [ItemDetailComponent, NoopAnimationsModule],
      providers: [
        { provide: ShopperCatalogService, useValue: catalogServiceSpy },
        { provide: ShopperCartService, useValue: cartServiceSpy },
        { provide: ShopperStoreService, useValue: storeServiceSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    mockCatalogService = TestBed.inject(ShopperCatalogService) as jasmine.SpyObj<ShopperCatalogService>;
    mockCartService = TestBed.inject(ShopperCartService) as jasmine.SpyObj<ShopperCartService>;
    mockStoreService = TestBed.inject(ShopperStoreService) as jasmine.SpyObj<ShopperStoreService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Default mock implementations
    mockStoreService.getStoreInfo.and.returnValue(of({ success: true, data: mockStoreInfo }));
    mockCatalogService.getItemDetail.and.returnValue(of(mockItemDetail));
    mockCartService.isItemInCart.and.returnValue(false);
    mockCartService.getItemQuantity.and.returnValue(0);
    mockCartService.addItem.and.returnValue(true);

    fixture = TestBed.createComponent(ItemDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load store info and item detail on init', fakeAsync(() => {
      fixture.detectChanges();
      tick();

      expect(component.storeSlug).toBe(mockStoreSlug);
      expect(component.itemId).toBe(mockItemId);
      expect(component.storeInfo).toEqual(mockStoreInfo);
      expect(component.item).toEqual(mockItemDetail.data);
      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);
      expect(mockCartService.setCurrentStore).toHaveBeenCalledWith(mockStoreSlug);
    }));

    it('should handle store loading error', fakeAsync(() => {
      mockStoreService.getStoreInfo.and.returnValue(throwError(() => ({ status: 404 })));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
      expect(component.error).toContain('Store not found');
    }));

    it('should handle item loading error', fakeAsync(() => {
      mockCatalogService.getItemDetail.and.returnValue(throwError(() => ({ status: 404 })));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
      expect(component.error).toContain('Item not found');
    }));

    it('should handle API failure error', fakeAsync(() => {
      mockCatalogService.getItemDetail.and.returnValue(throwError(() => ({ status: 500 })));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
      expect(component.error).toContain('Failed to load item details');
    }));
  });

  describe('Image Gallery', () => {
    beforeEach(fakeAsync(() => {
      fixture.detectChanges();
      tick();
    }));

    it('should select primary image by default', () => {
      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);
    });

    it('should change selected image', () => {
      const secondImage = mockItemDetail.data.images[1];

      component.selectImage(secondImage);

      expect(component.selectedImage).toEqual(secondImage);
    });

    it('should go to next image', () => {
      component.selectedImage = mockItemDetail.data.images[0];

      component.nextImage();

      expect(component.selectedImage).toEqual(mockItemDetail.data.images[1]);
    });

    it('should wrap to first image when at end', () => {
      component.selectedImage = mockItemDetail.data.images[2]; // Last image

      component.nextImage();

      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);
    });

    it('should go to previous image', () => {
      component.selectedImage = mockItemDetail.data.images[1];

      component.previousImage();

      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);
    });

    it('should wrap to last image when at beginning', () => {
      component.selectedImage = mockItemDetail.data.images[0];

      component.previousImage();

      expect(component.selectedImage).toEqual(mockItemDetail.data.images[2]);
    });

    it('should handle single image case', () => {
      component.item = {
        ...mockItemDetail.data,
        images: [mockItemDetail.data.images[0]]
      };

      component.nextImage();
      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);

      component.previousImage();
      expect(component.selectedImage).toEqual(mockItemDetail.data.images[0]);
    });
  });

  describe('Quantity Management', () => {
    beforeEach(fakeAsync(() => {
      fixture.detectChanges();
      tick();
    }));

    it('should increase quantity', () => {
      component.quantity = 1;

      component.increaseQuantity();

      expect(component.quantity).toBe(2);
    });

    it('should decrease quantity', () => {
      component.quantity = 2;

      component.decreaseQuantity();

      expect(component.quantity).toBe(1);
    });

    it('should not decrease quantity below 1', () => {
      component.quantity = 1;

      component.decreaseQuantity();

      expect(component.quantity).toBe(1);
    });

    it('should not increase quantity above max (100)', () => {
      component.quantity = 100;

      component.increaseQuantity();

      expect(component.quantity).toBe(100);
    });
  });

  describe('Cart Integration', () => {
    beforeEach(fakeAsync(() => {
      fixture.detectChanges();
      tick();
    }));

    it('should add item to cart', () => {
      component.quantity = 2;

      component.addToCart();

      expect(mockCartService.addItem).toHaveBeenCalledWith(component.item, 2);
    });

    it('should handle failed add to cart', () => {
      mockCartService.addItem.and.returnValue(false);

      component.addToCart();

      expect(component.addToCartError).toBe('Failed to add item to cart. Please try again.');
    });

    it('should clear add to cart error after successful add', () => {
      component.addToCartError = 'Previous error';
      mockCartService.addItem.and.returnValue(true);

      component.addToCart();

      expect(component.addToCartError).toBe('');
    });

    it('should check if item is in cart', () => {
      mockCartService.isItemInCart.and.returnValue(true);

      const result = component.isItemInCart();

      expect(result).toBe(true);
      expect(mockCartService.isItemInCart).toHaveBeenCalledWith(mockItemId);
    });

    it('should get item quantity in cart', () => {
      mockCartService.getItemQuantity.and.returnValue(3);

      const quantity = component.getItemQuantityInCart();

      expect(quantity).toBe(3);
      expect(mockCartService.getItemQuantity).toHaveBeenCalledWith(mockItemId);
    });
  });

  describe('Navigation', () => {
    beforeEach(fakeAsync(() => {
      fixture.detectChanges();
      tick();
    }));

    it('should navigate back to catalog', () => {
      component.goBack();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/shop', mockStoreSlug, 'catalog']);
    });

    it('should navigate to cart', () => {
      component.goToCart();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/shop', mockStoreSlug, 'cart']);
    });
  });

  describe('Template Rendering', () => {
    it('should display loading state', () => {
      component.loading = true;
      fixture.detectChanges();

      const loadingElement = fixture.debugElement.query(By.css('.loading'));
      expect(loadingElement).toBeTruthy();
    });

    it('should display error state', () => {
      component.loading = false;
      component.error = 'Test error message';
      fixture.detectChanges();

      const errorElement = fixture.debugElement.query(By.css('.error'));
      expect(errorElement).toBeTruthy();
      expect(errorElement.nativeElement.textContent).toContain('Test error message');
    });

    it('should display item details when loaded', fakeAsync(() => {
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const titleElement = fixture.debugElement.query(By.css('h1'));
      expect(titleElement.nativeElement.textContent).toContain('Test Item');

      const priceElement = fixture.debugElement.query(By.css('.price'));
      expect(priceElement.nativeElement.textContent).toContain('$50.00');
    }));

    it('should display breadcrumb navigation', fakeAsync(() => {
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const breadcrumbs = fixture.debugElement.queryAll(By.css('.breadcrumb a'));
      expect(breadcrumbs.length).toBeGreaterThan(0);
    }));

    it('should display image gallery', fakeAsync(() => {
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const mainImage = fixture.debugElement.query(By.css('.main-image img'));
      expect(mainImage).toBeTruthy();

      const thumbnails = fixture.debugElement.queryAll(By.css('.thumbnail'));
      expect(thumbnails.length).toBe(3);
    }));

    it('should display quantity controls', fakeAsync(() => {
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const decreaseButton = fixture.debugElement.query(By.css('button[aria-label="Decrease quantity"]'));
      const increaseButton = fixture.debugElement.query(By.css('button[aria-label="Increase quantity"]'));
      const quantityInput = fixture.debugElement.query(By.css('input[type="number"]'));

      expect(decreaseButton).toBeTruthy();
      expect(increaseButton).toBeTruthy();
      expect(quantityInput).toBeTruthy();
    }));

    it('should display add to cart button', fakeAsync(() => {
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const addToCartButton = fixture.debugElement.query(By.css('.add-to-cart'));
      expect(addToCartButton).toBeTruthy();
    }));
  });

  describe('Error Handling', () => {
    it('should handle missing route parameters', fakeAsync(() => {
      mockActivatedRoute.paramMap = of(new Map());

      fixture.detectChanges();
      tick();

      expect(component.error).toBeTruthy();
    }));

    it('should handle API errors gracefully', fakeAsync(() => {
      mockCatalogService.getItemDetail.and.returnValue(throwError(() => new Error('Network error')));

      fixture.detectChanges();
      tick();

      expect(component.loading).toBe(false);
      expect(component.error).toContain('Failed to load item details');
    }));
  });
});