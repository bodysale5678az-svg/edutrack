using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    /// <summary>
    /// سطر في جدول إدارة المستخدمين.
    /// </summary>
    public class UserRowViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// شاشة إدارة المستخدمين: قائمة + فورم الإضافة.
    /// فورم الإضافة بيتبعت مباشرة كـ CreateAdminViewModel (مش جوه wrapper)
    /// عشان الأسماء اللي بيتولّدها Tag Helper تطابق أسماء بروبرتيز الموديل.
    /// </summary>
    public class ManageUsersViewModel
    {
        public IReadOnlyList<UserRowViewModel> Users { get; set; } = new List<UserRowViewModel>();

        public CreateAdminViewModel NewAdmin { get; set; } = new();
    }

    /// <summary>
    /// فورم إضافة أدمن جديد.
    /// </summary>
    public class CreateAdminViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم لازم يكون بين 3 و 100 حرف")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور لازم تكون 6 حروف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// فورم تغيير كلمة المرور.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور لازم تكون 6 حروف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمة المرور وتأكيدها مش متطابقين")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
