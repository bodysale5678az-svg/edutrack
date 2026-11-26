using EduTrack.Data;
using EduTrack.Models;
using EduTrack.Models.Options;
using EduTrack.Services;
using EduTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduTrack.Controllers
{
    [Authorize]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileStorageService _files;
        private readonly FileStorageOptions _storage;

        public CoursesController(
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
        // 4.1 تصفح الكورسات — بفلترة حسب الرول
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IQueryable<Course> query = _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                .Include(c => c.Students);

            if (User.IsInRole("Instructor"))
            {
                var userId = _userManager.GetUserId(User);
                var instructor = await _context.Instructors
                    .FirstOrDefaultAsync(i => i.UserId == userId);

                if (instructor == null)
                {
                    return View(new List<Course>());
                }

                query = query.Where(c => c.InstructorId == instructor.Id);
            }
            else if (User.IsInRole("Student"))
            {
                var userId = _userManager.GetUserId(User);
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return View(new List<Course>());
                }

                query = query.Where(c => c.Id == student.CourseId ||
                    c.Enrollments.Any(e => e.StudentId == student.Id));
            }

            var courses = await query.ToListAsync();
            return View(courses);
        }

        // ============================================================
        // 4.2 إضافة كورس جديد — GET
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Instructors = new SelectList(
                await _context.Instructors.ToListAsync(), "Id", "FullName");

            return View();
        }

        // ============================================================
        // 4.3 إضافة كورس جديد — POST (مع رفع صورة الغلاف)
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Create(CourseFormViewModel model)
        {
            if (model.ThumbnailFile != null)
            {
                var check = FileStorageService.Validate(
                    model.ThumbnailFile, FileStorageService.ImageExtensions, FileStorageService.ImageMaxBytes);

                if (!check.Success)
                {
                    ModelState.AddModelError(nameof(model.ThumbnailFile), check.Error!);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Instructors = new SelectList(
                    await _context.Instructors.ToListAsync(), "Id", "FullName");
                return View(model);
            }

            string? savedImageName = null;

            if (model.ThumbnailFile != null)
            {
                savedImageName = await _files.SaveAsync(model.ThumbnailFile, _storage.CourseImagesFolder);
            }

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                TotalHours = model.TotalHours,
                InstructorId = model.InstructorId,
                ThumbnailPath = savedImageName
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إضافة الكورس {model.Title} بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 4.4 تعديل الكورس — GET
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            ViewBag.Instructors = new SelectList(
                await _context.Instructors.ToListAsync(), "Id", "FullName", course.InstructorId);

            var model = new CourseFormViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                TotalHours = course.TotalHours,
                InstructorId = course.InstructorId,
                ExistingThumbnailPath = course.ThumbnailPath
            };

            return View(model);
        }

        // ============================================================
        // 4.6 حذف صورة الغلاف فقط — POST (طلب من الصفحة)
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveThumbnail(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(course.ThumbnailPath))
            {
                _files.Delete(course.ThumbnailPath, _storage.CourseImagesFolder);
                course.ThumbnailPath = null;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم حذف صورة الغلاف.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ============================================================
        // 4.5 تعديل الكورس — POST (مع تحديث الصورة)
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Edit(CourseFormViewModel model)
        {
            var course = await _context.Courses.FindAsync(model.Id);
            if (course == null)
            {
                return NotFound();
            }

            if (model.ThumbnailFile != null)
            {
                var check = FileStorageService.Validate(
                    model.ThumbnailFile, FileStorageService.ImageExtensions, FileStorageService.ImageMaxBytes);

                if (!check.Success)
                {
                    ModelState.AddModelError(nameof(model.ThumbnailFile), check.Error!);
                }
            }

            if (!ModelState.IsValid)
            {
                model.ExistingThumbnailPath = course.ThumbnailPath;
                ViewBag.Instructors = new SelectList(
                    await _context.Instructors.ToListAsync(), "Id", "FullName", model.InstructorId);
                return View(model);
            }

            // نحفظ الصورة الجديدة الأول وبعدين نمسح القديمة (مش العكس)
            if (model.ThumbnailFile != null)
            {
                string? oldPath = course.ThumbnailPath;
                course.ThumbnailPath = await _files.SaveAsync(model.ThumbnailFile, _storage.CourseImagesFolder);
                _files.Delete(oldPath, _storage.CourseImagesFolder);
            }

            course.Title = model.Title;
            course.Description = model.Description;
            course.TotalHours = model.TotalHours;
            course.InstructorId = model.InstructorId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث بيانات الكورس بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // حذف الكورس — POST (ممنوع لو فيه طلاب أو تاسكات)
        // ============================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Students)
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            int studentsCount = course.Students.Count + course.Enrollments.Count;

            if (studentsCount > 0 || course.Assignments.Count > 0)
            {
                TempData["Error"] =
                    $"لا يمكن حذف الكورس ({course.Title}) لأنه فيه {studentsCount} طالب و{course.Assignments.Count} تاسك. احذفهم الأول أو انقلهم لكورس تاني.";

                return RedirectToAction(nameof(Index));
            }

            _files.Delete(course.ThumbnailPath, _storage.CourseImagesFolder);

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم حذف الكورس ({course.Title}) نهائيًا.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // 4.6 لوحة الكورس والتاسكات — GET
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Students)
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Submissions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }
    }
}
