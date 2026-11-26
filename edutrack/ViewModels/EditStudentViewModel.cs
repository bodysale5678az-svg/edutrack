using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في شاشة Students/Edit — الأدمن بيعدّل بيانات الطالب
    public class EditStudentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الطالب مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        [Display(Name = "اسم الطالب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(20)]
        [Display(Name = "رقم الموبايل / واتساب")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "الجامعة / الكلية")]
        public string? University { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "اختر الكورس الأساسي")]
        [Display(Name = "الكورس الأساسي")]
        public int CourseId { get; set; }

        // للقراءة فقط
        public string? Email { get; set; }
    }

    // يستخدم في شاشة Students/Details — عرض شامل
    public class StudentDetailsViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? University { get; set; }

        public string? Email { get; set; }

        public string? CourseTitle { get; set; }

        public int SubmissionCount { get; set; }

        public List<string> ExtraCourses { get; set; } = new();
    }
}
