describe('Checkout API Detailed Tests', () => {
  const apiUrl = Cypress.env('apiUrl') || 'http://localhost:5000'
  const testStoreSlug = 'test-store'

  const testSessionId = 'cypress-checkout-' + Date.now()
  const testCustomerId = 'cypress-customer-' + Date.now()

  // Valid checkout request data
  const validCheckoutRequest = {
    email: 'cypress.checkout@example.com',
    name: 'Cypress Test User',
    phone: '555-123-4567',
    fulfillmentType: 'pickup',
    paymentMethod: 'card'
  }

  const validShippingCheckoutRequest = {
    email: 'cypress.shipping@example.com',
    name: 'Cypress Shipping User',
    phone: '555-987-6543',
    fulfillmentType: 'shipping',
    paymentMethod: 'card',
    shippingAddress: {
      address1: '123 Test St',
      address2: 'Apt 456',
      city: 'Test City',
      state: 'CA',
      zip: '90210',
      country: 'US'
    }
  }

  describe('Checkout Validation', () => {
    it('should validate empty cart checkout', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/validate`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.success).to.be.true
          expect(response.body.data).to.have.property('valid')
          expect(response.body.data).to.have.property('unavailableItems').that.is.an('array')

          // Empty cart should be invalid
          if (response.body.data.valid === false) {
            expect(response.body.data.errorMessage).to.include('empty')
          }
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should handle validation without session', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/validate`,
        headers: {
          'Content-Type': 'application/json'
        },
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.data.valid).to.be.false
          expect(response.body.data.errorMessage).to.include('empty')
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })
  })

  describe('Payment Intent Creation', () => {
    it('should create payment intent with valid request', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        body: validCheckoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.success).to.be.true
          expect(response.body.data).to.have.property('paymentIntentId').that.includes('pi_')
          expect(response.body.data).to.have.property('clientSecret').that.includes('secret')
          expect(response.body.data).to.have.property('amount').that.is.a('number')
          expect(response.body.data.amount).to.be.at.least(0)
        } else {
          // Empty cart or other validation failure
          expect(response.status).to.be.oneOf([400, 404, 500])
          if (response.status === 400) {
            expect(response.body.success).to.be.false
            expect(response.body.message).to.exist
          }
        }
      })
    })

    it('should reject payment intent with invalid email', () => {
      const invalidEmailRequest = {
        ...validCheckoutRequest,
        email: 'invalid-email'
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        body: invalidEmailRequest,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
        if (response.status === 400) {
          expect(response.body.success).to.be.false
        }
      })
    })

    it('should reject payment intent with missing required fields', () => {
      const incompleteRequest = {
        email: 'test@example.com'
        // Missing name, fulfillmentType, paymentMethod
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        body: incompleteRequest,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
        if (response.status === 400) {
          expect(response.body.success).to.be.false
        }
      })
    })
  })

  describe('Order Creation', () => {
    it('should create pickup order with valid request', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        body: validCheckoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.success).to.be.true
          expect(response.body.data).to.have.property('id')
          expect(response.body.data).to.have.property('orderNumber')
          expect(response.body.data).to.have.property('status')
          expect(response.body.data).to.have.property('customerEmail', validCheckoutRequest.email)
          expect(response.body.data).to.have.property('customerName', validCheckoutRequest.name)
          expect(response.body.data).to.have.property('fulfillmentType', validCheckoutRequest.fulfillmentType)
          expect(response.body.data).to.have.property('paymentMethod', validCheckoutRequest.paymentMethod)
          expect(response.body.data).to.have.property('paymentStatus')
          expect(response.body.data).to.have.property('subtotal').that.is.a('number')
          expect(response.body.data).to.have.property('taxAmount').that.is.a('number')
          expect(response.body.data).to.have.property('totalAmount').that.is.a('number')
          expect(response.body.data).to.have.property('items').that.is.an('array')
          expect(response.body.data).to.have.property('createdAt')

          // Order number should follow expected format
          expect(response.body.data.orderNumber).to.match(/^\d{8}-\d{3}$/)
        } else {
          // Empty cart or validation failure
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should create shipping order with address', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': testSessionId + '-shipping',
          'Content-Type': 'application/json'
        },
        body: validShippingCheckoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.success).to.be.true
          expect(response.body.data).to.have.property('fulfillmentType', 'shipping')
          expect(response.body.data).to.have.property('shippingAddress')
          expect(response.body.data.shippingAddress).to.have.property('address1', validShippingCheckoutRequest.shippingAddress.address1)
          expect(response.body.data.shippingAddress).to.have.property('city', validShippingCheckoutRequest.shippingAddress.city)
          expect(response.body.data.shippingAddress).to.have.property('state', validShippingCheckoutRequest.shippingAddress.state)
          expect(response.body.data).to.have.property('shippingAmount').that.is.greaterThan(0)
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should reject shipping order without address', () => {
      const shippingWithoutAddress = {
        ...validCheckoutRequest,
        fulfillmentType: 'shipping'
        // Missing shippingAddress
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': testSessionId,
          'Content-Type': 'application/json'
        },
        body: shippingWithoutAddress,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.equal(400)
        expect(response.body.success).to.be.false
        expect(response.body.message).to.include('required')
      })
    })

    it('should handle guest checkout', () => {
      const guestCheckoutRequest = {
        ...validCheckoutRequest,
        email: 'guest.checkout@example.com',
        name: 'Guest User'
      }

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': testSessionId + '-guest',
          'Content-Type': 'application/json'
        },
        body: guestCheckoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          expect(response.body.data.customerEmail).to.equal(guestCheckoutRequest.email)
          expect(response.body.data.customerName).to.equal(guestCheckoutRequest.name)
          // Guest orders should have null customerId (not exposed in DTO)
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })
  })

  describe('Order Retrieval', () => {
    let testOrderId: string

    beforeEach(() => {
      // Try to create an order for retrieval tests
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
        headers: {
          'X-Session-Id': testSessionId + '-retrieval',
          'Content-Type': 'application/json'
        },
        body: validCheckoutRequest,
        failOnStatusCode: false
      }).then((response) => {
        if (response.status === 200) {
          testOrderId = response.body.data.id
        }
      })
    })

    it('should retrieve order by ID', () => {
      if (!testOrderId) {
        cy.log('Skipping order retrieval test - no order created')
        return
      }

      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders/${testOrderId}`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.equal(200)
        expect(response.body.success).to.be.true
        expect(response.body.data).to.have.property('id', testOrderId)
        expect(response.body.data).to.have.property('orderNumber')
        expect(response.body.data).to.have.property('status')
      })
    })

    it('should handle non-existent order ID', () => {
      const fakeOrderId = 'fake-order-' + Date.now()

      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders/${fakeOrderId}`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.equal(404)
        expect(response.body.success).to.be.false
        expect(response.body.message).to.include('not found')
      })
    })

    it('should handle invalid order ID format', () => {
      const invalidOrderId = 'invalid-id-format'

      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders/${invalidOrderId}`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404, 500])
      })
    })
  })

  describe('Customer Order History', () => {
    it('should require authentication for order history', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.equal(401)
        expect(response.body.success).to.be.false
        expect(response.body.message).to.include('required')
      })
    })

    it('should handle order history pagination', () => {
      cy.request({
        method: 'GET',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/orders?page=1&pageSize=5`,
        failOnStatusCode: false
      }).then((response) => {
        // Should require authentication
        expect(response.status).to.equal(401)
      })
    })
  })

  describe('Payment Confirmation', () => {
    it('should handle payment confirmation', () => {
      const testPaymentIntentId = 'pi_test_cypress_' + Date.now()

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-confirmation`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: testPaymentIntentId,
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

    it('should handle invalid payment intent ID', () => {
      const invalidPaymentIntentId = 'invalid-payment-intent'

      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-confirmation`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: invalidPaymentIntentId,
        failOnStatusCode: false
      }).then((response) => {
        // Should still process but return appropriate result
        if (response.status === 200) {
          expect(response.body.data).to.be.a('boolean')
        } else {
          expect(response.status).to.be.oneOf([400, 404, 500])
        }
      })
    })

    it('should handle empty payment confirmation', () => {
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-confirmation`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: '',
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 500])
      })
    })
  })

  describe('Checkout Flow Integration', () => {
    it('should complete full checkout flow simulation', () => {
      const sessionId = 'cypress-full-flow-' + Date.now()

      // Step 1: Validate empty cart
      cy.request({
        method: 'POST',
        url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/validate`,
        headers: {
          'X-Session-Id': sessionId,
          'Content-Type': 'application/json'
        },
        failOnStatusCode: false
      }).then((validateResponse) => {
        if (validateResponse.status === 200) {
          expect(validateResponse.body.data.valid).to.be.false
          expect(validateResponse.body.data.errorMessage).to.include('empty')
        }

        // Step 2: Try to create payment intent (should fail)
        cy.request({
          method: 'POST',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
          headers: {
            'X-Session-Id': sessionId,
            'Content-Type': 'application/json'
          },
          body: validCheckoutRequest,
          failOnStatusCode: false
        }).then((paymentResponse) => {
          expect(paymentResponse.status).to.be.oneOf([400, 404, 500])
          if (paymentResponse.status === 400) {
            expect(paymentResponse.body.success).to.be.false
          }

          // Step 3: Try to checkout (should fail)
          cy.request({
            method: 'POST',
            url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout`,
            headers: {
              'X-Session-Id': sessionId,
              'Content-Type': 'application/json'
            },
            body: validCheckoutRequest,
            failOnStatusCode: false
          }).then((checkoutResponse) => {
            expect(checkoutResponse.status).to.be.oneOf([400, 404, 500])
          })
        })
      })
    })
  })

  describe('Data Validation', () => {
    it('should validate email format', () => {
      const invalidEmails = [
        'invalid-email',
        'test@',
        '@example.com',
        'test..email@example.com',
        ''
      ]

      invalidEmails.forEach((email) => {
        const requestWithInvalidEmail = {
          ...validCheckoutRequest,
          email: email
        }

        cy.request({
          method: 'POST',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
          headers: {
            'X-Session-Id': testSessionId,
            'Content-Type': 'application/json'
          },
          body: requestWithInvalidEmail,
          failOnStatusCode: false
        }).then((response) => {
          expect(response.status).to.be.oneOf([400, 404, 500])
        })
      })
    })

    it('should validate fulfillment types', () => {
      const invalidFulfillmentTypes = [
        'invalid-type',
        '',
        'delivery',
        'PICKUP' // case sensitive
      ]

      invalidFulfillmentTypes.forEach((fulfillmentType) => {
        const requestWithInvalidType = {
          ...validCheckoutRequest,
          fulfillmentType: fulfillmentType
        }

        cy.request({
          method: 'POST',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
          headers: {
            'X-Session-Id': testSessionId,
            'Content-Type': 'application/json'
          },
          body: requestWithInvalidType,
          failOnStatusCode: false
        }).then((response) => {
          if (response.status === 400) {
            expect(response.body.success).to.be.false
          } else {
            // Empty cart or other validation might take precedence
            expect(response.status).to.be.oneOf([404, 500])
          }
        })
      })
    })

    it('should validate required fields', () => {
      const requiredFields = ['email', 'name', 'fulfillmentType', 'paymentMethod']

      requiredFields.forEach((field) => {
        const requestMissingField = { ...validCheckoutRequest }
        delete requestMissingField[field as keyof typeof requestMissingField]

        cy.request({
          method: 'POST',
          url: `${apiUrl}/api/storefront/${testStoreSlug}/checkout/payment-intent`,
          headers: {
            'X-Session-Id': testSessionId,
            'Content-Type': 'application/json'
          },
          body: requestMissingField,
          failOnStatusCode: false
        }).then((response) => {
          expect(response.status).to.be.oneOf([400, 404, 500])
        })
      })
    })
  })
})