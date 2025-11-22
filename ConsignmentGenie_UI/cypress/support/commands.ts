// cypress/support/commands.ts

// Login command for test convenience (admin/owner)
Cypress.Commands.add('login', (email: string, password: string = 'password123') => {
  cy.visit('/login')
  cy.get('input[name="email"]').type(email)
  cy.get('input[name="password"]').type(password)
  cy.get('button[type="submit"]').click()
})

// Test account login commands
Cypress.Commands.add('loginAsShopOwner', () => {
  cy.login('owner@demoshop.com')
})

Cypress.Commands.add('loginAsAdmin', () => {
  cy.login('admin@demoshop.com')
})

Cypress.Commands.add('loginAsProvider', () => {
  cy.login('provider@demoshop.com')
})

Cypress.Commands.add('loginAsCustomer', () => {
  cy.login('customer@demoshop.com')
})

// Check if user is redirected to the expected dashboard
Cypress.Commands.add('shouldBeOnDashboard', (userType: 'admin' | 'owner' | 'customer') => {
  const routes = {
    admin: '/admin/dashboard',
    owner: '/owner/dashboard',
    customer: '/customer/dashboard'
  }
  cy.url().should('include', routes[userType])
})

// Custom command to wait for API response
Cypress.Commands.add('waitForLoginAPI', () => {
  cy.intercept('POST', '**/api/auth/login').as('loginRequest')
  cy.wait('@loginRequest', { timeout: 10000 })
})

// Owner workflow specific commands
Cypress.Commands.add('loginAsOwnerWithMocks', () => {
  cy.window().then((win) => {
    win.localStorage.setItem('auth_token', 'mock-owner-token')
    win.localStorage.setItem('user_data', JSON.stringify({
      userId: 'owner-123',
      email: 'owner@demoshop.com',
      role: 1,
      organizationId: 'demo-org',
      organizationName: 'Demo Shop',
      businessName: 'Demo Consignment Shop'
    }))
    win.localStorage.setItem('tokenExpiry', new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString())
  })
})

Cypress.Commands.add('mockOwnerAPIs', () => {
  cy.fixture('owner-data').then((ownerData) => {
    // Mock common owner APIs
    cy.intercept('GET', '**/api/providers*', {
      statusCode: 200,
      body: { success: true, data: ownerData.providers }
    }).as('getProviders')

    cy.intercept('GET', '**/api/transactions*', {
      statusCode: 200,
      body: {
        success: true,
        data: {
          items: ownerData.salesData.transactions,
          totalPages: 1,
          currentPage: 1,
          totalItems: ownerData.salesData.transactions.length
        }
      }
    }).as('getTransactions')

    cy.intercept('GET', '**/api/transactions/summary*', {
      statusCode: 200,
      body: { success: true, data: ownerData.salesData.summary }
    }).as('getSummary')

    cy.intercept('GET', '**/api/transactions/metrics*', {
      statusCode: 200,
      body: {
        success: true,
        data: {
          totalSales: ownerData.dashboardData.summary.recentSales,
          transactionCount: ownerData.dashboardData.summary.recentSalesCount
        }
      }
    }).as('getSalesMetrics')

    cy.intercept('GET', '**/api/payouts/pending*', {
      statusCode: 200,
      body: {
        success: true,
        data: Array(ownerData.dashboardData.summary.pendingPayoutCount).fill({
          id: 'payout-1',
          providerId: 'prov-001',
          pendingAmount: ownerData.dashboardData.summary.pendingPayouts / ownerData.dashboardData.summary.pendingPayoutCount
        })
      }
    }).as('getPendingPayouts')

    cy.intercept('GET', '**/api/items*', {
      statusCode: 200,
      body: { success: true, data: ownerData.salesData.availableItems }
    }).as('getAvailableItems')
  })
})

// ===== SHOPPER COMMANDS =====

// Shopper login command
Cypress.Commands.add('shopperLogin', (storeSlug: string, email: string, password: string = 'password123') => {
  cy.visit(`/shop/${storeSlug}/login`)
  cy.get('input[formControlName="email"]').type(email)
  cy.get('input[formControlName="password"]').type(password)
  cy.get('button[type="submit"]').click()
})

// Shopper registration command
Cypress.Commands.add('shopperRegister', (storeSlug: string, userData: {
  fullName: string,
  email: string,
  password: string,
  phone?: string,
  address?: string,
  city?: string,
  state?: string,
  zipCode?: string
}) => {
  cy.visit(`/shop/${storeSlug}/register`)

  cy.get('input[formControlName="fullName"]').type(userData.fullName)
  cy.get('input[formControlName="email"]').type(userData.email)
  cy.get('input[formControlName="password"]').type(userData.password)
  cy.get('input[formControlName="confirmPassword"]').type(userData.password)

  if (userData.phone) {
    cy.get('input[formControlName="phone"]').type(userData.phone)
  }
  if (userData.address) {
    cy.get('input[formControlName="address"]').type(userData.address)
  }
  if (userData.city) {
    cy.get('input[formControlName="city"]').type(userData.city)
  }
  if (userData.state) {
    cy.get('select[formControlName="state"]').select(userData.state)
  }
  if (userData.zipCode) {
    cy.get('input[formControlName="zipCode"]').type(userData.zipCode)
  }

  cy.get('button[type="submit"]').click()
})

