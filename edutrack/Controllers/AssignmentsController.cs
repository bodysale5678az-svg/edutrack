using EduTrack.Data;
using EduTrack.Models;
using EduTrack.Models.Options;
using EduTrack.Services;
using EduTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduTrack.Controllers
{
    // أي مستخدم مسجّل يقدر يشوف تفاصيل التاسك (الطالب محتاجها عشان يسلّم الحل)
    // إنشاء التاسك نفسه محصور على المحاضر والأدمن في الـ Actions
    [Authorize]
    public class AssignmentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _files;
        private readonly FileStorageOptions _storage;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentsController(AppDbContext context, FileStorageService files,
            IOptions<FileStorageOptions> storage, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _files = files;
            _storage = storage.Value;
            _userManager = userManager;
        }

        // ============================================================
        // 5.1 إضافة تاسك جديد — GET
        // ============================================================
        [Authorize(Roles = "Instructor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create(int courseId)
        {
            // لو الرابط مافيهوش courseId (أو اتفتح من لينك قديم) منعملش 404 صفحة بيضاء
            if (courseId <= 0)
            {
                TempData["Error"] = "من فضلك اختار الكورس الأول من صفحة الكورسات، وبعدها اضغط (إضافة تاسك جديد).";
                return RedirectToAction("Index", "Courses");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["Error"] = "الكورس المطلوب غير موجود (ممكن يكون اتحذف). اختار كورس تاني من القائمة.";
                return RedirectToAction("Index", "Courses");
            }

            var model = new AssignmentViewModel
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Deadline = DateTime.Now.AddDays(7),
                MaxDegree = 100
            };

            return View(model);
        }

        // ============================================================
        // 5.2 إضافة تاسك جديد — POST
        // ============================================================
        [Authorize(Roles = "Instructor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> Create(AssignmentViewModel model)
        {
            // لازم الكورس يكون موجود قبل أي حفظ (منع خطأ Foreign Key لو الكورس اتحذف)
            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null)
            {
                TempData["Error"] = "الكورس المطلوب غير موجود (ممكن يكون اتحذف). اختار كورس تاني من القائمة.";
                return RedirectToAction("Index", "Courses");
            }

            // التحقق من أن الملف بصيغة PDF حقيقية (مش امتداد .pdf بس)
            if (model.AttachmentFile != null)
            {
                var check = FileStorageService.Validate(
                    model.AttachmentFile, FileStorageService.AttachmentExtensions, FileStorageService.AttachmentMaxBytes);

                if (!check.Success)
                {
                    ModelState.AddModelError(nameof(model.AttachmentFile), check.Error!);
                }
            }

            if (!ModelState.IsValid)
            {
                model.CourseTitle = course.Title;
                return View(model);
            }

            string? savedFileName = null;

            if (model.AttachmentFile != null)
            {
                savedFileName = await _files.SaveAsync(model.AttachmentFile, _storage.AssignmentFolder);
            }

            var assignment = new Assignment
            {
                Title = model.Title,
                Description = model.Description,
                Deadline = model.Deadline,
                MaxDegree = model.MaxDegree,
                CourseId = model.CourseId,
                AttachmentFilePath = savedFileName
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم نشر التاسك بنجاح!";
            return RedirectToAction("Details", "Courses", new { id = model.CourseId });
        }

        // ============================================================
        // 5.3 تفاصيل التاسك + تسليمات الطلاب — GET
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            bool isStaff = User.IsInRole("Instructor") || User.IsInRole("Admin");

            IQueryable<Assignment> query = _context.Assignments.Include(a => a.Course);

            // تسليمات الطلاب تظهر للمحاضر/الأدمن فقط — مش للطالب نفسه
            if (isStaff)
            {
                query = query
                    .Include(a => a.Submissions)
                        .ThenInclude(s => s.Student);
            }

            var assignment = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null)
            {
                return NotFound();
            }

            // الطالب يقدر يفتح التاسكات بتاعة الكورسات المسجل فيها بس
            if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

                bool allowed = student != null &&
                    (student.CourseId == assignment.CourseId ||
                     await _context.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == assignment.CourseId));

                if (!allowed)
                {
                    TempData["Error"] = "التاسك ده مش تابع لكورس من الكورسات المسجل فيها.";
                    return RedirectToAction("Index", "Courses");
                }
            }

            return View(assignment);
        }
    }
}
