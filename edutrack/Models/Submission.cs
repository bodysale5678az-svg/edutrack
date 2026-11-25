using System.ComponentModel.DataAnnotations;

namespace EduTrack.Models
{
    // يمثل حل الطالب للتاسك + تقييم المحاضر
    public class Submission
    {
        public int Id { get; set; }

        [Display(Name = "تاريخ التسليم")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        // اسم ملف الحل يُحفظ في مجلد wwwroot/uploads/submissions
        [Required]
        [StringLength(200)]
        [Display(Name = "ملف الحل")]
        public string SolutionFilePath { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "ملاحظات الطالب")]
        public string? StudentNotes { get; set; }

        // الدرجة (يحددها المحاضر بعد التصحيح)
        [Range(0, 1000)]
        [Display(Name = "الدرجة")]
        public int? Grade { get; set; }

        [StringLength(2000)]
        [Display(Name = "ملاحظات المحاضر (Feedback)")]
        public string? InstructorFeedback { get; set; }

        // Pending (لسه متصححش) أو Graded (تم التصحيح)
        [StringLength(20)]
        [Display(Name = "الحالة")]
        public string Status { get; set; } = "Pending";

        // العلاقات
        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }
    }
}
