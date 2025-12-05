# ConsignmentGenie API

**🚨 CRITICAL: Read [Master CLAUDE.md](../../CLAUDE.md) FIRST for ConsignmentGenie context, workspace structure, and development guidelines.**

## Project Overview
Multi-tenant consignment management platform with Phase 1 focus on core shop owner functionality: consigner tracking, inventory management, split payment calculation, and accounting export.

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
- Consigner management
- Item inventory tracking with consigner assignment
- Transaction recording with automatic split calculation
- Payout report generation and CSV export
- Multi-tenant data scoping

## Setup Reference
See [Full-Stack Setup Guide](../docs/NEW-PROJECT-SETUP-GUIDE.md)

## Configuration Details
- **Database**: PostgreSQL connection provided in config files
- **JWT**: Custom secret key with 24-hour expiration
- **Email Service**: Resend.com API Key: `re_yMtGsic1_MY5eDicfxseeziqq9a64FZEW`
- **Roles**: ShopOwner (Phase 1), Consigner (Phase 3), Shopper (Phase 5)
- **Multi-tenant**: All entities scoped by OrganizationId
- **Cross-Platform**: Angular environment files handle Windows vs WSL API URL differences
  - `environment.ts` - Windows development (localhost)
  - `environment.development.ts` - WSL development (localhost with different ports)

## Database Logging & Debugging
**Direct PostgreSQL Log Access** for debugging and monitoring:

```bash
# Database connection details from appsettings.Development.json
PGPASSWORD=npg_ZPId7K5GAlSf psql -h ep-little-king-ahhzzyuy-pooler.c-3.us-east-1.aws.neon.tech -p 5432 -U neondb_owner -d neondb

# Quick log queries for debugging:
# Get recent log entries
PGPASSWORD=npg_ZPId7K5GAlSf psql -h ep-little-king-ahhzzyuy-pooler.c-3.us-east-1.aws.neon.tech -p 5432 -U neondb_owner -d neondb -c "SELECT timestamp, level, message FROM logs ORDER BY timestamp DESC LIMIT 10;"

# Check for errors (level 0=Critical, 1=Error)
PGPASSWORD=npg_ZPId7K5GAlSf psql -h ep-little-king-ahhzzyuy-pooler.c-3.us-east-1.aws.neon.tech -p 5432 -U neondb_owner -d neondb -c "SELECT timestamp, level, message FROM logs WHERE level <= 1 ORDER BY timestamp DESC;"

# Log level summary
PGPASSWORD=npg_ZPId7K5GAlSf psql -h ep-little-king-ahhzzyuy-pooler.c-3.us-east-1.aws.neon.tech -p 5432 -U neondb_owner -d neondb -c "SELECT level, COUNT(*) as count, CASE level WHEN 0 THEN 'Critical' WHEN 1 THEN 'Error' WHEN 2 THEN 'Information' WHEN 3 THEN 'Warning' WHEN 4 THEN 'Debug' ELSE 'Unknown' END as level_name FROM logs GROUP BY level ORDER BY level;"

# Search specific log messages
PGPASSWORD=npg_ZPId7K5GAlSf psql -h ep-little-king-ahhzzyuy-pooler.c-3.us-east-1.aws.neon.tech -p 5432 -U neondb_owner -d neondb -c "SELECT timestamp, message FROM logs WHERE message LIKE '%search_term%' ORDER BY timestamp DESC;"
```

**Log Levels:**
- `0` = Critical
- `1` = Error
- `2` = Information
- `3` = Warning
- `4` = Debug

**Table Structure:** `logs` table contains `timestamp`, `level`, `message`, `message_template`, `exception`, and `log_event` (JSONB) columns.

## Related Projects
[Parent Directory](../CLAUDE.md)

## ⚠️ CRITICAL SECURITY RULES

**NEVER COMMIT SECRETS TO GIT:**
- API keys, tokens, passwords, connection strings
- Any sensitive configuration values
- Check .gitignore before ANY commit
- Use environment variables or config files (excluded from git)
- **This has happened TWICE - absolutely unacceptable**

## Universal Configuration
📋 **See [Universal Settings](../docs/UNIVERSAL-SETTINGS.md)** for authentication, development environment, and standards that apply to all projects.