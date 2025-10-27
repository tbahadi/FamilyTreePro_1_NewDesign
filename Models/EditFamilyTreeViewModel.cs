using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class EditFamilyTreeViewModel
    {
        // خصائص الإدخال (يتم التحقق منها)
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الشجرة العائلية مطلوب")]
        [Display(Name = "اسم الشجرة العائلية")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "اسم الشجرة يجب أن يكون بين {2} و {1} حرف")]
        public string Name { get; set; }

        [Display(Name = "وصف الشجرة")]
        [StringLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز {1} حرف")]
        public string Description { get; set; }

        [Required(ErrorMessage = "لون الشجرة مطلوب")]
        [Display(Name = "لون الشجرة")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "اللون يجب أن يكون بصيغة HEX صحيحة (مثل: #2c5aa0)")]
        public string Color { get; set; } = "#2c5aa0";

        [Required(ErrorMessage = "نوع الرؤية مطلوب")]
        [Display(Name = "رؤية الشجرة")]
        [Range(0, 1, ErrorMessage = "نوع الرؤية غير صالح")]
        public int Visibility { get; set; }

        [Required(ErrorMessage = "الدولة مطلوبة")]
        [Display(Name = "الدولة")]
        [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار دولة صحيحة")]
        public int CountryID { get; set; }

        // خصائص العرض فقط (لا توجد سمات تحقق)
        public DateTime? CreatedDate { get; set; }
        public int? PersonCount { get; set; }
        public string CountryName { get; set; }
    }
}