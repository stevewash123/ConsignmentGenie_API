// cypress/support/e2e.ts
import './commands'
import 'cypress-real-events'

// Hide XHR requests from command log for cleaner output
Cypress.on('window:before:load', (win) => {
  cy.spy(win.console, 'error').as('consoleError')
  cy.spy(win.console, 'warn').as('consoleWarn')
})

// Add global configuration
beforeEach(() => {
  // Set viewport for consistent testing
  cy.viewport(1280, 720)

  // Clear localStorage before each test
  cy.clearLocalStorage()
})