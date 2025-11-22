import { TestBed } from '@angular/core/testing';
import { ShopperCartService, CartItem, Cart } from './shopper-cart.service';
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

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Store Context Management', () => {
    it('should set current store', () => {
      service.setCurrentStore(mockStoreSlug);

      const cart$ = service.getCurrentCart$();
      expect(cart$).toBeTruthy();
    });

    it('should throw error when no store context set', () => {
      expect(() => service.getCurrentCart$()).toThrowError('No store context set. Call setCurrentStore() first.');
    });

    it('should get cart for specific store', () => {
      const cart$ = service.getCartForStore$(mockStoreSlug);
      expect(cart$).toBeTruthy();
    });
  });

  describe('Cart Operations', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
    });

    it('should add item to empty cart', () => {
      const result = service.addItem(mockItem, 1);
      expect(result).toBe(true);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(1);
        expect(cart.items[0].itemId).toBe(mockItem.itemId);
        expect(cart.items[0].quantity).toBe(1);
        expect(cart.itemCount).toBe(1);
        expect(cart.subtotal).toBe(50.00);
        expect(cart.tax).toBe(4.00); // 8% of 50
        expect(cart.total).toBe(54.00);
      });
    });

    it('should add multiple quantities of same item', () => {
      service.addItem(mockItem, 2);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(1);
        expect(cart.items[0].quantity).toBe(2);
        expect(cart.itemCount).toBe(2);
        expect(cart.subtotal).toBe(100.00);
      });
    });

    it('should update existing item quantity when adding same item', () => {
      service.addItem(mockItem, 1);
      service.addItem(mockItem, 2);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(1);
        expect(cart.items[0].quantity).toBe(3);
        expect(cart.itemCount).toBe(3);
      });
    });

    it('should not add item with invalid data', () => {
      const invalidItem = { ...mockItem, itemId: '' };
      const result = service.addItem(invalidItem, 1);
      expect(result).toBe(false);
    });

    it('should not add item with zero or negative quantity', () => {
      const result1 = service.addItem(mockItem, 0);
      const result2 = service.addItem(mockItem, -1);
      expect(result1).toBe(false);
      expect(result2).toBe(false);
    });
  });

  describe('Item Quantity Management', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
      service.addItem(mockItem, 2);
    });

    it('should update item quantity', () => {
      const result = service.updateItemQuantity(mockItem.itemId, 5);
      expect(result).toBe(true);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items[0].quantity).toBe(5);
        expect(cart.itemCount).toBe(5);
      });
    });

    it('should remove item when quantity set to 0', () => {
      const result = service.updateItemQuantity(mockItem.itemId, 0);
      expect(result).toBe(true);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(0);
        expect(cart.itemCount).toBe(0);
      });
    });

    it('should not update with negative quantity', () => {
      const result = service.updateItemQuantity(mockItem.itemId, -1);
      expect(result).toBe(false);
    });

    it('should not update non-existent item', () => {
      const result = service.updateItemQuantity('non-existent', 1);
      expect(result).toBe(false);
    });
  });

  describe('Item Removal', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
      service.addItem(mockItem, 1);
    });

    it('should remove item from cart', () => {
      const result = service.removeItem(mockItem.itemId);
      expect(result).toBe(true);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(0);
        expect(cart.itemCount).toBe(0);
      });
    });

    it('should not remove non-existent item', () => {
      const result = service.removeItem('non-existent');
      expect(result).toBe(false);
    });
  });

  describe('Cart Utilities', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
      service.addItem(mockItem, 3);
    });

    it('should get item quantity', () => {
      const quantity = service.getItemQuantity(mockItem.itemId);
      expect(quantity).toBe(3);
    });

    it('should return 0 for non-existent item quantity', () => {
      const quantity = service.getItemQuantity('non-existent');
      expect(quantity).toBe(0);
    });

    it('should check if item is in cart', () => {
      const inCart = service.isItemInCart(mockItem.itemId);
      expect(inCart).toBe(true);

      const notInCart = service.isItemInCart('non-existent');
      expect(notInCart).toBe(false);
    });

    it('should get total item count', () => {
      const count = service.getTotalItemCount();
      expect(count).toBe(3);
    });

    it('should clear cart', () => {
      service.clearCart();

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(0);
        expect(cart.itemCount).toBe(0);
        expect(cart.subtotal).toBe(0);
        expect(cart.total).toBe(0);
      });
    });
  });

  describe('Cart Summary', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
      service.addItem(mockItem, 2);
    });

    it('should get cart summary', () => {
      const summary = service.getCartSummary();
      expect(summary.storeSlug).toBe(mockStoreSlug);
      expect(summary.itemCount).toBe(2);
      expect(summary.subtotal).toBe(100.00);
      expect(summary.tax).toBe(8.00);
      expect(summary.total).toBe(108.00);
    });
  });

  describe('Cart Persistence', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
    });

    it('should persist cart to localStorage', () => {
      service.addItem(mockItem, 2);

      const storageKey = `shopper_cart_${mockStoreSlug}`;
      const stored = localStorage.getItem(storageKey);
      expect(stored).toBeTruthy();

      const parsed = JSON.parse(stored!);
      expect(parsed.items.length).toBe(1);
      expect(parsed.items[0].itemId).toBe(mockItem.itemId);
    });

    it('should load cart from localStorage', () => {
      // Add item to create cart in storage
      service.addItem(mockItem, 2);

      // Create new service instance to test loading
      const newService = new ShopperCartService();
      newService.setCurrentStore(mockStoreSlug);

      const cart$ = newService.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(1);
        expect(cart.items[0].itemId).toBe(mockItem.itemId);
        expect(cart.items[0].quantity).toBe(2);
      });
    });
  });

  describe('Guest Cart Merging', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
    });

    it('should merge guest cart with user cart', () => {
      const guestItem: CartItem = {
        itemId: mockItem.itemId,
        storeSlug: mockStoreSlug,
        title: mockItem.title,
        price: mockItem.price,
        condition: mockItem.condition,
        quantity: 2,
        addedAt: new Date(),
        isAvailable: true
      };

      const userCartItems: CartItem[] = [
        {
          ...guestItem,
          itemId: 'user-item-1',
          title: 'User Item',
          quantity: 1
        }
      ];

      // Add guest item first
      service.addItem(mockItem, 1);

      // Merge with user cart
      service.mergeGuestCartWithUserCart(userCartItems);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(2);

        // Check user item exists
        const userItem = cart.items.find(item => item.itemId === 'user-item-1');
        expect(userItem).toBeTruthy();
        expect(userItem!.quantity).toBe(1);

        // Check guest item exists with updated quantity
        const guestItemMerged = cart.items.find(item => item.itemId === mockItem.itemId);
        expect(guestItemMerged).toBeTruthy();
        expect(guestItemMerged!.quantity).toBe(1); // Original guest quantity
      });
    });

    it('should sum quantities when same item exists in both carts', () => {
      const userCartItems: CartItem[] = [
        {
          itemId: mockItem.itemId,
          storeSlug: mockStoreSlug,
          title: mockItem.title,
          price: mockItem.price,
          condition: mockItem.condition,
          quantity: 3,
          addedAt: new Date('2023-01-01'),
          isAvailable: true
        }
      ];

      // Add guest item
      service.addItem(mockItem, 2);

      // Merge with user cart containing same item
      service.mergeGuestCartWithUserCart(userCartItems);

      const cart$ = service.getCurrentCart$();
      cart$.subscribe(cart => {
        expect(cart.items.length).toBe(1);
        expect(cart.items[0].quantity).toBe(5); // 3 + 2
      });
    });
  });

  describe('Export for Checkout', () => {
    beforeEach(() => {
      service.setCurrentStore(mockStoreSlug);
      service.addItem(mockItem, 2);
    });

    it('should export cart for checkout', () => {
      const exported = service.exportCartForCheckout();

      expect(exported.storeSlug).toBe(mockStoreSlug);
      expect(exported.items.length).toBe(1);
      expect(exported.items[0].itemId).toBe(mockItem.itemId);
      expect(exported.items[0].quantity).toBe(2);
      expect(exported.items[0].price).toBe(50.00);
      expect(exported.summary.itemCount).toBe(2);
      expect(exported.summary.total).toBe(108.00);
    });
  });
});