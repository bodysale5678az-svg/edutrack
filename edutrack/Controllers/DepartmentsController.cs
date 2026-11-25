using EduTrack.Data;
using EduTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // قائمة الأقسام
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Instructors)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(departments);
        }

        // ============================================================
        // تفاصيل قسم + محاضريه
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Instructors)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // ============================================================
        // إضافة قسم — GET
        // ============================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Department());
        }

        // ============================================================
        // إضافة قسم — POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department model)
        {
            if (await NameExistsAsync(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "يوجد قسم بنفس الاسم بالفعل.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.Departments.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم إضافة القسم ({model.Name}) بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // تعديل قسم — GET
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // ============================================================
        // تعديل قسم — POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department model)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            if (await NameExistsAsync(model.Name, id))
            {
                ModelState.AddModelError(nameof(model.Name), "يوجد قسم بنفس الاسم بالفعل.");
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View(model);
            }

            department.Name = model.Name;
            department.Description = model.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث بيانات القسم بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // حذف قسم — POST (ممنوع لو فيه محاضرين)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Instructors)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound();
            }

            if (department.Instructors.Count > 0)
            {
                TempData["Error"] =
                    $"لا يمكن حذف القسم ({department.Name}) لأنه فيه {department.Instructors.Count} محاضر. انقلهم لقسم تاني الأول.";
                return RedirectToAction(nameof(Index));
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم حذف القسم ({department.Name}).";
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> NameExistsAsync(string? name, int? exceptId = null)
        {
            var normalized = (name ?? string.Empty).Trim();

            return _context.Departments.AnyAsync(d =>
                d.Name == normalized && (exceptId == null || d.Id != exceptId));
        }
    }
}
