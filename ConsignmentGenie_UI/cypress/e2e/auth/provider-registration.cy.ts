describe('Provider Registration Workflow', () => {
  const testProvider = {
    storeCode: '1234',
    fullName: 'Test Provider User',
    email: `test-provider-${Date.now()}@testshop.com`,
    phone: '555-987-6543',
    password: 'TestPassword123!',
    preferredPaymentMethod: 'Venmo',
    paymentDetails: '@testprovider'
  };

  const invalidStoreCode = '9999';
  const validShopName = 'Demo Consignment Shop';

  beforeEach(() => {
    cy.visit('/register/provider');
  });

  describe('Step 1: Store Code Validation', () => {
    it('should display the store code validation form correctly', () => {
      cy.get('h2').should('contain', 'Join as a Provider');
      cy.get('h3').should('contain', 'Step 1: Enter Store Code');
      cy.get('form').should('be.visible');

      // Check store code input is present
      cy.get('input[name="storeCode"]').should('be.visible');
      cy.get('button[type="submit"]').should('contain', 'Validate Store Code');

      // Registration form should not be visible
      cy.get('[data-cy="registration-form"]').should('not.exist');
    });

    it('should validate store code format', () => {
      // Test invalid formats
      cy.get('input[name="storeCode"]').type('123');
      cy.get('button[type="submit"]').should('be.disabled');

      cy.get('input[name="storeCode"]').clear().type('12345');
      cy.get('button[type="submit"]').should('be.disabled');

      cy.get('input[name="storeCode"]').clear().type('abcd');
      cy.get('button[type="submit"]').should('be.disabled');

      // Test valid format
      cy.get('input[name="storeCode"]').clear().type(testProvider.storeCode);
      cy.get('button[type="submit"]').should('not.be.disabled');
    });

    it('should validate valid store code successfully', () => {
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();

      // Should show loading state
      cy.get('button[type="submit"]').should('contain', 'Validating...');

      // Should show success state and proceed to step 2
      cy.get('.bg-green-50').should('be.visible');
      cy.get('.bg-green-50').should('contain', 'Store Code Validated');
      cy.get('.bg-green-50').should('contain', validShopName);

      // Registration form should now be visible
      cy.get('[data-cy="registration-form"]').should('be.visible');
      cy.get('h3').should('contain', 'Step 2: Complete Registration');
    });

    it('should handle invalid store code gracefully', () => {
      cy.get('input[name="storeCode"]').type(invalidStoreCode);
      cy.get('button[type="submit"]').click();

      // Should show error message
      cy.get('.text-red-600').should('be.visible');
      cy.get('.text-red-600').should('contain', 'Invalid');

      // Registration form should not be visible
      cy.get('[data-cy="registration-form"]').should('not.exist');
    });

    it('should allow resetting store code', () => {
      // First validate a store code
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();

      // Wait for validation success
      cy.get('.bg-green-50').should('be.visible');

      // Click the "Change store code" button
      cy.get('button').contains('Change store code').click();

      // Should reset to step 1
      cy.get('h3').should('contain', 'Step 1: Enter Store Code');
      cy.get('[data-cy="registration-form"]').should('not.exist');
      cy.get('input[name="storeCode"]').should('have.value', '');
    });
  });

  describe('Step 2: Registration Form', () => {
    beforeEach(() => {
      // Navigate to step 2 by validating store code first
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();
      cy.get('[data-cy="registration-form"]').should('be.visible');
    });

    it('should display the registration form correctly', () => {
      cy.get('h3').should('contain', 'Step 2: Complete Registration');

      // Check personal information section
      cy.get('h4').should('contain', 'Personal Information');
      cy.get('input[name="fullName"]').should('be.visible');
      cy.get('input[name="email"]').should('be.visible');
      cy.get('input[name="phone"]').should('be.visible');
      cy.get('input[name="password"]').should('be.visible');

      // Check payment preferences section
      cy.get('h4').should('contain', 'Payment Preferences');
      cy.get('select[name="preferredPaymentMethod"]').should('be.visible');

      // Check submit button
      cy.get('button[type="submit"]').should('contain', 'Create Provider Account');
      cy.get('button[type="submit"]').should('be.disabled');
    });

    it('should validate form fields properly', () => {
      // Test email validation
      cy.get('input[name="email"]').type('invalid-email');
      cy.get('input[name="fullName"]').click(); // trigger blur
      cy.get('.text-red-600').should('contain', 'Please enter a valid email address');

      // Clear and enter valid email
      cy.get('input[name="email"]').clear().type(testProvider.email);
      cy.get('input[name="fullName"]').click();

      // Test required field validation
      cy.get('input[name="fullName"]').clear();
      cy.get('input[name="email"]').click();
      cy.get('.text-red-600').should('contain', 'Full name is required');

      // Test password minimum length
      cy.get('input[name="password"]').type('123');
      cy.get('input[name="fullName"]').click();
      cy.get('.text-red-600').should('contain', 'Password must be at least 8 characters');

      // Test phone number validation
      cy.get('input[name="phone"]').type('invalid-phone');
      cy.get('input[name="fullName"]').click();
      cy.get('.text-red-600').should('contain', 'Please enter a valid phone number');
    });

    it('should show payment details field when payment method is selected', () => {
      // Initially no payment details field
      cy.get('input[name="paymentDetails"]').should('not.exist');

      // Select Venmo
      cy.get('select[name="preferredPaymentMethod"]').select('Venmo');
      cy.get('input[name="paymentDetails"]').should('be.visible');
      cy.get('input[name="paymentDetails"]').should('have.attr', 'placeholder', '@username');

      // Select PayPal
      cy.get('select[name="preferredPaymentMethod"]').select('PayPal');
      cy.get('input[name="paymentDetails"]').should('have.attr', 'placeholder', 'email@example.com');

      // Select Check
      cy.get('select[name="preferredPaymentMethod"]').select('Check');
      cy.get('input[name="paymentDetails"]').should('have.attr', 'placeholder', 'Mailing address');
    });

    it('should show appropriate help text for payment methods', () => {
      cy.get('select[name="preferredPaymentMethod"]').select('Venmo');
      cy.get('.text-gray-500').should('contain', 'Enter your Venmo username');

      cy.get('select[name="preferredPaymentMethod"]').select('Zelle');
      cy.get('.text-gray-500').should('contain', 'Enter email or phone number registered with Zelle');

      cy.get('select[name="preferredPaymentMethod"]').select('Cash');
      cy.get('.text-gray-500').should('contain', 'No additional details needed for cash pickup');
    });
  });

  describe('Complete Registration Flow', () => {
    beforeEach(() => {
      // Navigate to step 2
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();
      cy.get('[data-cy="registration-form"]').should('be.visible');
    });

    it('should successfully register a new provider with all details', () => {
      // Fill out the form completely
      cy.get('input[name="fullName"]').type(testProvider.fullName);
      cy.get('input[name="email"]').type(testProvider.email);
      cy.get('input[name="phone"]').type(testProvider.phone);
      cy.get('input[name="password"]').type(testProvider.password);
      cy.get('select[name="preferredPaymentMethod"]').select(testProvider.preferredPaymentMethod);
      cy.get('input[name="paymentDetails"]').type(testProvider.paymentDetails);

      // Form should be valid now
      cy.get('button[type="submit"]').should('not.be.disabled');

      // Submit the form
      cy.get('button[type="submit"]').click();

      // Should show loading state
      cy.get('button[type="submit"]').should('contain', 'Creating Account...');

      // Should redirect to success page
      cy.url().should('include', '/register/success');
      cy.get('h1').should('contain', 'Account Created!');

      // Should show correct success message for provider
      cy.get('.success-content').should('contain', testProvider.fullName.split(' ')[0]);
      cy.get('.success-content').should('contain', validShopName);
      cy.get('.success-content').should('contain', 'pending approval');
    });

    it('should successfully register a provider with minimal required details', () => {
      // Fill out only required fields
      cy.get('input[name="fullName"]').type('Minimal Provider');
      cy.get('input[name="email"]').type(`minimal-provider-${Date.now()}@testshop.com`);
      cy.get('input[name="password"]').type('TestPassword123!');

      // Form should be valid now
      cy.get('button[type="submit"]').should('not.be.disabled');

      // Submit the form
      cy.get('button[type="submit"]').click();

      // Should redirect to success page
      cy.url().should('include', '/register/success');
      cy.get('h1').should('contain', 'Account Created!');
    });

    it('should handle registration errors gracefully', () => {
      // Try to register with an email that might already exist
      cy.get('input[name="fullName"]').type('Duplicate Provider');
      cy.get('input[name="email"]').type('admin@demoshop.com'); // Known test email
      cy.get('input[name="password"]').type('TestPassword123!');

      cy.get('button[type="submit"]').click();

      // Should show error message
      cy.get('.bg-red-50').should('be.visible');
      cy.get('.text-red-800').should('contain', 'Registration Failed');
    });
  });

  describe('Navigation and Accessibility', () => {
    it('should allow navigation to login page', () => {
      cy.get('a[routerLink="/login"]').should('be.visible');
      cy.get('a[routerLink="/login"]').click();
      cy.url().should('include', '/login');
    });

    it('should have responsive design', () => {
      // Test mobile viewport
      cy.viewport(375, 667);
      cy.get('.bg-white').should('be.visible');
      cy.get('input[name="storeCode"]').should('be.visible');

      // Test tablet viewport
      cy.viewport(768, 1024);
      cy.get('.bg-white').should('be.visible');
      cy.get('input[name="storeCode"]').should('be.visible');
    });

    it('should have proper accessibility attributes', () => {
      cy.get('input[name="storeCode"]').should('have.attr', 'required');
      cy.get('label[for="storeCode"]').should('be.visible');

      // Navigate to step 2 to check form accessibility
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();
      cy.get('[data-cy="registration-form"]').should('be.visible');

      cy.get('input[name="fullName"]').should('have.attr', 'required');
      cy.get('input[name="email"]').should('have.attr', 'required');
      cy.get('input[name="password"]').should('have.attr', 'required');
      cy.get('label[for="fullName"]').should('be.visible');
      cy.get('label[for="email"]').should('be.visible');
      cy.get('label[for="password"]').should('be.visible');
    });
  });

  describe('Form Validation Edge Cases', () => {
    beforeEach(() => {
      // Navigate to step 2
      cy.get('input[name="storeCode"]').type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();
      cy.get('[data-cy="registration-form"]').should('be.visible');
    });

    it('should handle special characters in names', () => {
      cy.get('input[name="fullName"]').type("O'Connor-Smith Jr.");
      cy.get('input[name="email"]').type('test@example.com');
      cy.get('input[name="password"]').type('TestPassword123!');

      cy.get('button[type="submit"]').should('not.be.disabled');
    });

    it('should validate international phone numbers', () => {
      cy.get('input[name="phone"]').type('+44 20 7946 0958');
      cy.get('input[name="fullName"]').click();
      // Should not show validation error for international format
      cy.get('.text-red-600').should('not.contain', 'phone number');
    });

    it('should handle long payment details', () => {
      cy.get('select[name="preferredPaymentMethod"]').select('Bank Transfer');
      cy.get('input[name="paymentDetails"]').type('Very long bank account details that might exceed normal limits but should still be acceptable for bank transfer information');
      cy.get('input[name="fullName"]').click();
      // Should not show validation error
      cy.get('.text-red-600').should('not.contain', 'payment details');
    });
  });

  describe('Error Recovery', () => {
    it('should allow retry after store code validation failure', () => {
      // Try invalid store code first
      cy.get('input[name="storeCode"]').type(invalidStoreCode);
      cy.get('button[type="submit"]').click();
      cy.get('.text-red-600').should('be.visible');

      // Clear and try valid store code
      cy.get('input[name="storeCode"]').clear().type(testProvider.storeCode);
      cy.get('button[type="submit"]').click();

      // Should proceed to step 2
      cy.get('[data-cy="registration-form"]').should('be.visible');
    });

    it('should retain form data when payment method changes', () => {
      // Fill some form data
      cy.get('input[name="fullName"]').type(testProvider.fullName);
      cy.get('input[name="email"]').type(testProvider.email);

      // Change payment method
      cy.get('select[name="preferredPaymentMethod"]').select('Venmo');
      cy.get('input[name="paymentDetails"]').type('@venmo');

      cy.get('select[name="preferredPaymentMethod"]').select('PayPal');
      cy.get('input[name="paymentDetails"]').clear().type('paypal@email.com');

      // Original form data should be retained
      cy.get('input[name="fullName"]').should('have.value', testProvider.fullName);
      cy.get('input[name="email"]').should('have.value', testProvider.email);
    });
  });
});

