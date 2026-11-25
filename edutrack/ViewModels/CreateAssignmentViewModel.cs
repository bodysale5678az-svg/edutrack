using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في شاشة Assignments/Create لإضافة تاسك جديد مع رفع ملف PDF للشرح
    public class CreateAssignmentViewModel
    {
        [Required]
        public int CourseId { get; set; }

        public string? CourseTitle { get; set; }

        [Required(ErrorMessage = "عنوان التاسك مطلوب")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "العنوان يجب أن يكون بين 3 و 200 حرف")]
        [Display(Name = "عنوان التاسك")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "وصف التاسك مطلوب")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "الوصف يجب أن يكون بين 10 و 4000 حرف")]
        [Display(Name = "تفاصيل ومطلوبات التاسك")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "آخر موعد للتسليم مطلوب")]
        [Display(Name = "آخر موعد للتسليم (Deadline)")]
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);

        [Range(1, 1000)]
        [Display(Name = "الدرجة النهائية للتاسك")]
        public int MaxDegree { get; set; } = 100;

        [Display(Name = "ملف شرح التاسك (PDF اختياري)")]
        public IFormFile? AttachmentFile { get; set; }
    }
}
