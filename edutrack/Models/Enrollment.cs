using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduTrack.Models
{
    // تسجيل الطالب في كورس (Many-to-Many صريحة بين Student و Course)
    // الطالب ممكن يتسجل في أكتر من كورس، والكورس فيه أكتر من طالب
    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "الطالب")]
        public int StudentId { get; set; }

        [Required]
        [Display(Name = "الكورس")]
        public int CourseId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسجيل")]
        public DateTime EnrolledOn { get; set; } = DateTime.Today;

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }
    }
}
