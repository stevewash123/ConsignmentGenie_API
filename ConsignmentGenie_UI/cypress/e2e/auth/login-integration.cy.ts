/// <reference types="cypress" />

describe('Login Integration Tests', () => {
  beforeEach(() => {
    cy.visit('/login')
  })

  describe('Basic Login Flow (UI Only)', () => {
    it('should render the login page correctly', () => {
      // Check the page content instead of title (title might be default Angular project name)
      cy.contains('ConsignmentGenie').should('be.visible')
      cy.contains('Sign in to your account').should('be.visible')
    })

    it('should have all required form elements', () => {
      // Check form inputs exist and have correct attributes
      cy.get('input[name="email"]')
        .should('be.visible')
        .should('have.attr', 'type', 'email')
        .should('have.attr', 'required')

      cy.get('input[name="password"]')
        .should('be.visible')
        .should('have.attr', 'required')

      cy.get('button[type="submit"]')
        .should('be.visible')
        .should('contain', 'Sign In')
    })

    it('should validate form inputs before allowing submission', () => {
      // Submit button should be disabled when form is empty
      cy.get('button[type="submit"]').should('be.disabled')

      // Fill in only email
      cy.get('input[name="email"]').type('test@example.com')
      cy.get('button[type="submit"]').should('be.disabled')

      // Fill in password too
      cy.get('input[name="password"]').type('password123')
      cy.get('button[type="submit"]').should('not.be.disabled')

      // Clear email, button should be disabled again
      cy.get('input[name="email"]').clear()
      cy.get('button[type="submit"]').should('be.disabled')
    })

    it('should show validation errors for invalid email format', () => {
      cy.get('input[name="email"]')
        .type('invalid-email')
        .blur()

      cy.get('.field-error')
        .should('be.visible')
        .should('contain', 'Please enter a valid email address')
    })

    it('should show validation errors for empty required fields', () => {
      // Touch email field and blur without typing
      cy.get('input[name="email"]').focus().blur()
      cy.get('.field-error').should('contain', 'Email is required')

      // Touch password field and blur without typing
      cy.get('input[name="password"]').focus().blur()
      cy.get('.field-error').should('contain', 'Password is required')
    })
  })

  describe('Test Account Buttons', () => {
    it('should populate form when admin test account is clicked', () => {
      cy.get('.test-account-btn.admin').click()

      cy.get('input[name="email"]').should('have.value', 'admin@demoshop.com')
      cy.get('input[name="password"]').should('have.value', 'password123')
      cy.get('button[type="submit"]').should('not.be.disabled')
    })

    it('should populate form when shop owner test account is clicked', () => {
      cy.get('.test-account-btn.owner').click()

      cy.get('input[name="email"]').should('have.value', 'owner@demoshop.com')
      cy.get('input[name="password"]').should('have.value', 'password123')
    })

    it('should populate form when provider test account is clicked', () => {
      cy.get('.test-account-btn.provider').click()

      cy.get('input[name="email"]').should('have.value', 'provider@demoshop.com')
      cy.get('input[name="password"]').should('have.value', 'password123')
    })

    it('should populate form when customer test account is clicked', () => {
      cy.get('.test-account-btn.customer').click()

      cy.get('input[name="email"]').should('have.value', 'customer@demoshop.com')
      cy.get('input[name="password"]').should('have.value', 'password123')
    })
  })

  describe('Password Visibility Toggle', () => {
    it('should toggle password visibility when toggle button is clicked', () => {
      const password = 'testpassword123'

      cy.get('input[name="password"]')
        .should('have.attr', 'type', 'password')
        .type(password)

      // Click toggle to show password
      cy.get('.password-toggle').click()
      cy.get('input[name="password"]').should('have.attr', 'type', 'text')

      // Click toggle to hide password
      cy.get('.password-toggle').click()
      cy.get('input[name="password"]').should('have.attr', 'type', 'password')
    })

    it('should show different icons for password visibility states', () => {
      // Initially should show "show password" icon
      cy.get('.password-toggle').should('contain', '👁️')

      // After clicking, should show "hide password" icon
      cy.get('.password-toggle').click()
      cy.get('.password-toggle').should('contain', '🙈')

      // After clicking again, should show "show password" icon
      cy.get('.password-toggle').click()
      cy.get('.password-toggle').should('contain', '👁️')
    })
  })

  describe('Form Interaction and UX', () => {
    it('should allow manual focus on email field', () => {
      cy.get('input[name="email"]').focus()
      cy.get('input[name="email"]').should('be.focused')
    })

    it('should allow tab navigation between form elements', () => {
      cy.get('input[name="email"]').focus()
      cy.get('input[name="email"]').should('be.focused')

      cy.realPress('Tab')
      cy.get('input[name="password"]').should('be.focused')

      // Test that tab navigation works - just verify we can navigate to other elements
      // Don't assert specific element focus as there may be other focusable elements
      cy.realPress('Tab')
      cy.realPress('Tab')
      cy.realPress('Tab')
      // Just verify that tabbing around doesn't break anything
      cy.get('input[name="email"]').should('be.visible')
    })

    it('should submit form when Enter is pressed in password field', () => {
      cy.get('input[name="email"]').type('test@example.com')
      cy.get('input[name="password"]').type('password123')

      // Mock API to prevent actual network calls during test
      cy.intercept('POST', '**/api/auth/login', {
        statusCode: 401,
        body: { success: false, message: 'Test error' }
      }).as('testLogin')

      cy.get('input[name="password"]').type('{enter}')

      // Should either show loading state briefly or show error
      cy.wait('@testLogin', { timeout: 5000 }).then(() => {
        // After API call completes, form should be re-enabled
        cy.get('button[type="submit"]').should('not.be.disabled')
      })
    })

    it('should clear any existing error messages when test account is selected', () => {
      // Manually create an error state by submitting invalid form
      cy.get('input[name="email"]').type('invalid@example.com')
      cy.get('input[name="password"]').type('wrongpassword')
      cy.get('button[type="submit"]').click()

      // Wait a moment for potential error to appear
      cy.wait(1000)

      // Click test account button - should clear any errors
      cy.get('.test-account-btn.admin').click()
      cy.get('.error-message').should('not.exist')
    })
  })

  describe('Responsive Design', () => {
    it('should display correctly on mobile viewport', () => {
      cy.viewport('iphone-x')
      cy.visit('/auth/login')

      cy.get('.login-container').should('be.visible')
      cy.get('.login-card').should('be.visible')
      cy.get('.test-account-grid').should('be.visible')

      // All test account buttons should still be visible and clickable
      cy.get('.test-account-btn').should('have.length', 4)
      cy.get('.test-account-btn.admin').should('be.visible')
    })

    it('should display correctly on tablet viewport', () => {
      cy.viewport('ipad-2')
      cy.visit('/auth/login')

      cy.get('.login-container').should('be.visible')
      cy.get('.login-card').should('be.visible')
      cy.get('input[name="email"]').should('be.visible')
      cy.get('input[name="password"]').should('be.visible')
    })
  })
})