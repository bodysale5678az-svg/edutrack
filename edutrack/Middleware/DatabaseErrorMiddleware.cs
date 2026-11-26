using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Middleware
{
    /// <summary>
    /// بيمسك أخطاء قاعدة البيانات في أي مكان في التطبيق ويعرض صفحة واضحة
    /// بدل 500 صفحة بيضاء. باقي الاستثناءات بتتسجل وترجع للـ pipeline العادي.
    /// </summary>
    public sealed class DatabaseErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DatabaseErrorMiddleware> _logger;

        public DatabaseErrorMiddleware(RequestDelegate next, ILogger<DatabaseErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (IsDatabaseFailure(ex))
            {
                _logger.LogError(ex, "تعذر الوصول لقاعدة البيانات أثناء معالجة {Path}", context.Request.Path);

                // الـ API بيرجع JSON، والصفحات بترجع صفحة عربية واضحة
                if (context.Request.Path.StartsWithSegments("/health"))
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { status = "error", database = "unreachable" });
                    return;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync(BuildPage());
            }
        }

        private static bool IsDatabaseFailure(Exception ex) =>
            ex is DbUpdateException
            || ex is TimeoutException
            || ex.InnerException is not null && IsDatabaseFailure(ex.InnerException)
            || ex.GetType().FullName == "Microsoft.Data.SqlClient.SqlException";

        private static string BuildPage() => """
            <!DOCTYPE html>
            <html lang="ar" dir="rtl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>قاعدة البيانات غير متاحة — EduTrack</title>
              <style>
                :root { color-scheme: light; }
                body { margin:0; font-family: "Segoe UI", Tahoma, sans-serif;
                       background:#0f172a; color:#e2e8f0; display:flex;
                       min-height:100vh; align-items:center; justify-content:center; }
                .card { background:#1e293b; border-radius:18px; padding:40px 32px;
                        max-width:520px; text-align:center; box-shadow:0 20px 50px rgba(0,0,0,.35); }
                .icon { font-size:48px; margin-bottom:12px; }
                h1 { font-size:22px; margin:0 0 12px; }
                p  { color:#94a3b8; line-height:1.9; margin:0 0 18px; font-size:15px; }
                code { background:#0f172a; padding:2px 8px; border-radius:6px; color:#38bdf8; font-size:13px; }
                a { display:inline-block; margin-top:8px; background:#2563eb; color:#fff;
                    text-decoration:none; padding:10px 22px; border-radius:999px; font-size:14px; }
              </style>
            </head>
            <body>
              <div class="card">
                <div class="icon">🗄️</div>
                <h1>تعذر الاتصال بقاعدة البيانات</h1>
                <p>
                  الموقع شغال، لكن البيانات مش متاحة حاليًا.<br />
                  لو أنت المسؤول، تأكد من إعداد
                  <code>ConnectionStrings__DefaultConnection</code>
                  على السيرفر.
                </p>
                <a href="/health">فحص حالة الاتصال</a>
              </div>
            </body>
            </html>
            """;
    }
}
