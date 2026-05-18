# 💰 PennyWise — Kişisel Bütçe Takip Uygulaması

ASP.NET Core 8 MVC, Entity Framework Core ve PostgreSQL ile geliştirilmiş kişisel gelir/gider takip uygulaması.

---

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler

Bilgisayarınızda aşağıdakilerin kurulu olması gerekiyor:

| Gereksinim | İndirme |
|---|---|
| .NET 8 SDK | https://dotnet.microsoft.com/download/dotnet/8.0 |
| PostgreSQL (14+) | https://www.postgresql.org/download/ |

> ⚠️ PostgreSQL kurulumu sırasında belirlediğiniz **kullanıcı adı ve şifreyi** not alın.

---

### Adım Adım Kurulum

**1. Repoyu klonlayın:**
```bash
git clone https://github.com/KULLANICI_ADI/PennyWise.git
cd PennyWise
```

**2. Kişisel ayar dosyasını oluşturun:**

Proje kök dizininde (`PennyWise/` klasörü içinde) `appsettings.Development.json` adında yeni bir dosya oluşturun ve aşağıdaki içeriği yapıştırın. `YOUR_USERNAME` ve `YOUR_PASSWORD` kısımlarını kendi PostgreSQL bilgilerinizle değiştirin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=PennyWiseDb;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> 💡 Bu dosya `.gitignore`'a eklenmiştir, yani şifreniz git'e gitmez.

**3. EF Core CLI aracını yükleyin (daha önce yüklemediyseniz):**
```bash
dotnet tool install --global dotnet-ef
```

**4. Veritabanını oluşturun:**
```bash
dotnet ef database update
```

Bu komut PostgreSQL'de `PennyWiseDb` adlı veritabanını ve tüm tabloları otomatik oluşturur.

**5. Uygulamayı başlatın:**
```bash
dotnet run
```

Uygulama `http://localhost:5083` adresinde açılacaktır.

---

## 👤 Kullanıcı Hesapları

Uygulama açıldıktan sonra **Register** butonuna tıklayarak kendi hesabınızı oluşturabilirsiniz.

> Şifre için minimum 4 karakter yeterlidir (büyük harf, sayı zorunluluğu yok).

---

## 📋 Özellikler

- ✅ Kullanıcı kaydı ve girişi (ASP.NET Core Identity)
- ✅ Gelir ve gider işlemi ekleme (kategori bazlı)
- ✅ Aylık Dashboard — toplam gelir, gider ve net bakiye
- ✅ Kategori bazlı harcama dağılımı
- ✅ Tüm işlemler listesi
- ✅ Bütçe limiti aşım uyarısı
- ✅ Navbar'da anlık bakiye gösterimi
- ✅ Admin rolü — kategori yönetimi

---

## 🗂️ Proje Yapısı

```
PennyWise/
├── Controllers/         # MVC Controller'lar
├── Data/                # EF Core DbContext
├── Migrations/          # Veritabanı migration dosyaları
├── Models/              # Entity sınıfları
├── ViewComponents/      # Bakiye bileşeni
├── ViewModels/          # Form veri modelleri
├── Views/               # Razor arayüz dosyaları
├── appsettings.json     # Genel ayarlar (şifre yok)
└── Program.cs           # Uygulama başlangıç noktası
```

---

## 🛠️ Kullanılan Teknolojiler

- **ASP.NET Core 8 MVC**
- **Entity Framework Core 8**
- **PostgreSQL** (Npgsql provider)
- **ASP.NET Core Identity**
- **Bootstrap 5**
