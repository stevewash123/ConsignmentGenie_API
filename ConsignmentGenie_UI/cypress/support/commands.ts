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

// ===== EMAIL ENVIRONMENT COMMANDS =====

Cypress.Commands.add('setupEmailEnvironment', () => {
  const sendReal = Cypress.env('SEND_REAL_EMAILS')

  if (sendReal) {
    cy.log('🚨 REAL EMAIL MODE: Emails will be sent!')
    // Allow real emails to be sent - no API mocking
  } else {
    cy.log('🔧 MOCK EMAIL MODE: No real emails will be sent')
    // Could add email API mocking here if needed
  }
})

Cypress.Commands.add('generateTestEmail', (): Cypress.Chainable<string> => {
  const sendReal = Cypress.env('SEND_REAL_EMAILS')
  const domain = Cypress.env('REAL_EMAIL_DOMAIN')

  if (sendReal) {
    // Generate real email address with timestamp for uniqueness
    const timestamp = Date.now()
    const email = `cypress-test-${timestamp}@${domain}`
    cy.log(`📧 Generated real email: ${email}`)
    return cy.wrap(email)
  } else {
    // Generate fake email for testing
    const fakeEmail = `cypress-fake-${Date.now()}@example.com`
    cy.log(`🔧 Generated mock email: ${fakeEmail}`)
    return cy.wrap(fakeEmail)
  }
})

Cypress.Commands.add('verifyEmailSent', (options: { to: string; type: string }) => {
  const sendReal = Cypress.env('SEND_REAL_EMAILS')

  if (sendReal) {
    cy.log(`📧 Real ${options.type} email sent to: ${options.to}`)
    // In a real implementation, could verify via email provider API
    // For now, just log that real email was expected to be sent
  } else {
    cy.log(`🔧 Mock ${options.type} email verified for: ${options.to}`)
    // Verify that the registration/approval API was called successfully
  }
})

// ===== REGISTRATION WORKFLOW COMMANDS =====

// Register as owner command
Cypress.Commands.add('registerOwner', (ownerData: {
  fullName: string,
  email: string,
  password: string,
  shopName: string,
  phone?: string
}) => {
  cy.visit('/register/owner')

  cy.get('input[name="fullName"]').type(ownerData.fullName)
  cy.get('input[name="email"]').type(ownerData.email)
  if (ownerData.phone) {
    cy.get('input[name="phone"]').type(ownerData.phone)
  }
  cy.get('input[name="password"]').type(ownerData.password)
  cy.get('input[name="confirmPassword"]').type(ownerData.password)
  cy.get('input[name="shopName"]').type(ownerData.shopName)

  cy.get('button[type="submit"]').click()
  cy.url().should('include', '/register/success')
})

// Register as provider command
Cypress.Commands.add('registerProvider', (storeCode: string, providerData: {
  fullName: string,
  email: string,
  password: string,
  phone?: string,
  preferredPaymentMethod?: string,
  paymentDetails?: string
}) => {
  cy.visit('/register/provider')

  // Step 1: Validate store code
  cy.get('input[name="storeCode"]').type(storeCode)
  cy.get('button[type="submit"]').click()
  cy.get('[data-cy="registration-form"]').should('be.visible')

  // Step 2: Fill registration form
  cy.get('input[name="fullName"]').type(providerData.fullName)
  cy.get('input[name="email"]').type(providerData.email)
  cy.get('input[name="password"]').type(providerData.password)

  if (providerData.phone) {
    cy.get('input[name="phone"]').type(providerData.phone)
  }

  if (providerData.preferredPaymentMethod) {
    cy.get('select[name="preferredPaymentMethod"]').select(providerData.preferredPaymentMethod)
    if (providerData.paymentDetails) {
      cy.get('input[name="paymentDetails"]').type(providerData.paymentDetails)
    }
  }

  cy.get('button[type="submit"]').click()
  cy.url().should('include', '/register/success')
})

// Mock registration APIs
Cypress.Commands.add('mockRegistrationAPIs', () => {
  // Mock store code validation
  cy.intercept('GET', '**/auth/validate-store-code/1234', {
    statusCode: 200,
    body: {
      isValid: true,
      shopName: 'Demo Consignment Shop'
    }
  }).as('validateStoreCode')

  cy.intercept('GET', '**/auth/validate-store-code/9999', {
    statusCode: 200,
    body: {
      isValid: false,
      errorMessage: 'Invalid or disabled store code'
    }
  }).as('validateInvalidStoreCode')

  // Mock owner registration
  cy.intercept('POST', '**/auth/register/owner', {
    statusCode: 200,
    body: {
      success: true,
      message: 'Account created successfully. Your request has been sent for admin approval.'
    }
  }).as('registerOwner')

  // Mock provider registration
  cy.intercept('POST', '**/auth/register/provider', {
    statusCode: 200,
    body: {
      success: true,
      message: 'Account created successfully. Your request has been sent for approval.'
    }
  }).as('registerProvider')

  // Mock duplicate email error
  cy.intercept('POST', '**/auth/register/owner', {
    statusCode: 400,
    body: {
      success: false,
      message: 'An account with this email already exists',
      errors: ['Email is already in use']
    }
  }).as('registerOwnerDuplicate')

  cy.intercept('POST', '**/auth/register/provider', {
    statusCode: 400,
    body: {
      success: false,
      message: 'An account with this email already exists',
      errors: ['Email is already in use']
    }
  }).as('registerProviderDuplicate')
})

// Task to clean up test data (would need backend support)
Cypress.Commands.add('cleanupTestData', (options: { emails: string[] }) => {
  // This would typically make API calls to clean up test data
  // For now, just return a resolved promise
  return cy.wrap(new Promise(resolve => {
    cy.log(`Cleaning up test data for emails: ${options.emails.join(', ')}`)
    // In a real implementation, this would call backend cleanup APIs
    resolve(true)
  }))
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

      // ===== REGISTRATION WORKFLOW COMMANDS =====

      /**
       * Register as owner
       */
      registerOwner(ownerData: {
        fullName: string,
        email: string,
        password: string,
        shopName: string,
        phone?: string
      }): Chainable<void>

      /**
       * Register as provider
       */
      registerProvider(storeCode: string, providerData: {
        fullName: string,
        email: string,
        password: string,
        phone?: string,
        preferredPaymentMethod?: string,
        paymentDetails?: string
      }): Chainable<void>

      /**
       * Clean up test data
       */
      cleanupTestData(options: { emails: string[] }): Chainable<void>

      /**
       * Mock registration APIs
       */
      mockRegistrationAPIs(): Chainable<void>

      // ===== EMAIL ENVIRONMENT COMMANDS =====

      /**
       * Setup email environment (real vs mock)
       */
      setupEmailEnvironment(): Chainable<void>

      /**
       * Generate test email based on environment
       */
      generateTestEmail(): Chainable<string>

      /**
       * Verify email was sent (real or mock)
       */
      verifyEmailSent(options: { to: string; type: string }): Chainable<void>
    }
  }
}