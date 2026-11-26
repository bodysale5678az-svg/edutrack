# خطوات النشر على MonsterASP.NET

## ما تم تجهيزه مسبقًا
- `appsettings.Production.json` — إعدادات الإنتاج
- `web.config` — إعداد IIS (InProcess)
- `/health` — صفحة تشخيص تقولك حالة قاعدة البيانات
- نسخة Release جاهزة في: `C:\Users\HP\source\repos\edutrack\publish`

---

## الخطوات (٣ دقائق)

### 1) اعمل قاعدة البيانات
لوحة التحكم → **Databases** → **Create database** → اختار **MSSQL**
انسخ **Connection string (Local)** من زر **Users**.

### 2) اضبط الإعدادات على السيرفر
لوحة التحكم → **Websites** → موقعك → **Environment Variables** → أضف:

| Name | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | نص الاتصال الكامل (مع الباسورد) |

### 3) ارفع الملفات
طريقتين — اختار الأسهل:

**أ) Web Deploy (الأفضل):**
لوحة التحكم → **Deploy** → **WebDeploy** = Enabled → **Download publish profile**
في Visual Studio: كليك يمين على المشروع → **Publish** → **Import profile** → اختار الملف → **Publish**

**ب) FTP / WebFTP:**
ارفع محتوى مجلد `publish` **كله** جوّه `/wwwroot` (مش في مجلد فرعي).

### 4) تأكد إن الموقع شغال
افتح: `https://موقعك/health`

- `{"status":"ok","database":"connected"}` ← تمام ✅
- `{"status":"error","database":"unreachable"}` ← الاتصال غلط، راجع `ConnectionStrings__DefaultConnection`

### 5) لو ظهر 500
لوحة التحكم → **Logs** → **ASP.NET Core debug** → Enable → شوف السبب.

---

## ملاحظات مهمة
- التطبيق **مش هيقع** لو الداتابيز فشلت — هيفتح بس `/health` هتقولك المشكلة إيه.
- الملفات المرفوعة بتتخزن في مجلد `Uploads` خارج `wwwroot` (مسموح للكتابة).
- كلمة سر الأدمن الافتراضية `123456` — **غيّرها** من صفحة `Admin/ChangePassword`.
