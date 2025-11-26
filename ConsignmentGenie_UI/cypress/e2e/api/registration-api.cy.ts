describe('Registration API Tests', () => {
  const baseUrl = Cypress.env('API_BASE_URL') || 'http://localhost:5000/api';

  describe('Store Code Validation API', () => {
    it('should validate a valid store code', () => {
      const validStoreCode = '1234';

      cy.request({
        method: 'GET',
        url: `${baseUrl}/auth/validate-store-code/${validStoreCode}`,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('isValid', true);
        expect(response.body).to.have.property('shopName');
        expect(response.body.shopName).to.be.a('string');
        expect(response.body.shopName).to.have.length.greaterThan(0);
      });
    });

    it('should reject an invalid store code', () => {
      const invalidStoreCode = '9999';

      cy.request({
        method: 'GET',
        url: `${baseUrl}/auth/validate-store-code/${invalidStoreCode}`,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('isValid', false);
        expect(response.body).to.have.property('errorMessage');
        expect(response.body.errorMessage).to.be.a('string');
      });
    });

    it('should handle malformed store code formats', () => {
      const malformedCodes = ['abc', '12345', '12', ''];

      malformedCodes.forEach((code) => {
        cy.request({
          method: 'GET',
          url: `${baseUrl}/auth/validate-store-code/${code}`,
          failOnStatusCode: false,
        }).then((response) => {
          if (response.status === 200) {
            expect(response.body).to.have.property('isValid', false);
          } else {
            expect(response.status).to.be.oneOf([400, 404]);
          }
        });
      });
    });

    it('should handle disabled store codes', () => {
      // Assuming store code '0000' is configured as disabled for testing
      const disabledStoreCode = '0000';

      cy.request({
        method: 'GET',
        url: `${baseUrl}/auth/validate-store-code/${disabledStoreCode}`,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('isValid', false);
        expect(response.body).to.have.property('errorMessage');
        expect(response.body.errorMessage).to.include('disabled');
      });
    });
  });

  describe('Owner Registration API', () => {
    it('should successfully register a new owner', () => {
      const ownerData = {
        fullName: 'API Test Owner',
        email: `api-owner-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: 'API Test Consignment Shop',
        phone: '555-123-4567'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: ownerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
        expect(response.body).to.have.property('message');
        expect(response.body.message).to.include('successfully');
      });
    });

    it('should reject owner registration with duplicate email', () => {
      const duplicateEmail = 'admin@demoshop.com'; // Known test email

      const ownerData = {
        fullName: 'Duplicate Owner',
        email: duplicateEmail,
        password: 'TestPassword123!',
        shopName: 'Duplicate Shop'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: ownerData,
        failOnStatusCode: false,
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 409]);
        expect(response.body).to.have.property('success', false);
        expect(response.body).to.have.property('message');
        expect(response.body.message.toLowerCase()).to.include('email');
      });
    });

    it('should validate required fields for owner registration', () => {
      const invalidData = {
        fullName: '',
        email: 'invalid-email',
        password: '123', // Too short
        shopName: ''
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: invalidData,
        failOnStatusCode: false,
      }).then((response) => {
        expect(response.status).to.eq(400);
        expect(response.body).to.have.property('success', false);
        expect(response.body).to.have.property('errors');
        expect(response.body.errors).to.be.an('array');
        expect(response.body.errors.length).to.be.greaterThan(0);
      });
    });

    it('should handle optional phone field for owner registration', () => {
      const ownerDataWithoutPhone = {
        fullName: 'Owner Without Phone',
        email: `owner-no-phone-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: 'No Phone Shop'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: ownerDataWithoutPhone,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
      });
    });
  });

  describe('Provider Registration API', () => {
    it('should successfully register a new provider', () => {
      const providerData = {
        storeCode: '1234',
        fullName: 'API Test Provider',
        email: `api-provider-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        phone: '555-987-6543',
        preferredPaymentMethod: 'Venmo',
        paymentDetails: '@apitestprovider'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
        expect(response.body).to.have.property('message');
        expect(response.body.message).to.include('successfully');
      });
    });

    it('should successfully register provider with minimal required fields', () => {
      const minimalProviderData = {
        storeCode: '1234',
        fullName: 'Minimal Provider',
        email: `minimal-provider-${Date.now()}@testshop.com`,
        password: 'TestPassword123!'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: minimalProviderData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
      });
    });

    it('should reject provider registration with invalid store code', () => {
      const providerData = {
        storeCode: '9999', // Invalid store code
        fullName: 'Invalid Store Provider',
        email: `invalid-store-${Date.now()}@testshop.com`,
        password: 'TestPassword123!'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
        failOnStatusCode: false,
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 404]);
        expect(response.body).to.have.property('success', false);
        expect(response.body).to.have.property('message');
        expect(response.body.message.toLowerCase()).to.include('store code');
      });
    });

    it('should reject provider registration with duplicate email', () => {
      const duplicateEmail = 'admin@demoshop.com';

      const providerData = {
        storeCode: '1234',
        fullName: 'Duplicate Provider',
        email: duplicateEmail,
        password: 'TestPassword123!'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
        failOnStatusCode: false,
      }).then((response) => {
        expect(response.status).to.be.oneOf([400, 409]);
        expect(response.body).to.have.property('success', false);
        expect(response.body).to.have.property('message');
        expect(response.body.message.toLowerCase()).to.include('email');
      });
    });

    it('should validate required fields for provider registration', () => {
      const invalidProviderData = {
        storeCode: '', // Required
        fullName: '', // Required
        email: 'invalid-email', // Invalid format
        password: '123' // Too short
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: invalidProviderData,
        failOnStatusCode: false,
      }).then((response) => {
        expect(response.status).to.eq(400);
        expect(response.body).to.have.property('success', false);
        expect(response.body).to.have.property('errors');
        expect(response.body.errors).to.be.an('array');
        expect(response.body.errors.length).to.be.greaterThan(0);
      });
    });

    it('should handle all payment method options', () => {
      const paymentMethods = [
        { method: 'Venmo', details: '@testuser' },
        { method: 'PayPal', details: 'test@paypal.com' },
        { method: 'Zelle', details: 'test@zelle.com' },
        { method: 'Bank Transfer', details: 'Account: 123456789' },
        { method: 'Check', details: '123 Main St, City, ST 12345' },
        { method: 'Cash', details: '' }
      ];

      paymentMethods.forEach((payment, index) => {
        const providerData = {
          storeCode: '1234',
          fullName: `Payment Test Provider ${index}`,
          email: `payment-test-${index}-${Date.now()}@testshop.com`,
          password: 'TestPassword123!',
          preferredPaymentMethod: payment.method,
          paymentDetails: payment.details
        };

        cy.request({
          method: 'POST',
          url: `${baseUrl}/auth/register/provider`,
          body: providerData,
        }).then((response) => {
          expect(response.status).to.eq(200);
          expect(response.body).to.have.property('success', true);
        });
      });
    });
  });

  describe('Auto-Approve Provider Registration', () => {
    it('should auto-approve provider when organization has auto-approve enabled', () => {
      // Note: This test assumes store code '5678' is configured for auto-approve
      const autoApproveStoreCode = '5678';

      const providerData = {
        storeCode: autoApproveStoreCode,
        fullName: 'Auto Approve Provider',
        email: `auto-approve-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        preferredPaymentMethod: 'Check'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
        expect(response.body).to.have.property('message');
        expect(response.body.message).to.include('approved');
      });
    });

    it('should require approval for provider when organization has auto-approve disabled', () => {
      const manualApproveStoreCode = '1234';

      const providerData = {
        storeCode: manualApproveStoreCode,
        fullName: 'Manual Approve Provider',
        email: `manual-approve-${Date.now()}@testshop.com`,
        password: 'TestPassword123!'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body).to.have.property('success', true);
        expect(response.body).to.have.property('message');
        expect(response.body.message).to.include('pending');
      });
    });
  });

  describe('API Response Format Validation', () => {
    it('should have consistent response format for store code validation', () => {
      cy.request({
        method: 'GET',
        url: `${baseUrl}/auth/validate-store-code/1234`,
      }).then((response) => {
        expect(response.body).to.have.all.keys(['isValid', 'shopName', 'errorMessage']);
        expect(response.body.isValid).to.be.a('boolean');
        if (response.body.isValid) {
          expect(response.body.shopName).to.be.a('string');
          expect(response.body.errorMessage).to.be.null;
        } else {
          expect(response.body.shopName).to.be.null;
          expect(response.body.errorMessage).to.be.a('string');
        }
      });
    });

    it('should have consistent response format for registration success', () => {
      const ownerData = {
        fullName: 'Format Test Owner',
        email: `format-test-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: 'Format Test Shop'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: ownerData,
      }).then((response) => {
        expect(response.body).to.have.property('success');
        expect(response.body).to.have.property('message');
        expect(response.body.success).to.be.a('boolean');
        expect(response.body.message).to.be.a('string');

        if (!response.body.success) {
          expect(response.body).to.have.property('errors');
          expect(response.body.errors).to.be.an('array');
        }
      });
    });
  });

  describe('API Security and Validation', () => {
    it('should sanitize input data', () => {
      const maliciousData = {
        fullName: '<script>alert("xss")</script>',
        email: `malicious-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: '<img src="x" onerror="alert(1)">'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: maliciousData,
        failOnStatusCode: false,
      }).then((response) => {
        // Should either reject malicious input or sanitize it
        if (response.status === 200 && response.body.success) {
          // If accepted, ensure scripts are sanitized in any returned data
          expect(JSON.stringify(response.body)).to.not.include('<script>');
          expect(JSON.stringify(response.body)).to.not.include('onerror');
        }
      });
    });

    it('should enforce rate limiting (if implemented)', () => {
      const requests = [];
      for (let i = 0; i < 5; i++) {
        requests.push(
          cy.request({
            method: 'GET',
            url: `${baseUrl}/auth/validate-store-code/1234`,
            failOnStatusCode: false,
          })
        );
      }

      cy.wrap(Promise.all(requests)).then((responses) => {
        // Check if any rate limiting is in place
        const statusCodes = responses.map(r => r.status);
        expect(statusCodes.every(code => [200, 429].includes(code))).to.be.true;
      });
    });

    it('should handle large payload gracefully', () => {
      const largeString = 'A'.repeat(10000);
      const largeData = {
        fullName: largeString,
        email: `large-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: largeString
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: largeData,
        failOnStatusCode: false,
        timeout: 10000,
      }).then((response) => {
        // Should handle large payload gracefully - either accept or reject appropriately
        expect(response.status).to.be.oneOf([200, 400, 413, 422]);
        if (response.body) {
          expect(response.body).to.have.property('success');
        }
      });
    });
  });

  describe('Database Integration Tests', () => {
    it('should create user record with correct approval status for owner', () => {
      const ownerData = {
        fullName: 'DB Test Owner',
        email: `db-owner-${Date.now()}@testshop.com`,
        password: 'TestPassword123!',
        shopName: 'DB Test Shop'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/owner`,
        body: ownerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.success).to.be.true;

        // Verify user cannot log in immediately (pending approval)
        cy.request({
          method: 'POST',
          url: `${baseUrl}/auth/login`,
          body: {
            email: ownerData.email,
            password: ownerData.password
          },
          failOnStatusCode: false,
        }).then((loginResponse) => {
          expect(loginResponse.status).to.be.oneOf([400, 401, 403]);
          if (loginResponse.body.message) {
            expect(loginResponse.body.message.toLowerCase()).to.include('pending');
          }
        });
      });
    });

    it('should create provider record with correct approval status', () => {
      const providerData = {
        storeCode: '1234',
        fullName: 'DB Test Provider',
        email: `db-provider-${Date.now()}@testshop.com`,
        password: 'TestPassword123!'
      };

      cy.request({
        method: 'POST',
        url: `${baseUrl}/auth/register/provider`,
        body: providerData,
      }).then((response) => {
        expect(response.status).to.eq(200);
        expect(response.body.success).to.be.true;

        // Verify provider cannot log in immediately (pending approval)
        cy.request({
          method: 'POST',
          url: `${baseUrl}/auth/login`,
          body: {
            email: providerData.email,
            password: providerData.password
          },
          failOnStatusCode: false,
        }).then((loginResponse) => {
          expect(loginResponse.status).to.be.oneOf([400, 401, 403]);
          if (loginResponse.body.message) {
            expect(loginResponse.body.message.toLowerCase()).to.include('pending');
          }
        });
      });
    });
  });

  describe('Performance Tests', () => {
    it('should respond within acceptable time limits', () => {
      const startTime = Date.now();

      cy.request({
        method: 'GET',
        url: `${baseUrl}/auth/validate-store-code/1234`,
      }).then((response) => {
        const endTime = Date.now();
        const responseTime = endTime - startTime;

        expect(response.status).to.eq(200);
        expect(responseTime).to.be.lessThan(2000); // Should respond within 2 seconds
      });
    });

    it('should handle concurrent registration requests', () => {
      const concurrentRequests = [];

      for (let i = 0; i < 3; i++) {
        const providerData = {
          storeCode: '1234',
          fullName: `Concurrent Provider ${i}`,
          email: `concurrent-${i}-${Date.now()}@testshop.com`,
          password: 'TestPassword123!'
        };

        concurrentRequests.push(
          cy.request({
            method: 'POST',
            url: `${baseUrl}/auth/register/provider`,
            body: providerData,
            failOnStatusCode: false,
          })
        );
      }

      cy.wrap(Promise.all(concurrentRequests)).then((responses) => {
        responses.forEach((response, index) => {
          expect(response.status).to.be.oneOf([200, 400, 409, 429]);
          // At least some should succeed (database should handle concurrency)
        });
      });
    });
  });
});