using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduTrack.Models
{
    // يمثل بيانات المحاضر ويرتبط بحساب دخول (ApplicationUser) بنسبة واحدة
    public class Instructor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المحاضر مطلوب")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "اسم المحاضر")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "التخصص مطلوب")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "التخصص")]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "نبذة عن المحاضر")]
        public string? Bio { get; set; }

        // مسار الصف الشخصية داخل wwwroot/images/instructors
        [Display(Name = "الصورة الشخصية")]
        public string? ImagePath { get; set; }

        // الربط مع AspNetUsers (One-to-One)
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // القسم التابع له المحاضر — اختياري
        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // المحاضر الواحد ممكن يدرّس أكثر من كورس
        public List<Course> Courses { get; set; } = new List<Course>();
    }
}
