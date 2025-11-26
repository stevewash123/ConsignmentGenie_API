import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ShopperItemList } from './shopper-catalog.service';

export interface CartItem {
  itemId: string;
  storeSlug: string;
  title: string;
  price: number;
  primaryImageUrl?: string;
  category?: string;
  brand?: string;
  condition: string;
  quantity: number;
  addedAt: Date;
  isAvailable: boolean;
}

export interface Cart {
  storeSlug: string;
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  tax: number;
  total: number;
  lastUpdated: Date;
}

export interface CartSummary {
  storeSlug: string;
  itemCount: number;
  subtotal: number;
  tax: number;
  total: number;
}

@Injectable({
  providedIn: 'root'
})
export class ShopperCartService {
  private readonly TAX_RATE = 0.08; // 8% tax rate
  private readonly STORAGE_KEY_PREFIX = 'shopper_cart_';

  private cartSubjects = new Map<string, BehaviorSubject<Cart>>();
  private currentStoreSlug = '';

  constructor() {}

  /**
   * Set the current store context for cart operations
   */
  setCurrentStore(storeSlug: string): void {
    this.currentStoreSlug = storeSlug;
    if (!this.cartSubjects.has(storeSlug)) {
      const cart = this.loadCartFromStorage(storeSlug);
      this.cartSubjects.set(storeSlug, new BehaviorSubject<Cart>(cart));
    }
  }

  /**
   * Get the current cart for the active store
   */
  getCurrentCart$(): Observable<Cart> {
    if (!this.currentStoreSlug) {
      throw new Error('No store context set. Call setCurrentStore() first.');
    }

    if (!this.cartSubjects.has(this.currentStoreSlug)) {
      this.setCurrentStore(this.currentStoreSlug);
    }

    return this.cartSubjects.get(this.currentStoreSlug)!.asObservable();
  }

  /**
   * Get cart for a specific store
   */
  getCartForStore$(storeSlug: string): Observable<Cart> {
    if (!this.cartSubjects.has(storeSlug)) {
      const cart = this.loadCartFromStorage(storeSlug);
      this.cartSubjects.set(storeSlug, new BehaviorSubject<Cart>(cart));
    }

    return this.cartSubjects.get(storeSlug)!.asObservable();
  }

  /**
   * Get current cart summary
   */
  getCartSummary(): CartSummary {
    const cart = this.getCurrentCart();
    return {
      storeSlug: cart.storeSlug,
      itemCount: cart.itemCount,
      subtotal: cart.subtotal,
      tax: cart.tax,
      total: cart.total
    };
  }

  /**
   * Add item to cart
   */
  addItem(item: ShopperItemList, quantity: number = 1): boolean {
    if (!this.currentStoreSlug) {
      throw new Error('No store context set. Call setCurrentStore() first.');
    }

    if (!item.itemId || quantity <= 0) {
      return false;
    }

    const cart = this.getCurrentCart();
    const existingItemIndex = cart.items.findIndex(cartItem => cartItem.itemId === item.itemId);

    if (existingItemIndex >= 0) {
      // Update existing item quantity
      cart.items[existingItemIndex].quantity += quantity;
      cart.items[existingItemIndex].addedAt = new Date();
    } else {
      // Add new item
      const cartItem: CartItem = {
        itemId: item.itemId,
        storeSlug: this.currentStoreSlug,
        title: item.title,
        price: item.price,
        primaryImageUrl: item.primaryImageUrl,
        category: item.category,
        brand: item.brand,
        condition: item.condition,
        quantity: quantity,
        addedAt: new Date(),
        isAvailable: true // Assume available when adding
      };
      cart.items.push(cartItem);
    }

    this.updateCart(cart);
    return true;
  }

  /**
   * Update item quantity in cart
   */
  updateItemQuantity(itemId: string, quantity: number): boolean {
    if (quantity < 0) {
      return false;
    }

    const cart = this.getCurrentCart();
    const itemIndex = cart.items.findIndex(item => item.itemId === itemId);

    if (itemIndex === -1) {
      return false;
    }

    if (quantity === 0) {
      return this.removeItem(itemId);
    }

    cart.items[itemIndex].quantity = quantity;
    cart.items[itemIndex].addedAt = new Date();

    this.updateCart(cart);
    return true;
  }

  /**
   * Remove item from cart
   */
  removeItem(itemId: string): boolean {
    const cart = this.getCurrentCart();
    const originalLength = cart.items.length;
    cart.items = cart.items.filter(item => item.itemId !== itemId);

    if (cart.items.length !== originalLength) {
      this.updateCart(cart);
      return true;
    }

    return false;
  }

  /**
   * Clear entire cart
   */
  clearCart(): void {
    const cart = this.getCurrentCart();
    cart.items = [];
    this.updateCart(cart);
  }

