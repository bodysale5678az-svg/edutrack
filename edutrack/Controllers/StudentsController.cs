using EduTrack.Data;
using EduTrack.Models;
using EduTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // قائمة الطلاب المسجلين
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.User)
                .Include(s => s.Submissions)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return View(students);
        }

        // ============================================================
        // ملف الطالب — عرض شامل
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.User)
                .Include(s => s.Submissions)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            var model = new StudentDetailsViewModel
            {
                Id = student.Id,
                FullName = student.FullName,
                PhoneNumber = student.PhoneNumber,
                University = student.University,
                Email = student.User?.Email,
                CourseTitle = student.Course?.Title,
                SubmissionCount = student.Submissions.Count,
                ExtraCourses = student.Enrollments
                    .Select(e => e.Course?.Title)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t!)
                    .ToList()
            };

            return View(model);
        }

        // ============================================================
        // تعديل بيانات الطالب — GET
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.Courses = new SelectList(
                await _context.Courses.OrderBy(c => c.Title).ToListAsync(),
                "Id", "Title", student.CourseId);

            var model = new EditStudentViewModel
            {
                Id = student.Id,
                FullName = student.FullName,
                PhoneNumber = student.PhoneNumber,
                University = student.University,
                CourseId = student.CourseId,
                Email = student.User?.Email
            };

            return View(model);
        }

        // ============================================================
        // تعديل بيانات الطالب — POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditStudentViewModel model)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == model.Id);

            if (student == null)
            {
                return NotFound();
            }

            if (!await _context.Courses.AnyAsync(c => c.Id == model.CourseId))
            {
                ModelState.AddModelError(nameof(model.CourseId), "الكورس المختار غير موجود.");
            }

            if (!ModelState.IsValid)
            {
                model.Email = student.User?.Email;
                ViewBag.Courses = new SelectList(
                    await _context.Courses.OrderBy(c => c.Title).ToListAsync(),
                    "Id", "Title", model.CourseId);

                return View(model);
            }

            // لو الأدمن غيّر الكورس الأساسي، بنشيل أي تسجيل إضافي مكرر في نفس الكورس
            if (student.CourseId != model.CourseId)
            {
                var duplicates = await _context.Enrollments
                    .Where(e => e.StudentId == student.Id && e.CourseId == model.CourseId)
                    .ToListAsync();

                if (duplicates.Count > 0)
                {
                    _context.Enrollments.RemoveRange(duplicates);
                }
            }

            student.FullName = model.FullName;
            student.PhoneNumber = model.PhoneNumber;
            student.University = model.University;
            student.CourseId = model.CourseId;

            // الاسم بيتزامن مع حساب الدخول
            if (student.User != null)
            {
                student.User.FullName = model.FullName;
                student.User.PhoneNumber = model.PhoneNumber;

                var update = await _userManager.UpdateAsync(student.User);
                if (!update.Succeeded)
                {
                    foreach (var error in update.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    model.Email = student.User.Email;
                    ViewBag.Courses = new SelectList(
                        await _context.Courses.OrderBy(c => c.Title).ToListAsync(),
                        "Id", "Title", model.CourseId);

                    return View(model);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحديث بيانات الطالب ({student.FullName}) بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // تحويل الطالب لكورس تاني — POST (سريع من صفحة التسجيلات)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCourse(int id, int courseId)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            if (!await _context.Courses.AnyAsync(c => c.Id == courseId))
            {
                TempData["Error"] = "الكورس المختار غير موجود.";
                return RedirectToAction(nameof(Index));
            }

            // منع التكرار لو كان مسجل بالفعل في نفس الكورس في Enrollments
            var duplicates = await _context.Enrollments
                .Where(e => e.StudentId == student.Id && e.CourseId == courseId)
                .ToListAsync();

            if (duplicates.Count > 0)
            {
                _context.Enrollments.RemoveRange(duplicates);
            }

            student.CourseId = courseId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم تحويل الطالب ({student.FullName}) للكورس الجديد.";
            return RedirectToAction(nameof(Index));
        }
    }
}
