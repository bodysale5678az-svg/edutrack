using EduTrack.Data;
using EduTrack.Models;
using EduTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EnrollmentsController : Controller
    {
        private readonly AppDbContext _context;

        public EnrollmentsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // كل التسجيلات
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .OrderByDescending(e => e.EnrolledOn)
                .ToListAsync();

            return View(enrollments);
        }

        // ============================================================
        // تسجيل طلاب في كورس — دلوقتي (الرئيسية في نظام Many-to-Many)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["Error"] = "الكورس المطلوب غير موجود (ممكن يكون اتحذف).";
                return RedirectToAction("Index", "Courses");
            }

            return View(await BuildEnrollModelAsync(course));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId, int[]? selectedStudentIds)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["Error"] = "الكورس المطلوب غير موجود (ممكن يكون اتحذف).";
                return RedirectToAction("Index", "Courses");
            }

            var model = await BuildEnrollModelAsync(course);

            if (selectedStudentIds == null || selectedStudentIds.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "اختار طالب واحد على الأقل.");
                return View(model);
            }

            // نتحقق إن الـ Ids دي طلاب حقيقيين فعلاً
            var validIds = await _context.Students
                .Where(s => selectedStudentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            // الكورس الأساسي للطالب (Student.CourseId) معناه إنه مسجل فيه أصلاً
            var alreadyInCourse = await _context.Students
                .Where(s => validIds.Contains(s.Id) && s.CourseId == courseId)
                .Select(s => s.Id)
                .ToListAsync();

            var alreadyEnrolled = await _context.Enrollments
                .Where(e => e.CourseId == courseId && validIds.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .ToListAsync();

            var toAdd = validIds.Except(alreadyEnrolled).Except(alreadyInCourse).ToList();

            foreach (var studentId in toAdd)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    EnrolledOn = DateTime.Today
                });
            }

            if (toAdd.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            var skipped = validIds.Count - toAdd.Count;
            TempData["Success"] = skipped > 0
                ? $"تم تسجيل {toAdd.Count} طالب في الكورس. ({skipped} من الطلاب كانوا مسجلين بالفعل.)"
                : $"تم تسجيل {toAdd.Count} طالب في الكورس بنجاح!";

            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        // ============================================================
        // إلغاء تسجيل طالب من كورس
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إلغاء تسجيل ({enrollment.Student?.FullName}) من الكورس.";

            return RedirectToAction("Details", "Courses", new { id = enrollment.CourseId });
        }

        private async Task<EnrollStudentsViewModel> BuildEnrollModelAsync(Course course)
        {
            var enrolledIds = await _context.Enrollments
                .Where(e => e.CourseId == course.Id)
                .Select(e => e.StudentId)
                .ToListAsync();

            // الكورس الأساسي للطالب يعتبر تسجيل موجود أصلاً
            var available = await _context.Students
                .Include(s => s.User)
                .Where(s => !enrolledIds.Contains(s.Id) && s.CourseId != course.Id)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return new EnrollStudentsViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Students = available.Select(s => new StudentCheckboxItem
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    University = s.University
                }).ToList(),
                EnrolledCount = await _context.Courses
                    .Where(c => c.Id == course.Id)
                    .Select(c => c.Students.Count + c.Enrollments.Count)
                    .FirstOrDefaultAsync()
            };
        }
    }
}
