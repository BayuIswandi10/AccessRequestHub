# Access Request Hub MVP

## Prasyarat
- .NET 8 SDK
- SQL Server LocalDB (bawaan Visual Studio) / SQL Server

## Setup & Cara Menjalankan

```bash
# Buka folder src/AccessRequestHub.Web
cd src/AccessRequestHub.Web

# Restore dependencies
dotnet restore

# Build
dotnet build

# Jalankan aplikasi
dotnet run
```

Aplikasi akan berjalan (port disesuaikan dengan `launchSettings.json`).

## Database

Database dibuat secara otomatis dan diisi dengan data awal (seed) pada saat pertama kali dijalankan menggunakan EF Core `EnsureCreated()` + `DbInitializer`.

Untuk melakukan reset database, Anda bisa menghapus database dari SQL Server dan menjalankan ulang aplikasi.

## Daftar User Demo

| User  | Email                | Peran (Role)                     |
|-------|----------------------|--------------------------------|
| Alice | alice@example.local  | Requester (Melapor ke Bob)     |
| Bob   | bob@example.local    | Manajer untuk Alice              |
| Carol | carol@example.local  | Pemilik Sistem - CRM             |
| Dana  | dana@example.local   | Pemilik Sistem - Finance Portal  |
| Erin  | erin@example.local   | Admin / Auditor (Hanya melihat)  |

## Daftar Aplikasi Demo

| Aplikasi        | Pemilik Sistem |
|-----------------|--------------|
| CRM             | Carol        |
| Finance Portal  | Dana         |

## Skenario Demo (Alur Uji Coba)

### 1. Request Standar (Risiko Rendah)
1. Pilih **Alice** pada menu user switcher.
2. Buka **Create Request** -> Pilih CRM, NonProduction, Read.
3. Ganti user ke **Bob** -> Buka **Approval Inbox** -> Lakukan Approve.
4. Hasil: Status berubah menjadi **Approved**.

### 2. Request Risiko Tinggi (Production)
1. Pilih **Alice** -> Buka Create Request -> Pilih CRM, **Production**, Read.
2. Ganti user ke **Bob** -> Buka Approval Inbox -> Lakukan Approve.
3. Status berubah menjadi **PendingSystemOwner**.
4. Ganti user ke **Carol** -> Buka Approval Inbox -> Lakukan Approve.
5. Hasil: Status berubah menjadi **Approved**.

### 3. Request Risiko Tinggi (Akses Admin)
1. Pilih **Alice** -> Buka Create Request -> Pilih Finance Portal, NonProduction, **Admin**.
2. Ganti user ke **Bob** -> Lakukan Approve.
3. Status berubah menjadi **PendingSystemOwner** (Menunggu persetujuan Dana).

### 4. Alur Penolakan (Rejection)
1. Buat request apa saja sebagai Alice.
2. Ganti user ke Bob -> Buka Approval Inbox -> Masukkan alasan (reason) -> Lakukan Reject.
3. Hasil: Status berubah menjadi **Rejected**, rekam jejak (audit trail) menampilkan alasan penolakan.

## Menjalankan Pengujian (Tests)

```bash
dotnet test tests/AccessRequestHub.Tests/
```

Pengujian ini mencakup:
- Alur persetujuan request standar (risiko rendah).
- Request Production berisiko tinggi (persetujuan dua tahap).
- Request akses Admin berisiko tinggi.
- Pencegahan persetujuan dari user yang tidak berhak (Unauthorized).
- Pencegahan persetujuan oleh diri sendiri (Self-approval).
- Idempotensi untuk mencegah pengiriman request ganda.
- Pencegahan intervensi ganda melalui Optimistic Concurrency.
- Penolakan request lengkap dengan alasan (alasan wajib ada).
- Penolakan tanpa alasan (akan gagal divalidasi).
- Pengelolaan akses tanpa autentikasi yang sah.

## Struktur Proyek

```
src/
├── AccessRequestHub.Domain/           # Entitas & Enum
├── AccessRequestHub.Application/      # DTO, Layanan (Services), Antarmuka
├── AccessRequestHub.Infrastructure/   # EF Core DbContext, Data Seeder
└── AccessRequestHub.Web/              # Blazor UI + API Controllers
tests/
└── AccessRequestHub.Tests/            # Pengujian Terintegrasi (Integration Tests)
```
