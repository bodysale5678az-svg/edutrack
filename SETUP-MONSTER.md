# تجهيز الموقع على MonsterASP — نسخ ولصق

## القيم اللي هتحتاجها (٤ قيم)
من: **لوحة التحكم → Databases → موقعك → Users → Connection string**

هتلاقي نص شكله كده:

```
Server=db12345.public.databaseasp.net; Database=db12345; User Id=db12345; Password=XXXXXXXX;
```

استخرج منه:

| القيمة | مكانها في النص |
|---|---|
| `EDUTRACK_DB_SERVER` | اللي بعد `Server=` |
| `EDUTRACK_DB_NAME` | اللي بعد `Database=` |
| `EDUTRACK_DB_USER` | اللي بعد `User Id=` |
| `EDUTRACK_DB_PASS` | اللي بعد `Password=` |

---

## المتغيرات اللي تتحط في Environment Variables

روح: **Websites → موقعك → Environment Variables** وأضف هدول بالظبط:

| Name | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `EDUTRACK_DB_SERVER` | (من الخطوة اللي فوق) |
| `EDUTRACK_DB_NAME` | (من الخطوة اللي فوق) |
| `EDUTRACK_DB_USER` | (من الخطوة اللي فوق) |
| `EDUTRACK_DB_PASS` | (من الخطوة اللي فوق) |

> مفصولين ٥ متغيرات بدل نص واحد طويل — أسهل في التصحيح لو حرف اتكتب غلط.

---

## رفع الملفات

ارفع **محتوى** مجلد `C:\Users\HP\source\repos\edutrack\publish` جوّه `/wwwroot` مباشرة.
(مش ترفع المجلد نفسه جواه — افتحه وارفع اللي جوّه)

---

## التأكد

افتح: `https://موقعك/health`

- `{"status":"ok","database":"connected"}` → تمام ✅
- `{"status":"error", ...}` → الاتصال غلط، شوف الرسالة

بعد كده ادخل: `admin@edutrack.com` / `EduTrack@2026`
