using System.Diagnostics;
using EduTrack.Data;
using EduTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(AppDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        // الداتابيز ممكن تكون مش متاحة (مثلاً على الاستضافة قبل ضبط الاتصال).
        // لازم الصفحة الرئيسية تفضل تفتح وتقول للمستخدم المشكلة بدل ما ترجع 500.
        bool databaseAvailable = true;

        try
        {
            ViewBag.CoursesCount = _context.Courses.Count();
            ViewBag.InstructorsCount = _context.Instructors.Count();
            ViewBag.StudentsCount = _context.Students.Count();
            ViewBag.SubmissionsCount = _context.Submissions.Count();
        }
        catch (Exception ex)
        {
            databaseAvailable = false;

            _logger.LogError(ex, "تعذر قراءة إحصائيات الصفحة الرئيسية من قاعدة البيانات.");

            ViewBag.CoursesCount = 0;
            ViewBag.InstructorsCount = 0;
            ViewBag.StudentsCount = 0;
            ViewBag.SubmissionsCount = 0;
        }

        ViewBag.DatabaseAvailable = databaseAvailable;

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
