# PLAN.md - Access Request Hub MVP

## Pemahaman Masalah

Perusahaan masih mengelola permintaan akses aplikasi internal secara manual melalui email/chat. Masalah utama:
- Proses approval sulit dilacak (audit trail tidak jelas)
- Request bisa terkirim dua kali (duplicate submission)
- Dua approver bisa melakukan tindakan bersamaan (concurrency issue)

Solusi: Membangun MVP Access Request Hub sebagai sumber kebenaran tunggal (*single source of truth*) untuk request, approval, dan rekam jejak (*audit trail*).

## Arsitektur & Model Data

### Tech Stack
- **Frontend**: Blazor Server (.NET 8)
- **Backend**: ASP.NET Core Web API (berjalan pada proyek Blazor yang sama)
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server (LocalDB)
- **Pengujian (Testing)**: xUnit + WebApplicationFactory + SQL Server LocalDB

### Struktur Solusi (Layered / Clean Architecture)
```
src/
├── AccessRequestHub.Domain/           # Entities, Enums (tanpa dependencies)
├── AccessRequestHub.Application/      # DTOs, Services, Interfaces
├── AccessRequestHub.Infrastructure/   # EF Core DbContext, Migrations, Seeder
└── AccessRequestHub.Web/              # Blazor Server + API Controllers + Middleware
tests/
└── AccessRequestHub.Tests/            # xUnit Integration Tests
```

### Model Data
- **User**: Id, Name, Email, ManagerId (self-referencing FK)
- **Application**: Id, Name, SystemOwnerId (FK -> User)
- **AccessRequest**: Id, ClientRequestId (unique), RequesterId, ApplicationId, Environment, AccessLevel, Status, BusinessJustification, PolicyVersion, RowVersion
- **AuditEvent**: Id, AccessRequestId, ActorId, Action, Reason, Timestamp

### State Machine
```
PendingManager -> Approved (risiko rendah, di-approve manajer)
PendingManager -> PendingSystemOwner (risiko tinggi, di-approve manajer)
PendingManager -> Rejected (di-reject manajer)
PendingSystemOwner -> Approved (di-approve pemilik sistem)
PendingSystemOwner -> Rejected (di-reject pemilik sistem)
```

## Urutan Implementasi
1. Lapisan Domain (entitas, enum)
2. Lapisan Application (DTO, interface layanan, implementasi layanan)
3. Lapisan Infrastructure (DbContext dengan Fluent API, seeder data awal)
4. Lapisan API (middleware, controller)
5. Antarmuka Blazor (pemilih user, halaman)
6. Integration test (pengujian terintegrasi)
7. Dokumentasi

## Strategi Pengujian
- Integration test menggunakan WebApplicationFactory dengan SQL Server LocalDB
- Cakupan (*coverage*) pengujian meliputi semua 7 skenario demo yang diminta
- Fokus pada logika bisnis: otorisasi, idempotensi, konkurensi, dan transisi status (state)

## Kompromi Keputusan (Trade-offs)
1. **Satu host vs proyek terpisah**: Menggabungkan Blazor + API dalam satu proyek demi kesederhanaan. Trade-off: pemisahan arsitektur (*separation*) kurang tegas, namun lebih mudah dijalankan (cukup satu perintah `dotnet run`).
2. **SQL Server LocalDB vs Docker Container**: Menggunakan LocalDB karena sudah tersedia bawaan di Windows tanpa konfigurasi tambahan. Trade-off: tidak portabel ke sistem operasi non-Windows tanpa modifikasi.
3. **Simulasi otentikasi via header vs sesi**: Menggunakan HTTP header `X-User-Email` untuk menyimulasikan otentikasi. Trade-off: tidak aman untuk dipasang di tahap *production*, namun memenuhi syarat evaluasi tes dan jauh lebih memudahkan pengujian (testing).
