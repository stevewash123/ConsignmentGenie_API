/// <reference types="cypress" />

describe('Edit Transaction Tests', () => {
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

  describe('Opening Edit Transaction Modal', () => {
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

    it('should open edit modal when edit button is clicked', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })

      cy.get('.modal-overlay').should('be.visible')
      cy.get('.edit-modal-content').should('be.visible')
      cy.get('.modal-header h2').should('contain', 'Edit Transaction')
    })

    it('should open edit modal from view details modal', () => {
      // First open view details
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="View Details"]').click()
      })

      cy.get('.modal-overlay').should('be.visible')

      // Then click edit from detail modal
      cy.get('button').contains('Edit Transaction').click()

      cy.get('.edit-modal-content').should('be.visible')
      cy.get('.modal-header h2').should('contain', 'Edit Transaction')
    })

    it('should close edit modal when close button is clicked', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })

      cy.get('.edit-modal-content .close-btn').click()
      cy.get('.modal-overlay').should('not.exist')
    })

    it('should close edit modal when clicking outside', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })

      cy.get('.modal-overlay').click({ force: true })
      cy.get('.modal-overlay').should('not.exist')
    })
  })

  describe('Edit Transaction Form', () => {
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

      // Open edit modal for first transaction
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })
    })

    it('should pre-populate form with transaction data', function() {
      const transaction = this.ownerData.salesData.transactions[0]

      cy.get('.edit-modal-content form').within(() => {
        // Check that fields are pre-populated
        cy.get('input[name="saleDate"]').should('have.value', '2024-11-20')
        cy.get('input[name="salePrice"]').should('have.value', transaction.salePrice.toString())
        cy.get('input[name="salesTaxAmount"]').should('have.value', transaction.salesTaxAmount.toString())
        cy.get('select[name="paymentMethod"]').should('have.value', transaction.paymentMethod)
        cy.get('input[name="providerSplitPercentage"]').should('have.value', transaction.providerSplitPercentage.toString())
      })
    })

    it('should display item and provider information', function() {
      const transaction = this.ownerData.salesData.transactions[0]

      cy.get('.edit-modal-content').within(() => {
        // Item info should be displayed (read-only)
        cy.contains(transaction.item.name).should('be.visible')
        cy.contains(transaction.item.description).should('be.visible')

        // Provider info should be displayed
        cy.contains(transaction.provider.name).should('be.visible')
      })
    })

    it('should validate required fields', () => {
      cy.get('.edit-modal-content form').within(() => {
        // Clear required fields
        cy.get('input[name="salePrice"]').clear()
        cy.get('input[name="salesTaxAmount"]').clear()

        // Try to submit
        cy.get('button[type="submit"]').click()

        // Form should not submit due to validation
        cy.get('input[name="salePrice"]').should('be.focused')
      })
    })

    it('should validate numeric fields accept only numbers', () => {
      cy.get('.edit-modal-content form').within(() => {
        // Test sale price
        cy.get('input[name="salePrice"]').clear().type('invalid')
        cy.get('input[name="salePrice"]').should('have.value', '')

        cy.get('input[name="salePrice"]').clear().type('150.50')
        cy.get('input[name="salePrice"]').should('have.value', '150.50')

        // Test provider split percentage
        cy.get('input[name="providerSplitPercentage"]').clear().type('invalid')
        cy.get('input[name="providerSplitPercentage"]').should('have.value', '')

        cy.get('input[name="providerSplitPercentage"]').clear().type('55')
        cy.get('input[name="providerSplitPercentage"]').should('have.value', '55')
      })
    })

    it('should validate percentage field range', () => {
      cy.get('.edit-modal-content form').within(() => {
        // Test values outside valid range
        cy.get('input[name="providerSplitPercentage"]').clear().type('150')
        cy.get('input[name="providerSplitPercentage"]').should('have.attr', 'max', '100')

        cy.get('input[name="providerSplitPercentage"]').clear().type('-5')
        cy.get('input[name="providerSplitPercentage"]').should('have.attr', 'min', '0')
      })
    })

    it('should update calculations automatically when values change', function() {
      cy.get('.edit-modal-content form').within(() => {
        // Change sale price
        cy.get('input[name="salePrice"]').clear().type('200.00')

        // Check if calculations update
        cy.get('.breakdown-section').should('be.visible')
        cy.contains('Calculation Breakdown').should('be.visible')
      })
    })
  })

  describe('Transaction Calculations', () => {
    beforeEach(function() {
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

      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })
    })

    it('should display calculation breakdown section', () => {
      cy.get('.breakdown-section').should('be.visible')
      cy.contains('Calculation Breakdown').should('be.visible')

      cy.get('.breakdown-grid').within(() => {
        cy.contains('Total Sale Amount').should('be.visible')
        cy.contains('Provider Amount').should('be.visible')
        cy.contains('Shop Amount').should('be.visible')
      })
    })

    it('should calculate provider and shop amounts correctly', function() {
      const originalData = this.ownerData.editTransactionData.original

      cy.get('.edit-modal-content form').within(() => {
        // Set known values
        cy.get('input[name="salePrice"]').clear().type(originalData.salePrice.toString())
        cy.get('input[name="salesTaxAmount"]').clear().type(originalData.salesTaxAmount.toString())
        cy.get('input[name="providerSplitPercentage"]').clear().type(originalData.providerSplitPercentage.toString())
      })

      // Check calculations
      cy.get('.breakdown-grid').within(() => {
        cy.contains('$135.00').should('be.visible') // Total: 125 + 10
        cy.contains('$62.50').should('be.visible')  // Provider: 125 * 0.5
        cy.contains('$72.50').should('be.visible')  // Shop: 72.50 (total - provider)
      })
    })

    it('should update calculations when form values change', function() {
      const updatedData = this.ownerData.editTransactionData.updated

      cy.get('.edit-modal-content form').within(() => {
        // Update values
        cy.get('input[name="salePrice"]').clear().type(updatedData.salePrice.toString())
        cy.get('input[name="salesTaxAmount"]').clear().type(updatedData.salesTaxAmount.toString())
        cy.get('input[name="providerSplitPercentage"]').clear().type(updatedData.providerSplitPercentage.toString())
      })

      // Check updated calculations
      cy.get('.breakdown-grid').within(() => {
        cy.contains('$151.20').should('be.visible') // Total: 140 + 11.20
        cy.contains('$77.00').should('be.visible')  // Provider: 140 * 0.55
        cy.contains('$74.20').should('be.visible')  // Shop: 151.20 - 77.00
      })
    })
  })

  describe('Saving Transaction Changes', () => {
    beforeEach(function() {
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

      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Edit"]').click()
      })
    })

    it('should save changes successfully', function() {
      // Mock successful update API
      cy.intercept('PUT', '**/api/transactions/*', {
        statusCode: 200,
        body: { success: true, message: 'Transaction updated successfully' }
      }).as('updateTransaction')

      // Mock refreshed transactions list
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
      }).as('getRefreshedTransactions')

      cy.get('.edit-modal-content form').within(() => {
        // Make some changes
        cy.get('input[name="salePrice"]').clear().type('150.00')
        cy.get('select[name="paymentMethod"]').select('Cash')

        // Submit form
        cy.get('button').contains('Save Changes').click()
      })

      cy.wait('@updateTransaction')
      cy.wait('@getRefreshedTransactions')

      // Modal should close
      cy.get('.modal-overlay').should('not.exist')

      // Should show success message (if using toast notifications)
      // cy.contains('Transaction updated successfully').should('be.visible')
    })

    it('should handle save errors gracefully', function() {
      // Mock failed update API
      cy.intercept('PUT', '**/api/transactions/*', {
        statusCode: 400,
        body: { success: false, message: 'Invalid transaction data' }
      }).as('updateTransactionError')

      cy.get('.edit-modal-content form').within(() => {
        cy.get('input[name="salePrice"]').clear().type('150.00')
        cy.get('button').contains('Save Changes').click()
      })

      cy.wait('@updateTransactionError')

      // Modal should remain open
      cy.get('.modal-overlay').should('be.visible')

      // Should show error message
      cy.contains('Failed to update transaction').should('be.visible')
    })

    it('should show loading state during save', function() {
      // Mock slow API response
      cy.intercept('PUT', '**/api/transactions/*', (req) => {
        return new Promise(resolve => {
          setTimeout(() => {
            resolve({ statusCode: 200, body: { success: true } })
          }, 1000)
        })
      }).as('slowUpdateTransaction')

      cy.get('.edit-modal-content form').within(() => {
        cy.get('input[name="salePrice"]').clear().type('150.00')
        cy.get('button').contains('Save Changes').click()

        // Should show loading state
        cy.get('button').contains('Saving...').should('be.visible')
        cy.get('button').contains('Saving...').should('be.disabled')
      })

      cy.wait('@slowUpdateTransaction')
    })

    it('should cancel changes and close modal', () => {
      cy.get('.edit-modal-content form').within(() => {
        // Make some changes
        cy.get('input[name="salePrice"]').clear().type('150.00')

        // Click cancel
        cy.get('button').contains('Cancel').click()
      })

      // Modal should close without saving
      cy.get('.modal-overlay').should('not.exist')
    })

    it('should validate all required fields before saving', () => {
      cy.get('.edit-modal-content form').within(() => {
        // Clear required field
        cy.get('input[name="salePrice"]').clear()

        // Try to save
        cy.get('button').contains('Save Changes').click()

        // Should show validation error (browser native validation)
        cy.get('input[name="salePrice"]').should('be.focused')
      })

      // Modal should remain open
      cy.get('.modal-overlay').should('be.visible')
    })
  })

  describe('Delete Transaction', () => {
    beforeEach(function() {
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

    it('should show confirmation dialog when void sale is clicked', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Void Sale"]').click()
      })

      // Should show browser confirmation dialog
      cy.on('window:confirm', (str) => {
        expect(str).to.include('Are you sure you want to void this sale?')
        return false // Cancel the deletion
      })
    })

    it('should delete transaction when confirmed', function() {
      // Mock successful delete API
      cy.intercept('DELETE', '**/api/transactions/*', {
        statusCode: 200,
        body: { success: true, message: 'Transaction voided successfully' }
      }).as('deleteTransaction')

      // Mock refreshed transactions list
      cy.intercept('GET', '**/api/transactions*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            items: this.ownerData.salesData.transactions.slice(1), // Remove first transaction
            totalPages: 1,
            currentPage: 1,
            totalItems: this.ownerData.salesData.transactions.length - 1
          }
        }
      }).as('getRefreshedTransactions')

      cy.window().then((win) => {
        cy.stub(win, 'confirm').returns(true)
      })

      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Void Sale"]').click()
      })

      cy.wait('@deleteTransaction')
      cy.wait('@getRefreshedTransactions')

      // Transaction should be removed from list
      cy.get('.transaction-row').should('have.length', 2)
    })

    it('should handle delete errors gracefully', () => {
      // Mock failed delete API
      cy.intercept('DELETE', '**/api/transactions/*', {
        statusCode: 400,
        body: { success: false, message: 'Cannot void this transaction' }
      }).as('deleteTransactionError')

      cy.window().then((win) => {
        cy.stub(win, 'confirm').returns(true)
      })

      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="Void Sale"]').click()
      })

      cy.wait('@deleteTransactionError')

      // Should show error message
      cy.contains('Failed to void transaction').should('be.visible')
    })
  })

  describe('View Transaction Details', () => {
    beforeEach(function() {
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

    it('should open view details modal when view button is clicked', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="View Details"]').click()
      })

      cy.get('.modal-overlay').should('be.visible')
      cy.contains('Transaction Details').should('be.visible')
    })

    it('should display all transaction information in view modal', function() {
      const transaction = this.ownerData.salesData.transactions[0]

      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="View Details"]').click()
      })

      cy.get('.modal-content').within(() => {
        // Should display all transaction details
        cy.contains(transaction.item.name).should('be.visible')
        cy.contains(transaction.item.description).should('be.visible')
        cy.contains(transaction.provider.name).should('be.visible')
        cy.contains(`$${transaction.salePrice.toFixed(2)}`).should('be.visible')
        cy.contains(transaction.paymentMethod).should('be.visible')

        // Should show edit button
        cy.get('button').contains('Edit Transaction').should('be.visible')
      })
    })

    it('should close view details modal', () => {
      cy.get('.transaction-row').first().within(() => {
        cy.get('button[title="View Details"]').click()
      })

      cy.get('button').contains('Close').click()
      cy.get('.modal-overlay').should('not.exist')
    })
  })
})