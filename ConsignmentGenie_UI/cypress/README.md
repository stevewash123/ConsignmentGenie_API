# Cypress E2E Tests for ConsignmentGenie

## Overview

This directory contains end-to-end tests for the ConsignmentGenie application using Cypress. The tests provide comprehensive coverage of both admin/owner functionality and the new shopper-facing store experience.

## Test Structure

```
cypress/
├── e2e/
│   ├── admin/                       # Admin workflow tests
│   ├── auth/                        # Admin/owner authentication tests
│   │   ├── login.cy.ts              # Comprehensive login tests with API mocking
│   │   └── login-integration.cy.ts  # UI-focused integration tests
│   ├── owner/                       # Shop owner workflow tests
│   ├── shopper/                     # 🆕 Shopper-facing store tests
│   │   ├── auth/                    # Shopper authentication tests
│   │   │   └── shopper-authentication.cy.ts
│   │   ├── shopping/                # Shopping and cart functionality
│   │   │   └── shopping-navigation.cy.ts
│   │   └── workflows/               # Complete shopper journeys
│   │       └── complete-shopper-workflow.cy.ts
│   └── workflows/                   # Admin/owner complete workflows
├── fixtures/
│   ├── users.json                   # Test user data and credentials
│   └── catalog-items.json           # 🆕 Mock catalog data for shopping tests
├── support/
│   ├── commands.ts                  # Custom Cypress commands (updated with shopper commands)
│   └── e2e.ts                       # Global test configuration
└── README.md                        # This file
```

## Running Tests

### Prerequisites
1. Make sure the Angular application is running on `http://localhost:4200`
2. Ensure the API backend is running (for full integration tests)

### Commands

```bash
# Open Cypress Test Runner (interactive mode)
npm run cypress:open
npm run e2e:open

# Run tests headlessly (CI mode)
npm run cypress:run
npm run e2e

# Run specific test file
npx cypress run --spec "cypress/e2e/auth/login.cy.ts"

# 🆕 Run only shopper tests
npx cypress run --spec "cypress/e2e/shopper/**/*.cy.ts"

# 🆕 Run specific shopper test categories
npx cypress run --spec "cypress/e2e/shopper/auth/*.cy.ts"        # Authentication tests
npx cypress run --spec "cypress/e2e/shopper/shopping/*.cy.ts"    # Shopping tests
npx cypress run --spec "cypress/e2e/shopper/workflows/*.cy.ts"   # Complete workflows
```

## Test Files

### Admin/Owner Tests (Existing)

#### `auth/login.cy.ts`
Comprehensive login functionality tests including:
- **UI Component Tests**: Form elements, validation, responsive design
- **Authentication Flow Tests**: Successful login scenarios for different user roles
- **API Interaction Tests**: Mocked API responses, error handling, network issues
- **State Management Tests**: localStorage verification, token handling
- **Loading States**: UI behavior during API calls

#### `auth/login-integration.cy.ts`
UI-focused integration tests that don't rely on API mocking:
- **Form Validation**: Client-side validation, field requirements
- **User Interactions**: Password visibility toggle, test account buttons
- **Navigation**: Tab order, keyboard interactions
- **Responsive Design**: Mobile and tablet viewport testing

### 🆕 Shopper Tests (New in Phase 1)

#### `shopper/auth/shopper-authentication.cy.ts`
Complete shopper authentication flow testing:
- **Store Access**: Store branding, navigation, inactive store handling
- **Registration**: Form validation, error handling, success flows
- **Login/Logout**: Credential validation, remember me, session management
- **Protected Routes**: Auth guards, return URL redirection
- **Guest Sessions**: Guest checkout token creation
- **Multi-Store Auth**: Store-specific authentication isolation

#### `shopper/shopping/shopping-navigation.cy.ts`
Shopping experience and cart functionality:
- **Store Navigation**: Header, responsive design, mobile menu
- **Catalog Browsing**: Item display, search, filtering, sorting
- **Shopping Cart**: Add/remove items, quantity updates, persistence
- **Cart States**: Guest vs authenticated cart behavior
- **Checkout Flow**: Navigation to checkout, form prefilling
- **Account Management**: Dashboard navigation, favorites management
- **Error Handling**: API failures, network errors

