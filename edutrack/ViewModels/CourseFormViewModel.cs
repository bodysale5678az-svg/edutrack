using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EduTrack.ViewModels
{
    // يستخدم في شاشتي Courses/Create و Courses/Edit لرفع أو تحديث صورة غلاف الكورس
    public class CourseFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الكورس مطلوب")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "اسم الكورس يجب أن يكون بين 3 و 120 حرف")]
        [Display(Name = "اسم الكورس")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "وصف الكورس مطلوب")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "الوصف يجب أن يكون بين 10 و 2000 حرف")]
        [Display(Name = "وصف الكورس")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, 500, ErrorMessage = "عدد الساعات يجب أن يكون بين 1 و 500")]
        [Display(Name = "عدد الساعات التدريبية")]
        public int TotalHours { get; set; }

        [Required(ErrorMessage = "يرجى اختيار المحاضر")]
        [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار المحاضر")]
        [Display(Name = "المحاضر المسؤول")]
        public int InstructorId { get; set; }

        [Display(Name = "صورة غلاف الكورس")]
        public IFormFile? ThumbnailFile { get; set; }

        public string? ExistingThumbnailPath { get; set; }
    }
}
