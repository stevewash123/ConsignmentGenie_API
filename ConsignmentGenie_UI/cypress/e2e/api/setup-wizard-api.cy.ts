describe('Setup Wizard API Tests', () => {
  let authToken: string;
  let organizationId: string;

  before(() => {
    // Login and get auth token for API tests
    cy.loginAsOwner('owner@demoshop.com', 'password123').then(() => {
      authToken = Cypress.env('authToken');
      organizationId = Cypress.env('organizationId');
    });
  });

  describe('GET /api/setupwizard/progress', () => {
    it('should return wizard progress', () => {
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/progress',
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('currentStep');
        expect(response.body.data).to.have.property('totalSteps', 8);
        expect(response.body.data).to.have.property('progressPercentage');
        expect(response.body.data).to.have.property('steps');
        expect(response.body.data.steps).to.have.length(8);
      });
    });

    it('should require authentication', () => {
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/progress',
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(401);
      });
    });
  });

  describe('GET /api/setupwizard/step/{stepNumber}', () => {
    it('should return specific step data', () => {
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/step/1',
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stepNumber', 1);
        expect(response.body.data).to.have.property('stepTitle');
        expect(response.body.data).to.have.property('stepDescription');
        expect(response.body.data).to.have.property('isCompleted');
        expect(response.body.data).to.have.property('stepData');
      });
    });

    it('should return 400 for invalid step number', () => {
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/step/999',
        headers: {
          'Authorization': `Bearer ${authToken}`
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400);
        expect(response.body).to.have.property('error');
      });
    });
  });

  describe('POST /api/setupwizard/step/1/shop-profile', () => {
    it('should update shop profile', () => {
      const shopProfile = {
        shopProfile: {
          shopName: 'Cypress Test Shop',
          shopDescription: 'Test shop created by Cypress',
          shopEmail: 'cypress@testshop.com',
          shopPhone: '555-999-8888',
          shopAddress1: '456 Cypress Ave',
          shopCity: 'Test City',
          shopState: 'CA',
          shopZip: '90210',
          shopCountry: 'US',
          shopTimezone: 'America/New_York'
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/1/shop-profile',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: shopProfile
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stepNumber', 2);
        expect(response.body.success).to.be.true;
      });
    });

    it('should validate required fields', () => {
      const invalidProfile = {
        shopProfile: {
          shopName: '', // Required field is empty
          shopEmail: 'invalid-email' // Invalid email format
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/1/shop-profile',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: invalidProfile,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400);
      });
    });
  });

  describe('POST /api/setupwizard/step/2/business-settings', () => {
    it('should update business settings', () => {
      const businessSettings = {
        businessSettings: {
          defaultSplitPercentage: 65.0,
          taxRate: 0.085,
          currency: 'USD'
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/2/business-settings',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: businessSettings
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stepNumber', 3);
      });
    });

    it('should validate commission rate range', () => {
      const invalidSettings = {
        businessSettings: {
          defaultSplitPercentage: 150, // Invalid: over 100%
          taxRate: -0.05, // Invalid: negative tax
          currency: 'INVALID' // Invalid currency code
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/2/business-settings',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: invalidSettings,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400);
      });
    });
  });

  describe('POST /api/setupwizard/step/3/storefront-settings', () => {
    it('should update storefront settings', () => {
      const storefrontSettings = {
        storefrontSettings: {
          storeSlug: 'cypress-test-store',
          storeEnabled: true,
          shippingEnabled: true,
          shippingFlatRate: 15.00,
          pickupEnabled: true,
          pickupInstructions: 'Please call when you arrive',
          payOnPickupEnabled: true,
          onlinePaymentEnabled: false
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/3/storefront-settings',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: storefrontSettings
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stepNumber', 4);
      });
    });
  });

  describe('GET /api/setupwizard/integrations', () => {
    it('should return integration status', () => {
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/integrations',
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stripeConnected');
        expect(response.body.data).to.have.property('quickBooksConnected');
        expect(response.body.data).to.have.property('sendGridConnected');
        expect(response.body.data).to.have.property('cloudinaryConnected');
        expect(response.body.data).to.have.property('integrations');
        expect(response.body.data.integrations).to.be.an('array');
      });
    });
  });

  describe('POST /api/setupwizard/integrations/{integrationType}', () => {
    it('should setup stripe integration', () => {
      const stripeCredentials = {
        credentials: {
          publishableKey: 'pk_test_123',
          secretKey: 'sk_test_456'
        }
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/integrations/stripe',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: stripeCredentials
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.success).to.be.true;
      });
    });

    it('should return 400 for unknown integration type', () => {
      cy.request({
        method: 'POST',
        url: '/api/setupwizard/integrations/unknown-service',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: { credentials: {} },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400);
      });
    });
  });

  describe('POST /api/setupwizard/complete', () => {
    it('should complete setup wizard', () => {
      const completeRequest = {
        startTrial: true,
        subscriptionPlan: 'basic'
      };

      cy.request({
        method: 'POST',
        url: '/api/setupwizard/complete',
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        body: completeRequest
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('organizationName');
        expect(response.body.data).to.have.property('shopName');
        expect(response.body.data).to.have.property('completedAt');
        expect(response.body.data).to.have.property('trialInfo');
        expect(response.body.data.trialInfo).to.have.property('daysRemaining');
      });
    });
  });

  describe('POST /api/setupwizard/step/{stepNumber}/goto', () => {
    it('should navigate to specific step', () => {
      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/2/goto',
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.data).to.have.property('stepNumber', 2);
      });
    });

    it('should return 400 for invalid step number', () => {
      cy.request({
        method: 'POST',
        url: '/api/setupwizard/step/0/goto',
        headers: {
          'Authorization': `Bearer ${authToken}`
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400);
      });
    });
  });

  describe('Error Handling', () => {
    it('should handle server errors gracefully', () => {
      // This test would typically mock server errors or use test-specific endpoints
      cy.request({
        method: 'GET',
        url: '/api/setupwizard/nonexistent-endpoint',
        headers: {
          'Authorization': `Bearer ${authToken}`
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(404);
      });
    });
  });
});