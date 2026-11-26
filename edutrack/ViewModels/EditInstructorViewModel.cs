using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في شاشة Instructors/Edit لتعديل بيانات محاضر موجود وتغيير صورته
    public class EditInstructorViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المحاضر مطلوب")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        [Display(Name = "اسم المحاضر بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "التخصص مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "التخصص يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "التخصص التقني")]
        public string Specialization { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "نبذة مختصرة")]
        public string? Bio { get; set; }

        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [Display(Name = "الصورة الشخصية للمحاضر")]
        public IFormFile? ProfileImage { get; set; }

        // اسم الصورة الحالية — بيتعرض في الفورم
        public string? ExistingImagePath { get; set; }
    }
}