describe('Provider Registration Success Page', () => {
  const testParams = {
    type: 'provider',
    shopName: 'Test Consignment Shop',
    email: 'provider@example.com',
    fullName: 'Test Provider'
  };

  beforeEach(() => {
    const queryParams = new URLSearchParams(testParams).toString();
    cy.visit(`/register/success?${queryParams}`);
  });

  it('should display provider-specific success message', () => {
    cy.get('h1').should('contain', 'Account Created!');
    cy.get('.greeting').should('contain', 'Thanks for registering, Test!');
    cy.get('.status-message').should('contain', `Your account is pending approval from ${testParams.shopName}`);
    cy.get('.email-info').should('contain', testParams.email);
  });

  it('should show provider-specific next steps', () => {
    cy.get('.next-steps h3').should('contain', 'What happens next:');
    cy.get('.next-steps li').should('contain', 'The shop owner will review your request');
    cy.get('.next-steps li').should('contain', 'you can access your Provider Portal');
    cy.get('.next-steps li').should('contain', 'track your items and earnings');
  });

  it('should not show owner-specific content', () => {
    cy.get('.next-steps').should('not.contain', 'Our team will review your shop registration');
    cy.get('.next-steps').should('not.contain', 'you\'ll get your unique store code');
  });

  it('should provide navigation options', () => {
    cy.get('.home-btn').should('be.visible');
    cy.get('.login-btn').should('be.visible');

    cy.get('.login-btn').click();
    cy.url().should('include', '/login');
  });
});

