namespace EduTrack.Models.Options
{
    public class FileStorageOptions
    {
        public string CourseImagesFolder { get; set; } = "images/courses";

        public string InstructorImagesFolder { get; set; } = "images/instructors";

        public string AssignmentFolder { get; set; } = "uploads/assignments";

        public string SubmissionFolder { get; set; } = "uploads/submissions";
    }
}
