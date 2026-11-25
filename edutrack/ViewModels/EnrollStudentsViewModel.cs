using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // فورم تسجيل طلاب في كورس (Enrollments/Enroll)
    public class EnrollStudentsViewModel
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = string.Empty;

        public int EnrolledCount { get; set; }

        // الطلاب اللي لسه مش متسجلين في الكورس
        public List<StudentCheckboxItem> Students { get; set; } = new List<StudentCheckboxItem>();

        [Display(Name = "الطلاب")]
        public int[] SelectedStudentIds { get; set; } = Array.Empty<int>();
    }

    public class StudentCheckboxItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? University { get; set; }
        public bool IsSelected { get; set; }
    }
}
