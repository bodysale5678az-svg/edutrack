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

            // إضافة قسم (لو مفيش)
            Department? defaultDepartment = await context.Departments
                .FirstOrDefaultAsync(d => d.Name == DbSeeder.DemoDepartmentName);

            if (defaultDepartment == null)
            {
                defaultDepartment = new Department
                {
                    Name = DbSeeder.DemoDepartmentName,
                    Description = "قسم بيضم محاضري تطوير الويب والـ .NET."
                };

                context.Departments.Add(defaultDepartment);
                await context.SaveChangesAsync();
            }

            if (await context.Instructors.AnyAsync())
            {
                // ربط المحاضرين القدامى بالقسم لو لسه بدون قسم
                var unassigned = await context.Instructors
                    .Where(i => i.DepartmentId == null)
                    .ToListAsync();

                if (unassigned.Count > 0)
                {
                    foreach (var existing in unassigned)
                    {
                        existing.DepartmentId = defaultDepartment.Id;
                    }

                    await context.SaveChangesAsync();
                }

                return;
            }

            var instructorUser = await userManager.FindByEmailAsync(DbSeeder.InstructorEmail);
            if (instructorUser == null)
            {
                instructorUser = new ApplicationUser
                {
                    UserName = DbSeeder.InstructorEmail,
                    Email = DbSeeder.InstructorEmail,
                    EmailConfirmed = true,
                    FullName = "محاضر تجريبي"
                };

                var created = await userManager.CreateAsync(instructorUser, adminPassword);
                if (!created.Succeeded)
                {
                    logger.LogWarning("لم يتم إنشاء حساب المحاضر التجريبي: {Errors}",
                        string.Join(" | ", created.Errors.Select(e => e.Description)));
                    return;
                }

                await userManager.AddToRoleAsync(instructorUser, "Instructor");
            }

            var instructor = new Instructor
            {
                FullName = "محاضر تجريبي",
                Specialization = "تطوير الويب",
                Bio = "حساب تجريبي أُنشئ تلقائيًا لتجربة إنشاء الكورسات.",
                UserId = instructorUser.Id,
                DepartmentId = defaultDepartment.Id
            };

            context.Instructors.Add(instructor);
            await context.SaveChangesAsync();

            context.Courses.Add(new Course
            {
                Title = "كورس تجريبي: ASP.NET Core MVC",
                Description = "كورس تجريبي أُنشئ تلقائيًا عند أول تشغيل للتأكد من أن الإضافة تعمل.",
                TotalHours = 40,
                InstructorId = instructor.Id
            });

            await context.SaveChangesAsync();

            logger.LogInformation("تم زرع بيانات البداية (محاضر + كورس تجريبي).");
        }
    }
}
