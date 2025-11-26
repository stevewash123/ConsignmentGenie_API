describe('Complete Owner Onboarding Workflow', () => {
  const testOwner = {
    email: 'newowner@testshop.com',
    password: 'password123',
    fullName: 'Test Owner',
    organizationName: 'Test Consignment Shop',
    phone: '555-123-4567'
  };

  describe('End-to-End Owner Onboarding', () => {
    it('should complete the full owner registration and setup process', () => {
      // Step 1: Owner Registration
      cy.visit('/auth/register-owner');

      cy.get('[data-cy="full-name"]').type(testOwner.fullName);
      cy.get('[data-cy="email"]').type(testOwner.email);
      cy.get('[data-cy="phone"]').type(testOwner.phone);
      cy.get('[data-cy="password"]').type(testOwner.password);
      cy.get('[data-cy="confirm-password"]').type(testOwner.password);
      cy.get('[data-cy="organization-name"]').type(testOwner.organizationName);
      cy.get('[data-cy="terms-checkbox"]').check();

      cy.get('[data-cy="register-button"]').click();

      // Should show pending approval message
      cy.get('[data-cy="approval-pending"]').should('be.visible');
      cy.get('[data-cy="approval-message"]').should('contain.text', 'approval');

      // Step 2: Admin Approval (simulate)
      cy.approveOwnerRegistration(testOwner.email);

      // Step 3: Login after approval
      cy.visit('/auth/login');
      cy.get('[data-cy="email"]').type(testOwner.email);
      cy.get('[data-cy="password"]').type(testOwner.password);
      cy.get('[data-cy="login-button"]').click();

      // Should be redirected to setup wizard
      cy.url().should('include', '/owner/setup-wizard');

      // Step 4: Complete Setup Wizard
      cy.get('[data-cy="wizard-welcome"]').should('be.visible');

      // Shop Profile (Step 1)
      cy.get('[data-cy="shop-name"]').type('My Test Consignment Shop');
      cy.get('[data-cy="shop-description"]').type('A full-service consignment shop specializing in quality items');
      cy.get('[data-cy="shop-email"]').type('contact@mytestshop.com');
      cy.get('[data-cy="shop-phone"]').type('555-987-6543');
      cy.get('[data-cy="shop-website"]').type('https://mytestshop.com');
      cy.get('[data-cy="shop-address1"]').type('123 Main Street');
      cy.get('[data-cy="shop-city"]').type('Anytown');
      cy.get('[data-cy="shop-state"]').select('CA');
      cy.get('[data-cy="shop-zip"]').type('90210');
      cy.get('[data-cy="next-step"]').click();

      // Business Settings (Step 2)
      cy.get('[data-cy="commission-rate"]').clear().type('60');
      cy.get('[data-cy="tax-rate"]').clear().type('7.25');
      cy.get('[data-cy="currency"]').should('have.value', 'USD');
      cy.get('[data-cy="next-step"]').click();

      // Storefront Settings (Step 3)
      cy.get('[data-cy="store-slug"]').type('my-test-shop');
      cy.get('[data-cy="store-enabled"]').check();
      cy.get('[data-cy="shipping-enabled"]').check();
      cy.get('[data-cy="shipping-rate"]').type('12.99');
      cy.get('[data-cy="pickup-enabled"]').check();
      cy.get('[data-cy="pickup-instructions"]').type('Please call 30 minutes before arrival');
      cy.get('[data-cy="pay-on-pickup"]').check();
      cy.get('[data-cy="online-payment"]').check();
      cy.get('[data-cy="next-step"]').click();

      // Integration Setup (Steps 4-7)
      // For this test, we'll skip most integrations but setup one
      cy.get('[data-cy="setup-stripe"]').click();
      cy.get('[data-cy="stripe-test-mode"]').check();
      cy.get('[data-cy="stripe-publishable-key"]').type('pk_test_12345');
      cy.get('[data-cy="stripe-secret-key"]').type('sk_test_67890');
      cy.get('[data-cy="connect-stripe"]').click();

      cy.get('[data-cy="stripe-connected"]').should('be.visible');
      cy.get('[data-cy="skip-other-integrations"]').click();

      // Complete Setup (Step 8)
      cy.get('[data-cy="setup-summary"]').should('be.visible');
      cy.get('[data-cy="shop-name-summary"]').should('contain.text', 'My Test Consignment Shop');
      cy.get('[data-cy="commission-summary"]').should('contain.text', '60%');

      cy.get('[data-cy="start-trial"]').check();
      cy.get('[data-cy="complete-setup"]').click();

      // Step 5: Setup Completion
      cy.get('[data-cy="setup-complete"]').should('be.visible');
      cy.get('[data-cy="trial-started"]').should('contain.text', '14-day trial');
      cy.get('[data-cy="go-to-dashboard"]').click();

      // Step 6: Access Owner Dashboard
      cy.url().should('include', '/owner/dashboard');
      cy.get('[data-cy="welcome-message"]').should('contain.text', 'Welcome to My Test Consignment Shop');
      cy.get('[data-cy="trial-banner"]').should('be.visible');

      // Verify key dashboard elements are present
      cy.get('[data-cy="quick-stats"]').should('be.visible');
      cy.get('[data-cy="recent-items"]').should('exist');
      cy.get('[data-cy="navigation-menu"]').should('be.visible');
    });

    it('should handle setup wizard interruption and resume', () => {
      // Start setup wizard
      cy.loginAsOwner(testOwner.email, testOwner.password);
      cy.visit('/owner/setup-wizard');

      // Complete first two steps
      cy.completeShopProfile();
      cy.completeBusinessSettings();

      // Simulate interruption (logout/navigate away)
      cy.clearLocalStorage();
      cy.visit('/auth/login');

      // Login again
      cy.get('[data-cy="email"]').type(testOwner.email);
      cy.get('[data-cy="password"]').type(testOwner.password);
      cy.get('[data-cy="login-button"]').click();

      // Should resume at the correct step
      cy.url().should('include', '/owner/setup-wizard');
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 3');

      // Previous data should be preserved
      cy.get('[data-cy="step-1-indicator"]').should('have.class', 'completed');
      cy.get('[data-cy="step-2-indicator"]').should('have.class', 'completed');
    });

    it('should validate business rules during setup', () => {
      cy.loginAsOwner(testOwner.email, testOwner.password);
      cy.visit('/owner/setup-wizard');

      // Complete shop profile
      cy.completeShopProfile();

      // Test commission rate validation
      cy.get('[data-cy="commission-rate"]').clear().type('120');
      cy.get('[data-cy="next-step"]').click();
      cy.get('[data-cy="validation-error"]').should('contain.text', 'must be between');

      cy.get('[data-cy="commission-rate"]').clear().type('60');

      // Test tax rate validation
      cy.get('[data-cy="tax-rate"]').clear().type('-5');
      cy.get('[data-cy="next-step"]').click();
      cy.get('[data-cy="validation-error"]').should('contain.text', 'cannot be negative');

      cy.get('[data-cy="tax-rate"]').clear().type('8.5');
      cy.get('[data-cy="next-step"]').click();

      // Should advance to step 3
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 3');
    });
  });

  describe('Integration Testing', () => {
    beforeEach(() => {
      cy.loginAsOwner(testOwner.email, testOwner.password);
      cy.visit('/owner/setup-wizard');
      cy.completeShopProfile();
      cy.completeBusinessSettings();
      cy.completeStorefrontSettings();
    });

    it('should handle Stripe integration flow', () => {
      cy.get('[data-cy="stripe-integration-card"]').should('be.visible');
      cy.get('[data-cy="stripe-required-badge"]').should('contain.text', 'Required');

      cy.get('[data-cy="setup-stripe"]').click();

      // Stripe setup modal
      cy.get('[data-cy="stripe-modal"]').should('be.visible');
      cy.get('[data-cy="stripe-connect-option"]').should('be.visible');
      cy.get('[data-cy="stripe-manual-option"]').should('be.visible');

      // Choose manual setup
      cy.get('[data-cy="stripe-manual-option"]').click();
      cy.get('[data-cy="stripe-publishable-key"]').type('pk_test_valid_key');
      cy.get('[data-cy="stripe-secret-key"]').type('sk_test_valid_key');

      cy.intercept('POST', '/api/setupwizard/integrations/stripe', {
        statusCode: 200,
        body: { success: true, data: { stepNumber: 5 } }
      }).as('connectStripe');

      cy.get('[data-cy="test-connection"]').click();
      cy.get('[data-cy="connection-success"]').should('be.visible');

      cy.get('[data-cy="save-stripe-config"]').click();

      cy.wait('@connectStripe');
      cy.get('[data-cy="stripe-connected-status"]').should('be.visible');
    });

    it('should allow skipping optional integrations', () => {
      // QuickBooks should be optional
      cy.get('[data-cy="quickbooks-integration-card"]').should('be.visible');
      cy.get('[data-cy="quickbooks-optional-badge"]').should('contain.text', 'Optional');

      cy.get('[data-cy="skip-quickbooks"]').click();
      cy.get('[data-cy="quickbooks-skipped"]').should('be.visible');

      // Should be able to proceed to next step
      cy.get('[data-cy="continue-to-next"]').should('not.be.disabled');
    });
  });

  describe('Error Recovery', () => {
    it('should handle API failures during setup', () => {
      cy.loginAsOwner(testOwner.email, testOwner.password);
      cy.visit('/owner/setup-wizard');

      // Mock API failure
      cy.intercept('POST', '/api/setupwizard/step/1/shop-profile', {
        statusCode: 500,
        body: { error: 'Internal server error' }
      }).as('apiFailure');

      cy.completeShopProfile();

      cy.wait('@apiFailure');
      cy.get('[data-cy="error-notification"]').should('be.visible');
      cy.get('[data-cy="retry-button"]').should('be.visible');

      // Mock successful retry
      cy.intercept('POST', '/api/setupwizard/step/1/shop-profile', {
        statusCode: 200,
        body: { success: true, data: { stepNumber: 2 } }
      }).as('apiSuccess');

      cy.get('[data-cy="retry-button"]').click();

      cy.wait('@apiSuccess');
      cy.get('[data-cy="current-step"]').should('contain.text', 'Step 2');
    });

    it('should handle network connectivity issues', () => {
      cy.loginAsOwner(testOwner.email, testOwner.password);
      cy.visit('/owner/setup-wizard');

      // Simulate network failure
      cy.intercept('POST', '/api/setupwizard/step/1/shop-profile', { forceNetworkError: true }).as('networkError');

      cy.completeShopProfile();

      cy.wait('@networkError');
      cy.get('[data-cy="network-error-message"]').should('be.visible');
      cy.get('[data-cy="offline-mode-notice"]').should('contain.text', 'saved locally');
    });
  });
});