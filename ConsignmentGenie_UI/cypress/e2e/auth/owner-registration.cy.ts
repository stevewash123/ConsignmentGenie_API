describe('Owner Registration Workflow', () => {
  const testOwner = {
    fullName: 'Test Shop Owner',
    email: `test-owner-${Date.now()}@testshop.com`,
    phone: '555-123-4567',
    password: 'TestPassword123!',
    shopName: 'Test Consignment Shop'
  };

  beforeEach(() => {
    cy.visit('/register/owner');
  });

  it('should display the owner registration form correctly', () => {
    cy.get('h1').should('contain', 'Register Your Consignment Shop');
    cy.get('form').should('be.visible');

    // Check that all form fields are present
    cy.get('input[name="fullName"]').should('be.visible');
    cy.get('input[name="email"]').should('be.visible');
    cy.get('input[name="phone"]').should('be.visible');
    cy.get('input[name="password"]').should('be.visible');
    cy.get('input[name="confirmPassword"]').should('be.visible');
    cy.get('input[name="shopName"]').should('be.visible');

    // Check submit button is present but disabled initially
    cy.get('button[type="submit"]').should('be.visible').and('be.disabled');
  });

  it('should validate form fields properly', () => {
    // Test email validation
    cy.get('input[name="email"]').type('invalid-email');
    cy.get('input[name="fullName"]').click(); // trigger blur
    cy.get('.field-error').should('contain', 'Please enter a valid email address');

    // Clear and enter valid email
    cy.get('input[name="email"]').clear().type(testOwner.email);
    cy.get('.field-error').should('not.exist');

    // Test password confirmation mismatch
    cy.get('input[name="password"]').type('password123');
    cy.get('input[name="confirmPassword"]').type('different-password');
    cy.get('input[name="fullName"]').click(); // trigger blur
    cy.get('.field-error').should('contain', 'Passwords do not match');

    // Fix password confirmation
    cy.get('input[name="confirmPassword"]').clear().type('password123');
    cy.get('.field-error').should('not.exist');

    // Test required field validation
    cy.get('input[name="shopName"]').clear();
    cy.get('input[name="fullName"]').click();
    cy.get('.field-error').should('contain', 'Shop name is required');
  });

  it('should show password visibility toggle', () => {
    cy.get('input[name="password"]').should('have.attr', 'type', 'password');
    cy.get('.password-toggle').first().click();
    cy.get('input[name="password"]').should('have.attr', 'type', 'text');
    cy.get('.password-toggle').first().click();
    cy.get('input[name="password"]').should('have.attr', 'type', 'password');
  });

  it('should successfully register a new owner', () => {
    // Fill out the form
    cy.get('input[name="fullName"]').type(testOwner.fullName);
    cy.get('input[name="email"]').type(testOwner.email);
    cy.get('input[name="phone"]').type(testOwner.phone);
    cy.get('input[name="password"]').type(testOwner.password);
    cy.get('input[name="confirmPassword"]').type(testOwner.password);
    cy.get('input[name="shopName"]').type(testOwner.shopName);

    // Form should be valid now
    cy.get('button[type="submit"]').should('not.be.disabled');

    // Submit the form
    cy.get('button[type="submit"]').click();

    // Should show loading state
    cy.get('button[type="submit"]').should('contain', 'Creating Account...');

    // Should redirect to success page
    cy.url().should('include', '/register/success');
    cy.get('h1').should('contain', 'Registration Successful!');

    // Should show correct success message for owner
    cy.get('.success-content').should('contain', testOwner.fullName);
    cy.get('.success-content').should('contain', testOwner.shopName);
    cy.get('.success-content').should('contain', 'pending admin approval');
  });

  it('should handle registration errors gracefully', () => {
    // Try to register with an email that might already exist
    cy.get('input[name="fullName"]').type('Test User');
    cy.get('input[name="email"]').type('admin@demoshop.com'); // Known test email
    cy.get('input[name="phone"]').type('555-123-4567');
    cy.get('input[name="password"]').type('TestPassword123!');
    cy.get('input[name="confirmPassword"]').type('TestPassword123!');
    cy.get('input[name="shopName"]').type('Test Shop');

    cy.get('button[type="submit"]').click();

    // Should show error message
    cy.get('.error-message').should('be.visible');
    cy.get('.error-message').should('contain.text', 'already exists');
  });

  it('should allow navigation to login page', () => {
    cy.get('.login-link').should('be.visible');
    cy.get('.login-link a').click();
    cy.url().should('include', '/login');
  });

  it('should have responsive design', () => {
    // Test mobile viewport
    cy.viewport(375, 667);
    cy.get('.registration-card').should('be.visible');
    cy.get('form').should('be.visible');

    // Test tablet viewport
    cy.viewport(768, 1024);
    cy.get('.registration-card').should('be.visible');
    cy.get('form').should('be.visible');
  });
});

describe('Owner Login with Pending Approval', () => {
  const pendingUser = {
    email: 'pending-owner@testshop.com',
    password: 'TestPassword123!'
  };

  beforeEach(() => {
    cy.visit('/login');
  });

  it('should show pending approval message for unApproved owner', () => {
    // First register a user (this will be pending by default)
    cy.visit('/register/owner');

    cy.get('input[name="fullName"]').type('Pending Owner');
    cy.get('input[name="email"]').type(pendingUser.email);
    cy.get('input[name="phone"]').type('555-987-6543');
    cy.get('input[name="password"]').type(pendingUser.password);
    cy.get('input[name="confirmPassword"]').type(pendingUser.password);
    cy.get('input[name="shopName"]').type('Pending Test Shop');

    cy.get('button[type="submit"]').click();

    // Wait for success page
    cy.url().should('include', '/register/success');

    // Now try to login with the pending account
    cy.visit('/login');

    cy.get('input[name="email"]').type(pendingUser.email);
    cy.get('input[name="password"]').type(pendingUser.password);
    cy.get('button[type="submit"]').click();

    // Should show pending approval message
    cy.get('.error-message').should('be.visible');
    cy.get('.error-message').should('contain', 'pending admin approval');

    // Should stay on login page
    cy.url().should('include', '/login');
  });
});

describe('Registration Success Page', () => {
  beforeEach(() => {
    cy.visit('/register/success?type=owner&shopName=Test Shop&email=test@example.com');
  });

  it('should display success message correctly', () => {
    cy.get('h1').should('contain', 'Registration Successful!');
    cy.get('.success-content').should('contain', 'Test Shop');
    cy.get('.success-content').should('contain', 'pending admin approval');
    cy.get('.next-steps').should('be.visible');
  });

  it('should provide link back to login', () => {
    cy.get('a[href="/login"]').should('be.visible');
    cy.get('a[href="/login"]').click();
    cy.url().should('include', '/login');
  });
});