using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // الحقول اللي بتظهر في فورم إضافة التاسك بس (Assignments/Create)
    // بدون Course و Submissions — دول Relations في الـ Model مش في الفورم
    public class AssignmentViewModel
    {
        // مخفي في الفورم — بيتحدد من الكورس اللي المحاضر فاتحه
        public int CourseId { get; set; }

        // للعرض بس (اسم الكورس فوق الفورم)
        public string? CourseTitle { get; set; }

        [Required(ErrorMessage = "عنوان التاسك مطلوب")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "العنوان يجب أن يكون بين 3 و 200 حرف")]
        [Display(Name = "عنوان التاسك")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "تفاصيل التاسك مطلوبة")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "التفاصيل يجب أن تكون بين 10 و 4000 حرف")]
        [Display(Name = "تفاصيل ومطلوبات التاسك")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "آخر موعد للتسليم مطلوب")]
        [Display(Name = "آخر موعد للتسليم (Deadline)")]
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);

        [Range(1, 1000, ErrorMessage = "الدرجة يجب أن تكون بين 1 و 1000")]
        [Display(Name = "الدرجة النهائية للتاسك")]
        public int MaxDegree { get; set; } = 100;

        // الملف اللي المحاضر بيرفعه (IFormFile) — مش نخزّن المسار من الفورم
        [Display(Name = "ملف شرح التاسك (PDF اختياري)")]
        public IFormFile? AttachmentFile { get; set; }
    }
}
