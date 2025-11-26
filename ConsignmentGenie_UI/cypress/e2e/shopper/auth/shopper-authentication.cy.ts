describe('Shopper Authentication Flow', () => {
  const testStoreSlug = Cypress.env('testStoreSlug') || 'cypress-test-store'
  const testUserEmail = Cypress.env('testUserEmail') || 'cypress.shopper@example.com'
  const testUserPassword = Cypress.env('testUserPassword') || 'CypressTest123!'

  beforeEach(() => {
    // Clear localStorage before each test
    cy.clearShopperStorage(testStoreSlug)

    // Mock shopper APIs
    cy.mockShopperAPIs(testStoreSlug)
  })

  describe('Store Access', () => {
    it('should display store information on store page', () => {
      cy.visit(`/shop/${testStoreSlug}`)
      cy.wait('@getStoreInfo')

      // Should display store branding
      cy.get('[data-cy="store-header"]').should('be.visible')
      cy.contains('Cypress Test Store').should('be.visible')

      // Should show navigation menu
      cy.get('[data-cy="store-nav"]').should('be.visible')
      cy.get('[data-cy="cart-icon"]').should('be.visible')
      cy.get('[data-cy="account-menu"]').should('be.visible')
    })

    it('should handle non-existent store gracefully', () => {
      cy.intercept('GET', '**/api/shop/nonexistent-store/info', {
        statusCode: 404,
        body: { message: 'Store not found' }
      })

      cy.visit('/shop/nonexistent-store', { failOnStatusCode: false })

      // Should redirect or show error message
      cy.url().should('not.include', '/shop/nonexistent-store')
    })

    it('should handle inactive store', () => {
      cy.intercept('GET', `**/api/shop/${testStoreSlug}/info`, {
        statusCode: 404,
        body: { message: 'Store is not available' }
      })

      cy.visit(`/shop/${testStoreSlug}`, { failOnStatusCode: false })

      // Should not show store content
      cy.contains('Store is not available').should('be.visible')
    })
  })

  describe('Shopper Registration', () => {
    it('should successfully register a new shopper', () => {
      const newUser = {
        fullName: 'New Cypress User',
        email: 'new.cypress.user@example.com',
        password: 'NewPassword123!',
        phone: '555-123-4567',
        address: '123 New St',
        city: 'New City',
        state: 'CA',
        zipCode: '90210'
      }

      cy.shopperRegister(testStoreSlug, newUser)
      cy.wait('@shopperRegister')

      // Should redirect to store homepage after registration
      cy.shouldBeOnStorePage(testStoreSlug)

      // Should show user as authenticated
      cy.get('[data-cy="account-menu"]').click()
      cy.contains('New Cypress User').should('be.visible')
    })

    it('should validate required fields during registration', () => {
      cy.visit(`/shop/${testStoreSlug}/register`)

      // Try to submit with empty required fields
      cy.get('button[type="submit"]').click()

      // Should show validation errors
      cy.contains('Full name is required').should('be.visible')
      cy.contains('Email is required').should('be.visible')
      cy.contains('Password is required').should('be.visible')
    })

    it('should validate email format', () => {
      cy.visit(`/shop/${testStoreSlug}/register`)

      cy.get('input[formControlName="email"]').type('invalid-email')
      cy.get('input[formControlName="email"]').blur()

      cy.contains('Please enter a valid email').should('be.visible')
    })

    it('should validate password confirmation match', () => {
      cy.visit(`/shop/${testStoreSlug}/register`)

      cy.get('input[formControlName="password"]').type('Password123!')
      cy.get('input[formControlName="confirmPassword"]').type('DifferentPassword')
      cy.get('input[formControlName="confirmPassword"]').blur()

      cy.contains('Passwords do not match').should('be.visible')
    })

    it('should handle registration errors', () => {
      cy.intercept('POST', `**/api/shop/${testStoreSlug}/auth/register`, {
        statusCode: 400,
        body: {
          success: false,
          errorMessage: 'Email already exists'
        }
      })

      const existingUser = {
        fullName: 'Existing User',
        email: 'existing@example.com',
        password: 'Password123!'
      }

      cy.shopperRegister(testStoreSlug, existingUser)

      // Should display error message
      cy.contains('Email already exists').should('be.visible')

      // Should remain on registration page
      cy.url().should('include', 'register')
    })
  })

  describe('Shopper Login', () => {
    it('should successfully login with valid credentials', () => {
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')

      // Should redirect to store homepage
      cy.shouldBeOnStorePage(testStoreSlug)

      // Should show user as authenticated
      cy.get('[data-cy="account-menu"]').click()
      cy.contains('Cypress Test User').should('be.visible')
      cy.contains('My Account').should('be.visible')
      cy.contains('Logout').should('be.visible')
    })

    it('should handle invalid credentials', () => {
      cy.intercept('POST', `**/api/shop/${testStoreSlug}/auth/login`, {
        statusCode: 401,
        body: {
          success: false,
          errorMessage: 'Invalid email or password'
        }
      })

      cy.shopperLogin(testStoreSlug, 'wrong@email.com', 'wrongpassword')

      // Should display error message
      cy.contains('Invalid email or password').should('be.visible')

      // Should remain on login page
      cy.url().should('include', 'login')
    })

    it('should validate required fields during login', () => {
      cy.visit(`/shop/${testStoreSlug}/login`)

      // Try to submit with empty fields
      cy.get('button[type="submit"]').click()

      // Should show validation errors
      cy.contains('Email is required').should('be.visible')
      cy.contains('Password is required').should('be.visible')
    })

    it('should toggle password visibility', () => {
      cy.visit(`/shop/${testStoreSlug}/login`)

      cy.get('input[formControlName="password"]').should('have.attr', 'type', 'password')
      cy.get('[data-cy="password-toggle"]').click()
      cy.get('input[formControlName="password"]').should('have.attr', 'type', 'text')
    })

    it('should remember login credentials when remember me is checked', () => {
      cy.visit(`/shop/${testStoreSlug}/login`)

      cy.get('input[formControlName="email"]').type(testUserEmail)
      cy.get('input[formControlName="password"]').type(testUserPassword)
      cy.get('input[formControlName="rememberMe"]').check()
      cy.get('button[type="submit"]').click()

      cy.wait('@shopperLogin')

      // Verify localStorage has remember flag
      cy.window().then((win) => {
        const token = win.localStorage.getItem(`shopper_token_${testStoreSlug}`)
        expect(token).to.exist
      })
    })
  })

  describe('Logout', () => {
    beforeEach(() => {
      // Login before each logout test
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')
    })

    it('should successfully logout', () => {
      cy.get('[data-cy="account-menu"]').click()
      cy.get('[data-cy="logout-btn"]').click()

      // Should redirect to login page
      cy.url().should('include', 'login')

      // Should clear authentication state
      cy.window().then((win) => {
        const token = win.localStorage.getItem(`shopper_token_${testStoreSlug}`)
        expect(token).to.be.null
      })
    })
  })

  describe('Protected Routes', () => {
    it('should redirect unauthenticated users to login', () => {
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)

      // Should redirect to login with return URL
      cy.url().should('include', 'login')
      cy.url().should('include', 'returnUrl=')
    })

    it('should redirect to original URL after login', () => {
      // Try to access protected page
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)

      // Should be on login page
      cy.url().should('include', 'login')

      // Login
      cy.get('input[formControlName="email"]').type(testUserEmail)
      cy.get('input[formControlName="password"]').type(testUserPassword)
      cy.get('button[type="submit"]').click()

      cy.wait('@shopperLogin')

      // Should redirect back to original page
      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')
    })
  })

  describe('Guest Checkout Flow', () => {
    it('should allow guest checkout without registration', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Navigate to checkout as guest
      cy.contains('Continue as Guest').click()

      // Should be redirected to checkout page
      cy.shouldBeOnStorePage(testStoreSlug, 'checkout')

      // Should show guest checkout form
      cy.contains('Checkout').should('be.visible')
      cy.contains('Sign in for faster checkout, or continue as guest').should('be.visible')
    })

    it('should create guest session for checkout', () => {
      cy.intercept('POST', `**/api/shop/${testStoreSlug}/auth/guest`, {
        statusCode: 200,
        body: {
          success: true,
          token: 'guest-jwt-token',
          sessionId: 'guest-session-123'
        }
      }).as('createGuestSession')

      cy.visit(`/shop/${testStoreSlug}/checkout`)

      // Should automatically create guest session
      cy.wait('@createGuestSession')

      // Should show checkout form
      cy.contains('Contact Information').should('be.visible')
    })
  })

  describe('Multi-Store Authentication', () => {
    const secondStoreSlug = 'cypress-store-two'

    beforeEach(() => {
      cy.mockShopperAPIs(secondStoreSlug)
    })

    it('should maintain separate authentication for different stores', () => {
      // Login to first store
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')

      // Should be authenticated in first store
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)
      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')

      // Visit second store (not authenticated)
      cy.visit(`/shop/${secondStoreSlug}/account/dashboard`)
      cy.url().should('include', `${secondStoreSlug}/login`)

      // Should still be authenticated in first store
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)
      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')
    })
  })
})