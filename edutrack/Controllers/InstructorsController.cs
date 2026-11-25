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
    [Authorize(Roles = "Admin")]
    public class InstructorsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileStorageService _files;
        private readonly FileStorageOptions _storage;

        public InstructorsController(
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
        // 3.1 قائمة المحاضرين
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var instructors = await _context.Instructors
                .Include(i => i.User)
                .Include(i => i.Courses)
                .Include(i => i.Department)
                .ToListAsync();

            return View(instructors);
        }

        // ============================================================
        // 3.2 ملف المحاضر وكورساته
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var instructor = await _context.Instructors
                .Include(i => i.User)
                .Include(i => i.Courses)
                .Include(i => i.Department)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        // ============================================================
        // 3.3 إضافة محاضر جديد — GET
        // ============================================================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.Name), "Id", "Name");
            return View();
        }

        // ============================================================
        // 3.4 إضافة محاضر جديد — POST
        // (إنشاء حساب Identity + رفع الصورة + سجل Instructor)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInstructorViewModel model)
        {
            if (model.ProfileImage != null)
            {
                var check = FileStorageService.Validate(
                    model.ProfileImage, FileStorageService.ImageExtensions, FileStorageService.ImageMaxBytes);

                if (!check.Success)
                {
                    ModelState.AddModelError(nameof(model.ProfileImage), check.Error!);
                }
            }

            // القسم لو اتبعت لازم يكون موجود فعلاً
            if (model.DepartmentId.HasValue && !await _context.Departments.AnyAsync(d => d.Id == model.DepartmentId))
            {
                ModelState.AddModelError(nameof(model.DepartmentId), "القسم المختار غير موجود.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(
                    await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            // منع تكرار الإيميل قبل محاولة الإنشاء
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email), "هذا البريد الإلكتروني مستخدم بالفعل.");
                return View(model);
            }

            // 1) إنشاء حساب الدخول
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // 2) إسناد رول المحاضر — لو فشل نحذف الحساب اللي اتعمل
            var roleResult = await _userManager.AddToRoleAsync(user, "Instructor");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // 3) رفع الصورة الشخصية (اختياري)
            string? savedImageName = null;

            if (model.ProfileImage != null)
            {
                savedImageName = await _files.SaveAsync(model.ProfileImage, _storage.InstructorImagesFolder);
            }

            // 4) حفظ سجل المحاضر
            var instructor = new Instructor
            {
                FullName = model.FullName,
                Specialization = model.Specialization,
                Bio = model.Bio,
                ImagePath = savedImageName,
                UserId = user.Id,
                DepartmentId = model.DepartmentId
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إضافة المحاضر {model.FullName} وإنشاء حسابه بنجاح!";
            return RedirectToAction(nameof(Index));
        }
    }
}
