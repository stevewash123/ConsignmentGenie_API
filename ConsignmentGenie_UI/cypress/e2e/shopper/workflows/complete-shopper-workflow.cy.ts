describe('Complete Shopper Workflow', () => {
  const testStoreSlug = Cypress.env('testStoreSlug') || 'cypress-test-store'
  const testUserEmail = `workflow.test.${Date.now()}@cypress.com`
  const testUserPassword = Cypress.env('testUserPassword') || 'CypressTest123!'
  const testGuestEmail = Cypress.env('testGuestEmail') || 'cypress.guest@example.com'

  beforeEach(() => {
    // Clear all storage before each test
    cy.clearShopperStorage(testStoreSlug)

    // Mock all shopper APIs
    cy.mockShopperAPIs(testStoreSlug)

    // Mock catalog and checkout APIs
    cy.fixture('catalog-items').then((catalogData) => {
      cy.intercept('GET', `**/api/shop/${testStoreSlug}/catalog*`, {
        statusCode: 200,
        body: { success: true, data: catalogData.items }
      }).as('getCatalog')
    })

    // Mock order submission
    cy.intercept('POST', `**/api/shop/${testStoreSlug}/orders`, {
      statusCode: 200,
      body: {
        success: true,
        orderId: 'order-123',
        orderNumber: 'CYP-2024-001',
        message: 'Order placed successfully'
      }
    }).as('submitOrder')
  })

  describe('New User Registration to Purchase Flow', () => {
    it('should complete the entire journey from registration to order placement', () => {
      // Step 1: Visit store and browse items
      cy.visit(`/shop/${testStoreSlug}`)
      cy.wait('@getStoreInfo')

      // Verify store is loaded
      cy.contains('Cypress Test Store').should('be.visible')
      cy.contains('Vintage Leather Jacket').should('be.visible')

      // Step 2: Add items to cart as guest
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      cy.contains('Modern Art Print').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Verify cart count
      cy.get('[data-cy="cart-count"]').should('contain', '2')

      // Step 3: View cart
      cy.get('[data-cy="cart-icon"]').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'cart')

      // Verify cart contents
      cy.contains('Vintage Leather Jacket').should('be.visible')
      cy.contains('Modern Art Print').should('be.visible')
      cy.get('[data-cy="total"]').should('contain', '$216.00') // 125 + 75 + 8% tax

      // Step 4: Proceed to checkout
      cy.get('[data-cy="checkout-btn"]').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'checkout')

      // Step 5: Register during checkout
      cy.contains('Sign in').click()
      cy.url().should('include', 'login')

      cy.contains('Create Account').click()
      cy.url().should('include', 'register')

      // Fill registration form
      cy.get('input[formControlName="fullName"]').type('Workflow Test User')
      cy.get('input[formControlName="email"]').type(testUserEmail)
      cy.get('input[formControlName="password"]').type(testUserPassword)
      cy.get('input[formControlName="confirmPassword"]').type(testUserPassword)
      cy.get('input[formControlName="phone"]').type('555-123-4567')
      cy.get('input[formControlName="address"]').type('123 Workflow St')
      cy.get('input[formControlName="city"]').type('Test City')
      cy.get('select[formControlName="state"]').select('CA')
      cy.get('input[formControlName="zipCode"]').type('90210')

      cy.get('button[type="submit"]').click()
      cy.wait('@shopperRegister')

      // Should be redirected back to checkout
      cy.shouldBeOnStorePage(testStoreSlug, 'checkout')

      // Step 6: Complete checkout form
      // Contact info should be prefilled from registration
      cy.get('input[formControlName="firstName"]').should('have.value', 'Workflow')
      cy.get('input[formControlName="lastName"]').should('have.value', 'Test User')
      cy.get('input[formControlName="email"]').should('have.value', testUserEmail)

      cy.get('[data-cy="continue-btn"]').click()

      // Fill shipping address (should be prefilled from registration)
      cy.get('input[formControlName="address"]').should('have.value', '123 Workflow St')
      cy.get('input[formControlName="city"]').should('have.value', 'Test City')
      cy.get('select[formControlName="state"]').should('have.value', 'CA')
      cy.get('input[formControlName="zipCode"]').should('have.value', '90210')

      cy.get('[data-cy="continue-btn"]').click()

      // Payment step (mock payment for MVP)
      cy.contains('Payment Integration Coming Soon').should('be.visible')
      cy.get('[data-cy="continue-btn"]').click()

      // Review order
      cy.contains('Review Your Order').should('be.visible')
      cy.contains('Workflow Test User').should('be.visible')
      cy.contains('123 Workflow St').should('be.visible')

      // Step 7: Place order
      cy.get('[data-cy="place-order-btn"]').click()
      cy.wait('@submitOrder')

      // Should show success message
      cy.contains('Order submitted successfully').should('be.visible')

      // Step 8: Navigate to account to view order
      cy.get('[data-cy="account-menu"]').click()
      cy.contains('My Account').click()

      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')
      cy.contains('Welcome back, Workflow Test User').should('be.visible')

      // Check order history
      cy.contains('Order History').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'account/orders')
    })
  })

  describe('Returning User Shopping Flow', () => {
    beforeEach(() => {
      // Login as existing user
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')
    })

    it('should complete shopping flow with saved profile', () => {
      // Step 1: Browse and add to favorites
      cy.visit(`/shop/${testStoreSlug}`)

      cy.contains('Retro Coffee Table').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="favorite-btn"]').click()
      })

      // Step 2: Add to cart
      cy.contains('Ceramic Vase Set').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Step 3: Quick checkout with saved profile
      cy.get('[data-cy="cart-icon"]').click()
      cy.get('[data-cy="checkout-btn"]').click()

      // Profile should be prefilled
      cy.get('input[formControlName="firstName"]').should('have.value', 'Workflow')
      cy.get('input[formControlName="email"]').should('have.value', testUserEmail)

      // Fast checkout flow
      cy.get('[data-cy="continue-btn"]').click() // Contact
      cy.get('[data-cy="continue-btn"]').click() // Shipping
      cy.get('[data-cy="continue-btn"]').click() // Payment
      cy.get('[data-cy="place-order-btn"]').click() // Review

      cy.wait('@submitOrder')
      cy.contains('Order submitted successfully').should('be.visible')

      // Step 4: Check favorites
      cy.visit(`/shop/${testStoreSlug}/account/favorites`)
      cy.contains('Retro Coffee Table').should('be.visible')
    })

    it('should update profile information', () => {
      cy.visit(`/shop/${testStoreSlug}/account/settings`)

      // Update profile
      cy.get('input[formControlName="phone"]').clear().type('555-999-8888')
      cy.get('input[formControlName="address"]').clear().type('456 Updated St')

      cy.get('[data-cy="save-profile-btn"]').click()

      // Should show success message
      cy.contains('Profile updated successfully').should('be.visible')

      // Verify changes are saved
      cy.reload()
      cy.get('input[formControlName="phone"]').should('have.value', '555-999-8888')
      cy.get('input[formControlName="address"]').should('have.value', '456 Updated St')
    })
  })

  describe('Guest Checkout Flow', () => {
    it('should complete guest checkout without registration', () => {
      // Step 1: Add items to cart
      cy.visit(`/shop/${testStoreSlug}`)

      cy.contains('Silk Scarf Collection').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Step 2: Proceed to checkout as guest
      cy.get('[data-cy="cart-icon"]').click()
      cy.get('[data-cy="checkout-btn"]').click()

      // Step 3: Continue as guest
      cy.contains('Continue as Guest').click()

      // Step 4: Fill guest information
      cy.get('input[formControlName="firstName"]').type('Guest')
      cy.get('input[formControlName="lastName"]').type('User')
      cy.get('input[formControlName="email"]').type(testGuestEmail)
      cy.get('input[formControlName="phone"]').type('555-555-5555')

      cy.get('[data-cy="continue-btn"]').click()

      // Step 5: Fill shipping
      cy.get('input[formControlName="address"]').type('789 Guest Ave')
      cy.get('input[formControlName="city"]').type('Guest City')
      cy.get('select[formControlName="state"]').select('NY')
      cy.get('input[formControlName="zipCode"]').type('10001')

      cy.get('[data-cy="continue-btn"]').click()

      // Step 6: Payment (mock)
      cy.get('[data-cy="continue-btn"]').click()

      // Step 7: Review and place order
      cy.contains('Guest User').should('be.visible')
      cy.contains('789 Guest Ave').should('be.visible')
      cy.get('[data-cy="place-order-btn"]').click()

      cy.wait('@submitOrder')
      cy.contains('Order submitted successfully').should('be.visible')

      // Guest should see option to create account
      cy.contains('Create an account to track your orders').should('be.visible')
    })
  })

  describe('Multi-Store Shopping', () => {
    const secondStoreSlug = 'cypress-store-two'

    beforeEach(() => {
      cy.mockShopperAPIs(secondStoreSlug)
    })

    it('should maintain separate carts for different stores', () => {
      // Add item to first store cart
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })
      cy.get('[data-cy="cart-count"]').should('contain', '1')

      // Switch to second store
      cy.visit(`/shop/${secondStoreSlug}`)
      cy.wait('@getStoreInfo')

      // Cart should be empty for second store
      cy.get('[data-cy="cart-count"]').should('not.exist')

      // Add item to second store cart
      cy.contains('Modern Art Print').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })
      cy.get('[data-cy="cart-count"]').should('contain', '1')

      // Switch back to first store - should still have original item
      cy.visit(`/shop/${testStoreSlug}`)
      cy.get('[data-cy="cart-count"]').should('contain', '1')

      cy.get('[data-cy="cart-icon"]').click()
      cy.contains('Vintage Leather Jacket').should('be.visible')
    })

    it('should require separate authentication for each store', () => {
      // Login to first store
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')

      // Should be authenticated in first store
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)
      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')

      // Switch to second store - should not be authenticated
      cy.visit(`/shop/${secondStoreSlug}/account/dashboard`)
      cy.url().should('include', `${secondStoreSlug}/login`)
    })
  })

  describe('Error Recovery', () => {
    it('should handle API failures gracefully', () => {
      // Mock API failure
      cy.intercept('POST', `**/api/shop/${testStoreSlug}/orders`, {
        statusCode: 500,
        body: { message: 'Server error' }
      }).as('orderFailure')

      // Go through checkout
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      cy.get('[data-cy="cart-icon"]').click()
      cy.get('[data-cy="checkout-btn"]').click()

      // Fill guest checkout
      cy.contains('Continue as Guest').click()
      cy.get('input[formControlName="firstName"]').type('Test')
      cy.get('input[formControlName="lastName"]').type('User')
      cy.get('input[formControlName="email"]').type('test@example.com')
      cy.get('[data-cy="continue-btn"]').click()

      cy.get('input[formControlName="address"]').type('123 Test St')
      cy.get('input[formControlName="city"]').type('Test City')
      cy.get('select[formControlName="state"]').select('CA')
      cy.get('input[formControlName="zipCode"]').type('90210')
      cy.get('[data-cy="continue-btn"]').click()

      cy.get('[data-cy="continue-btn"]').click()

      // Try to place order
      cy.get('[data-cy="place-order-btn"]').click()
      cy.wait('@orderFailure')

      // Should show error message and allow retry
      cy.contains('An error occurred while placing your order').should('be.visible')
      cy.get('[data-cy="retry-order-btn"]').should('be.visible')

      // Should preserve form data for retry
      cy.get('input[formControlName="firstName"]').should('have.value', 'Test')
    })

    it('should preserve cart contents during session interruption', () => {
      // Add items to cart
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Simulate browser refresh/navigation
      cy.reload()
      cy.visit(`/shop/${testStoreSlug}/cart`)

      // Cart should be preserved
      cy.contains('Vintage Leather Jacket').should('be.visible')
    })
  })
})