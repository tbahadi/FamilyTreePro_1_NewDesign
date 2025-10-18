using System;
using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.ViewModels
{
    public class CreatePersonViewModel
    {
        [Required(ErrorMessage = "حقل الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        [StringLength(50, ErrorMessage = "الاسم الأول يجب ألا يتجاوز 50 حرفاً")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "حقل اسم الأب مطلوب")]
        [Display(Name = "اسم الأب")]
        [StringLength(50, ErrorMessage = "اسم الأب يجب ألا يتجاوز 50 حرفاً")]
        public string FatherName { get; set; }

        [Required(ErrorMessage = "حقل اسم الجد مطلوب")]
        [Display(Name = "اسم الجد")]
        [StringLength(50, ErrorMessage = "اسم الجد يجب ألا يتجاوز 50 حرفاً")]
        public string GrandFatherName { get; set; }

        [Required(ErrorMessage = "حقل اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        [StringLength(50, ErrorMessage = "اسم العائلة يجب ألا يتجاوز 50 حرفاً")]
        public string LastName { get; set; }

        [Display(Name = "اللقب")]
        [StringLength(50, ErrorMessage = "اللقب يجب ألا يتجاوز 50 حرفاً")]
        public string Nickname { get; set; }

        [Display(Name = "الاسم الكامل")]
        public string FullName => $"{FirstName} {FatherName} {GrandFatherName} {LastName} {(!string.IsNullOrEmpty(Nickname) ? "(" + Nickname + ")" : "")}".Trim();

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ الوفاة")]
        [DataType(DataType.Date)]
        public DateTime? DeathDate { get; set; }

        [Required(ErrorMessage = "حقل الجنس مطلوب")]
        [Display(Name = "الجنس")]
        [RegularExpression("^(Male|Female)$", ErrorMessage = "الجنس يجب أن يكون إما ذكر أو أنثى")]
        public string Gender { get; set; }

        [Display(Name = "المدينة")]
        [StringLength(100, ErrorMessage = "اسم المدينة يجب ألا يتجاوز 100 حرف")]
        public string City { get; set; }

        [Display(Name = "صورة")]
        [Url(ErrorMessage = "يرجى إدخال رابط صحيح للصورة")]
        [StringLength(500, ErrorMessage = "رابط الصورة يجب ألا يتجاوز 500 حرف")]
        public string Photo { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(1000, ErrorMessage = "الملاحظات يجب ألا تتجاوز 1000 حرف")]
        public string Notes { get; set; }

        [Display(Name = "المهنة")]
        public int? OccupationId { get; set; }

        [Display(Name = "الدولة")]
        public int? CountryId { get; set; }

        [Required(ErrorMessage = "حقل الشجرة العائلية مطلوب")]
        [Display(Name = "الشجرة العائلية")]
        public int FamilyTreeId { get; set; }

        [Display(Name = "الأب")]
        public int? FatherId { get; set; }

        [Display(Name = "الأم")]
        public int? MotherId { get; set; }

        [Display(Name = "سبب الإضافة")]
        [StringLength(500, ErrorMessage = "سبب الإضافة يجب ألا يتجاوز 500 حرف")]
        public string AdditionReason { get; set; }

        public CreatePersonViewModel()
        {
            Photo = string.Empty;
            City = string.Empty;
            Notes = string.Empty;
            Nickname = string.Empty;
            AdditionReason = string.Empty;
        }
    }
}