#### `shopper/workflows/complete-shopper-workflow.cy.ts`
End-to-end user journey testing:
- **New User Flow**: Registration → Shopping → Checkout → Order
- **Returning User**: Login → Quick checkout with saved profile
- **Guest Checkout**: Shopping → Checkout without registration
- **Multi-Store Shopping**: Separate carts and authentication per store
- **Error Recovery**: API failures, form persistence, retry scenarios

## Test Users

The following test accounts are available (defined in `fixtures/users.json`):

| Role | Email | Password | Expected Dashboard |
|------|-------|----------|-------------------|
| System Admin | admin@demoshop.com | password123 | /admin/dashboard |
| Shop Owner | owner@demoshop.com | password123 | /owner/dashboard |
| Provider | provider@demoshop.com | password123 | /customer/dashboard |
| Customer | customer@demoshop.com | password123 | /customer/dashboard |

## Custom Commands

The following custom Cypress commands are available:

### Admin/Owner Authentication Commands
```typescript
// Generic login (admin/owner)
cy.login('email@example.com', 'password')

// Role-specific login shortcuts
cy.loginAsAdmin()
cy.loginAsShopOwner()
cy.loginAsProvider()
cy.loginAsCustomer()

// Dashboard verification
cy.shouldBeOnDashboard('admin' | 'owner' | 'customer')

// API interaction
cy.waitForLoginAPI()
```

### 🆕 Shopper Commands
```typescript
// Shopper authentication
cy.shopperLogin(storeSlug, email, password)
cy.shopperRegister(storeSlug, userData)

// Navigation and verification
cy.shouldBeOnStorePage(storeSlug, page?)
cy.clearShopperStorage(storeSlug)

// Shopping actions
cy.addItemToCart(itemName)

// API mocking
cy.mockShopperAPIs(storeSlug)
```

### Environment Variables
The following environment variables are available for shopper tests:
```typescript
Cypress.env('testStoreSlug')      // 'cypress-test-store'
Cypress.env('testUserEmail')      // 'cypress.shopper@example.com'
Cypress.env('testUserPassword')   // 'CypressTest123!'
Cypress.env('testGuestEmail')     // 'cypress.guest@example.com'
```

## Configuration

### `cypress.config.ts`
- **Base URL**: `http://localhost:4200`
- **Viewport**: 1280x720 (desktop testing)
- **Video Recording**: Enabled for test runs
- **Screenshots**: Enabled on test failures

### Global Setup (`support/e2e.ts`)
- Clears localStorage before each test
- Sets consistent viewport
- Configures console error/warning spies

## Writing New Tests

### Best Practices

1. **Use Data Attributes**: Prefer `[data-cy="element"]` selectors over class names
2. **Use Fixtures**: Store test data in `fixtures/` directory
3. **Mock API Calls**: Use `cy.intercept()` for predictable API responses
4. **Test User Flows**: Focus on complete user journeys, not just individual components
5. **Handle Async Operations**: Always wait for API calls and DOM updates

### Example Test Structure
```typescript
describe('Feature Name', () => {
  beforeEach(() => {
    // Setup code
    cy.visit('/page-url')
  })

  describe('Specific Functionality', () => {
    it('should perform expected behavior', () => {
      // Test implementation
    })
  })
})
```

## Debugging Tests

### Common Issues
1. **Timing Issues**: Use `cy.wait()` or `cy.intercept()` for async operations
2. **Element Not Found**: Check selectors and ensure elements are visible
3. **API Responses**: Verify mock responses match expected format
4. **Viewport Issues**: Test on different screen sizes

### Debug Commands
```bash
# Run with debug output
npx cypress run --headed --no-exit

# Open specific test in debug mode
npx cypress open --spec "cypress/e2e/auth/login.cy.ts"
```

## CI/CD Integration

For continuous integration, use:
```bash
# Headless run with video recording
npm run cypress:run

# Generate test reports (if configured)
npx cypress run --reporter mochawesome
```

## Adding Data Attributes for Better Testing

To improve test reliability, consider adding `data-cy` attributes to key elements:

```html
<!-- In login.component.ts template -->
<h1 data-cy="login-header">ConsignmentGenie</h1>
<p data-cy="login-subtitle">Sign in to your account</p>
<input data-cy="email-input" name="email" type="email">
<input data-cy="password-input" name="password" type="password">
<button data-cy="login-submit" type="submit">Sign In</button>
```

This makes tests more maintainable and less brittle to CSS changes.