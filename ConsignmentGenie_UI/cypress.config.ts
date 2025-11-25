import { defineConfig } from 'cypress'

export default defineConfig({
  e2e: {
    baseUrl: 'http://localhost:4200',
    viewportWidth: 1280,
    viewportHeight: 720,
    supportFile: 'cypress/support/e2e.ts',
    specPattern: 'cypress/e2e/**/*.cy.{js,jsx,ts,tsx}',
    video: false,
    screenshotOnRunFailure: true,
    defaultCommandTimeout: 10000,
    requestTimeout: 10000,
    responseTimeout: 10000,
    env: {
      apiUrl: 'http://localhost:5000',
      testStoreSlug: 'cypress-test-store',
      testUserEmail: 'cypress.shopper@example.com',
      testUserPassword: 'CypressTest123!',
      testGuestEmail: 'cypress.guest@example.com',
      // Real email environment switch
      SEND_REAL_EMAILS: false, // Default to false for safety
      REAL_EMAIL_DOMAIN: 'cypresstest.microsaasbuilders.com',
      TEST_EMAIL_PROVIDER: 'sendgrid', // 'mock' | 'sendgrid' | 'ethereal'
      REQUIRE_EMAIL_CONFIRMATION: true // Safety check
    },
    setupNodeEvents(on, config) {
      // Real email environment setup
      if (config.env.SEND_REAL_EMAILS) {
        console.warn('🚨 REAL EMAIL MODE ENABLED - Emails will be sent!');
        console.log('📧 Email domain:', config.env.REAL_EMAIL_DOMAIN);
        console.log('📦 Provider:', config.env.TEST_EMAIL_PROVIDER);

        if (config.env.REQUIRE_EMAIL_CONFIRMATION) {
          console.error('❌ Set REQUIRE_EMAIL_CONFIRMATION=false to proceed with real emails');
          process.exit(1);
        }
      } else {
        console.log('🔧 Email mocking enabled - No real emails will be sent');
      }
      return config;
    },
  },
  component: {
    devServer: {
      framework: 'angular',
      bundler: 'webpack',
    },
    specPattern: '**/*.cy.ts'
  }
})