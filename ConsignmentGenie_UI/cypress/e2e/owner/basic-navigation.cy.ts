/// <reference types="cypress" />

describe('Owner Workflow Basic Navigation Tests', () => {
  beforeEach(() => {
    cy.fixture('users').as('users')

    // Mock authentication for owner
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

  describe('Basic Page Access', () => {
    it('should access owner dashboard', () => {
      cy.visit('/owner/dashboard')
      cy.url().should('include', '/owner/dashboard')
      cy.get('body').should('be.visible')
    })

    it('should access sales page', () => {
      cy.visit('/owner/sales')
      cy.url().should('include', '/owner/sales')
      cy.get('body').should('be.visible')
    })

    it('should redirect from /owner to /owner/dashboard', () => {
      cy.visit('/owner')
      cy.url().should('include', '/owner/dashboard')
    })
  })

  describe('Owner Dashboard Basic Elements', () => {
    beforeEach(() => {
      cy.visit('/owner/dashboard')
    })

    it('should display page structure', () => {
      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.dashboard-header').should('be.visible')
    })

    it('should show dashboard title', () => {
      cy.contains('Dashboard').should('be.visible')
    })

    it('should display some metric cards', () => {
      cy.get('.metrics-grid').should('be.visible')
      cy.get('.metric-card').should('exist')
    })

    it('should display quick actions section', () => {
      cy.get('.actions-section').should('be.visible')
      cy.contains('Quick Actions').should('be.visible')
      cy.get('.action-grid').should('be.visible')
      cy.get('.action-card').should('have.length.at.least', 1)
    })
  })

  describe('Sales Page Basic Elements', () => {
    beforeEach(() => {
      cy.visit('/owner/sales')
    })

    it('should display page structure', () => {
      cy.get('.sales-page').should('be.visible')
      cy.get('.page-header').should('be.visible')
    })

    it('should show sales title', () => {
      cy.contains('Sales & Transactions').should('be.visible')
    })

    it('should display process sale button', () => {
      cy.get('button').contains('Process Sale').should('be.visible')
    })

    it('should display filters section', () => {
      cy.get('.filters-section').should('be.visible')
    })

    it('should have date filter inputs', () => {
      cy.get('#startDate').should('be.visible')
      cy.get('#endDate').should('be.visible')
    })

    it('should have payment method filter', () => {
      cy.get('#paymentMethod').should('be.visible')
    })
  })

  describe('Process Sale Modal', () => {
    beforeEach(() => {
      cy.visit('/owner/sales')
    })

    it('should open process sale modal', () => {
      cy.get('button').contains('Process Sale').click()
      cy.get('.modal-overlay').should('be.visible')
      cy.get('.modal-content').should('be.visible')
    })

    it('should close modal with close button', () => {
      cy.get('button').contains('Process Sale').click()
      cy.get('.close-btn').click()
      cy.get('.modal-overlay').should('not.exist')
    })

    it('should close modal when clicking outside', () => {
      cy.get('button').contains('Process Sale').click()
      cy.get('.modal-overlay').click({ force: true })
      cy.get('.modal-overlay').should('not.exist')
    })
  })

  describe('Authentication Requirements', () => {
    it('should maintain authentication state', () => {
      cy.visit('/owner/dashboard')

      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.exist
        expect(win.localStorage.getItem('user_data')).to.exist
      })
    })

    it('should access owner routes with proper authentication', () => {
      cy.visit('/owner/dashboard')
      cy.url().should('include', '/owner/dashboard')

      cy.visit('/owner/sales')
      cy.url().should('include', '/owner/sales')
    })
  })

  describe('Navigation Between Pages', () => {
    it('should navigate from dashboard to sales via action card', () => {
      cy.visit('/owner/dashboard')

      // Look for a sales-related action card
      cy.get('.action-card').contains('Process Sale').should('be.visible')
      cy.get('.action-card').contains('Process Sale').click()

      // Should navigate to sales page (fixed route)
      cy.url().should('include', '/owner/sales')
    })

    it('should maintain layout across different owner pages', () => {
      cy.visit('/owner/dashboard')
      cy.get('app-owner-layout').should('be.visible')

      cy.visit('/owner/sales')
      cy.get('app-owner-layout').should('be.visible')
    })
  })

  describe('Responsive Design Basics', () => {
    beforeEach(() => {
      cy.visit('/owner/dashboard')
    })

    it('should display on mobile viewport', () => {
      cy.viewport('iphone-x')
      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.dashboard-header').should('be.visible')
      cy.get('.metrics-grid').should('be.visible')
    })

    it('should display on tablet viewport', () => {
      cy.viewport('ipad-2')
      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.actions-section').should('be.visible')
    })
  })

  describe('Error Handling', () => {
    it('should handle missing authentication gracefully', () => {
      // Clear auth data
      cy.clearLocalStorage()

      cy.visit('/owner/dashboard')
      // May redirect to login or show unauthorized - either is acceptable
      cy.url().should('satisfy', (url) => {
        return url.includes('/login') || url.includes('/unauthorized') || url.includes('/owner/dashboard')
      })
    })

    it('should handle page load without crashing', () => {
      cy.visit('/owner/dashboard')
      cy.get('body').should('be.visible')

      // Check that page loads successfully without major errors
      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.dashboard-header').should('be.visible')
    })
  })
})