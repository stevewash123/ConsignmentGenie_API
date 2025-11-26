import { Injectable, inject, signal, computed } from '@angular/core';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { ApiService } from '../../shared/services/api.service';
import { CustomerAuthService } from './customer-auth.service';
import {
  ShoppingCart,
  CartItem,
  AddToCartRequest,
  UpdateCartItemRequest
} from '../../shared/models/customer.models';

@Injectable({
  providedIn: 'root'
})
export class ShoppingCartService {
  private readonly apiService = inject(ApiService);
  private readonly authService = inject(CustomerAuthService);

  // Cart state
  private readonly cartSubject = new BehaviorSubject<ShoppingCart | null>(null);
  public cart$ = this.cartSubject.asObservable();

  // Signals for reactive UI
  public cart = signal<ShoppingCart | null>(null);
  public itemCount = computed(() => this.cart()?.itemCount || 0);
  public totalAmount = computed(() => this.cart()?.totalAmount || 0);
  public isEmpty = computed(() => this.itemCount() === 0);

  // Local storage key for guest cart
  private readonly GUEST_CART_KEY = 'cg_guest_cart';

  constructor() {
    // Load cart when service initializes
    this.loadCart();

    // Subscribe to auth state changes
    this.authService.currentCustomer$.subscribe(customer => {
      if (customer) {
        this.loadCart();
      } else {
        this.loadGuestCart();
      }
    });
  }

  loadCart(): void {
    if (this.authService.isAuthenticated()) {
      this.getCart().subscribe({
        next: (cart) => {
          this.updateCartState(cart);
        },
        error: (error) => {
          console.error('Error loading cart:', error);
          this.updateCartState(null);
        }
      });
    } else {
      this.loadGuestCart();
    }
  }

  getCart(): Observable<ShoppingCart | null> {
    return this.apiService.get<ShoppingCart>('/api/customer/cart')
      .pipe(
        map(response => {
          if (response.success) {
            return response.data || null;
          }
          return null;
        }),
        catchError(error => {
          console.error('Get cart error:', error);
          return [null];
        })
      );
  }