  /**
   * Get item quantity in cart
   */
  getItemQuantity(itemId: string): number {
    const cart = this.getCurrentCart();
    const item = cart.items.find(cartItem => cartItem.itemId === itemId);
    return item ? item.quantity : 0;
  }

  /**
   * Check if item is in cart
   */
  isItemInCart(itemId: string): boolean {
    return this.getItemQuantity(itemId) > 0;
  }

  /**
   * Get total number of items in cart
   */
  getTotalItemCount(): number {
    const cart = this.getCurrentCart();
    return cart.itemCount;
  }

  /**
   * Merge guest cart with user cart after login
   */
  mergeGuestCartWithUserCart(userCartItems: CartItem[]): void {
    const currentCart = this.getCurrentCart();

    // Merge items, giving preference to items already in user cart
    const mergedItems = [...userCartItems];

    currentCart.items.forEach(guestItem => {
      const existingItemIndex = mergedItems.findIndex(item => item.itemId === guestItem.itemId);
      if (existingItemIndex >= 0) {
        // Item exists in both carts, sum quantities
        mergedItems[existingItemIndex].quantity += guestItem.quantity;
        // Keep the more recent timestamp
        if (guestItem.addedAt > mergedItems[existingItemIndex].addedAt) {
          mergedItems[existingItemIndex].addedAt = guestItem.addedAt;
        }
      } else {
        // Item only in guest cart, add it
        mergedItems.push(guestItem);
      }
    });

    currentCart.items = mergedItems;
    this.updateCart(currentCart);
  }

  /**
   * Export cart for API submission
   */
  exportCartForCheckout() {
    const cart = this.getCurrentCart();
    return {
      storeSlug: cart.storeSlug,
      items: cart.items.map(item => ({
        itemId: item.itemId,
        quantity: item.quantity,
        price: item.price
      })),
      summary: this.getCartSummary()
    };
  }

  /**
   * Load cart from localStorage
   */
  private loadCartFromStorage(storeSlug: string): Cart {
    try {
      const stored = localStorage.getItem(this.getStorageKey(storeSlug));
      if (stored) {
        const parsed = JSON.parse(stored);
        // Convert date strings back to Date objects
        parsed.lastUpdated = new Date(parsed.lastUpdated);
        parsed.items.forEach((item: any) => {
          item.addedAt = new Date(item.addedAt);
        });
        // Recalculate totals in case tax rate changed
        return this.calculateCartTotals(parsed);
      }
    } catch (error) {
      console.error('Error loading cart from storage:', error);
    }

    return this.createEmptyCart(storeSlug);
  }

  /**
   * Save cart to localStorage
   */
  private saveCartToStorage(cart: Cart): void {
    try {
      localStorage.setItem(this.getStorageKey(cart.storeSlug), JSON.stringify(cart));
    } catch (error) {
      console.error('Error saving cart to storage:', error);
    }
  }

  /**
   * Get storage key for store cart
   */
  private getStorageKey(storeSlug: string): string {
    return `${this.STORAGE_KEY_PREFIX}${storeSlug}`;
  }

  /**
   * Create empty cart
   */
  private createEmptyCart(storeSlug: string): Cart {
    return {
      storeSlug,
      items: [],
      itemCount: 0,
      subtotal: 0,
      tax: 0,
      total: 0,
      lastUpdated: new Date()
    };
  }

  /**
   * Get current cart snapshot
   */
  private getCurrentCart(): Cart {
    if (!this.currentStoreSlug) {
      throw new Error('No store context set. Call setCurrentStore() first.');
    }

    if (!this.cartSubjects.has(this.currentStoreSlug)) {
      this.setCurrentStore(this.currentStoreSlug);
    }

    return this.cartSubjects.get(this.currentStoreSlug)!.getValue();
  }

  /**
   * Update cart and notify subscribers
   */
  private updateCart(cart: Cart): void {
    const updatedCart = this.calculateCartTotals(cart);
    updatedCart.lastUpdated = new Date();

    this.saveCartToStorage(updatedCart);

    if (this.cartSubjects.has(updatedCart.storeSlug)) {
      this.cartSubjects.get(updatedCart.storeSlug)!.next(updatedCart);
    }
  }

  /**
   * Calculate cart totals
   */
  private calculateCartTotals(cart: Cart): Cart {
    const itemCount = cart.items.reduce((total, item) => total + item.quantity, 0);
    const subtotal = cart.items.reduce((total, item) => total + (item.price * item.quantity), 0);
    const tax = subtotal * this.TAX_RATE;
    const total = subtotal + tax;

    return {
      ...cart,
      itemCount: Math.round(itemCount),
      subtotal: Math.round(subtotal * 100) / 100,
      tax: Math.round(tax * 100) / 100,
      total: Math.round(total * 100) / 100
    };
  }
}