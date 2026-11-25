namespace EduTrack.Models.Options
{
    /// <summary>
    /// حسابات الأدمن اللي بتتعمل تلقائيًا عند التشغيل.
    /// المصدر: قسم "SeedAdmins" في appsettings.json.
    /// </summary>
    public class SeedAdminOptions
    {
        public const string SectionName = "SeedAdmins";

        /// <summary>
        /// كلمة السر الافتراضية لكل حساب جديد. الحسابات الموجودة مش بتتأثر.
        /// </summary>
        public string DefaultPassword { get; set; } = string.Empty;

        /// <summary>
        /// قائمة إيميلات الأدمن. ضيف إيميل هنا وأعد تشغيل التطبيق.
        /// </summary>
        public List<string> Emails { get; set; } = new();
    }
}
