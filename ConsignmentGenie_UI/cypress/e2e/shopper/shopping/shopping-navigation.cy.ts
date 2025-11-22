describe('Shopping Navigation and Cart Flow', () => {
  const testStoreSlug = Cypress.env('testStoreSlug') || 'cypress-test-store'
  const testUserEmail = Cypress.env('testUserEmail') || 'cypress.shopper@example.com'
  const testUserPassword = Cypress.env('testUserPassword') || 'CypressTest123!'

  beforeEach(() => {
    // Clear localStorage before each test
    cy.clearShopperStorage(testStoreSlug)

    // Mock shopper APIs
    cy.mockShopperAPIs(testStoreSlug)

    // Mock catalog items for testing
    cy.fixture('catalog-items').then((catalogData) => {
      cy.intercept('GET', `**/api/shop/${testStoreSlug}/catalog*`, {
        statusCode: 200,
        body: {
          success: true,
          data: catalogData.items || [
            {
              id: '1',
              name: 'Vintage Leather Jacket',
              description: 'Classic brown leather jacket in excellent condition',
              price: 125.00,
              category: 'Clothing',
              isAvailable: true,
              imageUrl: '/assets/test-images/jacket.jpg'
            },
            {
              id: '2',
              name: 'Antique Wooden Table',
              description: 'Beautiful oak dining table from the 1960s',
              price: 450.00,
              category: 'Furniture',
              isAvailable: true,
              imageUrl: '/assets/test-images/table.jpg'
            },
            {
              id: '3',
              name: 'Designer Handbag',
              description: 'Authentic designer handbag with original tags',
              price: 280.00,
              category: 'Accessories',
              isAvailable: false,
              imageUrl: '/assets/test-images/handbag.jpg'
            }
          ]
        }
      }).as('getCatalogItems')
    })
  })

  describe('Store Navigation', () => {
    it('should display store header and navigation', () => {
      cy.visit(`/shop/${testStoreSlug}`)
      cy.wait('@getStoreInfo')

      // Check store header elements
      cy.get('[data-cy="store-header"]').within(() => {
        cy.contains('Cypress Test Store').should('be.visible')
        cy.get('[data-cy="store-logo"]').should('be.visible')
        cy.get('[data-cy="search-input"]').should('be.visible')
        cy.get('[data-cy="cart-icon"]').should('be.visible')
        cy.get('[data-cy="account-menu"]').should('be.visible')
      })

      // Check navigation menu
      cy.get('[data-cy="store-nav"]').within(() => {
        cy.contains('Shop').should('be.visible')
        cy.contains('Categories').should('be.visible')
      })
    })

    it('should show responsive navigation on mobile', () => {
      cy.viewport('iphone-x')
      cy.visit(`/shop/${testStoreSlug}`)

      // Should show mobile menu toggle
      cy.get('[data-cy="mobile-menu-toggle"]').should('be.visible')

      // Click to open mobile menu
      cy.get('[data-cy="mobile-menu-toggle"]').click()
      cy.get('[data-cy="mobile-menu"]').should('be.visible')

      // Check mobile menu items
      cy.get('[data-cy="mobile-menu"]').within(() => {
        cy.contains('Shop').should('be.visible')
        cy.contains('Categories').should('be.visible')
      })
    })

    it('should navigate between different store pages', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Navigate to account (should redirect to login)
      cy.get('[data-cy="account-menu"]').click()
      cy.contains('Sign In').click()
      cy.url().should('include', 'login')

      // Navigate to cart
      cy.visit(`/shop/${testStoreSlug}`)
      cy.get('[data-cy="cart-icon"]').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'cart')
    })
  })

  describe('Catalog Browsing', () => {
    it('should display catalog items', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Should show catalog items
      cy.contains('Vintage Leather Jacket').should('be.visible')
      cy.contains('Antique Wooden Table').should('be.visible')
      cy.contains('Designer Handbag').should('be.visible')

      // Should show item details
      cy.get('[data-cy="item-card"]').first().within(() => {
        cy.get('[data-cy="item-name"]').should('be.visible')
        cy.get('[data-cy="item-price"]').should('contain', '$')
        cy.get('[data-cy="item-description"]').should('be.visible')
        cy.get('[data-cy="add-to-cart-btn"]').should('be.visible')
      })
    })

    it('should filter items by category', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Select clothing category
      cy.get('[data-cy="category-filter"]').select('Clothing')

      // Should only show clothing items
      cy.contains('Vintage Leather Jacket').should('be.visible')
      cy.contains('Antique Wooden Table').should('not.exist')
      cy.contains('Designer Handbag').should('not.exist')
    })

    it('should search for items', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Search for "vintage"
      cy.get('[data-cy="search-input"]').type('vintage')
      cy.get('[data-cy="search-btn"]').click()

      // Should show only vintage items
      cy.contains('Vintage Leather Jacket').should('be.visible')
      cy.contains('Antique Wooden Table').should('not.exist')
      cy.contains('Designer Handbag').should('not.exist')
    })

    it('should sort items by price', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Sort by price low to high
      cy.get('[data-cy="sort-select"]').select('Price: Low to High')

      // Should show items in price order
      cy.get('[data-cy="item-card"]').first().should('contain', 'Vintage Leather Jacket')
      cy.get('[data-cy="item-card"]').last().should('contain', 'Antique Wooden Table')
    })

    it('should handle unavailable items', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Find unavailable item
      cy.contains('Designer Handbag').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').should('be.disabled')
        cy.contains('Sold Out').should('be.visible')
        cy.get('[data-cy="sold-overlay"]').should('be.visible')
      })
    })
  })

  describe('Shopping Cart', () => {
    it('should add items to cart', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Add item to cart
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Should show success feedback
      cy.contains('added to cart').should('be.visible')

      // Cart icon should show item count
      cy.get('[data-cy="cart-count"]').should('contain', '1')

      // Check cart page
      cy.get('[data-cy="cart-icon"]').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'cart')

      cy.get('[data-cy="cart-item"]').within(() => {
        cy.contains('Vintage Leather Jacket').should('be.visible')
        cy.contains('$125.00').should('be.visible')
        cy.get('[data-cy="quantity-input"]').should('have.value', '1')
      })
    })

    it('should update item quantities in cart', () => {
      // Add item to cart first
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Go to cart
      cy.get('[data-cy="cart-icon"]').click()

      // Increase quantity
      cy.get('[data-cy="increase-quantity"]').click()
      cy.get('[data-cy="quantity-input"]').should('have.value', '2')

      // Check updated total
      cy.contains('$250.00').should('be.visible')

      // Decrease quantity
      cy.get('[data-cy="decrease-quantity"]').click()
      cy.get('[data-cy="quantity-input"]').should('have.value', '1')
    })

    it('should remove items from cart', () => {
      // Add item to cart first
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Go to cart
      cy.get('[data-cy="cart-icon"]').click()

      // Remove item
      cy.get('[data-cy="remove-item-btn"]').click()

      // Should show empty cart
      cy.contains('Your cart is empty').should('be.visible')
      cy.get('[data-cy="cart-count"]').should('not.exist')
    })

    it('should calculate cart totals correctly', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Add multiple items
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      cy.contains('Antique Wooden Table').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Go to cart
      cy.get('[data-cy="cart-icon"]').click()

      // Check totals
      cy.get('[data-cy="subtotal"]').should('contain', '$575.00') // 125 + 450
      cy.get('[data-cy="tax"]').should('contain', '$46.00') // 8% tax
      cy.get('[data-cy="total"]').should('contain', '$621.00')
    })

    it('should persist cart items in localStorage', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Add item to cart
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })

      // Refresh page
      cy.reload()

      // Cart should still have items
      cy.get('[data-cy="cart-count"]').should('contain', '1')
    })

    it('should show different cart states for guest vs authenticated users', () => {
      // Guest cart
      cy.visit(`/shop/${testStoreSlug}/cart`)
      cy.contains('Sign in to save your cart').should('be.visible')

      // Login
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')

      // Authenticated cart
      cy.visit(`/shop/${testStoreSlug}/cart`)
      cy.contains('Sign in to save your cart').should('not.exist')
    })
  })

  describe('Checkout Flow', () => {
    beforeEach(() => {
      // Add items to cart for checkout tests
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="add-to-cart-btn"]').click()
      })
    })

    it('should navigate to checkout from cart', () => {
      cy.get('[data-cy="cart-icon"]').click()
      cy.get('[data-cy="checkout-btn"]').click()

      cy.shouldBeOnStorePage(testStoreSlug, 'checkout')
      cy.contains('Checkout').should('be.visible')
    })

    it('should show checkout steps', () => {
      cy.visit(`/shop/${testStoreSlug}/checkout`)

      // Should show checkout steps
      cy.get('[data-cy="checkout-steps"]').within(() => {
        cy.contains('Contact Info').should('be.visible')
        cy.contains('Shipping').should('be.visible')
        cy.contains('Payment').should('be.visible')
        cy.contains('Review').should('be.visible')
      })
    })

    it('should handle guest checkout', () => {
      cy.visit(`/shop/${testStoreSlug}/checkout`)

      // Should show guest checkout form
      cy.contains('Contact Information').should('be.visible')
      cy.contains('Sign in for faster checkout, or continue as guest').should('be.visible')

      // Fill guest information
      cy.get('input[formControlName="firstName"]').type('Guest')
      cy.get('input[formControlName="lastName"]').type('User')
      cy.get('input[formControlName="email"]').type('guest@example.com')

      cy.get('[data-cy="continue-btn"]').click()

      // Should proceed to shipping step
      cy.contains('Shipping Address').should('be.visible')
    })

    it('should prefill checkout for authenticated users', () => {
      // Login first
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')
      cy.wait('@getShopperProfile')

      cy.visit(`/shop/${testStoreSlug}/checkout`)

      // Should prefill with user data
      cy.get('input[formControlName="firstName"]').should('have.value', 'Cypress')
      cy.get('input[formControlName="lastName"]').should('have.value', 'Test User')
      cy.get('input[formControlName="email"]').should('have.value', testUserEmail)
    })

    it('should show order summary in checkout', () => {
      cy.visit(`/shop/${testStoreSlug}/checkout`)

      // Should show order summary
      cy.get('[data-cy="order-summary"]').within(() => {
        cy.contains('Order Summary').should('be.visible')
        cy.contains('Vintage Leather Jacket').should('be.visible')
        cy.contains('$125.00').should('be.visible')
        cy.get('[data-cy="order-total"]').should('contain', '$135.00') // Including tax
      })
    })
  })

  describe('Account Management', () => {
    beforeEach(() => {
      // Login before account tests
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')
    })

    it('should navigate to account dashboard', () => {
      cy.get('[data-cy="account-menu"]').click()
      cy.contains('My Account').click()

      cy.shouldBeOnStorePage(testStoreSlug, 'account/dashboard')
      cy.contains('Account Dashboard').should('be.visible')
      cy.contains('Welcome back, Cypress Test User').should('be.visible')
    })

    it('should show account navigation menu', () => {
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)

      cy.get('[data-cy="account-nav"]').within(() => {
        cy.contains('Dashboard').should('be.visible')
        cy.contains('Order History').should('be.visible')
        cy.contains('Favorites').should('be.visible')
        cy.contains('Settings').should('be.visible')
      })
    })

    it('should navigate between account pages', () => {
      cy.visit(`/shop/${testStoreSlug}/account/dashboard`)

      // Navigate to order history
      cy.contains('Order History').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'account/orders')
      cy.contains('Order History').should('be.visible')

      // Navigate to favorites
      cy.contains('Favorites').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'account/favorites')
      cy.contains('My Favorites').should('be.visible')

      // Navigate to settings
      cy.contains('Settings').click()
      cy.shouldBeOnStorePage(testStoreSlug, 'account/settings')
      cy.contains('Account Settings').should('be.visible')
    })
  })

  describe('Favorites Management', () => {
    beforeEach(() => {
      // Login before favorites tests
      cy.shopperLogin(testStoreSlug, testUserEmail, testUserPassword)
      cy.wait('@shopperLogin')
    })

    it('should add items to favorites', () => {
      cy.visit(`/shop/${testStoreSlug}`)

      // Add to favorites
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="favorite-btn"]').click()
      })

      // Should show feedback
      cy.contains('Added to favorites').should('be.visible')

      // Check favorites page
      cy.visit(`/shop/${testStoreSlug}/account/favorites`)
      cy.contains('Vintage Leather Jacket').should('be.visible')
    })

    it('should remove items from favorites', () => {
      // Add to favorites first
      cy.visit(`/shop/${testStoreSlug}`)
      cy.contains('Vintage Leather Jacket').parents('[data-cy="item-card"]').within(() => {
        cy.get('[data-cy="favorite-btn"]').click()
      })

      // Go to favorites page
      cy.visit(`/shop/${testStoreSlug}/account/favorites`)

      // Remove from favorites
      cy.get('[data-cy="remove-favorite-btn"]').click()

      // Should show empty state
      cy.contains('No favorites yet').should('be.visible')
    })
  })

  describe('Error Handling', () => {
    it('should handle API errors gracefully', () => {
      cy.intercept('GET', `**/api/shop/${testStoreSlug}/info`, {
        statusCode: 500,
        body: { message: 'Internal server error' }
      })

      cy.visit(`/shop/${testStoreSlug}`, { failOnStatusCode: false })

      // Should show error message
      cy.contains('Something went wrong').should('be.visible')
    })

    it('should handle network errors', () => {
      cy.intercept('GET', `**/api/shop/${testStoreSlug}/info`, { forceNetworkError: true })

      cy.visit(`/shop/${testStoreSlug}`, { failOnStatusCode: false })

      // Should show network error message
      cy.contains('Unable to connect').should('be.visible')
    })
  })
})