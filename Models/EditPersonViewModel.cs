using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.ViewModels
{
    public class EditPersonViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        [StringLength(50, ErrorMessage = "الاسم الأول يجب ألا يتجاوز 50 حرفاً")]
        public string FirstName { get; set; }

        [Display(Name = "اسم الأب")]
        [StringLength(50, ErrorMessage = "اسم الأب يجب ألا يتجاوز 50 حرفاً")]
        public string FatherName { get; set; } // إزالة Required

        [Display(Name = "اسم الجد")]
        [StringLength(50, ErrorMessage = "اسم الجد يجب ألا يتجاوز 50 حرفاً")]
        public string GrandFatherName { get; set; } // إزالة Required

        [Display(Name = "اسم العائلة")]
        [StringLength(50, ErrorMessage = "اسم العائلة يجب ألا يتجاوز 50 حرفاً")]
        public string LastName { get; set; } // إزالة Required

        [Display(Name = "اللقب")]
        [StringLength(50, ErrorMessage = "اللقب يجب ألا يتجاوز 50 حرفاً")]
        public string Nickname { get; set; }

        // خاصية محسوبة للاسم الكامل مع مراعاة المؤسس
        public string FullName
        {
            get
            {
                var names = new List<string>();
                if (!string.IsNullOrEmpty(FirstName)) names.Add(FirstName);

                // معالجة خاصة للمؤسس
                if (IsFounder)
                {
                    // للمؤسس: نعرض فقط الحقول التي تحتوي على بيانات
                    if (!string.IsNullOrEmpty(FatherName)) names.Add(FatherName);
                    if (!string.IsNullOrEmpty(GrandFatherName)) names.Add(GrandFatherName);
                    if (!string.IsNullOrEmpty(LastName)) names.Add(LastName);
                }
                else
                {
                    // لغير المؤسس: نعرض جميع الحقول مع قيم افتراضية
                    names.Add(!string.IsNullOrEmpty(FatherName) ? FatherName : "غير معروف");
                    names.Add(!string.IsNullOrEmpty(GrandFatherName) ? GrandFatherName : "غير معروف");
                    names.Add(!string.IsNullOrEmpty(LastName) ? LastName : "غير معروف");
                }

                return names.Any() ? string.Join(" ", names) : "غير معروف";
            }
        }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public string Gender { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ الوفاة")]
        [DataType(DataType.Date)]
        public DateTime? DeathDate { get; set; }

        [Display(Name = "المهنة")]
        public int? OccupationId { get; set; }

        [Display(Name = "الدولة")]
        public int? CountryId { get; set; }

        [Display(Name = "المدينة")]
        [StringLength(100, ErrorMessage = "اسم المدينة يجب ألا يتجاوز 100 حرف")]
        public string City { get; set; }

        [Display(Name = "ملاحظات")]
        [StringLength(1000, ErrorMessage = "الملاحظات يجب ألا تتجاوز 1000 حرف")]
        public string Notes { get; set; }

        [Display(Name = "الصورة")]
        [StringLength(500, ErrorMessage = "رابط الصورة يجب ألا يتجاوز 500 حرف")]
        public string Photo { get; set; }

        [Display(Name = "سبب الإضافة")]
        [StringLength(500, ErrorMessage = "سبب الإضافة يجب ألا يتجاوز 500 حرف")]
        public string AdditionReason { get; set; }

        [Display(Name = "هل هو سجل أصلي؟")]
        public bool IsOriginalRecord { get; set; }

        [Display(Name = "نقطة اتصال؟")]
        public bool IsConnectionPoint { get; set; }

        [Display(Name = "مؤسس الشجرة؟")]
        public bool IsFounder { get; set; } // إضافة حقل المؤسس

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "آخر تحديث")]
        public DateTime? LastUpdated { get; set; }

        [Required(ErrorMessage = "معرف الشجرة مطلوب")]
        public int FamilyTreeId { get; set; }

        [Display(Name = "الأب")]
        public int? FatherId { get; set; }

        [Display(Name = "الأم")]
        public int? MotherId { get; set; }

        // خصائص للعرض فقط
        [Display(Name = "اسم الأب")]
        public string FatherNameDisplay { get; set; }

        [Display(Name = "اسم الأم")]
        public string MotherNameDisplay { get; set; }

        [Display(Name = "المهنة")]
        public string OccupationName { get; set; }

        [Display(Name = "الدولة")]
        public string CountryName { get; set; }

        [Display(Name = "الشجرة العائلية")]
        public string FamilyTreeName { get; set; }

        public EditPersonViewModel()
        {
            // قيم افتراضية
            City = string.Empty;
            Photo = string.Empty;
            Notes = string.Empty;
            Nickname = string.Empty;
            AdditionReason = string.Empty;
            FatherName = string.Empty;
            GrandFatherName = string.Empty;
            LastName = string.Empty;
            FatherNameDisplay = string.Empty;
            MotherNameDisplay = string.Empty;
            OccupationName = string.Empty;
            CountryName = string.Empty;
            FamilyTreeName = string.Empty;
        }
    }
}