// Mock shopper authentication APIs
Cypress.Commands.add('mockShopperAPIs', (storeSlug: string) => {
  // Mock store info API
  cy.intercept('GET', `**/api/shop/${storeSlug}/info`, {
    statusCode: 200,
    body: {
      id: 'store-123',
      name: 'Cypress Test Store',
      slug: storeSlug,
      description: 'A test store for Cypress testing',
      address: '123 Test St',
      city: 'Test City',
      state: 'TC',
      zipCode: '12345',
      phone: '555-TEST-123',
      email: 'info@cypresstest.com',
      isActive: true
    }
  }).as('getStoreInfo')

  // Mock login API
  cy.intercept('POST', `**/api/shop/${storeSlug}/auth/login`, {
    statusCode: 200,
    body: {
      success: true,
      token: 'cypress-test-jwt-token',
      shopper: {
        id: 'shopper-123',
        fullName: 'Cypress Test User',
        email: Cypress.env('testUserEmail')
      }
    }
  }).as('shopperLogin')

  // Mock registration API
  cy.intercept('POST', `**/api/shop/${storeSlug}/auth/register`, {
    statusCode: 200,
    body: {
      success: true,
      token: 'cypress-test-jwt-token',
      shopper: {
        id: 'shopper-new-123',
        fullName: 'New Cypress User',
        email: 'new.user@cypress.com'
      }
    }
  }).as('shopperRegister')

  // Mock profile API
  cy.intercept('GET', `**/api/shop/${storeSlug}/account/profile`, {
    statusCode: 200,
    body: {
      id: 'shopper-123',
      fullName: 'Cypress Test User',
      email: Cypress.env('testUserEmail'),
      phone: '555-123-4567',
      address: '123 Test St',
      city: 'Test City',
      state: 'TC',
      zipCode: '12345'
    }
  }).as('getShopperProfile')
})

// Check if shopper is on store page
Cypress.Commands.add('shouldBeOnStorePage', (storeSlug: string, page?: string) => {
  const expectedPath = page ? `/shop/${storeSlug}/${page}` : `/shop/${storeSlug}`
  cy.url().should('include', expectedPath)
})

// Add items to cart
Cypress.Commands.add('addItemToCart', (itemName: string) => {
  cy.contains('.item-card', itemName)
    .find('button')
    .contains('Add to Cart')
    .click()
})

// Clear shopper localStorage for clean tests
Cypress.Commands.add('clearShopperStorage', (storeSlug: string) => {
  cy.window().then((win) => {
    win.localStorage.removeItem(`shopper_token_${storeSlug}`)
    win.localStorage.removeItem(`shopper_user_${storeSlug}`)
    win.localStorage.removeItem(`cart_${storeSlug}`)
    win.localStorage.removeItem(`favorites_${storeSlug}`)
  })
})

// Type declarations for custom commands
declare global {
  namespace Cypress {
    interface Chainable {
      /**
       * Custom command to login with email and password (admin/owner)
       */
      login(email: string, password?: string): Chainable<void>

      /**
       * Login as shop owner test account
       */
      loginAsShopOwner(): Chainable<void>

      /**
       * Login as admin test account
       */
      loginAsAdmin(): Chainable<void>

      /**
       * Login as provider test account
       */
      loginAsProvider(): Chainable<void>

      /**
       * Login as customer test account
       */
      loginAsCustomer(): Chainable<void>

      /**
       * Check if user is on the correct dashboard
       */
      shouldBeOnDashboard(userType: 'admin' | 'owner' | 'customer'): Chainable<void>

      /**
       * Wait for login API call to complete
       */
      waitForLoginAPI(): Chainable<void>

      /**
       * Login as owner and set up authentication data
       */
      loginAsOwnerWithMocks(): Chainable<void>

      /**
       * Mock common Owner workflow APIs
       */
      mockOwnerAPIs(): Chainable<void>

      // ===== SHOPPER COMMANDS =====

      /**
       * Login as shopper in a specific store
       */
      shopperLogin(storeSlug: string, email: string, password?: string): Chainable<void>

      /**
       * Register as shopper in a specific store
       */
      shopperRegister(storeSlug: string, userData: {
        fullName: string,
        email: string,
        password: string,
        phone?: string,
        address?: string,
        city?: string,
        state?: string,
        zipCode?: string
      }): Chainable<void>

      /**
       * Mock shopper-related APIs
       */
      mockShopperAPIs(storeSlug: string): Chainable<void>

      /**
       * Check if user is on store page
       */
      shouldBeOnStorePage(storeSlug: string, page?: string): Chainable<void>

      /**
       * Add item to cart
       */
      addItemToCart(itemName: string): Chainable<void>

      /**
       * Clear shopper-specific localStorage
       */
      clearShopperStorage(storeSlug: string): Chainable<void>
    }
  }
}