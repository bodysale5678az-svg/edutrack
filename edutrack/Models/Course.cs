using System.ComponentModel.DataAnnotations;

namespace EduTrack.Models
{
    // يمثل الكورس التعليمي ويوضع تحت مسؤولية أحد الأساتذة أو المحاضرين
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان الكورس مطلوب")]
        [StringLength(120, MinimumLength = 3)]
        [Display(Name = "اسم الكورس")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "وصف الكورس مطلوب")]
        [StringLength(2000, MinimumLength = 10)]
        [Display(Name = "وصف الكورس")]
        public string Description { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "عدد الساعات يجب أن يكون بين 1 و 500")]
        [Display(Name = "عدد الساعات")]
        public int TotalHours { get; set; }

        // مسار صورة الغلاف داخل wwwroot/images/courses
        [Display(Name = "صورة الغلاف")]
        public string? ThumbnailPath { get; set; }

        // المسؤول الرئيس عن الكورس
        [Display(Name = "المحاضر")]
        public int InstructorId { get; set; }
        public Instructor? Instructor { get; set; }

        // العلاقات: الكورس الواحد فيه طلاب + التاسكات الخاصة بالكورس
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();

        // التسجيلات — الطالب ممكن يكون مسجل في أكتر من كورس
        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
