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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ============================================================
        // 1. تهيئة النظام لأول مرة (إنشاء الـ 3 Roles + حساب Admin افتراضي)
        //    محمي: يشتغل فقط لو مفيش أي مستخدم في النظام
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> SetupSystem()
        {
            // حماية: بعد ما النظام يتهيأ، الباب يتقفل نهائياً
            if (_userManager.Users.Any())
            {
                TempData["Error"] = "النظام مُهيأ بالفعل. يرجى تسجيل الدخول.";
                return RedirectToAction("Login");
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await _roleManager.RoleExistsAsync("Instructor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Instructor"));
            }

            if (!await _roleManager.RoleExistsAsync("Student"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Student"));
            }

            const string adminEmail = "admin@edutrack.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "مدير الأكاديمية"
                };

                var result = await _userManager.CreateAsync(adminUser, "123456");

                if (!result.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Home");
                }

                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }

            TempData["Success"] = "تم تهيئة النظام بنجاح! ادخل بـ admin@edutrack.com / 123456";
            return RedirectToAction("Login");
        }

        // ============================================================
        // 2. تسجيل طالب جديد
        // ============================================================
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Courses = new SelectList(_context.Courses.ToList(), "Id", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Courses = new SelectList(_context.Courses.ToList(), "Id", "Title");
                return View(model);
            }

            // تحقق إن الكورس المختار موجود فعلاً (منع إرسال CourseId وهمي)
            if (!await _context.Courses.AnyAsync(c => c.Id == model.CourseId))
            {
                ModelState.AddModelError(nameof(model.CourseId), "الكورس المختار غير موجود.");
                ViewBag.Courses = new SelectList(_context.Courses.ToList(), "Id", "Title");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                ViewBag.Courses = new SelectList(_context.Courses.ToList(), "Id", "Title");
                return View(model);
            }

            // إسناد رول الطالب — لو فشل نمسح الحساب (منع حساب بدون رول)
            var roleResult = await _userManager.AddToRoleAsync(user, "Student");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                ViewBag.Courses = new SelectList(_context.Courses.ToList(), "Id", "Title");
                return View(model);
            }

            var student = new Student
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                University = model.University,
                CourseId = model.CourseId,
                UserId = user.Id
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Courses");
        }

        // ============================================================
        // 3. تسجيل الدخول
        // ============================================================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"أهلاً بك يا {user.FullName}!";
                    return RedirectToAction("Index", "Courses");
                }
            }

            ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        // ============================================================
        // 4. تسجيل الخروج (Logout) وشاشة عدم الصلاحية (AccessDenied)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ============================================================
        // 5. بياناتي — الطالب يعدّل بياناته الشخصية
        // ============================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // بيانات الطالب المرتبطة بالحساب (لو موجود)
            var student = await _context.Students
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            var model = new StudentProfileViewModel
            {
                FullName = student?.FullName ?? user.FullName,
                PhoneNumber = student?.PhoneNumber ?? user.PhoneNumber ?? string.Empty,
                University = student?.University,
                Email = user.Email,
                CourseTitle = student?.Course?.Title
            };

            ViewBag.HasStudentRecord = student != null;
            ViewBag.IsStudent = User.IsInRole("Student");

            return View(model);
        }

        // ============================================================
        // 6. بياناتي — حفظ التعديلات
        // ============================================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(StudentProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var student = await _context.Students
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                model.CourseTitle = student?.Course?.Title;
                ViewBag.HasStudentRecord = student != null;
                ViewBag.IsStudent = User.IsInRole("Student");

                return View(model);
            }

            // 1) سجل الطالب (لو موجود)
            if (student != null)
            {
                student.FullName = model.FullName;
                student.PhoneNumber = model.PhoneNumber;
                student.University = model.University;
            }

            // 2) حساب الدخول — الاسم بيتزامن عشان يظهر صح في كل الصفحات
            user.FullName = model.FullName;

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                user.PhoneNumber = model.PhoneNumber;
            }

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var error in update.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.Email = user.Email;
                model.CourseTitle = student?.Course?.Title;
                ViewBag.HasStudentRecord = student != null;
                ViewBag.IsStudent = User.IsInRole("Student");

                return View(model);
            }

            if (student != null)
            {
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "تم تحديث بياناتك بنجاح.";
            return RedirectToAction(nameof(Profile));
        }

        // ============================================================
        // 7. تغيير كلمة المرور (لأي مستخدم مسجل)
        // ============================================================
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangeOwnPasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangeOwnPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    // رسائل Identity بالإنجليزي — بنترجم أشهرها
                    string message = error.Code switch
                    {
                        "PasswordMismatch" => "كلمة المرور الحالية غير صحيحة.",
                        "PasswordTooShort" => "كلمة المرور الجديدة قصيرة جدًا.",
                        _ => error.Description
                    };

                    ModelState.AddModelError(string.Empty, message);
                }

                return View(model);
            }

            // نسجل دخول بالكلمة الجديدة عشان مايتطردش من الموقع
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
