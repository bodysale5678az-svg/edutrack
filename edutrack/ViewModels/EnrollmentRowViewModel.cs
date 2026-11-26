namespace EduTrack.ViewModels
{
    /// <summary>
    /// صف واحد في صفحة "كل التسجيلات". بيغطي مصدرين:
    ///   1) التسجيل الأساسي للطالب (Student.CourseId) — لما يسجّل من صفحة Register.
    ///   2) تسجيل إضافي في كورس تاني (جدول Enrollments).
    /// لو الاتنين بيعرضوا نفس الطالب/الكورس، الأساسي بيكسب ومفيش تكرار.
    /// </summary>
    public class EnrollmentRowViewModel
    {
        /// <summary>معرّف سجل Enrollments، أو null لو ده تسجيل أساسي.</summary>
        public int? EnrollmentId { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string? StudentEmail { get; set; }

        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = string.Empty;

        /// <summary>تاريخ التسجيل، أو null لو مش معروف (التسجيل الأساسي مالوش تاريخ).</summary>
        public DateTime? EnrolledOn { get; set; }

        /// <summary>true = التسجيل الأساسي، ومينفعش يتلغي من هنا.</summary>
        public bool IsPrimary { get; set; }
    }
}
