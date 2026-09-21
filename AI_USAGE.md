# AI_USAGE.md

## Sifat Kolaborasi
- **Peran Saya (Kandidat)**: Mengambil kendali penuh atas perancangan arsitektur, merumuskan logika bisnis inti (seperti alur persetujuan dan struktur tabel), merancang antarmuka pengguna (UI), serta melakukan penyelesaian masalah-masalah teknis yang kompleks (termasuk *bug fixing* spesifik, mitigasi *concurrency*, dan *idempotency*). Saya memastikan bahwa seluruh alur aplikasi berjalan sesuai standar.
- **Peran AI (Gemini)**: Berperan murni sebagai asisten pendukung. Saya menginstruksikan AI untuk mempercepat pembuatan struktur awal (*boilerplate*), membantu merapikan penulisan *unit test*, menerjemahkan teks dokumentasi, dan menelusuri *syntax error* ringan berdasarkan kerangka dan arahan *prompt* yang saya berikan.

## Perangkat AI yang Digunakan
- Google Gemini / Antigravity (Agen pembuat kode / koding utama)

## Interaksi Penting dengan AI

### 1. Merancang Arsitektur
- **Permintaan**: Mencari arsitektur yang pas untuk gabungan .NET Core, Blazor, dan SQL Server.
- **Saran AI**: Menggunakan pola *Clean Architecture* yang dipisah jadi 5 lapisan (Domain, Application, dsb).
- **Keputusan**: Saya timbang-timbang lagi, akhirnya saya putuskan untuk menggabungkan API dan antarmuka Web-nya jadi satu proyek saja biar lebih simpel.
- **Alasan**: Supaya aplikasinya gampang dijalankan (nggak perlu jalankan dua program terpisah) dan terhindar dari *error* CORS, tapi kodenya tetap rapi.

### 2. Mencegah Request Dobel dan Klik Barengan
- **Permintaan**: Cara mengatur database (EF Core) untuk menahan *request* ganda (idempotensi) dan dua manajer yang *approve* barengan (konkurensi).
- **Saran AI**: Pakai fitur *Unique Index* dan `IsRowVersion()`.
- **Keputusan**: Saya pastikan dulu kebiasaan ini di dokumentasi resmi Microsoft. Setelah yakin, saya susun sendiri kodenya lewat *Fluent API* (menambahkan `builder.HasIndex(e => e.ClientRequestId).IsUnique()` dan mengunci `RowVersion`).
- **Alasan**: Cara ini memang terbukti paling ampuh dan wajar dipakai buat jaga-jaga supaya data nggak tumpang tindih saat diakses bersamaan.

### 3. Mengisi Data Awal (Database Seeding)
- **Permintaan**: Cara paling gampang masukin data user dan aplikasi buat kebutuhan demo.
- **Saran AI**: Memakai angka ID (GUID) yang dibuat tetap/statis dari awal.
- **Keputusan**: Saya ambil idenya, tapi struktur datanya saya rombak sendiri biar sesuai dengan aturan soal, yakni dengan menautkan relasi manajernya secara eksplisit di dalam kode (contohnya `alice.ManagerId = BobId`).
- **Alasan**: Biar gampang pas dites nanti kodenya nggak error gara-gara ID user-nya berubah-ubah terus tiap kali aplikasi di-restart.

### 4. Fitur Ganti User (Simulasi Login)
- **Permintaan**: Membuat fitur ganti-ganti user di Blazor.
- **Saran AI**: Bikin kelas bantuan bernama `ApiClient` yang otomatis nambahin nama user di setiap *request* HTTP.
- **Keputusan**: Konsep awalnya saya bongkar. Saya atur fiturnya pakai `IHttpClientFactory` dan didaftarkan sebagai *Scoped Service* supaya cocok dengan cara kerja Blazor Server.
- **Alasan**: Jauh lebih aman buat nyimpen data "siapa yang lagi login" selama aplikasi dipakai, dan kodenya jadi lebih gampang buat diuji coba.

### 5. Mempersiapkan Lingkungan Tes
- **Permintaan**: Menyiapkan pengetesan otomatis (*integration test*) yang nyambung langsung ke SQL Server.
- **Saran AI**: Pakai bawaan `WebApplicationFactory` biar databasenya terisolasi.
- **Keputusan**: Saya akali lagi konfigurasinya dengan menulis kelas turunan `CustomWebApplicationFactory`. Di situ, saya mencegat pengaturan servisnya dan mengubah string koneksi (*Connection String*) agar menempelkan `Guid.NewGuid()` sebagai nama database baru setiap kali tes berjalan.
- **Alasan**: Sangat penting biar tesnya bisa jalan cepat secara bebarengan (paralel) tanpa takut datanya saling tabrakan.

## Tiga Kesalahan yang Dibuat oleh AI

1. **Tabrakan (collision) namespace `Application`**: AI sempat membuat entitas dengan nama `Application` yang ternyata bentrok (*clash*) dengan penamaan lapisan `AccessRequestHub.Application`. Harus diperbaiki secara manual dengan menambahkan kode `using AppEntity = AccessRequestHub.Domain.Entities.Application;`.

2. **Pendekatan InternalsVisibleTo**: AI menyarankan penggunaan atribut `[assembly: InternalsVisibleTo]` untuk membuka akses kelas `Program` ke proyek *testing*. Padahal, dengan adanya fitur *top-level statements* pada .NET 8, pendekatan modern yang paling tepat adalah dengan mendeklarasikan `public partial class Program { }` di bagian bawah file.

3. **Versi paket NuGet (NuGet package version)**: AI tidak memperhitungkan bahwa paket `Microsoft.AspNetCore.Mvc.Testing` versi terbaru (10.x) belum kompatibel dengan SDK .NET 8.0. Versi paket tersebut akhirnya harus dikunci (*pinned*) ke versi `8.0.*` secara spesifik.
