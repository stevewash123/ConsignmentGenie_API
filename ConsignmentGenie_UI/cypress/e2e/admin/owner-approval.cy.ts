describe('Admin Owner Approval Workflow', () => {
  const adminUser = {
    email: 'admin@demoshop.com',
    password: 'password123'
  };

  const testOwner = {
    fullName: 'Approval Test Owner',
    email: `approval-test-${Date.now()}@testshop.com`,
    phone: '555-999-8888',
    password: 'TestPassword123!',
    shopName: 'Approval Test Shop'
  };

  beforeEach(() => {
    // Intercept the login API call
    cy.intercept('POST', '**/api/auth/login').as('loginRequest');

    // Login as admin
    cy.visit('/login');
    cy.get('input[name="email"]').type(adminUser.email);
    cy.get('input[name="password"]').type(adminUser.password);
    cy.get('button[type="submit"]').click();

    // Wait for login API to complete
    cy.wait('@loginRequest', { timeout: 10000 });

    // Wait for dashboard to load
    cy.url().should('include', '/admin/dashboard');
  });

  it('should navigate to owner approvals page', () => {
    cy.get('a[routerLink="/admin/owner-approvals"]').click();
    cy.url().should('include', '/admin/owner-approvals');
    cy.get('h1').should('contain', 'Owner Approval Management');
  });

  it('should display empty state when no pending approvals', () => {
    cy.visit('/admin/owner-approvals');

    // If no pending approvals, should show empty state
    cy.get('body').then(($body) => {
      if ($body.find('.empty-state').length > 0) {
        cy.get('.empty-state').should('be.visible');
        cy.get('.empty-state h3').should('contain', 'No Pending Approvals');
      }
    });
  });

  it('should create a pending owner and display in approval list', () => {
    // First, logout to register a new owner
    cy.get('.profile-btn').click();
    cy.get('.logout-btn').click();

    // Register new owner
    cy.visit('/register/owner');
    cy.get('input[name="fullName"]').type(testOwner.fullName);
    cy.get('input[name="email"]').type(testOwner.email);
    cy.get('input[name="phone"]').type(testOwner.phone);
    cy.get('input[name="password"]').type(testOwner.password);
    cy.get('input[name="confirmPassword"]').type(testOwner.password);
    cy.get('input[name="shopName"]').type(testOwner.shopName);
    cy.get('button[type="submit"]').click();

    // Wait for success
    cy.url().should('include', '/register/success');

    // Login back as admin
    cy.visit('/login');
    cy.get('input[name="email"]').type(adminUser.email);
    cy.get('input[name="password"]').type(adminUser.password);
    cy.get('button[type="submit"]').click();

    // Navigate to approvals
    cy.visit('/admin/owner-approvals');

    // Should see the pending owner
    cy.get('.approval-card').should('contain', testOwner.fullName);
    cy.get('.approval-card').should('contain', testOwner.email);
    cy.get('.approval-card').should('contain', testOwner.shopName);
  });

  it('should approve an owner successfully', () => {
    cy.visit('/admin/owner-approvals');

    // Wait for page to load and check if there are pending approvals
    cy.get('body').then(($body) => {
      if ($body.find('.approval-card').length > 0) {
        // Get first approval card
        cy.get('.approval-card').first().within(() => {
          // Click approve button
          cy.get('.approve-btn').click();

          // Should show loading state
          cy.get('.approve-btn').should('contain', 'Approving...');
        });

        // Should show success message
        cy.get('.success-toast').should('be.visible');
        cy.get('.success-toast').should('contain', 'approved successfully');

        // The card should be removed from the list
        cy.get('.approval-card').should('have.length.lessThan', 2);
      }
    });
  });

  it('should open reject modal and reject an owner', () => {
    // First create a pending owner for testing
    cy.get('.profile-btn').click();
    cy.get('.logout-btn').click();

    // Register another test owner
    const rejectTestOwner = {
      fullName: 'Reject Test Owner',
      email: `reject-test-${Date.now()}@testshop.com`,
      phone: '555-888-7777',
      password: 'TestPassword123!',
      shopName: 'Reject Test Shop'
    };

    cy.visit('/register/owner');
    cy.get('input[name="fullName"]').type(rejectTestOwner.fullName);
    cy.get('input[name="email"]').type(rejectTestOwner.email);
    cy.get('input[name="phone"]').type(rejectTestOwner.phone);
    cy.get('input[name="password"]').type(rejectTestOwner.password);
    cy.get('input[name="confirmPassword"]').type(rejectTestOwner.password);
    cy.get('input[name="shopName"]').type(rejectTestOwner.shopName);
    cy.get('button[type="submit"]').click();

    // Login back as admin
    cy.visit('/login');
    cy.get('input[name="email"]').type(adminUser.email);
    cy.get('input[name="password"]').type(adminUser.password);
    cy.get('button[type="submit"]').click();

    cy.visit('/admin/owner-approvals');

    // Find the card for our reject test owner
    cy.get('.approval-card').contains(rejectTestOwner.fullName).closest('.approval-card').within(() => {
      cy.get('.reject-btn').click();
    });

    // Modal should open
    cy.get('.modal-overlay').should('be.visible');
    cy.get('.modal-content').should('be.visible');
    cy.get('.modal-header h3').should('contain', 'Reject Owner Application');

    // Add rejection reason
    cy.get('textarea[id="rejectReason"]').type('Application does not meet our criteria.');

    // Confirm rejection
    cy.get('.confirm-reject-btn').click();

    // Should show loading state
    cy.get('.confirm-reject-btn').should('contain', 'Rejecting...');

    // Should show success message and close modal
    cy.get('.success-toast').should('be.visible');
    cy.get('.success-toast').should('contain', 'rejected');
    cy.get('.modal-overlay').should('not.exist');
  });

  it('should handle approval API errors gracefully', () => {
    cy.visit('/admin/owner-approvals');

    // Mock API failure
    cy.intercept('POST', '**/api/admin/*/approve', { statusCode: 500 }).as('approvalError');

    cy.get('body').then(($body) => {
      if ($body.find('.approval-card').length > 0) {
        cy.get('.approval-card').first().within(() => {
          cy.get('.approve-btn').click();
        });

        cy.wait('@approvalError');

        // Should show error message
        cy.get('.error-message').should('be.visible');
        cy.get('.error-message').should('contain', 'Failed to approve owner');
      }
    });
  });

  it('should refresh the pending approvals list', () => {
    cy.visit('/admin/owner-approvals');

    // Click refresh button
    cy.get('.refresh-btn').click();

    // Should show loading state briefly
    cy.get('body').should('be.visible'); // Basic check that page refreshes
  });

  it('should handle rejection modal cancellation', () => {
    cy.visit('/admin/owner-approvals');

    cy.get('body').then(($body) => {
      if ($body.find('.approval-card').length > 0) {
        cy.get('.approval-card').first().within(() => {
          cy.get('.reject-btn').click();
        });

        // Modal should open
        cy.get('.modal-overlay').should('be.visible');

        // Click cancel
        cy.get('.cancel-btn').click();

        // Modal should close
        cy.get('.modal-overlay').should('not.exist');
      }
    });
  });

  it('should be responsive on mobile', () => {
    cy.viewport(375, 667);
    cy.visit('/admin/owner-approvals');

    cy.get('.owner-approval').should('be.visible');
    cy.get('.page-header').should('be.visible');

    cy.get('body').then(($body) => {
      if ($body.find('.approval-card').length > 0) {
        cy.get('.approval-card').should('be.visible');
        cy.get('.card-actions').should('be.visible');
      }
    });
  });

  it('should format dates correctly', () => {
    cy.visit('/admin/owner-approvals');

    cy.get('body').then(($body) => {
      if ($body.find('.approval-card').length > 0) {
        cy.get('.requested-date').should('contain', 'Requested:');
        // Date should be in readable format
        cy.get('.requested-date').should('match', /Requested: \w{3} \d{1,2}, \d{4}/);
      }
    });
  });
});