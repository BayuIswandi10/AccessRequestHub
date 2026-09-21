# Access Request Hub MVP

## Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (included with Visual Studio)

## Setup & Run

```bash
# Clone repository
git clone <repository-url>
cd AccessRequestHub

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the application
cd src/AccessRequestHub.Web
dotnet run
```

Application will be available at: `http://localhost:5000`

## Database

Database is automatically created and seeded on first run using EF Core `EnsureCreated()` + `DbInitializer`.

Connection string (default): `Server=(localdb)\mssqllocaldb;Database=AccessRequestHub;Trusted_Connection=True;MultipleActiveResultSets=true`

To reset the database, delete the database from SQL Server and restart the application.

## Demo Users

| User  | Email                | Role                           |
|-------|----------------------|--------------------------------|
| Alice | alice@example.local  | Requester (reports to Bob)     |
| Bob   | bob@example.local    | Manager for Alice              |
| Carol | carol@example.local  | System Owner - CRM             |
| Dana  | dana@example.local   | System Owner - Finance Portal  |
| Erin  | erin@example.local   | Admin / Auditor (view only)    |

## Demo Applications

| Application     | System Owner |
|-----------------|--------------|
| CRM             | Carol        |
| Finance Portal  | Dana         |

## Demo Flow

### 1. Standard Request (Non-High-Risk)
1. Select **Alice** in user switcher
2. Go to **Create Request** -> CRM, NonProduction, Read
3. Switch to **Bob** -> Go to **Approval Inbox** -> Approve
4. Result: Status = **Approved**

### 2. High-Risk Request (Production)
1. Select **Alice** -> Create Request -> CRM, **Production**, Read
2. Switch to **Bob** -> Approval Inbox -> Approve
3. Status becomes **PendingSystemOwner**
4. Switch to **Carol** -> Approval Inbox -> Approve
5. Result: Status = **Approved**

### 3. High-Risk Request (Admin Access)
1. Select **Alice** -> Create Request -> Finance Portal, NonProduction, **Admin**
2. Switch to **Bob** -> Approve
3. Status becomes **PendingSystemOwner** (waiting for Dana)

### 4. Rejection Flow
1. Create any request as Alice
2. Switch to Bob -> Approval Inbox -> Enter reason -> Reject
3. Result: Status = **Rejected**, audit trail shows reason

## Running Tests

```bash
dotnet test tests/AccessRequestHub.Tests/
```

Tests cover:
- Standard non-high-risk approval flow
- High-risk production request (two-step approval)
- High-risk admin access request
- Unauthorized approval attempt
- Self-approval prevention
- Idempotent duplicate request creation
- Concurrent approval conflict detection
- Rejection with reason and terminal state
- Rejection without reason validation
- Unauthenticated request handling

## Project Structure

```
src/
├── AccessRequestHub.Domain/           # Entities & Enums
├── AccessRequestHub.Application/      # DTOs, Services, Interfaces
├── AccessRequestHub.Infrastructure/   # EF Core DbContext, Seeder
└── AccessRequestHub.Web/              # Blazor UI + API Controllers
tests/
└── AccessRequestHub.Tests/            # Integration Tests
```
