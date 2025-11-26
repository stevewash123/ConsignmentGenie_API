describe('Complete Owner Registration and Approval Workflow', () => {
  const adminUser = {
    email: 'admin@demoshop.com',
    password: 'password123'
  };

  const newOwner = {
    fullName: 'Complete Workflow Owner',
    email: `workflow-${Date.now()}@completetest.com`,
    phone: '555-777-9999',
    password: 'WorkflowTest123!',
    shopName: 'Complete Workflow Shop'
  };

  it('should complete the entire owner registration and approval workflow', () => {
    // Step 1: Owner Registration
    cy.log('Step 1: Owner Registration');
    cy.visit('/register/owner');

    // Fill out registration form
    cy.get('input[name="fullName"]').type(newOwner.fullName);
    cy.get('input[name="email"]').type(newOwner.email);
    cy.get('input[name="phone"]').type(newOwner.phone);
    cy.get('input[name="password"]').type(newOwner.password);
    cy.get('input[name="confirmPassword"]').type(newOwner.password);
    cy.get('input[name="shopName"]').type(newOwner.shopName);

    // Submit registration
    cy.get('button[type="submit"]').click();

    // Verify success page
    cy.url().should('include', '/register/success');
    cy.get('h1').should('contain', 'Registration Successful!');
    cy.get('.success-content').should('contain', newOwner.shopName);

    // Step 2: Verify Login is Blocked (Pending Approval)
    cy.log('Step 2: Verify Login is Blocked (Pending Approval)');
    cy.visit('/login');

    cy.get('input[name="email"]').type(newOwner.email);
    cy.get('input[name="password"]').type(newOwner.password);
    cy.get('button[type="submit"]').click();

    // Should show pending approval message
    cy.get('.error-message').should('be.visible');
    cy.get('.error-message').should('contain', 'pending admin approval');

    // Step 3: Admin Login and Approval Process
    cy.log('Step 3: Admin Login and Approval Process');

    // Login as admin
    cy.get('input[name="email"]').clear().type(adminUser.email);
    cy.get('input[name="password"]').clear().type(adminUser.password);
    cy.get('button[type="submit"]').click();

    // Should redirect to admin dashboard
    cy.url().should('include', '/admin/dashboard');

    // Navigate to owner approvals
    cy.get('a[routerLink="/admin/owner-approvals"]').click();
    cy.url().should('include', '/admin/owner-approvals');

    // Find and approve the new owner
    cy.get('.approval-card').contains(newOwner.fullName).closest('.approval-card').within(() => {
      // Verify owner details are displayed correctly
      cy.contains(newOwner.fullName);
      cy.contains(newOwner.email);
      cy.contains(newOwner.shopName);

      // Click approve
      cy.get('.approve-btn').click();
    });

    // Should show success message
    cy.get('.success-toast').should('be.visible');
    cy.get('.success-toast').should('contain', 'approved successfully');

    // The owner should be removed from pending list
    cy.get('.approval-card').should('not.contain', newOwner.fullName);

    // Step 4: Admin Logout
    cy.log('Step 4: Admin Logout');
    cy.get('.profile-btn').click();
    cy.get('.logout-btn').click();
    cy.url().should('include', '/login');

    // Step 5: Owner Login After Approval
    cy.log('Step 5: Owner Login After Approval');
    cy.get('input[name="email"]').type(newOwner.email);
    cy.get('input[name="password"]').type(newOwner.password);
    cy.get('button[type="submit"]').click();

    // Should now successfully login and redirect to owner dashboard
    cy.url().should('include', '/owner/dashboard');

    // Verify owner is logged in successfully
    cy.get('body').should('contain', 'Dashboard'); // Basic check that dashboard loads

    // Step 6: Verify Owner Can Access Their Area
    cy.log('Step 6: Verify Owner Can Access Their Area');

    // Check that owner navigation is available
    cy.get('nav').should('be.visible');

    // The workflow is complete
    cy.log('✅ Complete Owner Registration and Approval Workflow Successful');
  });

  it('should handle rejection workflow correctly', () => {
    const rejectedOwner = {
      fullName: 'Rejected Owner',
      email: `rejected-${Date.now()}@rejecttest.com`,
      phone: '555-666-5555',
      password: 'RejectedTest123!',
      shopName: 'Rejected Test Shop'
    };

    // Step 1: Register new owner
    cy.log('Step 1: Register Owner for Rejection');
    cy.visit('/register/owner');

    cy.get('input[name="fullName"]').type(rejectedOwner.fullName);
    cy.get('input[name="email"]').type(rejectedOwner.email);
    cy.get('input[name="phone"]').type(rejectedOwner.phone);
    cy.get('input[name="password"]').type(rejectedOwner.password);
    cy.get('input[name="confirmPassword"]').type(rejectedOwner.password);
    cy.get('input[name="shopName"]').type(rejectedOwner.shopName);

    cy.get('button[type="submit"]').click();
    cy.url().should('include', '/register/success');

    // Step 2: Admin login and rejection
    cy.log('Step 2: Admin Login and Rejection');
    cy.visit('/login');

    cy.get('input[name="email"]').type(adminUser.email);
    cy.get('input[name="password"]').type(adminUser.password);
    cy.get('button[type="submit"]').click();

    cy.visit('/admin/owner-approvals');

    // Find and reject the owner
    cy.get('.approval-card').contains(rejectedOwner.fullName).closest('.approval-card').within(() => {
      cy.get('.reject-btn').click();
    });

    // Fill out rejection modal
    cy.get('.modal-overlay').should('be.visible');
    cy.get('textarea[id="rejectReason"]').type('Test rejection for workflow verification.');
    cy.get('.confirm-reject-btn').click();

    // Should show success message
    cy.get('.success-toast').should('be.visible');
    cy.get('.success-toast').should('contain', 'rejected');

    // Step 3: Admin logout and owner login attempt
    cy.log('Step 3: Verify Rejected Owner Cannot Login');
    cy.get('.profile-btn').click();
    cy.get('.logout-btn').click();

    // Try to login as rejected owner
    cy.get('input[name="email"]').type(rejectedOwner.email);
    cy.get('input[name="password"]').type(rejectedOwner.password);
    cy.get('button[type="submit"]').click();

    // Should show rejection message
    cy.get('.error-message').should('be.visible');
    cy.get('.error-message').should('contain', 'rejected');

    cy.log('✅ Owner Rejection Workflow Completed Successfully');
  });

  it('should handle multiple pending approvals correctly', () => {
    const owners = [
      {
        fullName: 'Bulk Test Owner 1',
        email: `bulk1-${Date.now()}@bulktest.com`,
        phone: '555-111-1111',
        password: 'BulkTest123!',
        shopName: 'Bulk Test Shop 1'
      },
      {
        fullName: 'Bulk Test Owner 2',
        email: `bulk2-${Date.now()}@bulktest.com`,
        phone: '555-222-2222',
        password: 'BulkTest123!',
        shopName: 'Bulk Test Shop 2'
      }
    ];

    // Register multiple owners
    cy.log('Step 1: Register Multiple Owners');
    owners.forEach((owner, index) => {
      cy.visit('/register/owner');

      cy.get('input[name="fullName"]').type(owner.fullName);
      cy.get('input[name="email"]').type(owner.email);
      cy.get('input[name="phone"]').type(owner.phone);
      cy.get('input[name="password"]').type(owner.password);
      cy.get('input[name="confirmPassword"]').type(owner.password);
      cy.get('input[name="shopName"]').type(owner.shopName);

      cy.get('button[type="submit"]').click();
      cy.url().should('include', '/register/success');
    });

    // Login as admin and verify all are in pending list
    cy.log('Step 2: Verify Multiple Pending Approvals');
    cy.visit('/login');

    cy.get('input[name="email"]').type(adminUser.email);
    cy.get('input[name="password"]').type(adminUser.password);
    cy.get('button[type="submit"]').click();

    cy.visit('/admin/owner-approvals');

    // Should see both owners in the list
    cy.get('.approval-card').should('contain', owners[0].fullName);
    cy.get('.approval-card').should('contain', owners[1].fullName);

    // Approve first, reject second
    cy.get('.approval-card').contains(owners[0].fullName).closest('.approval-card').within(() => {
      cy.get('.approve-btn').click();
    });

    cy.get('.success-toast').should('contain', 'approved successfully');

    cy.get('.approval-card').contains(owners[1].fullName).closest('.approval-card').within(() => {
      cy.get('.reject-btn').click();
    });

    cy.get('textarea[id="rejectReason"]').type('Bulk test rejection.');
    cy.get('.confirm-reject-btn').click();

    cy.get('.success-toast').should('contain', 'rejected');

    cy.log('✅ Bulk Approval/Rejection Workflow Completed Successfully');
  });
});