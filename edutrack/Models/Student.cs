using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduTrack.Models
{
    // يمثل الطالب المسجل بالمنصة ويرتبط بحساب دخول (ApplicationUser) بنسبة واحد لواحد
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الطالب مطلوب")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "اسم الطالب")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(20)]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "الجامعة / الكلية")]
        public string? University { get; set; }

        // الكورس الأساسي اللي الطالب سجّل منه (الكورسات الباقية في Enrollments)
        [Display(Name = "الكورس")]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        // الربط مع جدول AspNetUsers (One-to-One)
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // الطالب الواحد له عدة تسليمات
        public List<Submission> Submissions { get; set; } = new List<Submission>();

        // الطالب ممكن يتسجل في أكتر من كورس
        public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
