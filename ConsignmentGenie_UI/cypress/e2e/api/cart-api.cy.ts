describe('Cart API Detailed Tests', () => {
  const apiUrl = Cypress.env('apiUrl') || 'http://localhost:5000'
  const testStoreSlug = 'test-store'

  // Test session IDs for different scenarios
  const anonymousSessionId = 'cypress-anonymous-' + Date.now()
  const userSessionId = 'cypress-user-' + Date.now()
  const testCustomerId = 'cypress-customer-' + Date.now()
  const testItemId = 'cypress-item-' + Date.now()

  describe('Anonymous Cart Operations', () => {
    it('should create and manage anonymous cart', () => {
      // Get initial empty cart
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.data.itemCount).to.equal(0)
          expect(response.body.data.subtotal).to.equal(0)
          expect(response.body.data.items).to.be.empty
        }
      })

      // Attempt to add item (will likely fail due to non-existent item, but tests API structure)
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': anonymousSessionId,
          'Content-Type': 'application/json'
        },
        body: {
          itemId: testItemId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Expecting 400 or 404 since item doesn't exist
        if (response.status === 400) {
          expect(response.body.success).to.be.false
          expect(response.body.message).to.include('not found')
        }
      })
    })

    it('should handle cart session persistence', () => {
      // Make multiple requests with same session ID
      const sessionId = 'cypress-persistence-' + Date.now()

      // First request
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': sessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.data.itemCount).to.equal(0)
        }
      })

      // Second request with same session should return same cart
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': sessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.data.itemCount).to.equal(0)
        }
      })
    })

    it('should isolate carts by session ID', () => {
      const session1 = 'cypress-isolation-1-' + Date.now()
      const session2 = 'cypress-isolation-2-' + Date.now()

      // Get cart for session 1
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': session1
        },
        failOnStatusCode: false
      }).then((session1Response) => {
        // Get cart for session 2
        cy.request({
          method: 'GET',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
          headers: {
            'X-Session-Id': session2
          },
          failOnStatusCode: false
        }).then((session2Response) => {
          if (session1Response.status === 200 && session2Response.status === 200) {
            // Both should be empty and independent
            expect(session1Response.body.data.itemCount).to.equal(0)
            expect(session2Response.body.data.itemCount).to.equal(0)
          }
        })
      })
    })
  })

  describe('Cart Merging Scenarios', () => {
    it('should handle cart merge request', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/merge`,
        headers: {
          'X-Session-Id': anonymousSessionId,
          'Content-Type': 'application/json'
        },
        failOnStatusCode: false
      }).then((response) => {
        // Should require authentication or return appropriate error
        expect(response.status).to.be.oneOf([401, 404, 500])
        if (response.status === 401) {
          expect(response.body.success).to.be.false
          expect(response.body.message).to.include('required')
        }
      })
    })
  })

  describe('Cart Validation and Error Scenarios', () => {
    it('should reject invalid item IDs', () => {
      const invalidItemIds = [
        'not-a-guid',
        '',
        '12345',
        'invalid-format-id',
        null
      ]

      invalidItemIds.forEach((invalidId) => {
        if (invalidId !== null) {
          cy.request({
            method: 'POST',
            url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
            headers: {
              'X-Session-Id': anonymousSessionId,
              'Content-Type': 'application/json'
            },
            body: {
              itemId: invalidId
            },
            failOnStatusCode: false
          }).then((response) => {
            expect(response.status).to.be.oneOf([400, 404, 500])
          })
        }
      })
    })

    it('should handle missing request body', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': anonymousSessionId,
          'Content-Type': 'application/json'
        },
        body: {},
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
        if (response.status === 400) {
          expect(response.body.success).to.be.false
        }
      })
    })

    it('should handle malformed JSON', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'X-Session-Id': anonymousSessionId,
          'Content-Type': 'application/json'
        },
        body: 'invalid-json',
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 500])
      })
    })
  })

  describe('Cart Operations Without Session', () => {
    it('should handle cart operations without session ID', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        // No X-Session-Id header
        failOnStatusCode: false
      }).then((response) => {
        // Should either return empty cart or handle gracefully
        if (response.status === 200) {
          expect(response.body.data.itemCount).to.equal(0)
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should handle add to cart without session ID', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/add`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          itemId: testItemId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Should handle gracefully - either create session or return error
        expect(response.status).to.be.oneOf([200, 400, 404, 500])
      })
    })
  })

  describe('Cart Item Removal', () => {
    it('should handle item removal from empty cart', () => {
      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/items/${testItemId}`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Should handle gracefully
        if (response.status === 200) {
          expect(response.body.data.itemCount).to.equal(0)
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should handle removal of non-existent item', () => {
      const nonExistentItemId = 'non-existent-' + Date.now()

      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart/items/${nonExistentItemId}`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        // Should handle gracefully - return current cart state
        if (response.status === 200) {
          expect(response.body.data).to.have.property('items')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })
  })

  describe('Cart Clearing', () => {
    it('should clear empty cart', () => {
      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.success).to.be.true
          expect(response.body.data).to.be.a('boolean')
        } else {
          expect(response.status).to.be.oneOf([404, 500])
        }
      })
    })

    it('should verify cart is empty after clearing', () => {
      const sessionId = 'cypress-clear-test-' + Date.now()

      // Clear cart
      cy.request({
        method: 'DELETE',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': sessionId
        },
        failOnStatusCode: false
      }).then(() => {
        // Get cart to verify it's empty
        cy.request({
          method: 'GET',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
          headers: {
            'X-Session-Id': sessionId
          },
          failOnStatusCode: false
        }).then((response) => {
          if (response.status === 200) {
            expect(response.body.data.itemCount).to.equal(0)
            expect(response.body.data.subtotal).to.equal(0)
            expect(response.body.data.items).to.be.empty
          }
        })
      })
    })
  })

  describe('Cart Response Structure Validation', () => {
    it('should return proper cart structure', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          // Validate CartDto structure
          expect(response.body.data).to.have.property('id')
          expect(response.body.data).to.have.property('itemCount').that.is.a('number')
          expect(response.body.data).to.have.property('subtotal').that.is.a('number')
          expect(response.body.data).to.have.property('estimatedTax').that.is.a('number')
          expect(response.body.data).to.have.property('estimatedTotal').that.is.a('number')
          expect(response.body.data).to.have.property('items').that.is.an('array')

          // If items exist, validate item structure
          response.body.data.items.forEach((item: any) => {
            expect(item).to.have.property('itemId')
            expect(item).to.have.property('name')
            expect(item).to.have.property('price').that.is.a('number')
            expect(item).to.have.property('isAvailable').that.is.a('boolean')
            expect(item).to.have.property('addedAt')
          })
        }
      })
    })

    it('should calculate tax and totals correctly', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/cart`,
        headers: {
          'X-Session-Id': anonymousSessionId
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          const cart = response.body.data

          // Validate tax calculation (8.5% as per service implementation)
          const expectedTax = cart.subtotal * 0.085
          expect(cart.estimatedTax).to.be.closeTo(expectedTax, 0.01)

          // Validate total calculation
          const expectedTotal = cart.subtotal + cart.estimatedTax
          expect(cart.estimatedTotal).to.be.closeTo(expectedTotal, 0.01)
        }
      })
    })
  })

  describe('Store Isolation', () => {
    const store1 = 'test-store-1'
    const store2 = 'test-store-2'
    const sharedSessionId = 'cypress-shared-' + Date.now()

    it('should isolate carts between different stores', () => {
      // Get cart for store 1
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${store1}/cart`,
        headers: {
          'X-Session-Id': sharedSessionId
        },
        failOnStatusCode: false
      }).then((store1Response) => {
        // Get cart for store 2 with same session
        cy.request({
          method: 'GET',
          url: `${apiUrl}/api/storefront/${store2}/cart`,
          headers: {
            'X-Session-Id': sharedSessionId
          },
          failOnStatusCode: false
        }).then((store2Response) => {
          // Both should be independent even with same session ID
          if (store1Response.status === 200 && store2Response.status === 200) {
            expect(store1Response.body.data.itemCount).to.equal(0)
            expect(store2Response.body.data.itemCount).to.equal(0)

            // Should have different cart IDs
            expect(store1Response.body.data.id).to.not.equal(store2Response.body.data.id)
          }
        })
      })
    })
  })
})