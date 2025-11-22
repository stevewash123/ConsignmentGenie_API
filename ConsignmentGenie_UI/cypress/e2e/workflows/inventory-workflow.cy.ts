/// <reference types="cypress" />

describe('Complete Inventory Management Workflow', () => {
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

  describe('Complete Item Lifecycle Workflow', () => {
    beforeEach(function() {
      // Mock all necessary APIs
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/providers*', {
        statusCode: 200,
        body: { success: true, data: this.ownerData.providers }
      }).as('getProviders')

      cy.intercept('GET', '**/api/items*', {
        statusCode: 200,
        body: {
          ...this.inventoryData.pagedResult,
          items: this.inventoryData.items
        }
      }).as('getItems')

      // Mock SKU generation
      cy.intercept('GET', '**/api/items/generate-sku/*', {
        statusCode: 200,
        body: {
          success: true,
          data: 'ART2024006'
        }
      }).as('generateSku')

      // Mock item creation
      cy.intercept('POST', '**/api/items', {
        statusCode: 201,
        body: {
          success: true,
          data: {
            id: 'item-new-001',
            sku: 'ART2024006',
            ...this.inventoryData.newItem,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            images: []
          },
          message: 'Item created successfully'
        }
      }).as('createItem')

      // Mock item update
      cy.intercept('PUT', '**/api/items/*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            ...this.inventoryData.updatedItem,
            updatedAt: new Date().toISOString()
          },
          message: 'Item updated successfully'
        }
      }).as('updateItem')

      // Mock item retrieval for editing
      cy.intercept('GET', '**/api/items/item-001', {
        statusCode: 200,
        body: {
          success: true,
          data: this.inventoryData.items[0]
        }
      }).as('getItem')
    })

    it('should complete full item creation workflow', function() {
      // Start from inventory list
      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])

      // Click Add Item
      cy.get('[data-cy="add-item"]').click()
      cy.url().should('include', '/owner/inventory/add')

      // Fill out item creation form
      cy.wait('@getProviders')

      // Basic information
      cy.get('[data-cy="provider-select"]').select('Sarah Thompson')
      cy.get('[data-cy="category-select"]').select('Home & Decor')

      // Auto-generate SKU
      cy.get('[data-cy="generate-sku"]').click()
      cy.wait('@generateSku')
      cy.get('[data-cy="sku-input"]').should('have.value', 'ART2024006')

      // Item details
      cy.get('[data-cy="title-input"]').type(this.inventoryData.newItem.title)
      cy.get('[data-cy="description-textarea"]').type(this.inventoryData.newItem.description)
      cy.get('[data-cy="condition-select"]').select(this.inventoryData.newItem.condition)
      cy.get('[data-cy="price-input"]').type(this.inventoryData.newItem.price.toString())
      cy.get('[data-cy="original-price-input"]').type(this.inventoryData.newItem.originalPrice.toString())

      // Additional details
      cy.get('[data-cy="materials-input"]').type(this.inventoryData.newItem.materials)
      cy.get('[data-cy="measurements-input"]').type(this.inventoryData.newItem.measurements)
      cy.get('[data-cy="brand-input"]').type(this.inventoryData.newItem.brand)
      cy.get('[data-cy="color-input"]').type(this.inventoryData.newItem.color)
      cy.get('[data-cy="year-input"]').type(this.inventoryData.newItem.year.toString())

      // Location and notes
      cy.get('[data-cy="location-input"]').type(this.inventoryData.newItem.location)
      cy.get('[data-cy="notes-textarea"]').type(this.inventoryData.newItem.notes)
      cy.get('[data-cy="internal-notes-textarea"]').type(this.inventoryData.newItem.internalNotes)

      // Submit form
      cy.get('[data-cy="save-item"]').click()
      cy.wait('@createItem')

      // Should show success message
      cy.contains('Item created successfully').should('be.visible')

      // Should redirect back to inventory list
      cy.url().should('include', '/owner/inventory')
      cy.wait('@getItems')

      // New item should appear in the list
      cy.contains(this.inventoryData.newItem.title).should('be.visible')
      cy.contains('ART2024006').should('be.visible')
    })

    it('should complete full item editing workflow', function() {
      // Start from inventory list
      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])

      // Click edit on first item
      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="edit-item"]').click()
      })

      cy.url().should('include', '/owner/inventory/')
      cy.url().should('include', '/edit')
      cy.wait('@getItem')

      // Form should be pre-populated with existing data
      cy.get('[data-cy="title-input"]').should('have.value', this.inventoryData.items[0].title)
      cy.get('[data-cy="price-input"]').should('have.value', this.inventoryData.items[0].price.toString())

      // Update item details
      cy.get('[data-cy="title-input"]').clear().type(this.inventoryData.updatedItem.title)
      cy.get('[data-cy="description-textarea"]').clear().type(this.inventoryData.updatedItem.description)
      cy.get('[data-cy="condition-select"]').select(this.inventoryData.updatedItem.condition)
      cy.get('[data-cy="price-input"]').clear().type(this.inventoryData.updatedItem.price.toString())
      cy.get('[data-cy="location-input"]').clear().type(this.inventoryData.updatedItem.location)
      cy.get('[data-cy="notes-textarea"]').clear().type(this.inventoryData.updatedItem.notes)
      cy.get('[data-cy="internal-notes-textarea"]').clear().type(this.inventoryData.updatedItem.internalNotes)

      // Save changes
      cy.get('[data-cy="save-item"]').click()
      cy.wait('@updateItem')

      // Should show success message
      cy.contains('Item updated successfully').should('be.visible')

      // Should redirect back to inventory list
      cy.url().should('include', '/owner/inventory')
      cy.wait('@getItems')
    })

    it('should handle form validation errors gracefully', function() {
      cy.visit('/owner/inventory/add')
      cy.wait(['@getProviders', '@getCategories'])

      // Try to submit form without required fields
      cy.get('[data-cy="save-item"]').click()

      // Should show validation errors
      cy.contains('Provider is required').should('be.visible')
      cy.contains('Title is required').should('be.visible')
      cy.contains('Category is required').should('be.visible')
      cy.contains('Condition is required').should('be.visible')
      cy.contains('Price is required').should('be.visible')

      // Fill minimum required fields
      cy.get('[data-cy="provider-select"]').select('Sarah Thompson')
      cy.get('[data-cy="title-input"]').type('Test Item')
      cy.get('[data-cy="category-select"]').select('Clothing')
      cy.get('[data-cy="condition-select"]').select('Good')
      cy.get('[data-cy="price-input"]').type('50.00')

      // Now form should be valid
      cy.get('[data-cy="save-item"]').click()
      cy.wait('@createItem')
      cy.contains('Item created successfully').should('be.visible')
    })

    it('should cancel item creation and return to list', function() {
      cy.visit('/owner/inventory/add')
      cy.wait(['@getProviders', '@getCategories'])

      // Fill some fields
      cy.get('[data-cy="title-input"]').type('Partial Item')
      cy.get('[data-cy="price-input"]').type('25.00')

      // Cancel
      cy.get('[data-cy="cancel-item"]').click()

      // Should show confirmation dialog if form is dirty
      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.contains('Are you sure you want to discard your changes?').should('be.visible')

      cy.get('[data-cy="confirm-discard"]').click()

      // Should return to inventory list
      cy.url().should('include', '/owner/inventory')
      cy.url().should('not.include', '/add')
    })
  })

  describe('Category Management Workflow', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/categories/usage-stats*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categoryUsageStats }
      }).as('getCategoryUsageStats')

      cy.intercept('POST', '**/api/categories', {
        statusCode: 201,
        body: {
          success: true,
          data: {
            id: 'cat-new-001',
            name: 'Collectibles',
            displayOrder: 6,
            isActive: true,
            createdAt: new Date().toISOString()
          },
          message: 'Category created successfully'
        }
      }).as('createCategory')

      cy.intercept('PUT', '**/api/categories/*', {
        statusCode: 200,
        body: {
          success: true,
          data: {
            id: 'cat-001',
            name: 'Apparel',
            displayOrder: 1,
            isActive: true,
            createdAt: '2024-11-15T10:00:00Z'
          },
          message: 'Category updated successfully'
        }
      }).as('updateCategory')

      cy.intercept('DELETE', '**/api/categories/*', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Category deleted successfully'
        }
      }).as('deleteCategory')

      cy.intercept('PUT', '**/api/categories/reorder', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Categories reordered successfully'
        }
      }).as('reorderCategories')
    })

    it('should manage categories from inventory settings', function() {
      // Start from inventory list
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      // Open category management
      cy.get('[data-cy="manage-categories"]').click()

      // Should show category management modal or page
      cy.get('[data-cy="category-management"]').should('be.visible')
      cy.wait('@getCategoryUsageStats')

      // Should display existing categories with usage stats
      cy.get('.category-item').should('have.length', this.inventoryData.categories.length)

      // Check first category shows usage stats
      cy.get('.category-item').first().within(() => {
        cy.contains('Clothing').should('be.visible')
        cy.contains('2 items').should('be.visible')
        cy.contains('1 available').should('be.visible')
        cy.contains('1 sold').should('be.visible')
      })
    })

    it('should create a new category', function() {
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      cy.get('[data-cy="manage-categories"]').click()
      cy.get('[data-cy="category-management"]').should('be.visible')
      cy.wait('@getCategoryUsageStats')

      // Click add new category
      cy.get('[data-cy="add-category"]').click()

      // Fill category form
      cy.get('[data-cy="category-name"]').type('Collectibles')
      cy.get('[data-cy="category-display-order"]').type('6')

      // Save category
      cy.get('[data-cy="save-category"]').click()
      cy.wait('@createCategory')

      // Should show success message
      cy.contains('Category created successfully').should('be.visible')

      // Should refresh category list
      cy.wait('@getCategoryUsageStats')
    })

    it('should edit an existing category', function() {
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      cy.get('[data-cy="manage-categories"]').click()
      cy.wait('@getCategoryUsageStats')

      // Click edit on first category
      cy.get('.category-item').first().within(() => {
        cy.get('[data-cy="edit-category"]').click()
      })

      // Form should be pre-populated
      cy.get('[data-cy="category-name"]').should('have.value', 'Clothing')

      // Update category
      cy.get('[data-cy="category-name"]').clear().type('Apparel')

      // Save changes
      cy.get('[data-cy="save-category"]').click()
      cy.wait('@updateCategory')

      // Should show success message
      cy.contains('Category updated successfully').should('be.visible')
    })

    it('should delete a category with no items', function() {
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      cy.get('[data-cy="manage-categories"]').click()
      cy.wait('@getCategoryUsageStats')

      // Try to delete category with items (Books - no items)
      cy.get('.category-item').contains('Books').parent().within(() => {
        cy.get('[data-cy="delete-category"]').click()
      })

      // Should show confirmation dialog
      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.contains('Are you sure you want to delete the "Books" category?').should('be.visible')

      cy.get('[data-cy="confirm-delete"]').click()
      cy.wait('@deleteCategory')

      // Should show success message
      cy.contains('Category deleted successfully').should('be.visible')
    })

    it('should prevent deleting category with items', function() {
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      cy.get('[data-cy="manage-categories"]').click()
      cy.wait('@getCategoryUsageStats')

      // Try to delete category with items (Clothing - has items)
      cy.get('.category-item').contains('Clothing').parent().within(() => {
        cy.get('[data-cy="delete-category"]').should('be.disabled')
      })

      // Should show tooltip or message explaining why
      cy.get('.category-item').contains('Clothing').parent().within(() => {
        cy.get('[data-cy="delete-disabled-tooltip"]')
          .should('contain', 'Cannot delete category with assigned items')
      })
    })

    it('should reorder categories by drag and drop', function() {
      cy.visit('/owner/inventory')
      cy.wait('@getCategories')

      cy.get('[data-cy="manage-categories"]').click()
      cy.wait('@getCategoryUsageStats')

      // Drag first category to second position
      cy.get('.category-item').first().as('firstCategory')
      cy.get('.category-item').eq(1).as('secondCategory')

      cy.get('@firstCategory').drag('@secondCategory')

      // Should trigger reorder API call
      cy.wait('@reorderCategories')

      // Should show success message
      cy.contains('Categories reordered successfully').should('be.visible')
    })
  })

  describe('Photo Management Workflow', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/items/item-001', {
        statusCode: 200,
        body: {
          success: true,
          data: this.inventoryData.items[0]
        }
      }).as('getItem')

      cy.intercept('POST', '**/api/items/*/photos', {
        statusCode: 201,
        body: {
          success: true,
          data: 'https://example.com/images/new-photo.jpg',
          message: 'Photo uploaded successfully'
        }
      }).as('uploadPhoto')

      cy.intercept('DELETE', '**/api/items/*/photos', {
        statusCode: 200,
        body: {
          success: true,
          message: 'Photo deleted successfully'
        }
      }).as('deletePhoto')
    })

    it('should upload photos to an item', function() {
      // Navigate to item detail/edit page
      cy.visit('/owner/inventory/item-001/edit')
      cy.wait('@getItem')

      // Should show existing photos
      cy.get('[data-cy="item-photos"]').should('be.visible')
      cy.get('.photo-thumbnail').should('have.length', this.inventoryData.items[0].images.length)

      // Upload new photo
      cy.get('[data-cy="photo-upload"]').selectFile('cypress/fixtures/test-image.jpg', { force: true })

      cy.wait('@uploadPhoto')

      // Should show success message
      cy.contains('Photo uploaded successfully').should('be.visible')

      // Should show new photo in gallery
      cy.get('.photo-thumbnail').should('have.length', this.inventoryData.items[0].images.length + 1)
    })

    it('should delete a photo from an item', function() {
      cy.visit('/owner/inventory/item-001/edit')
      cy.wait('@getItem')

      // Click delete on first photo
      cy.get('.photo-thumbnail').first().within(() => {
        cy.get('[data-cy="delete-photo"]').click()
      })

      // Should show confirmation
      cy.get('[data-cy="confirm-dialog"]').should('be.visible')
      cy.contains('Are you sure you want to delete this photo?').should('be.visible')

      cy.get('[data-cy="confirm-delete"]').click()
      cy.wait('@deletePhoto')

      // Should show success message
      cy.contains('Photo deleted successfully').should('be.visible')
    })

    it('should set a photo as primary', function() {
      cy.visit('/owner/inventory/item-001/edit')
      cy.wait('@getItem')

      // Click "Set as Primary" on second photo
      cy.get('.photo-thumbnail').eq(1).within(() => {
        cy.get('[data-cy="set-primary"]').click()
      })

      // Should update visually
      cy.get('.photo-thumbnail').eq(1).should('have.class', 'primary')
      cy.get('[data-cy="primary-badge"]').should('be.visible')
    })
  })

  describe('Search and Filter Integration', () => {
    beforeEach(function() {
      cy.intercept('GET', '**/api/categories*', {
        statusCode: 200,
        body: { success: true, data: this.inventoryData.categories }
      }).as('getCategories')

      cy.intercept('GET', '**/api/items*', (req) => {
        // Handle various filter combinations
        const search = req.query.search?.toString()
        const category = req.query.category?.toString()
        const status = req.query.status?.toString()
        const condition = req.query.condition?.toString()
        const priceMin = req.query.priceMin ? parseFloat(req.query.priceMin.toString()) : null
        const priceMax = req.query.priceMax ? parseFloat(req.query.priceMax.toString()) : null

        let filteredItems = [...this.inventoryData.items]

        if (search) {
          filteredItems = filteredItems.filter(item =>
            item.title.toLowerCase().includes(search.toLowerCase()) ||
            item.sku.toLowerCase().includes(search.toLowerCase())
          )
        }

        if (category) {
          filteredItems = filteredItems.filter(item => item.category === category)
        }

        if (status) {
          filteredItems = filteredItems.filter(item => item.status === status)
        }

        if (condition) {
          filteredItems = filteredItems.filter(item => item.condition === condition)
        }

        if (priceMin !== null) {
          filteredItems = filteredItems.filter(item => item.price >= priceMin)
        }

        if (priceMax !== null) {
          filteredItems = filteredItems.filter(item => item.price <= priceMax)
        }

        req.reply({
          statusCode: 200,
          body: {
            ...this.inventoryData.pagedResult,
            items: filteredItems,
            totalCount: filteredItems.length
          }
        })
      }).as('getItems')
    })

    it('should apply multiple filters simultaneously', function() {
      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])

      // Apply multiple filters
      cy.get('[data-cy="search-input"]').type('vintage')
      cy.get('[data-cy="category-filter"]').select('Jewelry')
      cy.get('[data-cy="status-filter"]').select('Available')
      cy.get('[data-cy="condition-filter"]').select('Good')
      cy.get('[data-cy="price-min"]').type('100')
      cy.get('[data-cy="price-max"]').type('500')

      cy.get('[data-cy="apply-filters"]').click()
      cy.wait('@getItems')

      // Should update URL with all filter parameters
      cy.url().should('include', 'search=vintage')
      cy.url().should('include', 'category=Jewelry')
      cy.url().should('include', 'status=Available')
      cy.url().should('include', 'condition=Good')
      cy.url().should('include', 'priceMin=100')
      cy.url().should('include', 'priceMax=500')

      // Should show filtered results
      cy.contains('Vintage Watch').should('be.visible')
    })

    it('should preserve filters when navigating back from item details', function() {
      cy.visit('/owner/inventory')
      cy.wait(['@getCategories', '@getItems'])

      // Apply filters
      cy.get('[data-cy="category-filter"]').select('Clothing')
      cy.get('[data-cy="apply-filters"]').click()
      cy.wait('@getItems')

      // Navigate to item details
      cy.get('.item-row').first().within(() => {
        cy.get('[data-cy="view-item"]').click()
      })

      // Navigate back
      cy.go('back')

      // Filters should still be applied
      cy.get('[data-cy="category-filter"]').should('have.value', 'Clothing')
      cy.url().should('include', 'category=Clothing')
    })
  })
})