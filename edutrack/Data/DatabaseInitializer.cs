using EduTrack.Models;
using EduTrack.Models.Options;
using EduTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Data
{
    /// <summary>
    /// يُطبّق الـ Migrations المعلّقة ثم يزرع بيانات البداية.
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

            await context.Database.MigrateAsync();

            foreach (var role in new[] { "Admin", "Instructor", "Student" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // حسابات الأدمن بتتقرأ من قسم SeedAdmins في appsettings.json.
            // أي إيميل جديد في القائمة بيتعمله حساب Admin عند التشغيل،
            // والإيميلات الموجودة بتتساب زي ما هي (كلمات سرهم مش بتتغير).
            var configuration = services.GetRequiredService<IConfiguration>();
            var adminEmails = DbSeeder.GetAdminEmails(configuration);
            var adminPassword = DbSeeder.GetDefaultAdminPassword(configuration);

            foreach (var email in adminEmails)
            {
                var admin = await userManager.FindByEmailAsync(email);
                if (admin == null)
                {
                    admin = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = DbSeeder.DefaultAdminFullName
                    };

                    var created = await userManager.CreateAsync(admin, adminPassword);
                    if (!created.Succeeded)
                    {
                        logger.LogError("فشل إنشاء حساب الأدمن {Email}: {Errors}",
                            email, string.Join(" | ", created.Errors.Select(e => e.Description)));
                        continue;
                    }

                    logger.LogInformation("تم إنشاء حساب الأدمن {Email}.", email);
                }

                // الحساب الموجود برضه بنتأكد إنه معاه دور Admin
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        logger.LogWarning("لم يتم إسناد دور Admin لـ {Email}: {Errors}",
                            email, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            // ملاحظة: تم إيقاف زرع البيانات التجريبية (قسم + محاضر + كورس).
            // النظام يبدأ نظيف: الأدمن والأدوار فقط، وباقي البيانات تُضاف من الواجهة.

            logger.LogInformation("تم تهيئة قاعدة البيانات (Roles + Admins). لا يوجد زرع بيانات تجريبية.");
        }
    }
}
