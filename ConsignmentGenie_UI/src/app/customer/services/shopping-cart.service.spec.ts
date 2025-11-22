import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ShoppingCartService } from './shopping-cart.service';

describe('ShoppingCartService', () => {
  let service: ShoppingCartService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'https://localhost:7042';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ShoppingCartService]
    });
    service = TestBed.inject(ShoppingCartService);
    httpMock = TestBed.inject(HttpTestingController);

    // Clear localStorage
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('addToCart', () => {
    it('should add item to cart successfully for authenticated user', () => {
      // Mock authenticated user
      localStorage.setItem('cg_customer_token', 'valid-token');

      let result: any;
      service.addToCart('item-123', 2).subscribe(item => {
        result = item;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/shopping-cart/add`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ itemId: 'item-123', quantity: 2 });

      const mockResponse = { id: '1', itemId: 'item-123', quantity: 2 };
      req.flush(mockResponse);

      expect(result).toEqual(mockResponse);
    });

    it('should handle guest user cart operations', () => {
      // No auth token = guest user
      let result: any;
      service.addToCart('item-123', 1).subscribe(item => {
        result = item;
      });

      expect(result).toBeTruthy();

      // Check localStorage
      const guestCart = JSON.parse(localStorage.getItem('cg_guest_cart') || '[]');
      expect(guestCart.length).toBeGreaterThan(0);
    });
  });

  describe('removeFromCart', () => {
    it('should remove item from cart successfully for authenticated user', () => {
      localStorage.setItem('cg_customer_token', 'valid-token');

      service.removeFromCart('item-123').subscribe();

      const req = httpMock.expectOne(`${mockApiUrl}/api/shopping-cart/remove/item-123`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('should remove item from localStorage for guest user', () => {
      const guestCart = [{ id: 'item-123', itemId: 'item-123', quantity: 1 }];
      localStorage.setItem('cg_guest_cart', JSON.stringify(guestCart));

      service.removeFromCart('item-123').subscribe();

      const updatedCart = JSON.parse(localStorage.getItem('cg_guest_cart') || '[]');
      expect(updatedCart).toEqual([]);
    });
  });

  describe('getCart', () => {
    it('should get cart from server for authenticated user', () => {
      localStorage.setItem('cg_customer_token', 'valid-token');

      let result: any;
      service.getCart().subscribe(cart => {
        result = cart;
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/shopping-cart`);
      expect(req.request.method).toBe('GET');
      req.flush([]);

      expect(result).toEqual([]);
    });

    it('should get cart from localStorage for guest user', () => {
      const guestCart = [{ id: 'item-123', itemId: 'item-123', quantity: 1 }];
      localStorage.setItem('cg_guest_cart', JSON.stringify(guestCart));

      let result: any;
      service.getCart().subscribe(cart => {
        result = cart;
      });

      expect(result).toBeTruthy();
    });
  });

  describe('clearCart', () => {
    it('should clear cart on server for authenticated user', () => {
      localStorage.setItem('cg_customer_token', 'valid-token');

      service.clearCart().subscribe();

      const req = httpMock.expectOne(`${mockApiUrl}/api/shopping-cart/clear`);
      expect(req.request.method).toBe('DELETE');
      req.flush({});
    });

    it('should clear localStorage cart for guest user', () => {
      localStorage.setItem('cg_guest_cart', JSON.stringify([{ id: 'test' }]));

      service.clearCart().subscribe();

      expect(localStorage.getItem('cg_guest_cart')).toBeNull();
    });
  });

  describe('isItemInCart', () => {
    it('should check if item is in cart', () => {
      const result = service.isItemInCart('item-123');
      expect(typeof result).toBe('boolean');
    });
  });

  describe('error handling', () => {
    it('should handle server error when adding to cart', () => {
      localStorage.setItem('cg_customer_token', 'valid-token');

      let error: any;
      service.addToCart('item-123', 1).subscribe({
        next: () => {},
        error: (err) => {
          error = err;
        }
      });

      const req = httpMock.expectOne(`${mockApiUrl}/api/shopping-cart/add`);
      req.flush({ message: 'Item not found' }, { status: 404, statusText: 'Not Found' });

      expect(error).toBeTruthy();
    });
  });
});