  addToCart(itemId: string, quantity: number = 1): Observable<ShoppingCart | null> {
    if (!this.authService.isAuthenticated()) {
      return this.addToGuestCart(itemId, quantity);
    }

    const request: AddToCartRequest = {
      itemId,
      quantity
    };

    return this.apiService.post<ShoppingCart>('/api/customer/cart/items', request)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.updateCartState(response.data);
          }
        }),
        map(response => {
          if (response.success) {
            return response.data || null;
          }
          throw new Error(response.message || 'Failed to add item to cart');
        }),
        catchError(error => {
          console.error('Add to cart error:', error);
          return throwError(() => error);
        })
      );
  }

  updateCartItem(itemId: string, quantity: number): Observable<ShoppingCart | null> {
    if (!this.authService.isAuthenticated()) {
      return this.updateGuestCartItem(itemId, quantity);
    }

    const request: UpdateCartItemRequest = {
      quantity
    };

    return this.apiService.put<ShoppingCart>(`/api/customer/cart/items/${itemId}`, request)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.updateCartState(response.data);
          }
        }),
        map(response => {
          if (response.success) {
            return response.data || null;
          }
          throw new Error(response.message || 'Failed to update cart item');
        }),
        catchError(error => {
          console.error('Update cart item error:', error);
          return throwError(() => error);
        })
      );
  }

  removeFromCart(itemId: string): Observable<ShoppingCart | null> {
    if (!this.authService.isAuthenticated()) {
      return this.removeFromGuestCart(itemId);
    }

    return this.apiService.delete<ShoppingCart>(`/api/customer/cart/items/${itemId}`)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.updateCartState(response.data);
          }
        }),
        map(response => {
          if (response.success) {
            return response.data || null;
          }
          throw new Error(response.message || 'Failed to remove item from cart');
        }),
        catchError(error => {
          console.error('Remove from cart error:', error);
          return throwError(() => error);
        })
      );
  }

  clearCart(): Observable<any> {
    if (!this.authService.isAuthenticated()) {
      return this.clearGuestCart();
    }

    return this.apiService.delete('/api/customer/cart')
      .pipe(
        tap(response => {
          if (response.success) {
            this.updateCartState(null);
          }
        }),
        map(response => {
          if (response.success) {
            return response.data;
          }
          throw new Error(response.message || 'Failed to clear cart');
        }),
        catchError(error => {
          console.error('Clear cart error:', error);
          return throwError(() => error);
        })
      );
  }

  // Guest cart functionality (localStorage)
  private addToGuestCart(itemId: string, quantity: number): Observable<ShoppingCart | null> {
    return new Observable(observer => {
      try {
        const guestCart = this.getGuestCart();
        const existingItem = guestCart.items.find(item => item.itemId === itemId);

        if (existingItem) {
          existingItem.quantity += quantity;
        } else {
          // Note: In a real implementation, you'd need to fetch item details
          const newItem: CartItem = {
            id: `guest-${Date.now()}`,
            itemId,
            item: {} as any, // Would need to fetch item details
            quantity,
            addedAt: new Date().toISOString(),
            lineTotal: 0 // Would need to calculate
          };
          guestCart.items.push(newItem);
        }

        this.saveGuestCart(guestCart);
        this.updateCartState(guestCart);
        observer.next(guestCart);
        observer.complete();
      } catch (error) {
        observer.error(error);
      }
    });
  }

  private updateGuestCartItem(itemId: string, quantity: number): Observable<ShoppingCart | null> {
    return new Observable(observer => {
      try {
        const guestCart = this.getGuestCart();
        const item = guestCart.items.find(item => item.itemId === itemId);

        if (item) {
          if (quantity <= 0) {
            guestCart.items = guestCart.items.filter(item => item.itemId !== itemId);
          } else {
            item.quantity = quantity;
          }
          this.saveGuestCart(guestCart);
          this.updateCartState(guestCart);
        }

        observer.next(guestCart);
        observer.complete();
      } catch (error) {
        observer.error(error);
      }
    });
  }

  private removeFromGuestCart(itemId: string): Observable<ShoppingCart | null> {
    return new Observable(observer => {
      try {
        const guestCart = this.getGuestCart();
        guestCart.items = guestCart.items.filter(item => item.itemId !== itemId);
        this.saveGuestCart(guestCart);
        this.updateCartState(guestCart);
        observer.next(guestCart);
        observer.complete();
      } catch (error) {
        observer.error(error);
      }
    });
  }

  private clearGuestCart(): Observable<any> {
    return new Observable(observer => {
      try {
        localStorage.removeItem(this.GUEST_CART_KEY);
        this.updateCartState(null);
        observer.next(true);
        observer.complete();
      } catch (error) {
        observer.error(error);
      }
    });
  }

  private loadGuestCart(): void {
    const guestCart = this.getGuestCart();
    this.updateCartState(guestCart.items.length > 0 ? guestCart : null);
  }

  private getGuestCart(): ShoppingCart {
    try {
      const cartData = localStorage.getItem(this.GUEST_CART_KEY);
      if (cartData) {
        return JSON.parse(cartData);
      }
    } catch (error) {
      console.error('Error parsing guest cart:', error);
    }

    // Return empty cart
    return {
      id: 'guest-cart',
      customerId: 'guest',
      organizationId: '',
      items: [],
      itemCount: 0,
      totalAmount: 0,
      lastUpdatedAt: new Date().toISOString()
    };
  }

  private saveGuestCart(cart: ShoppingCart): void {
    try {
      // Update computed properties
      cart.itemCount = cart.items.reduce((sum, item) => sum + item.quantity, 0);
      cart.totalAmount = cart.items.reduce((sum, item) => sum + item.lineTotal, 0);
      cart.lastUpdatedAt = new Date().toISOString();

      localStorage.setItem(this.GUEST_CART_KEY, JSON.stringify(cart));
    } catch (error) {
      console.error('Error saving guest cart:', error);
    }
  }

  private updateCartState(cart: ShoppingCart | null): void {
    this.cartSubject.next(cart);
    this.cart.set(cart);
  }

  // Utility methods
  getItemInCart(itemId: string): CartItem | null {
    const currentCart = this.cart();
    return currentCart?.items.find(item => item.itemId === itemId) || null;
  }

  getItemQuantity(itemId: string): number {
    const item = this.getItemInCart(itemId);
    return item?.quantity || 0;
  }

  isItemInCart(itemId: string): boolean {
    return this.getItemQuantity(itemId) > 0;
  }
}