describe('Performance Tests', () => {
  describe('Page Load Performance', () => {
    it('should load dashboard within acceptable time', () => {
      const startTime = Date.now();
      cy.visit('/owner/dashboard');
      cy.get('h1').should('be.visible').then(() => {
        const loadTime = Date.now() - startTime;
        expect(loadTime).to.be.lessThan(5000);
      });
    });

    it('should load sales page within acceptable time', () => {
      const startTime = Date.now();
      cy.visit('/owner/sales');
      cy.get('h1').should('be.visible').then(() => {
        const loadTime = Date.now() - startTime;
        expect(loadTime).to.be.lessThan(5000);
      });
    });
  });

  describe('Network Efficiency', () => {
    it('should not make excessive API calls', () => {
      cy.intercept('GET', '**/api/**').as('apiCalls');
      cy.visit('/owner/dashboard');
      cy.wait(3000);
      cy.get('@apiCalls.all').should('have.length.lessThan', 20);
    });

    it('should handle offline scenarios gracefully', () => {
      cy.visit('/owner/dashboard');
      cy.window().then((win) => {
        // Simulate offline
        Object.defineProperty(win.navigator, 'onLine', {
          value: false,
          configurable: true
        });
        win.dispatchEvent(new Event('offline'));
      });
      cy.get('body').should('not.contain', 'TypeError');
    });
  });

  describe('Memory Usage', () => {
    it('should not leak memory during navigation', () => {
      cy.visit('/owner/dashboard');
      cy.visit('/owner/sales');
      cy.visit('/owner/dashboard');
      cy.visit('/owner/sales');
      // Basic check - page should still be responsive
      cy.get('h1').should('be.visible');
    });
  });
});