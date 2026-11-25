using System.ComponentModel.DataAnnotations;

namespace EduTrack.Models
{
    // القسم / التخصص الأكاديمي، والمحاضرون بيتوزعوا عليه
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم القسم يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "اسم القسم")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "وصف القسم")]
        public string? Description { get; set; }

        [Display(Name = "المحاضرون")]
        public List<Instructor> Instructors { get; set; } = new List<Instructor>();
    }
}
