# ConsignmentGenie

## Project Overview
Multi-tenant consignment management platform with Phase 1 focus on core shop owner functionality: provider tracking, inventory management, split payment calculation, and accounting export.

## Architecture
- **Backend**: C# ASP.NET Core Web API (.NET 8)
- **Frontend**: Angular with standalone components
- **Database**: PostgreSQL (Neon.tech hosted)
- **Authentication**: JWT tokens with role-based authorization
- **Pattern**: N-tier architecture (API, Core, Infrastructure, Application, Tests)

## Project Structure
- **ConsignmentGenie_API/** - Backend solution with N-tier structure
- **ConsignmentGenie_UI/** - Angular frontend application
- **Documents/** - Project documentation
- **Scripts/** - Launch and utility scripts

## Key Features (Phase 1)
- Shop owner registration/authentication
- Provider (consigner/artist/vendor) management
- Item inventory tracking with provider assignment
- Transaction recording with automatic split calculation
- Payout report generation and CSV export
- Multi-tenant data scoping

## Setup Reference
See [Full-Stack Setup Guide](../docs/NEW-PROJECT-SETUP-GUIDE.md)

## Configuration Details
- **Database**: PostgreSQL connection provided in config files
- **JWT**: Custom secret key with 24-hour expiration
- **Roles**: ShopOwner (Phase 1), Provider (Phase 3), Shopper (Phase 5)
- **Multi-tenant**: All entities scoped by OrganizationId

## Related Projects
[Parent Directory](../CLAUDE.md)

## Universal Configuration
📋 **See [Universal Settings](../docs/UNIVERSAL-SETTINGS.md)** for authentication, development environment, and standards that apply to all projects.