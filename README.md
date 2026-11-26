# EduTrack — نظام إدارة التعليم

[![CI](https://github.com/bodysale5678az-svg/edutrack/actions/workflows/ci.yml/badge.svg)](https://github.com/bodysale5678az-svg/edutrack/actions/workflows/ci.yml)

تطبيق ويب عربي (RTL) لإدارة العملية التعليمية، مبني بـ **ASP.NET Core MVC** و**Entity Framework Core** و**ASP.NET Core Identity**.
بيغطي إدارة الأقسام والمحاضرين والكورسات والطلاب والتسجيل في الكورسات والواجبات وتسليمها وتصحيحها.

---

## المميزات

| المجال | الإمكانيات |
|---|---|
| **الحسابات والصلاحيات** | تسجيل دخول/إنشاء حساب، ٣ أدوار: `Admin` / `Instructor` / `Student`، صفحات مخصصة لرفض الوصول |
| **الأقسام** | إضافة / تعديل / حذف / عرض، وربط المحاضرين بكل قسم |
| **المحاضرين** | إدارة كاملة + رفع صورة شخصية + **حذف** مع حسابه وصورته (ممنوع لو عنده كورسات) |
| **الكورسات** | إدارة كاملة + صورة مصغّرة + ربط الكورس بالمحاضر + **حذف** (ممنوع لو فيه طلاب/تاسكات) |
| **الطلاب** | عرض وإدارة بيانات الطلاب |
| **التسجيل (Enrollments)** | تسجيل طالب في أكتر من كورس (علاقة Many-to-Many) |
| **الواجبات** | إنشاء واجب بكورس محدد + ملف PDF + موعد تسليم + الدرجة النهائية |
| **التسليمات** | الطالب يرفع ملف الحل، المحاضر يصحّح ويكتب ملاحظات ودرجة |
| **الملفات** | خدمة `FileStorageService` للتحقق من الامتداد والحجم وتخزين الملفات |
| **الواجهة** | Bootstrap، تصميم عربي RTL، صفحات خطأ عربية بدل الصفحات البيضاء |

---

## التقنيات

- **.NET 10** / ASP.NET Core MVC
- **Entity Framework Core 10** + SQL Server
- **ASP.NET Core Identity** (أدوار + قفل الحساب بعد محاولات فاشلة)
- **Bootstrap 5** + jQuery + jQuery Validation
- **Razor Views** (`.cshtml`)

---

## المتطلبات

- [.NET SDK 10](https://dotnet.microsoft.com/download) أو أحدث
- **SQL Server** (أو SQL Server Express / LocalDB) شغّال محليًا
- (اختياري) `dotnet-ef` لو هتضيف Migrations جديدة

---

## التشغيل

```bash
# 1) استعادة الحزم
dotnet restore

# 2) تشغيل التطبيق
dotnet run --project edutrack/edutrack.csproj
```

بعد التشغيل افتح: <http://localhost:5104>

> التطبيق بيطبّق أي Migrations ناقصة **تلقائيًا** عند بدء التشغيل، وبيعمل **الأدوار
> وحساب الأدمن فقط**. لا توجد بيانات تجريبية — بتضيف الأقسام والمحاضرين والكورسات من الواجهة.

### سلسلة الاتصال

سلسلة التطوير في `edutrack/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=EduTrackDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=True"
}
```

عدّلها لو SQL Server عندك على instance مختلف (مثلًا `Server=.\SQLEXPRESS`).

**على الاستضافة** متحطش السلسلة في الملفات — استخدم متغيرات بيئة:

| المتغير | القيمة |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | نص الاتصال الكامل |

أو مفصولة: `EDUTRACK_DB_SERVER` / `EDUTRACK_DB_NAME` / `EDUTRACK_DB_USER` / `EDUTRACK_DB_PASS`.

### فحص الحالة

`GET /health` بيرجع `{"status":"ok","database":"connected"}` أو `503` لو قاعدة البيانات مش متاحة.

---

## حسابات الدخول

| الدور | البريد | كلمة السر |
|---|---|---|
| Admin | من `SeedAdmins:Emails` | من `SeedAdmins:DefaultPassword` |

> ⚠️ الافتراضي في التطوير `EduTrack@2026`. **غيّرها قبل أي نشر حقيقي** —
> حطّ `SeedAdmins__DefaultPassword` كمتغير بيئة على السيرفر.
> الحسابات الموجودة مش بتتغير كلمة سرها، فالتغيير بيأثر على الحسابات الجديدة فقط.

---

## هيكل المشروع

```
edutrack/
├── edutrack.slnx                  # Solution
└── edutrack/
    ├── Controllers/               # Account, Admin, Assignments, Courses, Departments,
    │                              # Enrollments, Home, Instructors, Students, Submissions
    ├── Data/                      # AppDbContext + DatabaseInitializer (migrate + seed)
    ├── Migrations/                # EF Core migrations
    ├── Models/                    # ApplicationUser, Department, Instructor, Course,
    │                              # Student, Assignment, Submission, Enrollment, Options
    ├── Services/                  # DbSeeder + FileStorageService
    ├── ViewModels/                # View models لكل شاشة
    ├── Views/                     # Razor views (RTL)
    ├── wwwroot/                   # CSS/JS/lib + مجلدات الملفات المرفوعة
    ├── appsettings.json
    └── Program.cs
```

---

## أوامر مفيدة

```bash
# بناء المشروع
dotnet build edutrack.slnx

# إضافة Migration جديدة (بعد تعديل الموديلز)
dotnet ef migrations add <Name> --project edutrack/edutrack.csproj

# تطبيق الـ Migrations يدويًا
dotnet ef database update --project edutrack/edutrack.csproj
```

---

## ملاحظة

ملف `EduTrack-Sharh-almashroo3.pdf` في جذر الريبو هو شرح المشروع (وثيقة التسليم).

---

## الترخيص

هذا المشروع مرخّص تحت [MIT License](LICENSE).
