describe('Owner Setup Wizard', () => {
  beforeEach(() => {
    // Login as owner and navigate to setup wizard
    cy.loginAsOwner('owner@demoshop.com', 'password123');
    cy.visit('/owner/setup-wizard');
  });

  it('should display wizard progress and steps', () => {
    cy.get('[data-cy="wizard-progress"]').should('be.visible');
    cy.get('[data-cy="step-indicator"]').should('have.length', 8);
    cy.get('[data-cy="current-step"]').should('contain.text', 'Step 1');
  });

  describe('Step 1: Shop Profile', () => {
    it('should complete shop profile step', () => {
      cy.get('[data-cy="shop-name"]').type('Test Consignment Shop');
      cy.get('[data-cy="shop-description"]').type('A test shop for consignment items');
      cy.get('[data-cy="shop-email"]').type('test@testshop.com');
      cy.get('[data-cy="shop-phone"]').type('555-123-4567');
      cy.get('[data-cy="shop-address1"]').type('123 Test Street');
      cy.get('[data-cy="shop-city"]').type('Test City');
      cy.get('[data-cy="shop-state"]').select('CA');
      cy.get('[data-cy="shop-zip"]').type('90210');

      cy.get('[data-cy="next-step"]').click();

      // Should advance to step 2
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 2');
    });

    it('should validate required fields', () => {
      cy.get('[data-cy="next-step"]').click();

      cy.get('[data-cy="shop-name"]').should('have.class', 'ng-invalid');
      cy.get('[data-cy="shop-email"]').should('have.class', 'ng-invalid');
    });
  });

  describe('Step 2: Business Settings', () => {
    beforeEach(() => {
      // Complete step 1 first
      cy.completeShopProfile();
    });

    it('should configure business settings', () => {
      cy.get('[data-cy="commission-rate"]').clear().type('65');
      cy.get('[data-cy="tax-rate"]').clear().type('8.5');
      cy.get('[data-cy="currency"]').select('USD');

      cy.get('[data-cy="next-step"]').click();

      // Should advance to step 3
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 3');
    });

    it('should validate commission rate range', () => {
      cy.get('[data-cy="commission-rate"]').clear().type('150');
      cy.get('[data-cy="next-step"]').click();

      cy.get('[data-cy="commission-rate-error"]').should('be.visible');
    });
  });

  describe('Step 3: Storefront Settings', () => {
    beforeEach(() => {
      cy.completeShopProfile();
      cy.completeBusinessSettings();
    });

    it('should configure storefront options', () => {
      cy.get('[data-cy="store-slug"]').type('test-store');
      cy.get('[data-cy="store-enabled"]').check();
      cy.get('[data-cy="shipping-enabled"]').check();
      cy.get('[data-cy="shipping-rate"]').type('10.00');
      cy.get('[data-cy="pickup-enabled"]').check();
      cy.get('[data-cy="online-payment"]').check();

      cy.get('[data-cy="next-step"]').click();

      // Should advance to step 4
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 4');
    });
  });

  describe('Steps 4-7: Integrations', () => {
    beforeEach(() => {
      cy.completeShopProfile();
      cy.completeBusinessSettings();
      cy.completeStorefrontSettings();
    });

    it('should show integration options', () => {
      cy.get('[data-cy="stripe-integration"]').should('be.visible');
      cy.get('[data-cy="quickbooks-integration"]').should('be.visible');
      cy.get('[data-cy="sendgrid-integration"]').should('be.visible');
      cy.get('[data-cy="cloudinary-integration"]').should('be.visible');
    });

    it('should allow skipping integrations', () => {
      cy.get('[data-cy="skip-integrations"]').click();

      // Should advance to step 8
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 8');
    });

    it('should setup Stripe integration', () => {
      cy.get('[data-cy="setup-stripe"]').click();

      cy.get('[data-cy="stripe-modal"]').should('be.visible');
      cy.get('[data-cy="stripe-test-mode"]').check();
      cy.get('[data-cy="stripe-connect"]').click();

      cy.get('[data-cy="stripe-status"]').should('contain.text', 'Connected');
    });
  });

  describe('Step 8: Complete Setup', () => {
    beforeEach(() => {
      cy.completeAllSteps();
    });

    it('should complete wizard and start trial', () => {
      cy.get('[data-cy="start-trial"]').check();
      cy.get('[data-cy="complete-setup"]').click();

      cy.get('[data-cy="success-message"]').should('be.visible');
      cy.get('[data-cy="trial-info"]').should('contain.text', '14-day trial');

      // Should redirect to owner dashboard
      cy.url().should('include', '/owner/dashboard');
    });

    it('should show setup summary', () => {
      cy.get('[data-cy="setup-summary"]').should('be.visible');
      cy.get('[data-cy="shop-name-summary"]').should('contain.text', 'Test Consignment Shop');
      cy.get('[data-cy="commission-summary"]').should('contain.text', '65%');
      cy.get('[data-cy="integrations-summary"]').should('be.visible');
    });
  });

  describe('Navigation and Progress', () => {
    it('should allow going back to previous steps', () => {
      cy.completeShopProfile();

      cy.get('[data-cy="previous-step"]').click();
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 1');

      // Data should be preserved
      cy.get('[data-cy="shop-name"]').should('have.value', 'Test Consignment Shop');
    });

    it('should show progress percentage', () => {
      cy.get('[data-cy="progress-bar"]').should('exist');
      cy.get('[data-cy="progress-percentage"]').should('contain.text', '12.5%'); // Step 1 of 8

      cy.completeShopProfile();
      cy.get('[data-cy="progress-percentage"]').should('contain.text', '25%'); // Step 2 of 8
    });

    it('should allow jumping to completed steps', () => {
      cy.completeShopProfile();
      cy.completeBusinessSettings();

      cy.get('[data-cy="step-1-indicator"]').click();
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 1');

      // Should not allow jumping to incomplete steps
      cy.get('[data-cy="step-4-indicator"]').should('have.class', 'disabled');
    });
  });

  describe('API Integration', () => {
    it('should save progress to backend', () => {
      cy.intercept('POST', '/api/setupwizard/step/1/shop-profile').as('saveShopProfile');

      cy.completeShopProfile();

      cy.wait('@saveShopProfile').then((interception) => {
        expect(interception.response?.statusCode).to.eq(200);
        expect(interception.request.body).to.have.property('shopProfile');
      });
    });

    it('should handle API errors gracefully', () => {
      cy.intercept('POST', '/api/setupwizard/step/1/shop-profile', {
        statusCode: 500,
        body: { error: 'Server error' }
      }).as('saveError');

      cy.completeShopProfile();

      cy.get('[data-cy="error-message"]').should('be.visible');
      cy.get('[data-cy="retry-button"]').should('be.visible');
    });

    it('should load existing progress', () => {
      // Mock existing progress
      cy.intercept('GET', '/api/setupwizard/progress', {
        body: {
          data: {
            currentStep: 3,
            progressPercentage: 37.5,
            steps: [
              { stepNumber: 1, isCompleted: true },
              { stepNumber: 2, isCompleted: true },
              { stepNumber: 3, isCompleted: false, isCurrentStep: true }
            ]
          }
        }
      });

      cy.visit('/owner/setup-wizard');

      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 3');
      cy.get('[data-cy="progress-percentage"]').should('contain.text', '37.5%');
    });
  });
});

