// Customer Authentication Models
export interface CustomerRegistrationRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  orgSlug: string;
}

export interface CustomerLoginRequest {
  email: string;
  password: string;
  orgSlug: string;
  rememberMe?: boolean;
}

export interface Customer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isEmailVerified: boolean;
  lastLoginAt?: string;
  fullName: string;
}

export interface CustomerLoginResponse {
  token: string;
  customer: Customer;
  organizationId: string;
  expiresAt: string;
}

export interface CustomerProfile extends Customer {
  orderCount: number;
  totalSpent: number;
  wishlistCount: number;
  memberSince: string;
}

// Shopping Cart Models
export interface CartItem {
  id: string;
  itemId: string;
  item: PublicItem;
  quantity: number;
  addedAt: string;
  lineTotal: number;
}

export interface ShoppingCart {
  id: string;
  customerId: string;
  organizationId: string;
  items: CartItem[];
  itemCount: number;
  totalAmount: number;
  lastUpdatedAt: string;
}

export interface AddToCartRequest {
  itemId: string;
  quantity: number;
  customerId?: string;
  organizationId?: string;
}

export interface UpdateCartItemRequest {
  customerId?: string;
  itemId?: string;
  quantity: number;
}

// Order Models
export interface CreateOrderRequest {
  customerId?: string;
  organizationId?: string;
  shippingAddress?: Address;
  billingAddress?: Address;
  customerNotes?: string;
  shippingMethod?: string;
}

export interface Address {
  firstName: string;
  lastName: string;
  company?: string;
  address1: string;
  address2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone?: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  organizationId: string;
  status: OrderStatus;
  subTotal: number;
  tax: number;
  shipping: number;
  total: number;
  items: OrderItem[];
  shippingAddress?: Address;
  billingAddress?: Address;
  customerNotes?: string;
  trackingNumber?: string;
  createdAt: string;
  shippedAt?: string;
  deliveredAt?: string;
}

export interface OrderItem {
  id: string;
  itemId: string;
  item: PublicItem;
  quantity: number;
  price: number;
  commissionAmount: number;
  lineTotal: number;
}

export enum OrderStatus {
  Pending = 'Pending',
  PaymentProcessing = 'PaymentProcessing',
  Paid = 'Paid',
  Processing = 'Processing',
  Shipped = 'Shipped',
  Delivered = 'Delivered',
  Cancelled = 'Cancelled',
  Refunded = 'Refunded',
  Failed = 'Failed'
}

// Wishlist Models
export interface WishlistItem {
  id: string;
  customerId: string;
  itemId: string;
  item: PublicItem;
  addedAt: string;
}

export interface AddToWishlistRequest {
  itemId: string;
  customerId?: string;
}

export interface MoveToCartRequest {
  customerId?: string;
  itemId?: string;
  quantity: number;
}

// Import shared models from api.models.ts
import { PublicItem } from './api.models';