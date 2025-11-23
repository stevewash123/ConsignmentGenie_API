describe('Form Validation Tests', () => {
  describe('Process Sale Form', () => {
    beforeEach(() => {
      cy.visit('/owner/sales');
      cy.get('button').contains('Process Sale').click();
      cy.get('[role="dialog"], .modal').should('be.visible');
    });

    it('should display all required form fields', () => {
      cy.get('form').within(() => {
        cy.get('input, select, textarea').should('have.length.greaterThan', 0);
      });
    });

    it('should show validation errors for empty required fields', () => {
      cy.get('button[type="submit"], button').contains(/submit|save|process/i).click();
      cy.get('.error, .invalid, [aria-invalid="true"]').should('exist');
    });

    it('should clear validation errors when fields are filled', () => {
      // Try to submit first to trigger validation
      cy.get('button[type="submit"], button').contains(/submit|save|process/i).click();

      cy.get('input[required]').first().then($input => {
        if ($input.length > 0) {
          cy.wrap($input).type('test value');
          cy.wrap($input).should('not.have.class', 'ng-invalid');
        }
      });
    });
  });

  describe('Filter Forms', () => {
    beforeEach(() => {
      cy.visit('/owner/sales');
    });

    it('should have working date filters', () => {
      cy.get('input[type="date"]').should('exist').first().type('2024-01-01');
      cy.get('input[type="date"]').first().should('have.value', '2024-01-01');
    });

    it('should have working dropdown filters', () => {
      cy.get('select').should('exist').first().select(1);
      cy.get('select').first().should('not.have.value', '');
    });
  });

  describe('Search Functionality', () => {
    it('should accept search input', () => {
      cy.visit('/owner/sales');
      cy.get('input[type="search"], input[placeholder*="search" i]').first().then($search => {
        if ($search.length > 0) {
          cy.wrap($search).type('test search');
          cy.wrap($search).should('have.value', 'test search');
        }
      });
    });
  });
});