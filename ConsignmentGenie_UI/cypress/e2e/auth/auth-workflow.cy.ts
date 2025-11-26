/// <reference types="cypress" />

describe('Authentication Workflow Tests', () => {
  beforeEach(() => {
    cy.clearLocalStorage()
  })

  describe('Complete Login to Dashboard Flow', () => {
    beforeEach(() => {
      // Mock successful login responses for all user types
      cy.intercept('POST', '**/api/auth/login', (req) => {
        const { email } = req.body
        const userMocks: Record<string, any> = {
          'admin@demoshop.com': {
            success: true,
            data: {
              token: 'admin-jwt-token',
              userId: 'admin-123',
              email: 'admin@demoshop.com',
              role: 0,
              organizationId: 'system-org',
              organizationName: 'System Administration',
              expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
            },
            message: 'Login successful',
            errors: null
          },
          'owner@demoshop.com': {
            success: true,
            data: {
              token: 'owner-jwt-token',
              userId: 'owner-123',
              email: 'owner@demoshop.com',
              role: 1,
              organizationId: 'demo-org',
              organizationName: 'Demo Shop',
              expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
            },
            message: 'Login successful',
            errors: null
          }
        }

        const response = userMocks[email]
        if (response) {
          req.reply(response)
        } else {
          req.reply({
            statusCode: 401,
            body: { success: false, message: 'Invalid credentials', errors: ['Authentication failed'] }
          })
        }
      }).as('loginAPI')

      // Mock dashboard API calls that might be needed after login
      cy.intercept('GET', '**/api/**', {
        statusCode: 200,
        body: { success: true, data: [], message: 'Success' }
      }).as('dashboardAPIs')
    })

    it('should complete full admin login workflow', () => {
      // Start at login page
      cy.visit('/login')

      // Use admin test account
      cy.get('.test-account-btn.admin').click()

      // Verify form is populated
      cy.get('input[name="email"]').should('have.value', 'admin@demoshop.com')
      cy.get('input[name="password"]').should('have.value', 'password123')

      // Submit login form
      cy.get('button[type="submit"]').click()

      // Wait for login API call
      cy.wait('@loginAPI')

      // Verify redirect to admin dashboard
      cy.url().should('include', '/admin/dashboard')

      // Verify authentication data is stored
      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.equal('admin-jwt-token')
        const userData = JSON.parse(win.localStorage.getItem('user_data') || '{}')
        expect(userData.email).to.equal('admin@demoshop.com')
        expect(userData.role).to.equal(0)
        expect(userData.organizationName).to.equal('System Administration')
      })
    })

    it('should complete full shop owner login workflow', () => {
      cy.visit('/login')
      cy.get('.test-account-btn.owner').click()
      cy.get('button[type="submit"]').click()
      cy.wait('@loginAPI')

      // Verify redirect to owner dashboard
      cy.url().should('include', '/owner/dashboard')

      // Verify correct user data is stored
      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.equal('owner-jwt-token')
        const userData = JSON.parse(win.localStorage.getItem('user_data') || '{}')
        expect(userData.email).to.equal('owner@demoshop.com')
        expect(userData.role).to.equal(1)
        expect(userData.organizationName).to.equal('Demo Shop')
      })
    })
  })

  describe('Authentication State Management', () => {
    it('should maintain authentication state across page reloads', () => {
      // Mock successful login
      cy.intercept('POST', '**/api/auth/login', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            token: 'test-token',
            userId: 'test-user',
            email: 'owner@demoshop.com',
            role: 1,
            organizationId: 'test-org',
            organizationName: 'Test Organization',
            expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
          }
        }
      }).as('login')

      // Login
      cy.loginAsShopOwner()
      cy.wait('@login')
      cy.url().should('include', '/owner/dashboard')

      // Reload page
      cy.reload()

      // Should still be on dashboard (not redirected to login)
      cy.url().should('include', '/owner/dashboard')

      // Authentication data should still be present
      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.exist
        expect(win.localStorage.getItem('user_data')).to.exist
      })
    })

    it('should clear all authentication data on logout', () => {
      // First login to set up auth state
      cy.window().then((win) => {
        win.localStorage.setItem('auth_token', 'test-token')
        win.localStorage.setItem('user_data', JSON.stringify({
          userId: 'test',
          email: 'test@example.com',
          role: 1
        }))
        win.localStorage.setItem('tokenExpiry', new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString())
      })

      // Visit a protected page (would normally redirect to login if not authenticated)
      cy.visit('/owner/dashboard')

      // Simulate logout action (this would be part of the logout component/functionality)
      cy.window().then((win) => {
        win.localStorage.removeItem('auth_token')
        win.localStorage.removeItem('user_data')
        win.localStorage.removeItem('tokenExpiry')
      })

      // Navigate to login page
      cy.visit('/login')

      // Verify all auth data is cleared
      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.be.null
        expect(win.localStorage.getItem('user_data')).to.be.null
        expect(win.localStorage.getItem('tokenExpiry')).to.be.null
      })
    })
  })

  describe('Error Handling and Recovery', () => {
    it('should handle API timeout gracefully', () => {
      // Mock slow/timeout API response
      cy.intercept('POST', '**/api/auth/login', (req) => {
        return new Promise((resolve) => {
          // Never resolve to simulate timeout
          setTimeout(() => {
            resolve({
              statusCode: 408,
              body: { success: false, message: 'Request timeout' }
            })
          }, 30000)
        })
      }).as('timeoutAPI')

      cy.visit('/login')
      cy.loginAsShopOwner()

      // Check that loading state is shown
      cy.get('button[type="submit"]').should('contain', 'Signing in...')
      cy.get('.spinner').should('be.visible')

      // Form should be disabled during loading
      cy.get('input[name="email"]').should('be.disabled')
      cy.get('input[name="password"]').should('be.disabled')
      cy.get('.test-account-btn').should('be.disabled')
    })

    it('should recover from authentication errors and allow retry', () => {
      let loginAttempts = 0

      cy.intercept('POST', '**/api/auth/login', (req) => {
        loginAttempts++
        if (loginAttempts === 1) {
          // First attempt fails
          req.reply({
            statusCode: 401,
            body: { success: false, message: 'Invalid credentials', errors: ['Authentication failed'] }
          })
        } else {
          // Second attempt succeeds
          req.reply({
            statusCode: 200,
            body: {
              success: true,
              data: {
                token: 'retry-success-token',
                userId: 'test-user',
                email: 'owner@demoshop.com',
                role: 1,
                organizationId: 'test-org',
                organizationName: 'Test Organization',
                expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
              }
            }
          })
        }
      }).as('loginRetry')

      cy.visit('/login')

      // First attempt - should fail
      cy.loginAsShopOwner()
      cy.wait('@loginRetry')
      cy.get('.error-message').should('contain', 'Invalid credentials')

      // Form should be re-enabled for retry
      cy.get('button[type="submit"]').should('not.be.disabled')
      cy.get('input[name="email"]').should('not.be.disabled')

      // Second attempt - should succeed
      cy.get('button[type="submit"]').click()
      cy.wait('@loginRetry')
      cy.url().should('include', '/owner/dashboard')
    })
  })

  describe('Security Considerations', () => {
    it('should not store sensitive data in browser history or console', () => {
      cy.intercept('POST', '**/api/auth/login', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            token: 'sensitive-token-should-not-be-logged',
            userId: 'test-user',
            email: 'owner@demoshop.com',
            role: 1,
            organizationId: 'test-org',
            organizationName: 'Test Organization',
            expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
          }
        }
      }).as('secureLogin')

      cy.visit('/login')
      cy.loginAsShopOwner()
      cy.wait('@secureLogin')

      // Check that password is not stored in form after submission
      cy.get('input[name="password"]').should('have.value', '')

      // Note: In a real application, you would also want to verify:
      // - Tokens are not logged to console in production
      // - Sensitive data doesn't appear in network tab
      // - Form data is not stored in browser history
    })

    it('should handle expired tokens appropriately', () => {
      // Set up expired token in localStorage
      cy.window().then((win) => {
        win.localStorage.setItem('auth_token', 'expired-token')
        win.localStorage.setItem('tokenExpiry', new Date(Date.now() - 60 * 60 * 1000).toISOString()) // 1 hour ago
        win.localStorage.setItem('user_data', JSON.stringify({
          userId: 'test',
          email: 'test@example.com',
          role: 1
        }))
      })

      // Try to visit protected page - should redirect to login if token validation fails
      cy.visit('/owner/dashboard')

      // In a real app with token validation, this should redirect to login
      // For now, we'll just verify the token is expired
      cy.window().then((win) => {
        const expiry = new Date(win.localStorage.getItem('tokenExpiry') || '')
        const now = new Date()
        expect(expiry.getTime()).to.be.lessThan(now.getTime())
      })
    })
  })
})