// Custom commands for setup wizard testing
Cypress.Commands.add('completeShopProfile', () => {
  cy.get('[data-cy="shop-name"]').type('Test Consignment Shop');
  cy.get('[data-cy="shop-description"]').type('A test shop for consignment items');
  cy.get('[data-cy="shop-email"]').type('test@testshop.com');
  cy.get('[data-cy="shop-phone"]').type('555-123-4567');
  cy.get('[data-cy="shop-address1"]').type('123 Test Street');
  cy.get('[data-cy="shop-city"]').type('Test City');
  cy.get('[data-cy="shop-state"]').select('CA');
  cy.get('[data-cy="shop-zip"]').type('90210');
  cy.get('[data-cy="next-step"]').click();
});

Cypress.Commands.add('completeBusinessSettings', () => {
  cy.get('[data-cy="commission-rate"]').clear().type('65');
  cy.get('[data-cy="tax-rate"]').clear().type('8.5');
  cy.get('[data-cy="currency"]').select('USD');
  cy.get('[data-cy="next-step"]').click();
});

Cypress.Commands.add('completeStorefrontSettings', () => {
  cy.get('[data-cy="store-slug"]').type('test-store');
  cy.get('[data-cy="store-enabled"]').check();
  cy.get('[data-cy="pickup-enabled"]').check();
  cy.get('[data-cy="next-step"]').click();
});

Cypress.Commands.add('completeAllSteps', () => {
  cy.completeShopProfile();
  cy.completeBusinessSettings();
  cy.completeStorefrontSettings();

  // Skip integrations for quick completion
  cy.get('[data-cy="skip-integrations"]').click();
});

declare global {
  namespace Cypress {
    interface Chainable {
      completeShopProfile(): Chainable<void>;
      completeBusinessSettings(): Chainable<void>;
      completeStorefrontSettings(): Chainable<void>;
      completeAllSteps(): Chainable<void>;
    }
  }
}