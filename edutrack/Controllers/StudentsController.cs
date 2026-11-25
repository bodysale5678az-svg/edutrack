using EduTrack.Data;
using EduTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 3.5 قائمة الطلاب المسجلين (للأدمن)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Course)
                .Include(s => s.User)
                .Include(s => s.Submissions)
                .ToListAsync();

            return View(students);
        }
    }
}
