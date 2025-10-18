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
        public string FirstName { get; set; }

        [Required(ErrorMessage = "اسم الأب مطلوب")]
        [Display(Name = "اسم الأب")]
        public string FatherName { get; set; }

        [Display(Name = "اسم الجد")]
        public string GrandFatherName { get; set; }

        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; }

        [Display(Name = "اللقب")]
        public string Nickname { get; set; }

        // خاصية محسوبة للاسم الكامل
        public string FullName
        {
            get
            {
                var names = new List<string>();

                if (!string.IsNullOrEmpty(FirstName))
                    names.Add(FirstName);

                if (!string.IsNullOrEmpty(FatherName))
                    names.Add(FatherName);

                if (!string.IsNullOrEmpty(GrandFatherName))
                    names.Add(GrandFatherName);

                if (!string.IsNullOrEmpty(LastName))
                    names.Add(LastName);

                return string.Join(" ", names);
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

        [Display(Name = "البلد")]
        public int? CountryId { get; set; }

        [Display(Name = "المدينة")]
        public string City { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        [Display(Name = "الصورة")]
        public string Photo { get; set; }

        [Display(Name = "سبب الإضافة")]
        public string AdditionReason { get; set; }

        [Display(Name = "هل هو سجل أصلي؟")]
        public bool IsOriginalRecord { get; set; }

        [Display(Name = "نقطة اتصال؟")]
        public bool IsConnectionPoint { get; set; } // الخاصية المضافة

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "آخر تحديث")]
        public DateTime? LastUpdated { get; set; }

        public int FamilyTreeId { get; set; }
        public int? FatherId { get; set; }
        public int? MotherId { get; set; }
    }
}