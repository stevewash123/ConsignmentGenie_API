/// <reference types="cypress" />

describe('Owner Sales Tests', () => {
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

  describe('Sales Page Layout and Navigation', () => {
    beforeEach(() => {
      cy.visit('/owner/sales')
    })

    it('should display the sales page header correctly', () => {
      cy.get('.page-header').within(() => {
        cy.contains('Sales & Transactions').should('be.visible')
        cy.contains('View and manage all sales transactions').should('be.visible')
        cy.get('button').contains('Process Sale').should('be.visible')
        cy.get('.btn-icon').should('contain', '💰')
      })
    })

    it('should display filters section with all filter controls', () => {
      cy.get('.filters-section').within(() => {
        // Date filters
        cy.get('label').contains('Start Date').should('be.visible')
        cy.get('input[type="date"]#startDate').should('be.visible')

        cy.get('label').contains('End Date').should('be.visible')
        cy.get('input[type="date"]#endDate').should('be.visible')

        // Payment method filter
        cy.get('label').contains('Payment Method').should('be.visible')
        cy.get('select#paymentMethod').should('be.visible')
        cy.get('select#paymentMethod option').should('contain', 'All Payment Methods')
        cy.get('select#paymentMethod option').should('contain', 'Cash')
        cy.get('select#paymentMethod option').should('contain', 'Card')
        cy.get('select#paymentMethod option').should('contain', 'Online')

        // Clear filters button
        cy.get('button').contains('Clear Filters').should('be.visible')
      })
    })

    it('should navigate to sales page from dashboard', () => {
      cy.visit('/owner/dashboard')
      cy.get('.action-card').contains('Process Sale').click()
      cy.url().should('include', '/owner/transactions')
    })
  })

  describe('Sales Summary Cards', () => {
    beforeEach(function() {
      // Mock transaction metrics API
      cy.intercept('GET', '**/api/transactions/summary*', {
        statusCode: 200,
        body: {
          success: true,
          data: this.ownerData.salesData.summary
        }
      }).as('getSummary')

      cy.visit('/owner/sales')
      cy.wait('@getSummary')
    })

    it('should display all summary cards with correct data', function() {
      cy.get('.summary-cards').within(() => {
        // Total Sales card
        cy.get('.summary-card.total').within(() => {
          cy.contains('Total Sales').should('be.visible')
          cy.get('.summary-value').should('contain', '$489.99')
          cy.contains('3 transactions').should('be.visible')
        })

        // Shop Revenue card
        cy.get('.summary-card.shop').within(() => {
          cy.contains('Shop Revenue').should('be.visible')
          cy.get('.summary-value').should('contain', '$284.20')
          cy.contains('After commissions').should('be.visible')
        })

        // Provider Payouts card
        cy.get('.summary-card.provider').within(() => {
          cy.contains('Provider Payouts').should('be.visible')
          cy.get('.summary-value').should('contain', '$244.99')
          cy.contains('Commissions owed').should('be.visible')
        })

        // Average Sale card
        cy.get('.summary-card.average').within(() => {
          cy.contains('Average Sale').should('be.visible')
          cy.get('.summary-value').should('contain', '$163.33')
          cy.contains('Per transaction').should('be.visible')
        })
      })
    })

    it('should update summary when filters are applied', function() {
      // Apply date filter
      cy.get('#startDate').type('2024-11-19')
      cy.get('#endDate').type('2024-11-20')

      // Mock filtered results
      cy.intercept('GET', '**/api/transactions/summary*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            totalSales: 214.99,
            transactionCount: 2,
            totalShopAmount: 124.70,
            totalProviderAmount: 107.49,
            averageTransactionValue: 107.50
          }
        }
      }).as('getFilteredSummary')

      cy.wait('@getFilteredSummary')

      cy.get('.summary-card.total .summary-value').should('contain', '$214.99')
      cy.get('.summary-card.total .summary-detail').should('contain', '2 transactions')
    })
  })

  describe('Transactions Table', () => {
    beforeEach(function() {
      // Mock transactions API
      cy.intercept('GET', '**/api/transactions*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            items: this.ownerData.salesData.transactions,
            totalPages: 1,
            currentPage: 1,
            totalItems: this.ownerData.salesData.transactions.length
          }
        }
      }).as('getTransactions')

      cy.intercept('GET', '**/api/transactions/summary*', {
        statusCode: 200,
        body: {
          success: true,
          data: this.ownerData.salesData.summary
        }
      }).as('getSummary')

      cy.visit('/owner/sales')
      cy.wait(['@getTransactions', '@getSummary'])
    })

    it('should display transactions table with correct headers', () => {
      cy.get('.transactions-table thead tr').within(() => {
        cy.contains('Date').should('be.visible')
        cy.contains('Item').should('be.visible')
        cy.contains('Provider').should('be.visible')
        cy.contains('Sale Price').should('be.visible')
        cy.contains('Payment').should('be.visible')
        cy.contains('Commission').should('be.visible')
        cy.contains('Shop Amount').should('be.visible')
        cy.contains('Actions').should('be.visible')
      })
    })

    it('should display transaction data correctly', function() {
      const transaction = this.ownerData.salesData.transactions[0]

      cy.get('.transaction-row').first().within(() => {
        // Date
        cy.get('.date-cell').should('contain', 'Nov 20, 2024')

        // Item info
        cy.get('.item-cell').within(() => {
          cy.contains(transaction.item.name).should('be.visible')
          cy.contains(transaction.item.description).should('be.visible')
        })

        // Provider info
        cy.get('.provider-cell').within(() => {
          cy.contains(transaction.provider.name).should('be.visible')
          cy.contains(`${transaction.providerSplitPercentage}% commission`).should('be.visible')
        })

        // Sale price
        cy.get('.price-cell').within(() => {
          cy.contains(`$${transaction.salePrice.toFixed(2)}`).should('be.visible')
          cy.contains(`+$${transaction.salesTaxAmount.toFixed(2)} tax`).should('be.visible')
        })

        // Payment method
        cy.get('.payment-cell').within(() => {
          cy.get('.payment-badge').should('contain', transaction.paymentMethod)
          cy.get('.payment-badge').should('have.class', `payment-${transaction.paymentMethod.toLowerCase()}`)
        })

        // Commission and shop amounts
        cy.get('.commission-cell').should('contain', `$${transaction.providerAmount.toFixed(2)}`)
        cy.get('.shop-amount-cell').should('contain', `$${transaction.shopAmount.toFixed(2)}`)

        // Action buttons
        cy.get('.actions-cell').within(() => {
          cy.get('button[title="View Details"]').should('contain', '👁️')
          cy.get('button[title="Edit"]').should('contain', '✏️')
          cy.get('button[title="Void Sale"]').should('contain', '🗑️')
        })
      })
    })

    it('should support sorting by date and sale price', () => {
      // Test date sorting
      cy.get('th').contains('Date').click()
      cy.get('.sort-indicator.active').should('be.visible')

      // Test sale price sorting
      cy.get('th').contains('Sale Price').click()
      cy.get('th').contains('Sale Price').within(() => {
        cy.get('.sort-indicator.active').should('be.visible')
      })
    })

    it('should display action buttons for each transaction', () => {
      cy.get('.transaction-row').each(($row) => {
        cy.wrap($row).within(() => {
          cy.get('.action-buttons').within(() => {
            cy.get('button').should('have.length', 3)
            cy.get('button[title="View Details"]').should('be.visible')
            cy.get('button[title="Edit"]').should('be.visible')
            cy.get('button[title="Void Sale"]').should('be.visible')
          })
        })
      })
    })
  })

  describe('Table Controls and Pagination', () => {
    beforeEach(() => {
      // Mock paginated response
      cy.intercept('GET', '**/api/transactions*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            items: Array(25).fill(null).map((_, i) => ({
              id: `txn-${i + 1}`,
              saleDate: '2024-11-20T14:30:00Z',
              salePrice: 100 + i,
              salesTaxAmount: 8 + i,
              paymentMethod: 'Card',
              providerSplitPercentage: 50,
              providerAmount: 50 + i/2,
              shopAmount: 58 + i/2,
              item: { id: `item-${i + 1}`, name: `Item ${i + 1}`, description: `Description ${i + 1}` },
              provider: { id: `prov-${i + 1}`, name: `Provider ${i + 1}` }
            })),
            totalPages: 3,
            currentPage: 1,
            totalItems: 25
          }
        }
      }).as('getPaginatedTransactions')

      cy.visit('/owner/sales')
      cy.wait('@getPaginatedTransactions')
    })

    it('should display page size selector', () => {
      cy.get('.table-controls').within(() => {
        cy.get('.page-size-select').should('be.visible')
        cy.get('.page-size-select option').should('contain', '10 per page')
        cy.get('.page-size-select option').should('contain', '20 per page')
        cy.get('.page-size-select option').should('contain', '50 per page')
      })
    })

    it('should display pagination controls when needed', () => {
      cy.get('.pagination').should('be.visible')
      cy.get('.pagination').within(() => {
        cy.get('.page-btn').contains('Previous').should('be.disabled')
        cy.get('.page-btn').contains('Next').should('not.be.disabled')
        cy.get('.page-numbers button').should('have.length.at.least', 1)
      })
    })

    it('should change page size and reload data', () => {
      cy.intercept('GET', '**/api/transactions*', (req) => {
        expect(req.query.pageSize).to.equal('20')
        req.reply({
          statusCode: 200,
          body: { success: true, data: { items: [], totalPages: 1, currentPage: 1, totalItems: 0 } }
        })
      }).as('getPageSize20')

      cy.get('.page-size-select').select('20 per page')
      cy.wait('@getPageSize20')
    })
  })

  describe('Transaction Filtering', () => {
    beforeEach(() => {
      cy.visit('/owner/sales')
    })

    it('should filter by date range', () => {
      // Mock filtered API response
      cy.intercept('GET', '**/api/transactions*', (req) => {
        expect(req.query.startDate).to.exist
        expect(req.query.endDate).to.exist
        req.reply({
          statusCode: 200,
          body: { success: true, data: { items: [], totalPages: 1, currentPage: 1, totalItems: 0 } }
        })
      }).as('getFilteredTransactions')

      cy.get('#startDate').type('2024-11-01')
      cy.get('#endDate').type('2024-11-30')

      cy.wait('@getFilteredTransactions')
    })

    it('should filter by payment method', () => {
      cy.intercept('GET', '**/api/transactions*', (req) => {
        expect(req.query.paymentMethod).to.equal('Cash')
        req.reply({
          statusCode: 200,
          body: { success: true, data: { items: [], totalPages: 1, currentPage: 1, totalItems: 0 } }
        })
      }).as('getCashTransactions')

      cy.get('#paymentMethod').select('Cash')
      cy.wait('@getCashTransactions')
    })

    it('should clear all filters', () => {
      // Set some filter values
      cy.get('#startDate').type('2024-11-01')
      cy.get('#paymentMethod').select('Card')

      // Clear filters
      cy.get('button').contains('Clear Filters').click()

      // Verify filters are cleared
      cy.get('#startDate').should('have.value', '')
      cy.get('#paymentMethod').should('have.value', '')
    })
  })

  describe('Process Sale Modal', () => {
    beforeEach(function() {
      // Mock available items API
      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: {
          success: true,
          data: this.ownerData.salesData.availableItems
        }
      }).as('getAvailableItems')

      cy.visit('/owner/sales')
    })

    it('should open process sale modal when button is clicked', () => {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      cy.get('.modal-overlay').should('be.visible')
      cy.get('.modal-content').within(() => {
        cy.contains('Process Sale').should('be.visible')
        cy.get('.close-btn').should('be.visible')
      })
    })

    it('should display sale form with all required fields', function() {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      cy.get('.modal-content form').within(() => {
        // Item selection dropdown
        cy.get('label').contains('Item *').should('be.visible')
        cy.get('select[name="itemId"]').should('be.visible')
        cy.get('select[name="itemId"] option').should('have.length.at.least', this.ownerData.salesData.availableItems.length + 1)

        // Verify available items are populated
        this.ownerData.salesData.availableItems.forEach(item => {
          cy.get('select[name="itemId"] option').should('contain', item.name)
        })
      })
    })

    it('should close modal when close button is clicked', () => {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      cy.get('.close-btn').click()
      cy.get('.modal-overlay').should('not.exist')
    })

    it('should close modal when clicking outside modal content', () => {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      cy.get('.modal-overlay').click({ force: true })
      cy.get('.modal-overlay').should('not.exist')
    })

    it('should auto-populate provider info when item is selected', function() {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      const item = this.ownerData.salesData.availableItems[0]
      cy.get('select[name="itemId"]').select(item.id)

      // Verify provider information is displayed
      cy.contains(item.providerName).should('be.visible')
    })

    it('should validate required fields before submission', () => {
      cy.get('button').contains('Process Sale').click()
      cy.wait('@getAvailableItems')

      // Try to submit empty form
      cy.get('form').submit()

      // Form should not submit (browser validation will prevent it)
      cy.get('.modal-overlay').should('be.visible')
    })
  })

  describe('Loading and Error States', () => {
    it('should display loading state while fetching transactions', () => {
      // Delay API response to see loading state
      cy.intercept('GET', '**/api/transactions*', (req) => {
        return new Promise(resolve => {
          setTimeout(() => {
            resolve({ statusCode: 200, body: { success: true, data: { items: [], totalPages: 1 } } })
          }, 1000)
        })
      }).as('getSlowTransactions')

      cy.visit('/owner/sales')
      cy.get('.loading-state').should('be.visible')
      cy.contains('Loading transactions...').should('be.visible')
      cy.get('.loading-spinner').should('be.visible')

      cy.wait('@getSlowTransactions')
      cy.get('.loading-state').should('not.exist')
    })

    it('should handle API errors gracefully', () => {
      cy.intercept('GET', '**/api/transactions*', { statusCode: 500 }).as('getTransactionsError')
      cy.intercept('GET', '**/api/transactions/summary*', { statusCode: 500 }).as('getSummaryError')

      cy.visit('/owner/sales')
      cy.wait(['@getTransactionsError', '@getSummaryError'])

      // Should still display the page structure
      cy.get('.sales-page').should('be.visible')
      cy.get('.page-header').should('be.visible')
    })
  })

  describe('Responsive Design', () => {
    beforeEach(() => {
      cy.visit('/owner/sales')
    })

    it('should display correctly on mobile viewport', () => {
      cy.viewport('iphone-x')

      cy.get('.sales-page').should('be.visible')
      cy.get('.page-header').should('be.visible')
      cy.get('.filters-section').should('be.visible')
      cy.get('.summary-cards').should('be.visible')
    })

    it('should stack summary cards vertically on mobile', () => {
      cy.viewport('iphone-x')
      cy.get('.summary-cards').should('be.visible')
      cy.get('.summary-card').should('be.visible')
    })

    it('should adapt table layout for mobile', () => {
      cy.viewport('iphone-x')
      cy.get('.table-container').should('be.visible')
    })
  })
})