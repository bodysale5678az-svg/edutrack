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
    [Authorize]
    public class SubmissionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileStorageService _files;
        private readonly FileStorageOptions _storage;

        public SubmissionsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            FileStorageService files,
            IOptions<FileStorageOptions> storage)
        {
            _context = context;
            _userManager = userManager;
            _files = files;
            _storage = storage.Value;
        }

        // ============================================================
        // 6.1 تسليم الحل من الطالب — GET
        // ============================================================
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Submit(int assignmentId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return NotFound();
            }

            // الطالب ماينفعش يفتح تاسك من كورس مش مسجل فيه
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return Forbid();
            }

            if (!await IsEnrolledAsync(student.Id, assignment.CourseId))
            {
                TempData["Error"] = "التاسك ده مش تابع لكورس من الكورسات المسجل فيها.";
                return RedirectToAction("Index", "Courses");
            }

            var model = new SubmitAssignmentViewModel
            {
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                CourseTitle = assignment.Course?.Title,
                Deadline = assignment.Deadline
            };

            return View(model);
        }

        // ============================================================
        // 6.2 تسليم الحل من الطالب — POST
        // ============================================================
        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> Submit(SubmitAssignmentViewModel model)
        {
            // ملف الحل لازم يكون .zip أو .rar أو .pdf (ولازم يكون ملف حقيقي من نفس النوع)
            if (model.SolutionFile != null)
            {
                var check = FileStorageService.Validate(
                    model.SolutionFile, FileStorageService.SubmissionExtensions, FileStorageService.SubmissionMaxBytes);

                if (!check.Success)
                {
                    ModelState.AddModelError(nameof(model.SolutionFile), check.Error!);
                }
            }

            if (!ModelState.IsValid)
            {
                var a = await _context.Assignments
                    .Include(x => x.Course)
                    .FirstOrDefaultAsync(x => x.Id == model.AssignmentId);

                model.AssignmentTitle = a?.Title;
                model.CourseTitle = a?.Course?.Title;
                model.Deadline = a?.Deadline;

                return View(model);
            }

            // 1) الطالب الحالي من Identity (مش من الفورم)
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return Forbid();
            }

            // 2) التاسك لازم يكون موجود، وتابع لكورس الطالب، وميعاده لسه مفتوح
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == model.AssignmentId);

            if (assignment == null)
            {
                TempData["Error"] = "التاسك المطلوب غير موجود (ممكن يكون اتحذف).";
                return RedirectToAction("Index", "Courses");
            }

            if (assignment.CourseId != student.CourseId && !await IsEnrolledAsync(student.Id, assignment.CourseId))
            {
                TempData["Error"] = "التاسك ده مش تابع لكورس من الكورسات المسجل فيها.";
                return RedirectToAction("Index", "Courses");
            }

            if (DateTime.Now > assignment.Deadline)
            {
                ModelState.AddModelError(string.Empty, "انتهى موعد تسليم هذا التاسك، تواصل مع المحاضر لو محتاج تمديد.");
                model.AssignmentTitle = assignment.Title;
                model.CourseTitle = assignment.Course?.Title;
                model.Deadline = assignment.Deadline;
                return View(model);
            }

            // 3) حفظ ملف الحل
            if (model.SolutionFile == null || model.SolutionFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.SolutionFile), "يجب إرفاق ملف الحل (PDF أو ZIP).");
                var missing = await _context.Assignments
                    .Include(x => x.Course)
                    .FirstOrDefaultAsync(x => x.Id == model.AssignmentId);

                model.AssignmentTitle = missing?.Title;
                model.CourseTitle = missing?.Course?.Title;
                model.Deadline = missing?.Deadline;

                return View(model);
            }

            var savedName = await _files.SaveAsync(model.SolutionFile, _storage.SubmissionFolder);

            // 3) سجل التسليم بحالة Pending
            var submission = new Submission
            {
                AssignmentId = model.AssignmentId,
                StudentId = student.Id,
                SolutionFilePath = savedName,
                StudentNotes = model.StudentNotes,
                SubmittedAt = DateTime.Now,
                Status = "Pending"
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفع حل التاسك بنجاح! هيتم تصحيحه من المحاضر.";
            return RedirectToAction(nameof(MySubmissions));
        }

        // ============================================================
        // 6.3 تصحيح التسليم من المحاضر — GET
        // ============================================================
        [Authorize(Roles = "Instructor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            var model = new GradeSubmissionViewModel
            {
                SubmissionId = submission.Id,
                AssignmentId = submission.AssignmentId,
                StudentName = submission.Student?.FullName,
                AssignmentTitle = submission.Assignment?.Title,
                MaxDegree = submission.Assignment?.MaxDegree ?? 100,
                SolutionFilePath = submission.SolutionFilePath,
                StudentNotes = submission.StudentNotes,
                SubmittedAt = submission.SubmittedAt,
                Grade = submission.Grade ?? 0,
                InstructorFeedback = submission.InstructorFeedback
            };

            return View(model);
        }

        // ============================================================
        // 6.4 تصحيح التسليم من المحاضر — POST
        // ============================================================
        [Authorize(Roles = "Instructor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(GradeSubmissionViewModel model)
        {
            var submission = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null)
            {
                return NotFound();
            }

            // التحقق من الدرجة ضد MaxDegree الحقيقي
            var maxDegree = submission.Assignment?.MaxDegree ?? 100;
            if (model.Grade < 0 || model.Grade > maxDegree)
            {
                ModelState.AddModelError(nameof(model.Grade),
                    $"الدرجة يجب أن تكون بين 0 و {maxDegree}.");
            }

            if (!ModelState.IsValid)
            {
                model.StudentName = submission.Student?.FullName;
                model.AssignmentTitle = submission.Assignment?.Title;
                model.MaxDegree = maxDegree;
                model.SolutionFilePath = submission.SolutionFilePath;
                model.StudentNotes = submission.StudentNotes;
                model.SubmittedAt = submission.SubmittedAt;
                model.AssignmentId = submission.AssignmentId;
                return View(model);
            }

            submission.Grade = model.Grade;
            submission.InstructorFeedback = model.InstructorFeedback;
            submission.Status = "Graded";

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رصد الدرجة والتقييم بنجاح!";
            return RedirectToAction("Details", "Assignments", new { id = submission.AssignmentId });
        }

        // ============================================================
        // 6.5 تسليماتي ودرجاتي — للطالب
        // ============================================================
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> MySubmissions()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return View(new List<Submission>());
            }

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a!.Course)
                .Where(s => s.StudentId == student.Id)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            return View(submissions);
        }

        // الطالب مسجل في الكورس إما بكورس أساسي أو بتسجيل في جدول Enrollments
        private async Task<bool> IsEnrolledAsync(int studentId, int courseId)
        {
            bool isPrimaryCourse = await _context.Students
                .AnyAsync(s => s.Id == studentId && s.CourseId == courseId);

            if (isPrimaryCourse)
            {
                return true;
            }

            return await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        }
    }
}