describe('Provider Login with Pending Approval', () => {
  const pendingProvider = {
    email: 'pending-provider@testshop.com',
    password: 'TestPassword123!',
    storeCode: '1234'
  };

  it('should show pending approval message for unapproved provider', () => {
    // First register a provider (this will be pending by default)
    cy.visit('/register/provider');

    // Step 1: Validate store code
    cy.get('input[name="storeCode"]').type(pendingProvider.storeCode);
    cy.get('button[type="submit"]').click();
    cy.get('[data-cy="registration-form"]').should('be.visible');

    // Step 2: Complete registration
    cy.get('input[name="fullName"]').type('Pending Provider');
    cy.get('input[name="email"]').type(pendingProvider.email);
    cy.get('input[name="password"]').type(pendingProvider.password);
    cy.get('button[type="submit"]').click();

    // Wait for success page
    cy.url().should('include', '/register/success');

    // Now try to login with the pending account
    cy.visit('/login');

    cy.get('input[name="email"]').type(pendingProvider.email);
    cy.get('input[name="password"]').type(pendingProvider.password);
    cy.get('button[type="submit"]').click();

    // Should show pending approval message
    cy.get('.error-message').should('be.visible');
    cy.get('.error-message').should('contain', 'pending approval');

    // Should stay on login page
    cy.url().should('include', '/login');
  });
});