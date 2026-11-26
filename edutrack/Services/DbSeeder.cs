using EduTrack.Models.Options;

namespace EduTrack.Services
{
    /// <summary>
    /// بيانات الدخول الأساسية للنظام (Admins فقط — لا يوجد زرع بيانات تجريبية).
    /// </summary>
    public static class DbSeeder
    {
        public const string DefaultAdminFullName = "مدير النظام";

        // إيميل الأدمن الافتراضي لما SeedAdmins ناقص
        private const string FallbackEmail = "admin@edutrack.com";

        // كلمة سر احتياطية قوية — الحساب الموجود مش بتتغير كلمة سره
        private const string FallbackPassword = "EduTrack@2026";

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
                cleaned.Add(FallbackEmail);
            }

            return cleaned;
        }

        /// <summary>
        /// كلمة السر الافتراضية للحسابات الجديدة، من SeedAdmins:DefaultPassword.
        /// </summary>
        public static string GetDefaultAdminPassword(IConfiguration configuration)
        {
            string? password = configuration[$"{SeedAdminOptions.SectionName}:DefaultPassword"];

            return string.IsNullOrWhiteSpace(password) ? FallbackPassword : password;
        }
    }
}
