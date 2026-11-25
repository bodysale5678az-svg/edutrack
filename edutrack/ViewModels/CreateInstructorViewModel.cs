using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في شاشة Instructors/Create لإنشاء محاضر + حساب Identity + رفع صورته الشخصية
    public class CreateInstructorViewModel
    {
        [Required(ErrorMessage = "اسم المحاضر مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        [Display(Name = "اسم المحاضر بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "التخصص مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "التخصص يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "التخصص التقني")]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "نبذة مختصرة")]
        public string? Bio { get; set; }

        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني (للدخول)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "الصورة الشخصية للمحاضر")]
        public IFormFile? ProfileImage { get; set; }
    }
}
