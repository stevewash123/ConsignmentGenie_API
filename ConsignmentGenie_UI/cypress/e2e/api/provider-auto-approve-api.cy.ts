/// <reference types="cypress" />

describe('Provider Auto-Approve API Tests', () => {
  const baseUrl = Cypress.env('apiUrl') || 'http://localhost:5000'
  const authToken = 'test-owner-token'

  beforeEach(() => {
    // Set up authentication headers
    cy.window().then((win) => {
      win.localStorage.setItem('auth_token', authToken)
    })
  })

  describe('Organization Settings API', () => {
    it('should get organization settings', () => {
      cy.request({
        method: 'GET',
        url: `${baseUrl}/api/dashboard/organization/settings`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.data).to.have.property('autoApproveProviders')
        expect(response.body.data).to.have.property('storeCodeEnabled')
        expect(response.body.data).to.have.property('storeCode')
      })
    })

    it('should update auto-approve setting to enabled', () => {
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: {
          autoApproveProviders: true
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.data).to.have.property('autoApproveProviders', true)
        expect(response.body.message).to.contain('auto-approval enabled')
      })
    })

    it('should update auto-approve setting to disabled', () => {
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: {
          autoApproveProviders: false
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.data).to.have.property('autoApproveProviders', false)
        expect(response.body.message).to.contain('auto-approval disabled')
      })
    })

    it('should require authentication for settings endpoints', () => {
      cy.request({
        method: 'GET',
        url: `${baseUrl}/api/dashboard/organization/settings`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(401)
      })
    })

    it('should validate request body for auto-approve update', () => {
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: {
          invalidField: true
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400)
      })
    })
  })

  describe('Provider Registration API with Auto-Approve', () => {
    beforeEach(() => {
      // Ensure auto-approve is enabled for these tests
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: {
          autoApproveProviders: true
        }
      })
    })

    it('should auto-approve provider with valid store code when enabled', () => {
      const uniqueEmail = `test-${Date.now()}@test.com`

      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'DEMO',
          fullName: 'John Doe',
          email: uniqueEmail,
          password: 'password123',
          phone: '555-123-4567',
          paymentDetails: 'Check preferred'
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.message).to.contain('Welcome to')
        expect(response.body.message).to.contain('You can now start adding items')
      })
    })

    it('should require manual approval when auto-approve disabled', () => {
      // First disable auto-approve
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: {
          autoApproveProviders: false
        }
      })

      const uniqueEmail = `test-manual-${Date.now()}@test.com`

      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'DEMO',
          fullName: 'Jane Smith',
          email: uniqueEmail,
          password: 'password123',
          phone: '555-987-6543',
          paymentDetails: 'PayPal preferred'
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.message).to.contain('pending approval')
      })
    })

    it('should validate store code before registration', () => {
      cy.request({
        method: 'GET',
        url: `${baseUrl}/api/registration/validate-store-code/INVALID`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('isValid', false)
      })
    })

    it('should reject registration with invalid store code', () => {
      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'INVALID',
          fullName: 'Invalid User',
          email: 'invalid@test.com',
          password: 'password123'
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', false)
        expect(response.body.message).to.contain('Invalid store code')
      })
    })

    it('should validate required fields', () => {
      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'DEMO'
          // Missing required fields
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400)
      })
    })

    it('should reject duplicate email registrations', () => {
      const duplicateEmail = 'duplicate@test.com'

      // First registration
      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'DEMO',
          fullName: 'First User',
          email: duplicateEmail,
          password: 'password123',
          phone: '555-111-1111'
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
      })

      // Duplicate registration attempt
      cy.request({
        method: 'POST',
        url: `${baseUrl}/api/registration/register/provider`,
        headers: {
          'Content-Type': 'application/json'
        },
        body: {
          storeCode: 'DEMO',
          fullName: 'Second User',
          email: duplicateEmail,
          password: 'password123',
          phone: '555-222-2222'
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', false)
        expect(response.body.message).to.contain('Email already registered')
      })
    })
  })

  describe('Provider Status Tracking', () => {
    it('should track provider status in database after registration', () => {
      // This would require a separate API endpoint to check provider status
      // or could be verified through the providers list endpoint

      cy.request({
        method: 'GET',
        url: `${baseUrl}/api/providers`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      }).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body).to.have.property('success', true)
        expect(response.body.data).to.be.an('array')

        // Check if any providers have been auto-approved
        const providers = response.body.data
        const autoApprovedProviders = providers.filter(p => p.status === 'Active')
        expect(autoApprovedProviders.length).to.be.greaterThan(0)
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle server errors gracefully', () => {
      // Test with malformed request
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: 'invalid-json',
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 500])
      })
    })

    it('should validate authorization for owner-only endpoints', () => {
      // Test with invalid token
      cy.request({
        method: 'PUT',
        url: `${baseUrl}/api/dashboard/organization/settings/auto-approve`,
        headers: {
          'Authorization': 'Bearer invalid-token',
          'Content-Type': 'application/json'
        },
        body: {
          autoApproveProviders: true
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(401)
      })
    })
  })

  describe('Performance and Load', () => {
    it('should handle multiple rapid registration requests', () => {
      const requests = []

      // Create multiple registration requests
      for (let i = 0; i < 5; i++) {
        requests.push(
          cy.request({
            method: 'POST',
            url: `${baseUrl}/api/registration/register/provider`,
            headers: {
              'Content-Type': 'application/json'
            },
            body: {
              storeCode: 'DEMO',
              fullName: `Test User ${i}`,
              email: `test-load-${i}-${Date.now()}@test.com`,
              password: 'password123',
              phone: `555-000-000${i}`
            }
          })
        )
      }

      // All requests should complete successfully
      Promise.all(requests).then((responses) => {
        responses.forEach((response) => {
          expect(response.status).to.eq(200)
          expect(response.body).to.have.property('success', true)
        })
      })
    })

    it('should respond within acceptable time limits', () => {
      const startTime = Date.now()

      cy.request({
        method: 'GET',
        url: `${baseUrl}/api/dashboard/organization/settings`
      }).then((response) => {
        const endTime = Date.now()
        const responseTime = endTime - startTime

        expect(response.status).to.eq(200)
        expect(responseTime).to.be.lessThan(2000) // Should respond within 2 seconds
      })
    })
  })
})