using EduTrack.Data;
using EduTrack.Models;
using EduTrack.Models.Options;
using EduTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// نسجل الأخطاء في ملف داخل الموقع — لازم ندخل الـ logger قبل الـ pipeline
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

// بنعمل التحويل لـ HTTPS فقط لو فيه منفذ https مضبوط (بروفايل https).
// تشغيل بروفايل http مباشرة كان بيطلّع تحذير كل Request.
if (app.Urls.Any(url => url.StartsWith("https", StringComparison.OrdinalIgnoreCase)))
{
    app.UseHttpsRedirection();
}

// أي كود حالة (404/403/500) يظهر صفحة عربية مفهومة بدل صفحة بيضاء فاضية
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

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

// 5) صفحة تشخيص تعرض سبب الفشل للأدمن فقط
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        bool ok = await db.Database.CanConnectAsync();
        return ok
            ? Results.Ok(new { status = "ok", database = "connected" })
            : Results.Json(new { status = "error", database = "unreachable" }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", message = ex.Message }, statusCode: 503);
    }
});

app.Run();
