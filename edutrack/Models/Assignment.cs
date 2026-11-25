using System.ComponentModel.DataAnnotations;

namespace EduTrack.Models
{
    // يمثل تاسك (واجب عملي) تابع لكورس معين
    public class Assignment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان التاسك مطلوب")]
        [StringLength(200, MinimumLength = 3)]
        [Display(Name = "عنوان التاسك")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "تفاصيل التاسك مطلوبة")]
        [StringLength(4000, MinimumLength = 10)]
        [Display(Name = "وصف التاسك")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "آخر موعد للتسليم مطلوب")]
        [Display(Name = "آخر موعد للتسليم (Deadline)")]
        public DateTime Deadline { get; set; }

        [Range(1, 1000)]
        [Display(Name = "الدرجة النهائية")]
        public int MaxDegree { get; set; } = 100;

        // ملف شرح التاسك PDF يُحفظ في مجلد wwwroot/uploads/assignments
        [Display(Name = "ملف شرح التاسك (PDF)")]
        public string? AttachmentFilePath { get; set; }

        // التاسكات مرتبطة بكورس
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // التاسكات الواحد له عدة تسليمات
        public List<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
