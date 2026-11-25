using System.Diagnostics;
using EduTrack.Data;
using EduTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.CoursesCount = _context.Courses.Count();
        ViewBag.InstructorsCount = _context.Instructors.Count();
        ViewBag.StudentsCount = _context.Students.Count();
        ViewBag.SubmissionsCount = _context.Submissions.Count();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // صفحة عربية واضحة لأي كود حالة (404 / 403 / 500...) بدل الصفحة البيضاء الفاضية
    public IActionResult Status(int code = 404)
    {
        Response.StatusCode = code;
        ViewData["StatusCode"] = code;

        ViewData["StatusTitle"] = code switch
        {
            400 => "طلب غير صحيح",
            401 => "يجب تسجيل الدخول",
            403 => "لا تملك صلاحية للوصول لهذه الصفحة",
            404 => "الصفحة أو العنصر المطلوب غير موجود",
            500 => "حدث خطأ في السيرفر",
            _ => "حدث خطأ غير متوقع"
        };

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
