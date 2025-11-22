/// <reference types="cypress" />

describe('Owner Inventory Management Tests', () => {
  beforeEach(() => {
    cy.fixture('inventory-data').as('inventoryData')
    cy.fixture('owner-data').as('ownerData')

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

  describe('Inventory List Page Loading and Display', () => {
    beforeEach(function() {
      // Mock categories API
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: {
          success: true,
          data: this.inventoryData.categories
        }
      }).as('getCategories')

      // Mock items API with pagination
      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: pagedResult
      }).as('getItems')

      cy.visit('/owner/inventory')
    })

    it('should display the inventory page header and navigation', function() {
      cy.wait(['@getCategories', '@getItems'])

      // Check header
      cy.contains('Inventory Management').should('be.visible')
      cy.contains('Manage your consignment inventory items').should('be.visible')

      // Check navigation breadcrumb
      cy.get('.inventory-header').should('be.visible')
    })

    it('should load and display inventory items in table format', function() {
      cy.wait(['@getCategories', '@getItems'])

      // Check table headers
      cy.get('.inventory-table .table-header').within(() => {
        cy.contains('Image').should('be.visible')
        cy.contains('SKU').should('be.visible')
        cy.contains('Item').should('be.visible')
        cy.contains('Category').should('be.visible')
        cy.contains('Condition').should('be.visible')
        cy.contains('Price').should('be.visible')
        cy.contains('Status').should('be.visible')
        cy.contains('Provider').should('be.visible')
        cy.contains('Actions').should('be.visible')
      })

      // Check that items are displayed
      cy.get('.item-row').should('have.length', this.inventoryData.items.length)

      // Check first item details
      cy.get('.item-row').first().within(() => {
        cy.contains(this.inventoryData.items[0].sku).should('be.visible')
        cy.contains(this.inventoryData.items[0].title).should('be.visible')
        cy.contains(this.inventoryData.items[0].category).should('be.visible')
        cy.contains(this.inventoryData.items[0].condition).should('be.visible')
        cy.contains(`$${this.inventoryData.items[0].price.toFixed(2)}`).should('be.visible')
        cy.contains(this.inventoryData.items[0].status).should('be.visible')
        cy.contains(this.inventoryData.items[0].providerName).should('be.visible')
      })
    })

    it('should display item images when available', function() {
      cy.wait(['@getCategories', '@getItems'])

      // Check that primary images are displayed
      cy.get('.item-row').each((row, index) => {
        const item = this.inventoryData.items[index]
        if (item.primaryImageUrl) {
          cy.wrap(row).within(() => {
            cy.get('.item-thumbnail img').should('have.attr', 'src', item.primaryImageUrl)
          })
        } else {
          cy.wrap(row).within(() => {
            cy.get('.item-thumbnail .no-image').should('be.visible')
          })
        }
      })
    })

    it('should display correct status badges with appropriate styling', function() {
      cy.wait(['@getCategories', '@getItems'])

      // Available status should have green styling
      cy.get('.item-row').contains('Available').parent().should('have.class', 'status-available')

      // Sold status should have blue styling
      cy.get('.item-row').contains('Sold').parent().should('have.class', 'status-sold')
    })
  })

  describe('Search and Filtering Functionality', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', (req) => {
        // Check if this is a search request
        if (req.query.search) {
          const searchTerm = req.query.search.toString().toLowerCase()
          const filteredItems = this.inventoryData.items.filter(item =>
            item.title.toLowerCase().includes(searchTerm) ||
            item.sku.toLowerCase().includes(searchTerm) ||
            item.description?.toLowerCase().includes(searchTerm)
          )
          req.reply({
            statusCode: 200,
            body: {
              ...pagedResult,
              items: filteredItems,
              totalCount: filteredItems.length
            }
          })
        } else {
          req.reply({
            statusCode: 200,
            body: pagedResult
          })
        }
      }).as('getItems')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should filter items by search term', function() {
      const searchTerm = 'leather'

      cy.get('[data-cy="search-input"]').type(searchTerm)
      cy.get('[data-cy="search-button"]').click()

      cy.wait('@getItems')

      // Should show only items matching search
      cy.get('.item-row').should('have.length', 1)
      cy.contains('Vintage Leather Jacket').should('be.visible')
    })

    it('should filter items by category', function() {
      // Select Jewelry category
      cy.get('[data-cy="category-filter"]').select('Jewelry')
      cy.wait('@getItems')

      // Should update URL with filter parameter
      cy.url().should('include', 'category=Jewelry')
    })

    it('should filter items by status', function() {
      // Select Available status
      cy.get('[data-cy="status-filter"]').select('Available')
      cy.wait('@getItems')

      // Should update URL with filter parameter
      cy.url().should('include', 'status=Available')
    })

    it('should filter items by condition', function() {
      // Select Good condition
      cy.get('[data-cy="condition-filter"]').select('Good')
      cy.wait('@getItems')

      // Should update URL with filter parameter
      cy.url().should('include', 'condition=Good')
    })

    it('should filter items by price range', function() {
      // Set price range
      cy.get('[data-cy="price-min"]').type('50')
      cy.get('[data-cy="price-max"]').type('200')
      cy.get('[data-cy="apply-filters"]').click()

      cy.wait('@getItems')

      // Should update URL with filter parameters
      cy.url().should('include', 'priceMin=50')
      cy.url().should('include', 'priceMax=200')
    })

    it('should clear all filters', function() {
      // Apply some filters
      cy.get('[data-cy="search-input"]').type('jacket')
      cy.get('[data-cy="category-filter"]').select('Clothing')
      cy.get('[data-cy="apply-filters"]').click()

      cy.wait('@getItems')

      // Clear filters
      cy.get('[data-cy="clear-filters"]').click()

      cy.wait('@getItems')

      // Should reset all filter inputs
      cy.get('[data-cy="search-input"]').should('have.value', '')
      cy.get('[data-cy="category-filter"]').should('have.value', '')

      // Should show all items again
      cy.get('.item-row').should('have.length', this.inventoryData.items.length)
    })
  })

  describe('Pagination Controls', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      // Mock paginated response
      cy.intercept('GET', '**/api/items*', (req) => {
        const page = parseInt(req.query.page?.toString() || '1')
        const pageSize = parseInt(req.query.pageSize?.toString() || '10')

        req.reply({
          statusCode: 200,
          body: {
            items: this.inventoryData.items.slice((page - 1) * pageSize, page * pageSize),
            totalCount: 25, // Simulate more items for pagination
            page: page,
            pageSize: pageSize,
            totalPages: Math.ceil(25 / pageSize),
            hasNextPage: page < Math.ceil(25 / pageSize),
            hasPreviousPage: page > 1,
            organizationId: 'demo-org'
          }
        })
      }).as('getItems')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should display pagination controls when there are multiple pages', function() {
      cy.get('.pagination').should('be.visible')
      cy.get('.pagination .page-info').should('contain', 'Page 1 of 3')
      cy.get('[data-cy="next-page"]').should('be.enabled')
      cy.get('[data-cy="prev-page"]').should('be.disabled')
    })

    it('should navigate to next page', function() {
      cy.get('[data-cy="next-page"]').click()
      cy.wait('@getItems')

      cy.url().should('include', 'page=2')
      cy.get('.pagination .page-info').should('contain', 'Page 2 of 3')
    })

    it('should change page size', function() {
      cy.get('[data-cy="page-size-select"]').select('25')
      cy.wait('@getItems')

      cy.url().should('include', 'pageSize=25')
    })
  })

  describe('Item Actions', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: pagedResult
      }).as('getItems')

      // Mock individual item API
      cy.intercept('GET', '**/api/items/*', {
        statusCode: 200,
        body: {
          success: true,
          data: this.inventoryData.items[0]
        }
      }).as('getItem')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should open item details when view action is clicked', function() {
      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="view-item"]').click()
      })

      // Should navigate to item detail page
      cy.url().should('include', '/owner/inventory/')
      cy.url().should('include', this.inventoryData.items[0].id)
    })

    it('should open edit form when edit action is clicked', function() {
      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="edit-item"]').click()
      })

      // Should navigate to item edit page
      cy.url().should('include', '/owner/inventory/')
      cy.url().should('include', '/edit')
    })

    it('should mark item as removed when remove action is clicked', function() {
      // Mock status update API
      cy.intercept('PUT', '**/api/items/*/status', {
        statusCode: 200,
        body: {
          success: true,
          data: { ...this.inventoryData.items[0], status: 'Removed' },
          message: 'Item status updated successfully'
        }
      }).as('updateItemStatus')

      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="remove-item"]').click()
      })

      // Should show confirmation dialog
      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.contains('Are you sure you want to mark this item as removed?').should('be.visible')

      cy.get('[data-cy="confirm-remove"]').click()
      cy.wait('@updateItemStatus')

      // Should show success message
      cy.contains('Item marked as removed successfully').should('be.visible')
    })

    it('should delete item when delete action is clicked and confirmed', function() {
      // Mock delete API
      cy.intercept('DELETE', '**/api/items/*', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Item deleted successfully'
        }
      }).as('deleteItem')

      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="delete-item"]').click()
      })

      // Should show confirmation dialog
      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.contains('Are you sure you want to permanently delete this item?').should('be.visible')
      cy.contains('This action cannot be undone').should('be.visible')

      cy.get('[data-cy="confirm-delete"]').click()
      cy.wait('@deleteItem')

      // Should show success message
      cy.contains('Item deleted successfully').should('be.visible')

      // Should refresh the list
      cy.wait('@getItems')
    })

    it('should cancel delete action when cancel is clicked', function() {
      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="delete-item"]').click()
      })

      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.get('[data-cy="cancel-delete"]').click()

      // Dialog should close without deleting
      cy.get('[data-cy="confirm-dialog"]').should('not.exist')
    })
  })

  describe('Sorting Functionality', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/items*', (req) => {
        const sortBy = req.query.sortBy?.toString()
        const sortDirection = req.query.sortDirection?.toString()

        let sortedItems = [...this.inventoryData.items]
        if (sortBy && sortDirection) {
          sortedItems.sort((a, b) => {
            let aVal = a[sortBy as keyof typeof a]
            let bVal = b[sortBy as keyof typeof b]

            // Handle different data types
            if (typeof aVal === 'string') {
              aVal = aVal.toLowerCase()
              bVal = (bVal as string).toLowerCase()
            }

            if (sortDirection === 'desc') {
              return aVal > bVal ? -1 : aVal < bVal ? 1 : 0
            } else {
              return aVal < bVal ? -1 : aVal > bVal ? 1 : 0
            }
          })
        }

        req.reply({
          statusCode: 200,
          body: {
            ...this.inventoryData.pagedResult,
            items: sortedItems
          }
        })
      }).as('getItems')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should sort by title ascending', function() {
      cy.get('[data-cy="sort-select"]').select('title-asc')
      cy.wait('@getItems')

      cy.url().should('include', 'sortBy=title')
      cy.url().should('include', 'sortDirection=asc')
    })

    it('should sort by price descending', function() {
      cy.get('[data-cy="sort-select"]').select('price-desc')
      cy.wait('@getItems')

      cy.url().should('include', 'sortBy=price')
      cy.url().should('include', 'sortDirection=desc')
    })

    it('should sort by created date newest first', function() {
      cy.get('[data-cy="sort-select"]').select('createdAt-desc')
      cy.wait('@getItems')

      cy.url().should('include', 'sortBy=createdAt')
      cy.url().should('include', 'sortDirection=desc')
    })
  })

  describe('Add New Item Functionality', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: { success: true, data: this.ownerData.providers }
      }).as('getProviders')

      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: pagedResult
      }).as('getItems')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should navigate to add item form when add button is clicked', function() {
      cy.get('[data-cy="add-item"]').click()

      // Should navigate to add item page
      cy.url().should('include', '/owner/inventory/add')
    })

    it('should show quick add form when quick add is clicked', function() {
      cy.get('[data-cy="quick-add"]').click()

      // Should show quick add modal or form
      cy.get('[data-cy="quick-add-form"]').should('be.visible')
      cy.contains('Quick Add Item').should('be.visible')
    })
  })

  describe('Error Handling', () => {
    beforeEach(() => {
      cy.visit('/owner/inventory')
    })

    it('should handle API errors gracefully when loading items', function() {
      // Mock failed API responses
      cy.intercept('GET', '**/api/categories*', { statusCode: 500 }).as('getCategoriesError')
      cy.intercept('GET', '**/api/items*', { statusCode: 500 }).as('getItemsError')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategoriesError', '@getItemsError'])

      // Should show error message
      cy.contains('Failed to load inventory items').should('be.visible')
      cy.get('[data-cy="retry-button"]').should('be.visible')
    })

    it('should retry loading when retry button is clicked', function() {
      // First load with error
      cy.intercept('GET', '**/api/categories*', { statusCode: 500 }).as('getCategoriesError')
      cy.intercept('GET', '**/api/items*', { statusCode: 500 }).as('getItemsError')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategoriesError', '@getItemsError'])

      // Mock successful retry
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategoriesRetry')

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: {
          ...this.inventoryData.pagedResult,
          items: this.inventoryData.items
        }
      }).as('getItemsRetry')

      cy.get('[data-cy="retry-button"]').click()
      cy.wait(['@getCategoriesRetry', '@getItemsRetry'])

      // Should show items table
      cy.get('.inventory-table').should('be.visible')
    })

    it('should handle individual item action errors', function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: pagedResult
      }).as('getItems')

      // Mock failed delete
      cy.intercept('DELETE', '**/api/items/*', {
        statusCode: 500,
        body: {
          success: false,
          message: 'Failed to delete item'
        }
      }).as('deleteItemError')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])

      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="delete-item"]').click()
      })

      cy.get('[data-cy="confirm-delete"]').click()
      cy.wait('@deleteItemError')

      // Should show error message
      cy.contains('Failed to delete item').should('be.visible')
    })
  })

  describe('Loading States', () => {
    beforeEach(() => {
      cy.visit('/owner/inventory')
    })

    it('should show loading spinner while data is being fetched', function() {
      // Delay API responses to see loading state
      cy.intercept('GET', '**/api/categories*', (req) => {
        return new Promise((resolve) => {
          setTimeout(() => resolve(req.reply({
            statusCode: 200,
            body: { success: true, data: this.inventoryData.categories }
          })), 1000)
        })
      }).as('getCategoriesDelay')

      cy.intercept('GET', '**/api/items*', (req) => {
        return new Promise((resolve) => {
          setTimeout(() => resolve(req.reply({
            statusCode: 200,
            body: {
              ...this.inventoryData.pagedResult,
              items: this.inventoryData.items
            }
          })), 1000)
        })
      }).as('getItemsDelay')

      cy.visit('/owner/inventory')

      // Should show loading state
      cy.get('[data-cy="loading-spinner"]').should('be.visible')
      cy.contains('Loading inventory items...').should('be.visible')

      cy.wait(['@getCategoriesDelay', '@getItemsDelay'])

      // Loading should disappear
      cy.get('[data-cy="loading-spinner"]').should('not.exist')
    })
  })

  describe('Responsive Design', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      const pagedResult = {
        ...this.inventoryData.pagedResult,
        items: this.inventoryData.items
      }

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: pagedResult
      }).as('getItems')

      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])
    })

    it('should display correctly on mobile viewport', function() {
      cy.viewport('iphone-x')

      // Inventory page should be responsive
      cy.get('.inventory-container').should('be.visible')
      cy.get('.filters-section').should('be.visible')
      cy.get('.inventory-table').should('be.visible')
    })

    it('should display correctly on tablet viewport', function() {
      cy.viewport('ipad-2')

      cy.get('.inventory-container').should('be.visible')
      cy.get('.filters-section').should('be.visible')
      cy.get('.inventory-table').should('be.visible')
    })
  })
})