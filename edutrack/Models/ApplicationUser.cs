using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.Models
{
    // يمثل حساب المستخدم في النظام (AspNetUsers) ويستخدم في جميع عمليات الدخول
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;
    }
}
