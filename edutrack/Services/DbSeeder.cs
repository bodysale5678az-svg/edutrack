using EduTrack.Models.Options;

namespace EduTrack.Services
{
    /// <summary>
    /// بيانات البداية المستخدمة للتجربة (Roles + Admins + محاضر + كورس).
    /// </summary>
    public static class DbSeeder
    {
        public const string InstructorEmail = "instructor@edutrack.com";
        public const string DemoDepartmentName = "قسم تطوير الويب";
        public const string DefaultAdminFullName = "مدير النظام";

        /// <summary>
        /// إيميلات الأدمن النشطة. القائمة بتتقرأ من قسم SeedAdmins في appsettings.json،
        /// ولو القسم ناقص أو فاضي بنرجع لإيميل افتراضي واحد عشان النظام يفضل شغال.
        /// </summary>
        public static IReadOnlyList<string> GetAdminEmails(IConfiguration configuration)
        {
            var emails = configuration
                .GetSection($"{SeedAdminOptions.SectionName}:Emails")
                .Get<List<string>>() ?? new List<string>();

            var cleaned = emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleaned.Count == 0)
            {
                cleaned.Add("admin@edutrack.com");
            }

            return cleaned;
        }

        /// <summary>
        /// كلمة السر الافتراضية للحسابات الجديدة، من SeedAdmins:DefaultPassword.
        /// </summary>
        public static string GetDefaultAdminPassword(IConfiguration configuration)
        {
            string? password = configuration[$"{SeedAdminOptions.SectionName}:DefaultPassword"];

            return string.IsNullOrWhiteSpace(password) ? "123456" : password;
        }
    }
}
