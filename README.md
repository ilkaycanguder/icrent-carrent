# ICRent - Araç Çalışma Süreleri Takip Sistemi

## 📋 Proje Özeti

ICRent, araç kiralama firmalarının araçlarının çalışma sürelerini değerlendirmek amacıyla geliştirilmiş bir web uygulamasıdır. Sistem, 7×24 çalışma modeline uygun olarak araçların aktif çalışma, bakım ve boşta bekleme sürelerini takip eder.

## 🎯 Temel Özellikler

### ✅ Görev 1: Araç Kayıt Yönetimi

- Araç adı ve plaka bilgileri ile kayıt oluşturma
- Araç bilgilerini düzenleme ve silme
- Plaka ve isim benzersizlik kontrolü
- Sadece Admin rolündeki kullanıcılar erişebilir

### ✅ Görev 2: Çalışma Süreleri Girişi

- Günlük aktif çalışma süresi girişi
- Günlük bakım süresi girişi
- MERGE operasyonu ile aynı gün için saatlerin toplanması
- 7×24 (168 saat) çalışma modeli
- Sadece User rolündeki kullanıcılar erişebilir

### ✅ Görev 3: Raporlama ve Grafikler

- Haftalık aktif çalışma süresi yüzde analizi
- Haftalık boşta bekleme süresi yüzde analizi
- Dinamik grafik gösterimleri
- Sadece Admin rolündeki kullanıcılar erişebilir

### ✅ Görev 4: Rol Tabanlı Yetkilendirme

- **Admin Rolü**: Araç yönetimi ve raporlama
- **User Rolü**: Sadece çalışma süreleri girişi
- Tüm işlemler için kullanıcı takibi

### 🎯 Joker Görev: Gantt Chart

- Seçilen araçlar için tarih aralığında Gantt diyagramı
- Birden fazla araç seçimi
- Kullanıcı bilgisi ile birlikte gösterim
- Aktif çalışma sürelerinin görselleştirilmesi

## 🏗️ Teknik Mimari

### Katman Yapısı

```
ICRent/
├── src/
│   ├── Core/
│   │   ├── ICRent.Domain/          # Entity'ler ve iş kuralları
│   │   └── ICRent.Application/     # İş mantığı ve servisler
│   ├── Infrastructure/
│   │   ├── ICRent.Infrastructure/  # Altyapı servisleri
│   │   └── ICRent.Persistence/     # Veri erişim katmanı
│   └── Presentation/
│       └── ICRent.Web/             # Web arayüzü (MVC)
```

### Teknoloji Stack

- **.NET 8.0** - Ana framework
- **ASP.NET Core MVC** - Web framework
- **SQL Server** - Veritabanı
- **ADO.NET** - Veri erişimi
- **Bootstrap 5** - Frontend framework
- **jQuery** - JavaScript kütüphanesi
- **Chart.js** - Grafik kütüphanesi

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler

- .NET 8.0 SDK
- SQL Server (LocalDB veya Full)
- Visual Studio 2022 veya VS Code

### Kurulum Adımları

1. **Projeyi klonlayın**

```bash
git clone https://github.com/ilkaycanguder/icrent-carrent.git
cd ICRent
```

2. **Veritabanı bağlantısını yapılandırın**

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Sql": "Server=YOUR_SERVER;Database=ICRentDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. **Veritabanını yedekten geri yükleyin**

```sql
-- SQL Server Management Studio ile ICRentDb.bak dosyasını geri yükleyin
-- Veya T-SQL ile:
RESTORE DATABASE ICRentDb
FROM DISK = 'C:\path\to\ICRentDb.bak'
WITH REPLACE, MOVE 'ICRentDb' TO 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ICRentDb.mdf',
MOVE 'ICRentDb_Log' TO 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\ICRentDb_Log.ldf';
```

4. **Projeyi çalıştırın**

```bash
dotnet run --project src/Presentation/ICRent.Web
```

5. **Admin girişi için**

```
Kullanıcı Adı: admin
Şifre: 123456
```

## 👥 Kullanıcı Rolleri

### Admin Kullanıcısı

- **Araç Yönetimi**: Araç ekleme, düzenleme, silme
- **Raporlama**: Haftalık analiz, Gantt chart, audit log
- **Sistem Yönetimi**: Kullanıcı işlemlerini izleme

### User Kullanıcısı

- **Çalışma Kayıtları**: Günlük aktif ve bakım süreleri girişi
- **Kayıt Görüntüleme**: Kendi kayıtlarını listeleme
- **Profil Yönetimi**: Kendi bilgilerini güncelleme

## 📊 Raporlama Özellikleri

### Haftalık Yüzde Analizi

- 168 saat (7×24) bazlı hesaplama
- Aktif çalışma süresi yüzdesi
- Boşta bekleme süresi yüzdesi
- Dinamik grafik gösterimi

### Gantt Chart

- Seçilen araçlar için tarih aralığı
- Aktif çalışma sürelerinin görselleştirilmesi
- Kullanıcı bilgisi ile birlikte gösterim
- Çoklu araç seçimi

### Audit Log

- Tüm işlemlerin detaylı kaydı
- Kullanıcı bazlı filtreleme
- Tarih aralığı filtreleme
- JSON formatında detaylı bilgi

## 🔒 Güvenlik Özellikleri

- **Şifre Güvenliği**: PBKDF2 + SHA256 hashleme
- **SQL Injection Koruması**: Parametreli sorgular
- **CSRF Koruması**: AntiForgeryToken
- **Rol Tabanlı Yetkilendirme**: Policy-based authorization
- **Session Yönetimi**: Cookie authentication

## 🎨 UI/UX Özellikleri

- **Responsive Tasarım**: Mobil uyumlu
- **Neon Tema**: Modern ve çekici görünüm
- **Bootstrap 5**: Güncel CSS framework
- **jQuery**: Dinamik etkileşimler
- **Modal Onayları**: Güvenli silme işlemleri

## 📈 Performans Optimizasyonları

- **Repository Pattern**: Veri erişim katmanı
- **Connection Factory**: Veritabanı bağlantı yönetimi
- **Async/Await**: Asenkron işlemler
- **Pagination**: Sayfalama desteği
- **Caching**: View bazlı önbellekleme

## 🧪 Test Verileri

### Örnek Araç Verileri

```
Araç Adı: Kamyon-001, Plaka: 34ABC123
Araç Adı: Minibüs-002, Plaka: 06XYZ789
Araç Adı: Tır-003, Plaka: 35DEF456
```

### Örnek Çalışma Kayıtları

```
Tarih: 2024-01-15
Aktif Saat: 8.5
Bakım Saat: 1.0
Boşta Saat: 14.5 (168 - 8.5 - 1.0 = 158.5)
```

## 🔧 Geliştirme Notları

### Mimari Kararlar

- **Clean Architecture** prensiplerine uygunluk
- **Dependency Injection** kullanımı
- **Interface Segregation** uygulaması
- **Single Responsibility** prensibi

### Kod Kalitesi

- **Nullable Reference Types** aktif
- **Implicit Usings** kullanımı
- **Modern C#** özellikleri
- **Temiz kod** prensipleri

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 👨‍💻 Geliştirici

Proje, araç kiralama firmalarının çalışma sürelerini takip etmek amacıyla geliştirilmiştir.

---
