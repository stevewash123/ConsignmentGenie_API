import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ShopperCartService, CartItem } from './shopper-cart.service';
import { ShopperItemList } from './shopper-catalog.service';

describe('ShopperCartService', () => {
  let service: ShopperCartService;
  const mockStoreSlug = 'test-store';
  const mockItem: ShopperItemList = {
    itemId: 'item-1',
    title: 'Test Item',
    description: 'Test Description',
    price: 50.00,
    category: 'Electronics',
    brand: 'TestBrand',
    size: 'M',
    color: 'Blue',
    condition: 'Good',
    primaryImageUrl: 'test-image.jpg',
    images: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ShopperCartService);
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should set current store', () => {
    service.setCurrentStore(mockStoreSlug);
    const cart$ = service.getCurrentCart$();
    expect(cart$).toBeTruthy();
  });

  it('should throw error when no store context set', () => {
    expect(() => service.getCurrentCart$()).toThrowError('No store context set. Call setCurrentStore() first.');
  });

  it('should add item to empty cart', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    const result = service.addItem(mockItem, 1);
    expect(result).toBe(true);

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items.length).toBe(1);
      expect(cart.items[0].itemId).toBe(mockItem.itemId);
      expect(cart.items[0].quantity).toBe(1);
      expect(cart.itemCount).toBe(1);
      expect(cart.subtotal).toBe(50.00);
    });

    tick();
  }));

  it('should update existing item quantity when adding same item', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 1);
    service.addItem(mockItem, 2);

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items.length).toBe(1);
      expect(cart.items[0].quantity).toBe(3);
      expect(cart.itemCount).toBe(3);
    });

    tick();
  }));

  it('should not add item with invalid data', () => {
    service.setCurrentStore(mockStoreSlug);
    const invalidItem = { ...mockItem, itemId: '' };
    const result = service.addItem(invalidItem, 1);
    expect(result).toBe(false);
  });

  it('should not add item with zero or negative quantity', () => {
    service.setCurrentStore(mockStoreSlug);
    const result1 = service.addItem(mockItem, 0);
    const result2 = service.addItem(mockItem, -1);
    expect(result1).toBe(false);
    expect(result2).toBe(false);
  });

  it('should update item quantity', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 2);

    const result = service.updateItemQuantity(mockItem.itemId, 5);
    expect(result).toBe(true);

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items[0].quantity).toBe(5);
      expect(cart.itemCount).toBe(5);
    });

    tick();
  }));

  it('should remove item when quantity set to 0', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 2);

    const result = service.updateItemQuantity(mockItem.itemId, 0);
    expect(result).toBe(true);

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items.length).toBe(0);
      expect(cart.itemCount).toBe(0);
    });

    tick();
  }));

  it('should not update with negative quantity', () => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 2);

    const result = service.updateItemQuantity(mockItem.itemId, -1);
    expect(result).toBe(false);
  });

  it('should not update non-existent item', () => {
    service.setCurrentStore(mockStoreSlug);
    const result = service.updateItemQuantity('non-existent', 1);
    expect(result).toBe(false);
  });

  it('should remove item from cart', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 1);

    const result = service.removeItem(mockItem.itemId);
    expect(result).toBe(true);

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items.length).toBe(0);
      expect(cart.itemCount).toBe(0);
    });

    tick();
  }));

  it('should get item quantity', () => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 3);

    const quantity = service.getItemQuantity(mockItem.itemId);
    expect(quantity).toBe(3);
  });

  it('should return 0 for non-existent item quantity', () => {
    service.setCurrentStore(mockStoreSlug);
    const quantity = service.getItemQuantity('non-existent');
    expect(quantity).toBe(0);
  });

  it('should check if item is in cart', () => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 3);

    const inCart = service.isItemInCart(mockItem.itemId);
    expect(inCart).toBe(true);

    const notInCart = service.isItemInCart('non-existent');
    expect(notInCart).toBe(false);
  });

  it('should clear cart', fakeAsync(() => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 3);

    service.clearCart();

    const cart$ = service.getCurrentCart$();
    cart$.subscribe(cart => {
      expect(cart.items.length).toBe(0);
      expect(cart.itemCount).toBe(0);
      expect(cart.subtotal).toBe(0);
      expect(cart.total).toBe(0);
    });

    tick();
  }));

  it('should get cart summary', () => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 2);

    const summary = service.getCartSummary();
    expect(summary.storeSlug).toBe(mockStoreSlug);
    expect(summary.itemCount).toBe(2);
    expect(summary.subtotal).toBe(100.00);
  });

  it('should persist cart to localStorage', () => {
    service.setCurrentStore(mockStoreSlug);
    service.addItem(mockItem, 2);

    const storageKey = `shopper_cart_${mockStoreSlug}`;
    const stored = localStorage.getItem(storageKey);
    expect(stored).toBeTruthy();

    const parsed = JSON.parse(stored!);
    expect(parsed.items.length).toBe(1);
    expect(parsed.items[0].itemId).toBe(mockItem.itemId);
  });
});