# AI_USAGE.md

## AI Tools Used
- Google Gemini / Antigravity (primary coding agent)

## Key AI Interactions

### 1. Architecture Design
- **Ask**: Arsitektur untuk .NET Core + Blazor + SQL Server
- **Suggestion**: Clean Architecture dengan 5 layers (Domain, Application, Infrastructure, Api, Web)
- **Decision**: Accepted dengan modifikasi - menggabungkan Api dan Web menjadi satu project untuk simplicity
- **Why**: Mengurangi complexity deployment dan menghindari CORS issues

### 2. EF Core Configuration (Idempotency & Concurrency)
- **Ask**: Konfigurasi EF Core untuk idempotency dan optimistic concurrency
- **Suggestion**: Unique Index pada ClientRequestId + IsRowVersion() pada RowVersion
- **Decision**: Accepted as-is
- **Why**: Ini adalah best practice EF Core untuk kedua requirement tersebut

### 3. Database Seeding Strategy
- **Ask**: Cara seed demo users dengan fixed relationships
- **Suggestion**: Static GUIDs di DbInitializer untuk predictable testing
- **Decision**: Accepted
- **Why**: Memudahkan test assertions dan demo reproducibility

### 4. Blazor User Switcher
- **Ask**: Implementasi simulated authentication di Blazor
- **Suggestion**: ApiClient wrapper yang menambahkan X-User-Email header
- **Decision**: Accepted with changes - menggunakan HttpClient dari DI container
- **Why**: Lebih testable dan sesuai pattern Blazor Server

### 5. Test Infrastructure
- **Ask**: Setup integration tests dengan SQL Server
- **Suggestion**: WebApplicationFactory dengan database per test run
- **Decision**: Accepted
- **Why**: Memastikan tests isolated dan database constraints terverifikasi

## Three Things AI Got Wrong

1. **Namespace collision `Application`**: AI menggunakan entity name `Application` yang clash dengan namespace `AccessRequestHub.Application`. Harus di-fix manual dengan using alias `AppEntity`.

2. **InternalsVisibleTo approach**: AI menyarankan `[assembly: InternalsVisibleTo]` untuk expose Program class ke test project, padahal dengan top-level statements di .NET 8, pendekatan yang benar adalah `public partial class Program`.

3. **NuGet package version**: AI tidak memperhitungkan bahwa `Microsoft.AspNetCore.Mvc.Testing` versi latest (10.x) tidak compatible dengan .NET 8.0. Harus di-pin ke version 8.0.*.
