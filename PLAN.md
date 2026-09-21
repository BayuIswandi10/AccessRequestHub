# PLAN.md - Access Request Hub MVP

## Problem Understanding

Perusahaan masih mengelola permintaan akses aplikasi internal secara manual melalui email/chat. Masalah utama:
- Proses approval sulit dilacak (audit trail tidak jelas)
- Request bisa terkirim dua kali (duplicate submission)
- Dua approver bisa melakukan action bersamaan (concurrency issue)

Solusi: Membangun MVP Access Request Hub sebagai single source of truth untuk request, approval, dan audit trail.

## Architecture & Data Model

### Tech Stack
- **Frontend**: Blazor Server (.NET 8)
- **Backend**: ASP.NET Core Web API (hosted in same Blazor project)
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (LocalDB)
- **Testing**: xUnit + WebApplicationFactory + SQL Server LocalDB

### Solution Structure (Layered / Clean Architecture)
```
src/
├── AccessRequestHub.Domain/           # Entities, Enums (zero dependencies)
├── AccessRequestHub.Application/      # DTOs, Services, Interfaces
├── AccessRequestHub.Infrastructure/   # EF Core DbContext, Migrations, Seeder
└── AccessRequestHub.Web/              # Blazor Server + API Controllers + Middleware
tests/
└── AccessRequestHub.Tests/            # xUnit Integration Tests
```

### Data Model
- **User**: Id, Name, Email, ManagerId (self-referencing FK)
- **Application**: Id, Name, SystemOwnerId (FK -> User)
- **AccessRequest**: Id, ClientRequestId (unique), RequesterId, ApplicationId, Environment, AccessLevel, Status, BusinessJustification, PolicyVersion, RowVersion
- **AuditEvent**: Id, AccessRequestId, ActorId, Action, Reason, Timestamp

### State Machine
```
PendingManager -> Approved (non-high-risk, manager approve)
PendingManager -> PendingSystemOwner (high-risk, manager approve)
PendingManager -> Rejected (manager reject)
PendingSystemOwner -> Approved (system owner approve)
PendingSystemOwner -> Rejected (system owner reject)
```

## Implementation Order
1. Domain layer (entities, enums)
2. Application layer (DTOs, service interfaces, service implementation)
3. Infrastructure layer (DbContext with Fluent API, seeder)
4. API layer (middleware, controllers)
5. Blazor UI (user switcher, pages)
6. Integration tests
7. Documentation

## Test Strategy
- Integration tests menggunakan WebApplicationFactory dengan SQL Server LocalDB
- Test coverage mencakup semua 7 demo scenarios yang diminta
- Fokus pada business logic: authorization, idempotency, concurrency, state transitions

## Trade-offs
1. **Single host vs separate projects**: Blazor + API dalam satu project untuk simplicity. Trade-off: less separation, tapi lebih mudah dijalankan (satu `dotnet run`).
2. **SQL Server LocalDB vs containerized**: Menggunakan LocalDB karena sudah tersedia di Windows tanpa setup tambahan. Trade-off: tidak portable ke non-Windows tanpa modifikasi.
3. **Simulated auth via header vs session**: Menggunakan HTTP header `X-User-Email` untuk simulasi authentication. Trade-off: tidak secure untuk production, tapi memenuhi requirement assessment dan memudahkan testing.
