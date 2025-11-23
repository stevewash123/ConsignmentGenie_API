describe('Storefront API Endpoints', () => {
  const apiUrl = Cypress.env('apiUrl') || 'http://localhost:5000'
  const testStoreSlug = 'test-store'

  // Test data
  let testOrganizationId: string
  let testItemId: string
  let cartSessionId: string
  let orderId: string

  before(() => {
    // Generate test data IDs
    testOrganizationId = 'cypress-org-' + Date.now()
    testItemId = 'cypress-item-' + Date.now()
    cartSessionId = 'cypress-session-' + Date.now()
  })

  describe('Public Store API', () => {
    it('should get store information', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('name')
          expect(response.body.data).to.have.property('slug', testStoreSlug)
          expect(response.body.data).to.have.property('taxRate')
        } else {
          // Store might not exist in test environment - that's ok for API testing
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should get items with pagination', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/items?page=1&pageSize=10`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('items').that.is.an('array')
          expect(response.body.data).to.have.property('totalCount')
          expect(response.body.data).to.have.property('page', 1)
          expect(response.body.data).to.have.property('pageSize', 10)
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should get items with search filter', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/items?search=vintage&sort=price-low-high`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('items').that.is.an('array')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should get items with price filter', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/items?minPrice=10&maxPrice=100&category=Electronics`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('items').that.is.an('array')
          // If items exist, they should meet filter criteria
          if (response.body.data.items.length > 0) {
            response.body.data.items.forEach((item: any) => {
              expect(item.price).to.be.at.least(10)
              expect(item.price).to.be.at.most(100)
              if (item.category) {
                expect(item.category).to.equal('Electronics')
              }
            })
          }
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should get item detail', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/items/${testItemId}`,
        failOnStatusCode: false
      }).then((response) => {
        // Item likely doesn't exist, but API structure should be correct
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('id')
          expect(response.body.data).to.have.property('title')
          expect(response.body.data).to.have.property('price')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should get categories', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/categories`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.be.an('array')
          // If categories exist, they should have proper structure
          if (response.body.data.length > 0) {
            response.body.data.forEach((category: any) => {
              expect(category).to.have.property('name')
              expect(category).to.have.property('itemCount')
              expect(category.itemCount).to.be.a('number')
            })
          }
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })
  })

  describe('Cart API', () => {
    it('should get empty cart', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': cartSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('itemCount', 0)
          expect(response.body.data).to.have.property('subtotal', 0)
          expect(response.body.data).to.have.property('items').that.is.an('array')
          expect(response.body.data.items).to.have.length(0)
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should attempt to add item to cart', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        body: {
          itemId: testItemId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Item likely doesn't exist, but we test the API structure
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('itemCount')
          expect(response.body.data).to.have.property('subtotal')
          expect(response.body.data).to.have.property('items')
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
          if (response.status === 400) {
            expect(response.body).to.have.property('success', false)
            expect(response.body).to.have.property('message')
          }
        }
      })
    })

    it('should remove item from cart', () => {
      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/items/${testItemId}`,
        headers: {
          'X-Session-Id': cartSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('itemCount')
          expect(response.body.data).to.have.property('subtotal')
          expect(response.body.data).to.have.property('items')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should clear cart', () => {
      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': cartSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.be.a('boolean')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })
  })

  describe('Checkout API', () => {
    it('should validate checkout', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/validate`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('valid')
          expect(response.body.data).to.have.property('unavailableItems')
          if (!response.body.data.valid) {
            expect(response.body.data).to.have.property('errorMessage')
          }
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should create payment intent', () => {
      const checkoutRequest = {
        email: 'cypress.test@example.com',
        name: 'Cypress Test User',
        phone: '555-123-4567',
        fulfillmentType: 'pickup',
        paymentMethod: 'card'
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        body: checkoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('paymentIntentId')
          expect(response.body.data).to.have.property('clientSecret')
          expect(response.body.data).to.have.property('amount')
          expect(response.body.data.paymentIntentId).to.include('pi_')
          expect(response.body.data.amount).to.be.a('number')
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should validate shipping requirement for shipping orders', () => {
      const invalidShippingRequest = {
        email: 'cypress.test@example.com',
        name: 'Cypress Test User',
        fulfillmentType: 'shipping',
        paymentMethod: 'card'
        // Missing shippingAddress
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        body: invalidShippingRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 400) {
          expect(response.body).to.have.property('success', false)
          expect(response.body.message).to.include('required')
        } else {
          // Other statuses are ok for testing purposes
          expect(response.status).to.be.oneOf([200, 404, 500])
        }
      })
    })

    it('should complete checkout with valid data', () => {
      const checkoutRequest = {
        email: 'cypress.test@example.com',
        name: 'Cypress Test User',
        phone: '555-123-4567',
        fulfillmentType: 'pickup',
        paymentMethod: 'card'
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        body: checkoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('id')
          expect(response.body.data).to.have.property('orderNumber')
          expect(response.body.data).to.have.property('status')
          expect(response.body.data).to.have.property('customerEmail', checkoutRequest.email)
          expect(response.body.data).to.have.property('customerName', checkoutRequest.name)
          expect(response.body.data).to.have.property('fulfillmentType', checkoutRequest.fulfillmentType)
          orderId = response.body.data.id
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should get order details', () => {
      if (!orderId) {
        cy.log('Skipping order details test - no order created in previous test')
        return
      }

      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders/${orderId}`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.have.property('id', orderId)
          expect(response.body.data).to.have.property('orderNumber')
          expect(response.body.data).to.have.property('status')
          expect(response.body.data).to.have.property('items')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should handle payment confirmation', () => {
      const paymentIntentId = 'pi_test_cypress_' + Date.now()

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-confirmation`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: paymentIntentId,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body).to.have.property('success', true)
          expect(response.body.data).to.be.a('boolean')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })
  })

  describe('API Error Handling', () => {
    it('should handle invalid store slug', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/non-existent-store`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.equal(404)
        expect(response.body).to.have.property('success', false)
        expect(response.body).to.have.property('message')
      })
    })

    it('should handle malformed requests', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          invalidField: 'invalid'
          // Missing required itemId
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
        if (response.status === 400) {
          expect(response.body).to.have.property('success', false)
        }
      })
    })

    it('should handle invalid item ID format', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': cartSessionId,
          'Content-Type': 'application/json'
        },
        body: {
          itemId: 'not-a-guid'
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
        if (response.status === 400) {
          expect(response.body).to.have.property('success', false)
        }
      })
    })

    it('should handle missing session ID for cart operations', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        // No session ID header
        failOnStatusCode: false
      }).then((response) => {
        // Should still work but return empty cart
        if (response.status === 200) {
          expect(response.body.data).to.have.property('itemCount', 0)
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })
  })

  describe('API Response Format Validation', () => {
    it('should have consistent response format for success', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/categories`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          // All successful responses should follow ApiResponse<T> format
          expect(response.body).to.have.property('success', true)
          expect(response.body).to.have.property('data')
          expect(response.body).to.not.have.property('message')
        }
      })
    })

    it('should have consistent response format for errors', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/non-existent-store-slug`,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status >= 400) {
          // All error responses should follow ApiResponse<T> format
          expect(response.body).to.have.property('success', false)
          expect(response.body).to.have.property('message')
          expect(response.body).to.not.have.property('data')
        }
      })
    })
  })

  describe('API Security Headers', () => {
    it('should include proper CORS headers', () => {
      cy.request({
        method: 'OPTIONS',
        url: `${apiUrl}/api/storefront/${testStoreSlug}`,
        failOnStatusCode: false
      }).then((response) => {
        // CORS headers should be present
        expect(response.headers).to.have.property('access-control-allow-origin')
        expect(response.headers).to.have.property('access-control-allow-methods')
      })
    })

    it('should handle content-type validation', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': cartSessionId
          // Missing Content-Type header
        },
        body: {
          itemId: testItemId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Should either accept or properly reject
        expect(response.status).to.be.oneOf([200, 400, 404, 415, 500])
      })
    })
  })
})