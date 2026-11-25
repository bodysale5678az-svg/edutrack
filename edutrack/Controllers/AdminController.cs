using EduTrack.Data;
using EduTrack.Models;
using EduTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Controllers
{
    /// <summary>
    /// إدارة حسابات الأدمن من داخل الموقع.
    /// متاح للأدمن بس — كل الإجراءات محمية ضد CSRF.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ============================================================
        // 1) قائمة المستخدمين + فورم الإضافة
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var model = new ManageUsersViewModel
            {
                Users = await LoadUserRowsAsync()
            };

            return View(model);
        }

        // ============================================================
        // 2) إضافة أدمن جديد
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            string email = (model.Email ?? string.Empty).Trim();

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "البريد الإلكتروني ده مستخدم بالفعل.");
            }

            if (!ModelState.IsValid)
            {
                var invalid = new ManageUsersViewModel
                {
                    Users = await LoadUserRowsAsync(),
                    NewAdmin = model
                };

                return View("Users", invalid);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = model.FullName.Trim()
            };

            var created = await _userManager.CreateAsync(user, model.Password);
            if (!created.Succeeded)
            {
                foreach (var error in created.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var failed = new ManageUsersViewModel
                {
                    Users = await LoadUserRowsAsync(),
                    NewAdmin = model
                };

                return View("Users", failed);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                // حساب بدون رول مرفوض — بنمسحه عشان مايفضلش معلق
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var roleFailed = new ManageUsersViewModel
                {
                    Users = await LoadUserRowsAsync(),
                    NewAdmin = model
                };

                return View("Users", roleFailed);
            }

            TempData["Success"] = $"تم إضافة الأدمن {email} بنجاح.";
            return RedirectToAction(nameof(Users));
        }

        // ============================================================
        // 3) حذف أدمن
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id ?? string.Empty);
            if (user == null)
            {
                TempData["Error"] = "المستخدم المطلوب غير موجود.";
                return RedirectToAction(nameof(Users));
            }

            // ممنوع تحذف نفسك — النظام لازم يفضل فيه أدمن واحد على الأقل بيقدر يدخل
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "مش مسموح تحذف حسابك انت حاليًا.";
                return RedirectToAction(nameof(Users));
            }

            // الحماية النهائية: مايفضلش صفر أدمن — بس لما اللي بنحذفه يكون أدمن فعلاً
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "مش ممكن تحذف آخر أدمن في النظام.";
                    return RedirectToAction(nameof(Users));
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            TempData["Success"] = $"تم حذف المستخدم {user.Email}.";
            return RedirectToAction(nameof(Users));
        }

        // ============================================================
        // 3.1) حذف حساب الطالب مع بياناته (Student + التسجيلات + التسليمات)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id ?? string.Empty);
            if (user == null)
            {
                TempData["Error"] = "المستخدم المطلوب غير موجود.";
                return RedirectToAction(nameof(Users));
            }

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "مش مسموح تحذف حسابك انت حاليًا.";
                return RedirectToAction(nameof(Users));
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .Include(s => s.Submissions)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null)
            {
                TempData["Error"] = "الحساب ده مش مربوط بطالب مسجّل.";
                return RedirectToAction(nameof(Users));
            }

            // التسليمات معمول عليها Restrict في الـschema — لازم تتمسح الأول
            if (student.Submissions.Count > 0)
            {
                _context.Submissions.RemoveRange(student.Submissions);
            }

            // التسجيلات معمول عليها Cascade، بس بنشيلها صريح عشان يبقى واضح
            if (student.Enrollments.Count > 0)
            {
                _context.Enrollments.RemoveRange(student.Enrollments);
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            TempData["Success"] = $"تم حذف الطالب {user.Email} مع بياناته.";
            return RedirectToAction(nameof(Users));
        }

        // ============================================================
        // 4) تغيير كلمة المرور (لنفسك)
        // ============================================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "تعذر قراءة بيانات الحساب. سجل الدخول تاني.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح.";
            return RedirectToAction(nameof(Users));
        }

        // ============================================================
        // مساعد: بناء صفوف المستخدمين مع أدوارهم
        // ============================================================
        private async Task<IReadOnlyList<UserRowViewModel>> LoadUserRowsAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = admins.Select(a => a.Id).ToHashSet();

            var rows = new List<UserRowViewModel>();

            foreach (var user in _userManager.Users.OrderBy(u => u.Email).ToList())
            {
                var roles = await _userManager.GetRolesAsync(user);
                string role = adminIds.Contains(user.Id)
                    ? "Admin"
                    : roles.FirstOrDefault() ?? "-";

                rows.Add(new UserRowViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Role = role,
                    IsCurrentUser = user.Id == currentUserId
                });
            }

            // الأدمنات فوق عشان تبقى واضحة
            return rows
                .OrderByDescending(r => r.Role == "Admin")
                .ThenBy(r => r.Email)
                .ToList();
        }
    }
}
