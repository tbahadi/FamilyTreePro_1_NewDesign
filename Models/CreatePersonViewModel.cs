using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.ViewModels
{
    public class CreatePersonViewModel
    {
        [Required(ErrorMessage = "حقل الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        [StringLength(50, ErrorMessage = "الاسم الأول يجب ألا يتجاوز 50 حرفاً")]
        public string FirstName { get; set; }

        // ⭐⭐ تغيير هذه الحقول من Required إلى غير مطلوبة ⭐⭐
        [Display(Name = "اسم الأب")]
        [StringLength(50, ErrorMessage = "اسم الأب يجب ألا يتجاوز 50 حرفاً")]
        public string FatherName { get; set; }

        [Display(Name = "اسم الجد")]
        [StringLength(50, ErrorMessage = "اسم الجد يجب ألا يتجاوز 50 حرفاً")]
        public string GrandFatherName { get; set; }

        [Display(Name = "اسم العائلة")]
        [StringLength(50, ErrorMessage = "اسم العائلة يجب ألا يتجاوز 50 حرفاً")]
        public string LastName { get; set; }

        [Display(Name = "اللقب")]
        [StringLength(50, ErrorMessage = "اللقب يجب ألا يتجاوز 50 حرفاً")]
        public string Nickname { get; set; }

        // خاصية محسوبة للاسم الكامل
        public string FullName
        {
            get
            {
                var names = new List<string>();
                if (!string.IsNullOrEmpty(FirstName)) names.Add(FirstName);
                if (!string.IsNullOrEmpty(FatherName)) names.Add(FatherName);
                if (!string.IsNullOrEmpty(GrandFatherName)) names.Add(GrandFatherName);
                if (!string.IsNullOrEmpty(LastName)) names.Add(LastName);
                return string.Join(" ", names);
            }
        }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ الوفاة")]
        [DataType(DataType.Date)]
        public DateTime? DeathDate { get; set; }

        [Required(ErrorMessage = "حقل الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public string Gender { get; set; }

        [Display(Name = "المدينة")]
        [StringLength(100, ErrorMessage = "اسم المدينة يجب ألا يتجاوز 100 حرف")]
        public string City { get; set; }

        [Display(Name = "صورة")]
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

        // أضف هذه الخاصية الجديدة هنا
        [Display(Name = "مؤسس الشجرة")]
        public bool IsFounder { get; set; }

        // ⭐⭐ إضافة خاصية AddType ⭐⭐
        [Display(Name = "نوع الإضافة")]
        public string AddType { get; set; }

        public CreatePersonViewModel()
        {
            // قيم افتراضية
            City = string.Empty;
            Photo = string.Empty;
            Notes = string.Empty;
            Nickname = string.Empty;
            AdditionReason = string.Empty;
            IsFounder = false;
            FatherName = string.Empty;
            GrandFatherName = string.Empty;
            LastName = string.Empty;
        }
    }
}