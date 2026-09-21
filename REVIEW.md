# REVIEW.md - Evaluasi Kesiapan Rilis (Self-Review Production Readiness)

## Temuan (Findings)

| # | Temuan | Tingkat Keparahan (Severity) | Tindakan | Status | Bukti (Evidence) |
|---|---------|----------|--------|--------|----------|
| 1 | Simulasi otentikasi hanya via HTTP header - belum ada mekanisme login (autentikasi) yang nyata | Tinggi (High) | Dapat diterima untuk uji coba MVP; namun membutuhkan integrasi OIDC/OAuth untuk tahap rilis (*production*). | Ditunda (Deferred) | Middleware membaca otentikasi hanya dari header `X-User-Email`. |
| 2 | Aplikasi tidak terkonfigurasi untuk HTTPS | Menengah (Medium) | Tambahkan sertifikat HTTPS jika akan dirilis ke server produksi. | Ditunda (Deferred) | Penggunaan bendera (*flag*) `--no-https` demi kepraktisan pengembangan lokal (*local dev*). |
| 3 | Tidak ada mekanisme paginasi (*pagination*) pada endpoint *list* | Rendah (Low) | Tambahkan *pagination* jika suatu saat data semakin banyak. | Ditunda (Deferred) | Ketentuan soal menyatakan "tidak wajib" untuk membuat pencarian tingkat lanjut atau paginasi. |
| 4 | Belum ada mekanisme pembatasan (*rate limiting*) pada endpoint API | Menengah (Medium) | Tambahkan middleware pembatasan jumlah akses (*rate limiting*) pada tahap produksi. | Ditunda (Deferred) | Belum dibutuhkan untuk tahap MVP saat ini. |
| 5 | Database masih menggunakan fungsi `EnsureCreated` daripada Migrations (EF Migrations) | Rendah (Low) | Ubah metode pembentukan skema menjadi EF Migrations agar versi database mudah dikelola pada tahap rilis. | Ditunda (Deferred) | `EnsureCreated` dinilai sangat cukup untuk mereproduksi tabel pada MVP saat ini. |
| 6 | Belum ada fitur notifikasi email saat persetujuan dilakukan | Rendah (Low) | Bukan bagian dari cakupan (*out of scope*). | N/A | Aturan soal tes secara spesifik menyebut bahwa ini belum dibutuhkan. |

## Keterbatasan yang Diketahui (Known Limitations)

1. **Otentikasi masih berupa simulasi** - Memanfaatkan header `X-User-Email`, bukan penyedia identitas otentikasi (Identity Provider) sungguhan.
2. **LocalDB untuk Pengguna Tunggal** - Tidak dirancang untuk diakses oleh banyak pengguna (secara konkuren) pada tahapan *production*.
3. **Tanpa Paginasi (No pagination)** - Endpoint list akan terus mengembalikan semua rekaman/baris (*records*) tanpa batas halaman.
4. **Tidak adanya middleware logging** - Saat ini hanya tersedia fungsi *logging* dasar ke layar konsol.
5. **State (Status Data) pada Blazor Server** - Pilihan 'User Switcher' saat ini masih bisa diatur ulang ke pengguna *default* (Alice) jika Anda menyegarkan halaman browser (*refresh*).
6. **Tidak adanya pertahanan CSRF pada API** - Endpoint API belum melakukan pengecekan atau validasi token anti-pemalsuan (anti-forgery tokens).

## Pekerjaan Tertunda (Ditunda untuk Fase 2+)

- Menambahkan otentikasi masuk via OAuth/OIDC.
- Memberikan peringatan (*email notifications*) pada saat perubahan status akses.
- Paginasi, serta fitur pencarian/filter data (*search/filter*) pada tabel antarmuka pengguna.
- Pelacakan skema model lewat EF Core Migrations (sebagai pengganti `EnsureCreated`).
- Penambahan penangan pesan kesalahan (*error handling middleware*) yang jauh lebih komprehensif.
- Logging yang terstruktur (contohnya menggunakan Serilog atau OpenTelemetry).
- Containerization aplikasi (menggunakan Docker).
- Menyiapkan alur pengujian CI/CD (*Continuous Integration & Delivery*).
