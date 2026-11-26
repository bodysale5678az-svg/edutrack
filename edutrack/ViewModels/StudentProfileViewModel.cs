using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في صفحة Account/Profile — الطالب بيعدّل بياناته الشخصية
    public class StudentProfileViewModel
    {
        [Required(ErrorMessage = "الاسم بالكامل مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        [Display(Name = "الاسم بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(20)]
        [Display(Name = "رقم الموبايل / واتساب")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "الجامعة / الكلية")]
        public string? University { get; set; }

        // للقراءة فقط — البريد هو اسم الدخول، تغييره من هنا ممنوع
        public string? Email { get; set; }

        public string? CourseTitle { get; set; }
    }

    // تغيير كلمة المرور (متاح لكل المستخدمين المسجلين)
    public class ChangeOwnPasswordViewModel
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
