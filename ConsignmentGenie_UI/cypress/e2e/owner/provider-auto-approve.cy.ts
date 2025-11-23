/// <reference types="cypress" />

describe('Provider Auto-Approval Settings Tests', () => {
  beforeEach(() => {
    cy.fixture('owner-data').as('ownerData')
    cy.fixture('users').as('users')

    // Mock authentication - set up as shop owner
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

  describe('Organization Settings API', () => {
    it('should get organization settings with auto-approve status', () => {
      cy.intercept('GET', '**/api/dashboard/organization/settings', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            autoApproveProviders: true,
            storeCodeEnabled: true,
            storeCode: "DEMO"
          }
        }
      }).as('getOrgSettings')

      cy.request('GET', '/api/dashboard/organization/settings')
      cy.wait('@getOrgSettings')
    })

    it('should update auto-approve setting via API', () => {
      cy.intercept('PUT', '**/api/dashboard/organization/settings/auto-approve', {
        statusCode: 200,
        body: {
          success: true,
          message: "Provider auto-approval enabled. New providers will be automatically approved.",
          data: { autoApproveProviders: true }
        }
      }).as('updateAutoApprove')

      cy.request('PUT', '/api/dashboard/organization/settings/auto-approve', {
        autoApproveProviders: true
      })
      cy.wait('@updateAutoApprove')
    })
  })

  describe('Settings UI Integration', () => {
    beforeEach(() => {
      // Mock organization settings API
      cy.intercept('GET', '**/api/dashboard/organization/settings', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            autoApproveProviders: true,
            storeCodeEnabled: true,
            storeCode: "DEMO"
          }
        }
      }).as('getOrgSettings')

      cy.intercept('PUT', '**/api/dashboard/organization/settings/auto-approve', {
        statusCode: 200,
        body: {
          success: true,
          message: "Provider auto-approval setting updated successfully.",
          data: { autoApproveProviders: true }
        }
      }).as('updateAutoApprove')
    })

    it('should display auto-approve toggle in settings page', () => {
      cy.visit('/owner/settings')
      cy.wait('@getOrgSettings')

      cy.get('[data-cy="auto-approve-section"]').within(() => {
        cy.contains('Provider Auto-Approval').should('be.visible')
        cy.contains('Automatically approve new providers with valid store codes').should('be.visible')
        cy.get('[data-cy="auto-approve-toggle"]').should('be.visible')
      })
    })

    it('should toggle auto-approve setting', () => {
      cy.visit('/owner/settings')
      cy.wait('@getOrgSettings')

      // Toggle should be enabled initially
      cy.get('[data-cy="auto-approve-toggle"]').should('be.checked')

      // Click to disable
      cy.get('[data-cy="auto-approve-toggle"]').click()
      cy.wait('@updateAutoApprove')

      // Should show confirmation message
      cy.contains('Auto-approval setting updated').should('be.visible')
    })

    it('should show correct help text for auto-approve options', () => {
      cy.visit('/owner/settings')
      cy.wait('@getOrgSettings')

      cy.get('[data-cy="auto-approve-section"]').within(() => {
        cy.contains('When enabled, providers with valid store codes will be automatically approved').should('be.visible')
        cy.contains('When disabled, you must manually approve each provider request').should('be.visible')
      })
    })

    it('should handle API errors gracefully', () => {
      cy.intercept('PUT', '**/api/dashboard/organization/settings/auto-approve', {
        statusCode: 500,
        body: { success: false, message: 'Server error' }
      }).as('updateAutoApproveError')

      cy.visit('/owner/settings')
      cy.wait('@getOrgSettings')

      cy.get('[data-cy="auto-approve-toggle"]').click()
      cy.wait('@updateAutoApproveError')

      cy.contains('Failed to update setting').should('be.visible')
    })
  })

  describe('Provider Registration Flow with Auto-Approve', () => {
    beforeEach(() => {
      // Mock store code validation
      cy.intercept('GET', '**/api/registration/validate-store-code/DEMO', {
        statusCode: 200,
        body: {
          isValid: true,
          shopName: "Demo Consignment Shop"
        }
      }).as('validateStoreCode')
    })

    it('should register provider with auto-approve enabled', () => {
      cy.intercept('POST', '**/api/registration/register/provider', {
        statusCode: 200,
        body: {
          success: true,
          message: "Registration successful! Welcome to Demo Consignment Shop. You can now start adding items."
        }
      }).as('registerProviderAutoApprove')

      cy.visit('/provider/register')

      // Fill in registration form
      cy.get('[data-cy="store-code-input"]').type('DEMO')
      cy.get('[data-cy="validate-store-code"]').click()
      cy.wait('@validateStoreCode')

      cy.get('[data-cy="full-name-input"]').type('John Doe')
      cy.get('[data-cy="email-input"]').type('john.doe@test.com')
      cy.get('[data-cy="phone-input"]').type('555-123-4567')
      cy.get('[data-cy="password-input"]').type('password123')
      cy.get('[data-cy="payment-details-input"]').type('Check preferred')

      cy.get('[data-cy="register-submit"]').click()
      cy.wait('@registerProviderAutoApprove')

      // Should show auto-approved success message
      cy.contains('Welcome to Demo Consignment Shop').should('be.visible')
      cy.contains('You can now start adding items').should('be.visible')
    })

    it('should register provider with auto-approve disabled', () => {
      cy.intercept('POST', '**/api/registration/register/provider', {
        statusCode: 200,
        body: {
          success: true,
          message: "Registration successful! Your request to join Demo Consignment Shop is pending approval."
        }
      }).as('registerProviderManualApprove')

      cy.visit('/provider/register')

      // Fill in registration form
      cy.get('[data-cy="store-code-input"]').type('DEMO')
      cy.get('[data-cy="validate-store-code"]').click()
      cy.wait('@validateStoreCode')

      cy.get('[data-cy="full-name-input"]').type('Jane Smith')
      cy.get('[data-cy="email-input"]').type('jane.smith@test.com')
      cy.get('[data-cy="phone-input"]').type('555-987-6543')
      cy.get('[data-cy="password-input"]').type('password123')
      cy.get('[data-cy="payment-details-input"]').type('PayPal preferred')

      cy.get('[data-cy="register-submit"]').click()
      cy.wait('@registerProviderManualApprove')

      // Should show pending approval message
      cy.contains('Your request to join Demo Consignment Shop is pending approval').should('be.visible')
      cy.contains('You will be notified when your account is approved').should('be.visible')
    })

    it('should handle registration validation errors', () => {
      cy.visit('/provider/register')

      // Try to submit with empty form
      cy.get('[data-cy="register-submit"]').click()

      // Should show validation errors
      cy.contains('Store code is required').should('be.visible')
      cy.contains('Full name is required').should('be.visible')
      cy.contains('Email is required').should('be.visible')
      cy.contains('Password is required').should('be.visible')
    })
  })

  describe('Provider Management with Auto-Approve', () => {
    beforeEach(() => {
      // Mock providers list with different statuses
      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: {
          success: true,
          data: [
            {
              id: 'prov-001',
              displayName: 'John Doe (Auto-Approved)',
              email: 'john@test.com',
              status: 'Active',
              createdAt: new Date().toISOString(),
              isAutoApproved: true
            },
            {
              id: 'prov-002',
              displayName: 'Jane Smith (Pending)',
              email: 'jane@test.com',
              status: 'Pending',
              createdAt: new Date().toISOString(),
              isAutoApproved: false
            }
          ]
        }
      }).as('getProviders')
    })

    it('should display provider approval status indicators', () => {
      cy.visit('/owner/providers')
      cy.wait('@getProviders')

      // Should show auto-approved provider with indicator
      cy.get('[data-cy="provider-prov-001"]').within(() => {
        cy.contains('John Doe (Auto-Approved)').should('be.visible')
        cy.get('[data-cy="auto-approved-badge"]').should('be.visible')
        cy.contains('Active').should('be.visible')
      })

      // Should show pending provider without auto-approved indicator
      cy.get('[data-cy="provider-prov-002"]').within(() => {
        cy.contains('Jane Smith (Pending)').should('be.visible')
        cy.contains('Pending').should('be.visible')
        cy.get('[data-cy="approve-button"]').should('be.visible')
      })
    })

    it('should filter providers by approval status', () => {
      cy.visit('/owner/providers')
      cy.wait('@getProviders')

      // Filter for auto-approved providers
      cy.get('[data-cy="filter-auto-approved"]').click()
      cy.get('[data-cy="provider-prov-001"]').should('be.visible')
      cy.get('[data-cy="provider-prov-002"]').should('not.be.visible')

      // Filter for pending providers
      cy.get('[data-cy="filter-pending"]').click()
      cy.get('[data-cy="provider-prov-001"]').should('not.be.visible')
      cy.get('[data-cy="provider-prov-002"]').should('be.visible')

      // Clear filters
      cy.get('[data-cy="filter-all"]').click()
      cy.get('[data-cy="provider-prov-001"]').should('be.visible')
      cy.get('[data-cy="provider-prov-002"]').should('be.visible')
    })
  })

  describe('Dashboard Analytics for Auto-Approve', () => {
    beforeEach(() => {
      cy.intercept('GET', '**/api/dashboard/organization/settings', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            autoApproveProviders: true,
            storeCodeEnabled: true,
            storeCode: "DEMO"
          }
        }
      }).as('getOrgSettings')

      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: {
          success: true,
          data: [
            { id: 'p1', status: 'Active', isAutoApproved: true },
            { id: 'p2', status: 'Active', isAutoApproved: true },
            { id: 'p3', status: 'Pending', isAutoApproved: false }
          ]
        }
      }).as('getProviders')

      cy.intercept('GET', '**/api/transactions/metrics*', {
        statusCode: 200,
        body: { success: true, data: { totalSales: 1000, transactionCount: 5 } }
      }).as('getMetrics')

      cy.intercept('GET', '**/api/payouts/pending*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getPendingPayouts')
    })

    it('should show auto-approve status in dashboard summary', () => {
      cy.visit('/owner/dashboard')
      cy.wait(['@getOrgSettings', '@getProviders', '@getMetrics', '@getPendingPayouts'])

      cy.get('.provider-metrics').within(() => {
        cy.contains('Auto-Approval: Enabled').should('be.visible')
        cy.contains('2 auto-approved').should('be.visible')
        cy.contains('1 pending manual review').should('be.visible')
      })
    })

    it('should display auto-approve efficiency metrics', () => {
      cy.visit('/owner/analytics')
      cy.wait(['@getProviders'])

      cy.get('.approval-metrics').within(() => {
        cy.contains('Provider Approval Efficiency').should('be.visible')
        cy.contains('67% auto-approved').should('be.visible') // 2 out of 3
        cy.contains('Average approval time: Instant').should('be.visible')
      })
    })
  })

  describe('Mobile Responsiveness', () => {
    beforeEach(() => {
      cy.intercept('GET', '**/api/dashboard/organization/settings', {
        statusCode: 200,
        body: {
          success: true,
          data: { autoApproveProviders: true, storeCodeEnabled: true, storeCode: "DEMO" }
        }
      }).as('getOrgSettings')
    })

    it('should display auto-approve toggle correctly on mobile', () => {
      cy.viewport('iphone-x')
      cy.visit('/owner/settings')
      cy.wait('@getOrgSettings')

      cy.get('[data-cy="auto-approve-section"]').should('be.visible')
      cy.get('[data-cy="auto-approve-toggle"]').should('be.visible')
      cy.contains('Provider Auto-Approval').should('be.visible')
    })

    it('should handle provider registration on mobile', () => {
      cy.viewport('iphone-x')
      cy.visit('/provider/register')

      cy.get('[data-cy="store-code-input"]').should('be.visible')
      cy.get('[data-cy="full-name-input"]').should('be.visible')
      cy.get('[data-cy="register-submit"]').should('be.visible')
    })
  })

  describe('Integration with Notifications', () => {
    it('should show notification when auto-approve setting changes', () => {
      cy.intercept('PUT', '**/api/dashboard/organization/settings/auto-approve', {
        statusCode: 200,
        body: {
          success: true,
          message: "Provider auto-approval disabled. New providers will require manual approval.",
          data: { autoApproveProviders: false }
        }
      }).as('disableAutoApprove')

      cy.visit('/owner/settings')

      cy.get('[data-cy="auto-approve-toggle"]').click()
      cy.wait('@disableAutoApprove')

      // Should show toast notification
      cy.get('[data-cy="toast-notification"]').should('be.visible')
      cy.contains('New providers will require manual approval').should('be.visible')
    })

    it('should show owner notification when provider auto-registers', () => {
      // This would be handled by real-time notifications in the actual app
      cy.visit('/owner/dashboard')

      // Mock receiving a notification
      cy.window().then((win) => {
        win.postMessage({
          type: 'PROVIDER_AUTO_APPROVED',
          data: {
            providerName: 'John Doe',
            email: 'john@test.com'
          }
        })
      })

      cy.get('[data-cy="notification-bell"]').should('have.class', 'has-notifications')
      cy.get('[data-cy="notification-bell"]').click()
      cy.contains('John Doe has been automatically approved').should('be.visible')
    })
  })
})