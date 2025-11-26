/// <reference types="cypress" />

describe('Owner Dashboard Tests', () => {
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

  describe('Dashboard Loading and Display', () => {
    beforeEach(function() {
      // Mock API responses for dashboard data
      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: { success: true, data: this.ownerData.providers }
      }).as('getProviders')

      cy.intercept('GET', '**/api/transactions/metrics*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            totalSales: this.ownerData.dashboardData.summary.recentSales,
            transactionCount: this.ownerData.dashboardData.summary.recentSalesCount
          }
        }
      }).as('getSalesMetrics')

      cy.intercept('GET', '**/api/payouts/pending*', {
        statusCode: 200,
        body: {
          success: true,
          data: Array(this.ownerData.dashboardData.summary.pendingPayoutCount).fill({
            id: 'payout-1',
            providerId: 'prov-001',
            pendingAmount: this.ownerData.dashboardData.summary.pendingPayouts / this.ownerData.dashboardData.summary.pendingPayoutCount
          })
        }
      }).as('getPendingPayouts')

      cy.visit('/owner/dashboard')
    })

    it('should display the dashboard header with shop name and welcome message', function() {
      cy.contains('Demo Consignment Shop Dashboard').should('be.visible')
      cy.contains('Welcome back, owner!').should('be.visible')
      cy.contains("Here's what's happening in your consignment shop").should('be.visible')
    })

    it('should load and display key metrics cards', function() {
      cy.wait(['@getProviders', '@getSalesMetrics', '@getPendingPayouts'])

      // Active Providers metric
      cy.get('.metric-card.providers').within(() => {
        cy.contains('Active Providers').should('be.visible')
        cy.get('.metric-value').should('contain', this.ownerData.providers.filter(p => p.isActive).length)
        cy.get('.metric-icon').should('contain', '👥')
      })

      // Inventory Value metric
      cy.get('.metric-card.inventory-value').within(() => {
        cy.contains('Inventory Value').should('be.visible')
        cy.get('.metric-value').should('contain', '$42,750.80')
        cy.contains('342 items on floor').should('be.visible')
        cy.get('.metric-icon').should('contain', '💎')
      })

      // Recent Sales metric
      cy.get('.metric-card.recent-sales').within(() => {
        cy.contains('Last 30 Days').should('be.visible')
        cy.get('.metric-value').should('contain', '$8,945.67')
        cy.contains('23 transactions').should('be.visible')
        cy.get('.metric-icon').should('contain', '📊')
      })

      // Pending Payouts metric
      cy.get('.metric-card.pending-payouts').within(() => {
        cy.contains('Pending Payouts').should('be.visible')
        cy.get('.metric-value').should('contain', '$3,247.60')
        cy.contains('8 providers waiting').should('be.visible')
        cy.get('.metric-icon').should('contain', '⏳')
      })
    })

    it('should display loading state before data is loaded', () => {
      // Visit page without waiting for API calls
      cy.visit('/owner/dashboard')
      cy.contains('Loading dashboard data...').should('be.visible')
    })

    it('should handle API errors gracefully', () => {
      // Mock failed API responses
      cy.intercept('GET', '**/api/providers*', { statusCode: 500 }).as('getProvidersError')
      cy.intercept('GET', '**/api/transactions/metrics*', { statusCode: 500 }).as('getSalesMetricsError')
      cy.intercept('GET', '**/api/payouts/pending*', { statusCode: 500 }).as('getPendingPayoutsError')

      cy.visit('/owner/dashboard')
      cy.wait(['@getProvidersError', '@getSalesMetricsError', '@getPendingPayoutsError'])

      // Should still show metrics with fallback data
      cy.get('.metrics-grid').should('be.visible')
      cy.get('.metric-card').should('have.length', 4)
    })
  })

  describe('Quick Actions Section', () => {
    beforeEach(() => {
      cy.visit('/owner/dashboard')
    })

    it('should display all quick action cards', () => {
      cy.get('.actions-section').within(() => {
        cy.contains('Quick Actions').should('be.visible')

        // Check all action cards
        cy.get('.action-card').should('have.length', 6)

        // Process Sale
        cy.get('.action-card').contains('Process Sale').should('be.visible')
        cy.contains('Record a new transaction and automatically calculate splits').should('be.visible')

        // Manage Providers
        cy.get('.action-card').contains('Manage Providers').should('be.visible')
        cy.contains('View providers, update commission rates, and track performance').should('be.visible')

        // Inventory Check
        cy.get('.action-card').contains('Inventory Check').should('be.visible')
        cy.contains('Review current stock levels and add new items').should('be.visible')

        // Generate Payouts
        cy.get('.action-card').contains('Generate Payouts').should('be.visible')
        cy.contains('Create payout reports and process provider payments').should('be.visible')

        // View Reports
        cy.get('.action-card').contains('View Reports').should('be.visible')
        cy.contains('Analyze sales trends, provider performance, and profits').should('be.visible')

        // Shop Settings
        cy.get('.action-card').contains('Shop Settings').should('be.visible')
        cy.contains('Configure integrations, manage settings, and preferences').should('be.visible')
      })
    })

    it('should navigate to correct routes when action cards are clicked', () => {
      // Test navigation for a few key actions
      cy.get('.action-card').contains('Process Sale').click()
      cy.url().should('include', '/owner/transactions')

      cy.go('back')

      cy.get('.action-card').contains('Manage Providers').click()
      cy.url().should('include', '/owner/providers')

      cy.go('back')

      cy.get('.action-card').contains('Generate Payouts').click()
      cy.url().should('include', '/owner/payouts')
    })

    it('should have hover effects on action cards', () => {
      cy.get('.action-card').first().trigger('mouseover')
      cy.get('.action-card').first().should('have.css', 'transform').and('include', 'matrix')
    })
  })

  describe('Recent Transactions Section', () => {
    beforeEach(function() {
      // Mock API with recent transactions
      const dashboardSummary = {
        ...this.ownerData.dashboardData.summary,
        recentTransactions: this.ownerData.dashboardData.summary.recentTransactions
      }

      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: { success: true, data: this.ownerData.providers }
      }).as('getProviders')

      cy.intercept('GET', '**/api/transactions/metrics*', {
        statusCode: 200,
        body: { success: true, data: dashboardSummary }
      }).as('getSalesMetrics')

      cy.intercept('GET', '**/api/payouts/pending*', {
        statusCode: 200,
        body: { success: true, data: [] }
      }).as('getPendingPayouts')

      cy.visit('/owner/dashboard')
      cy.wait(['@getProviders', '@getSalesMetrics', '@getPendingPayouts'])
    })

    it('should display recent transactions table when data is available', function() {
      cy.get('.activity-section').within(() => {
        cy.contains('Recent Transactions').should('be.visible')

        // Check table headers
        cy.get('.table-header').within(() => {
          cy.contains('Date').should('be.visible')
          cy.contains('Item').should('be.visible')
          cy.contains('Provider').should('be.visible')
          cy.contains('Sale Amount').should('be.visible')
          cy.contains('Commission').should('be.visible')
        })

        // Check transaction rows
        cy.get('.transaction-row').should('have.length', this.ownerData.dashboardData.summary.recentTransactions.length)

        // Check first transaction
        cy.get('.transaction-row').first().within(() => {
          cy.contains('Vintage Leather Jacket').should('be.visible')
          cy.contains('Sarah Thompson').should('be.visible')
          cy.contains('$125.00').should('be.visible')
          cy.contains('$62.50').should('be.visible')
        })
      })
    })

    it('should format transaction dates correctly', function() {
      cy.get('.transaction-row').first().within(() => {
        // Should display formatted date
        cy.get('.col-date').should('contain', 'Nov 20, 2024')
      })
    })

    it('should format currency amounts correctly', function() {
      cy.get('.transaction-row').each((row, index) => {
        const transaction = this.ownerData.dashboardData.summary.recentTransactions[index]
        cy.wrap(row).within(() => {
          cy.contains(`$${transaction.amount.toFixed(2)}`).should('be.visible')
          cy.contains(`$${transaction.commission.toFixed(2)}`).should('be.visible')
        })
      })
    })
  })

  describe('Pending Payouts Integration', () => {
    it('should highlight pending payouts card when there are pending payouts', function() {
      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: { success: true, data: this.ownerData.providers }
      }).as('getProviders')

      cy.intercept('GET', '**/api/transactions/metrics*', {
        statusCode: 200,
        body: { success: true, data: { totalSales: 0, transactionCount: 0 } }
      }).as('getSalesMetrics')

      cy.intercept('GET', '**/api/payouts/pending*', {
        statusCode: 200,
        body: {
          success: true,
          data: [
            { id: 'payout-1', providerId: 'prov-001', pendingAmount: 100.00 },
            { id: 'payout-2', providerId: 'prov-002', pendingAmount: 150.00 }
          ]
        }
      }).as('getPendingPayouts')

      cy.visit('/owner/dashboard')
      cy.wait(['@getProviders', '@getSalesMetrics', '@getPendingPayouts'])

      cy.get('.metric-card.pending-payouts').should('have.class', 'has-pending')
      cy.contains('→ Click to process').should('be.visible')
    })

    it('should navigate to payouts page when pending payouts card is clicked', () => {
      cy.visit('/owner/dashboard')
      cy.get('.metric-card.pending-payouts').click()
      cy.url().should('include', '/owner/payouts')
    })
  })

  describe('Responsive Design', () => {
    beforeEach(() => {
      cy.visit('/owner/dashboard')
    })

    it('should display correctly on mobile viewport', function() {
      cy.viewport('iphone-x')

      // Dashboard should be responsive
      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.metrics-grid').should('be.visible')
      cy.get('.action-grid').should('be.visible')

      // Metrics should stack vertically
      cy.get('.metric-card').should('be.visible')
      cy.get('.action-card').should('be.visible')
    })

    it('should display correctly on tablet viewport', function() {
      cy.viewport('ipad-2')

      cy.get('.owner-dashboard').should('be.visible')
      cy.get('.metrics-grid').should('be.visible')
      cy.get('.actions-section').should('be.visible')
    })

    it('should handle transactions table responsively', function() {
      cy.viewport('iphone-x')

      // On mobile, transaction table should adapt
      cy.get('.transactions-table').should('be.visible')
    })
  })

  describe('Dashboard Navigation', () => {
    beforeEach(() => {
      cy.visit('/owner/dashboard')
    })

    it('should be accessible directly via /owner/dashboard route', () => {
      cy.url().should('include', '/owner/dashboard')
      cy.contains('Dashboard').should('be.visible')
    })

    it('should redirect from /owner to /owner/dashboard', () => {
      cy.visit('/owner')
      cy.url().should('include', '/owner/dashboard')
    })

    it('should maintain authentication state', () => {
      // Verify auth data is still present
      cy.window().then((win) => {
        expect(win.localStorage.getItem('auth_token')).to.exist
        expect(win.localStorage.getItem('user_data')).to.exist
      })
    })
  })
})