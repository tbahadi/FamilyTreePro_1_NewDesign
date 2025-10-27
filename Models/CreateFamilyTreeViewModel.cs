using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class CreateFamilyTreeViewModel
    {
        [Required(ErrorMessage = "اسم الشجرة مطلوب")]
        [Display(Name = "اسم الشجرة")]
        [StringLength(100, ErrorMessage = "اسم الشجرة يجب ألا يتجاوز 100 حرف")]
        public string Name { get; set; }

        [Display(Name = "الوصف")]
        [StringLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز 500 حرف")]
        public string Description { get; set; }

        [Display(Name = "لون الشجرة")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "اللون يجب أن يكون بصيغة HEX (مثل: #007bff)")]
        public string Color { get; set; } = "#007bff";

        [Required(ErrorMessage = "حالة visibility الشجرة مطلوبة")]
        [Display(Name = "رؤية الشجرة")]
        public int Visibility { get; set; } = 0; // Default to Private (0)

        [Required(ErrorMessage = "الدولة مطلوبة")]
        [Display(Name = "الدولة")]
        public int CountryID { get; set; } = 1; // Default to 1
    }
}