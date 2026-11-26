using EduTrack.Data;
using EduTrack.Models;
using EduTrack.Models.Options;
using EduTrack.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------
// نص الاتصال: إما ConnectionStrings:DefaultConnection جاهز،
// أو يتجمّع من متغيرات البيئة المفصولة (أسهل للضبط على الاستضافة).
// ------------------------------------------------------------------
string? resolvedConnection =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"];

string? dbServer = builder.Configuration["EDUTRACK_DB_SERVER"];
string? dbName = builder.Configuration["EDUTRACK_DB_NAME"];
string? dbUser = builder.Configuration["EDUTRACK_DB_USER"];
string? dbPass = builder.Configuration["EDUTRACK_DB_PASS"];

if (!string.IsNullOrWhiteSpace(dbServer) && !string.IsNullOrWhiteSpace(dbName))
{
    resolvedConnection =
        $"Server={dbServer};Database={dbName};" +
        $"User Id={dbUser ?? dbName};Password={dbPass};" +
        "Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
}

if (string.IsNullOrWhiteSpace(resolvedConnection))
{
    // مفيش اتصال مضبوط: نستخدم اسم قاعدة بيانات وهمي عشان التطبيق يقوم
    // وتظهر صفحة التشخيص بدل ما يفشل عند بدء التشغيل.
    resolvedConnection = "Server=(local);Database=__not_configured__;Integrated Security=true;" +
                         "Connect Timeout=2;TrustServerCertificate=True;";
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = resolvedConnection;

builder.Services.AddControllersWithViews();

// 1) ربط قاعدة البيانات — SplitQuery يمنع تضخم النتائج عند تحميل أكثر من Collection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 1.1) مجلدات رفع الملفات + خدمة التحقق من نوع الملف وحجمه
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// إيميلات الأدمن اللي بتتعمل تلقائيًا — المصدر: قسم SeedAdmins في appsettings.json
builder.Services.Configure<SeedAdminOptions>(builder.Configuration.GetSection(SeedAdminOptions.SectionName));
builder.Services.AddScoped<FileStorageService>();

// 2) Identity — شروط باسورد مبسطة للتدريب
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3) صفحات الدخول والرفض
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 3.1) مفاتيح تشفير الكوكيز بتتحفظ على القرص.
// من غير كده المفاتيح بتتولد من جديد مع كل restart، وكل المستخدمين بيطردوا.
// المسار ده جوه الموقع وبيتكتب عليه، فبيشتغل على الاستضافة.
var keysPath = builder.Configuration["DataProtection:KeysPath"];

if (string.IsNullOrWhiteSpace(keysPath))
{
    keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
}

keysPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keysPath));

try
{
    Directory.CreateDirectory(keysPath);

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("EduTrack");
}
catch (Exception ex)
{
    // لو المجلد مش قابل للكتابة، التطبيق يفضل يشتغل (بس المستخدمين هيطردوا مع كل restart)
    Console.Error.WriteLine($"تعذر تجهيز مجلد مفاتيح التشفير ({keysPath}): {ex.Message}");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// نسجل الأخطاء في اللوج — لازم ندخل الـ logger قبل الـ pipeline
var fileLogger = app.Logger;

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        fileLogger.LogError(ex, "خطأ غير متوقع في المسار {Path}", context.Request.Path);
        throw;
    }
});

// أخطاء قاعدة البيانات بتتحول لصفحة عربية واضحة بدل 500
app.UseMiddleware<EduTrack.Middleware.DatabaseErrorMiddleware>();

// بنعمل التحويل لـ HTTPS فقط لو فيه منفذ https مضبوط (بروفايل https).
// تشغيل بروفايل http مباشرة كان بيطلّع تحذير كل Request.
if (app.Urls.Any(url => url.StartsWith("https", StringComparison.OrdinalIgnoreCase)))
{
    app.UseHttpsRedirection();
}

// أي كود حالة (404/403/500) يظهر صفحة عربية مفهومة بدل صفحة بيضاء فاضية
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

// الملفات الثابتة (CSS/JS/lib) + الملفات المرفوعة وقت التشغيل.
// بنستخدم UseStaticFiles بدل MapStaticAssets عشان الملفات اللي بتترفع
// بعد الـ build (صور المحاضرين/الكورسات، ملفات التسليم) تتخدم برضه.
app.UseStaticFiles();

// الـ wwwroot الافتراضي مش هيغطي /images و /uploads لو الملفات اتخزنت
// خارج المجلد — بس هنا التخزين جوه wwwroot، فالـ UseStaticFiles فوق كفاية.
// السطور دي بتبقى احتياط لو اتغير RootFolder في المستقبل.
var storageRoot = builder.Configuration["FileStorage:RootFolder"];
if (!string.IsNullOrWhiteSpace(storageRoot))
{
    var physical = Path.Combine(app.Environment.ContentRootPath, storageRoot);
    Directory.CreateDirectory(physical);

    foreach (var subPath in new[] { "/images", "/uploads" })
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(physical),
            RequestPath = subPath
        });
    }
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 4) تطبيق أي Migrations ناقصة — من غير ما يوقف التطبيق لو قاعدة البيانات مش متاحة
using (var scope = app.Services.CreateScope())
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        // قاعدة البيانات مش متاحة؟ الموقع يفضل شغال ويعرض رسالة واضحة بدل HTTP 500
        startupLogger.LogError(ex, "فشل تهيئة قاعدة البيانات أثناء بدء التطبيق.");
    }
}

// 5) نقطة تشخيص: بتقول هل قاعدة البيانات متاحة من غير ما تسرّب كلمة السر أو تفاصيل الاتصال
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        bool ok = await db.Database.CanConnectAsync();

        return ok
            ? Results.Ok(new { status = "ok", database = "connected" })
            : Results.Json(new { status = "error", database = "unreachable" }, statusCode: 503);
    }
    catch
    {
        // التفاصيل الكاملة بتتسجل في اللوج، ورد المستخدم عام عشان ما نسرّبش بيانات الاتصال
        return Results.Json(new { status = "error", database = "unreachable" }, statusCode: 503);
    }
});

app.Run();
