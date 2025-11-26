# ConsignmentGenie

Multi-tenant consignment management platform supporting multiple business verticals (consignment shops, art galleries, booth rentals, farmers markets) with configurable terminology and workflows.

## Project Overview
Track provider (consigner/artist/vendor) inventory, calculate split payments automatically, generate payout reports, and export to accounting software.

## Architecture
- **Backend**: C# ASP.NET Core Web API (.NET 8)
- **Frontend**: Angular with TypeScript
- **Database**: PostgreSQL (Neon.tech)
- **Authentication**: JWT tokens
- **ORM**: Entity Framework Core

## Setup
See [NEW-PROJECT-SETUP-GUIDE.md](../docs/NEW-PROJECT-SETUP-GUIDE.md) for complete setup instructions.

## Development
- **API**: `cd ConsignmentGenie_API && dotnet run`
- **Frontend**: `cd ConsignmentGenie_UI && npm start`

## Documentation
- **ConsignmentGenie_API/README.md** - Backend setup and API documentation
- **ConsignmentGenie_UI/README.md** - Frontend development guide