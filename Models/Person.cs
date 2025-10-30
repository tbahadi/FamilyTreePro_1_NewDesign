using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTreePro.Models
{
    public class Person
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; }

        [Display(Name = "اسم الأب")]
        public string? FatherName { get; set; } // nullable للمؤسس

        [Display(Name = "اسم الجد")]
        public string? GrandFatherName { get; set; } // nullable للمؤسس

        [Display(Name = "اسم العائلة")]
        public string? LastName { get; set; } // nullable للمؤسس

        [Display(Name = "اللقب")]
        public string Nickname { get; set; } = "لا يوجد";

        // خاصية محسوبة للاسم الكامل مع مراعاة المؤسس
        [NotMapped]
        [Display(Name = "الاسم الكامل")]
        public string FullName
        {
            get
            {
                var names = new List<string>();

                if (!string.IsNullOrEmpty(FirstName))
                    names.Add(FirstName.Trim());

                if (IsFounder)
                {
                    // للمؤسس: نعرض فقط الحقول التي تحتوي على بيانات
                    if (!string.IsNullOrEmpty(FatherName))
                        names.Add(FatherName.Trim());
                    if (!string.IsNullOrEmpty(GrandFatherName))
                        names.Add(GrandFatherName.Trim());
                    if (!string.IsNullOrEmpty(LastName))
                        names.Add(LastName.Trim());
                }
                else
                {
                    // لغير المؤسس: نستخدم القيم أو "غير معروف"
                    names.Add(!string.IsNullOrEmpty(FatherName) ? FatherName.Trim() : "غير معروف");
                    names.Add(!string.IsNullOrEmpty(GrandFatherName) ? GrandFatherName.Trim() : "غير معروف");
                    names.Add(!string.IsNullOrEmpty(LastName) ? LastName.Trim() : "غير معروف");
                }

                return names.Count > 0 ? string.Join(" ", names) : "غير معروف";
            }
        }

        [Display(Name = "سجل أصلي")]
        public bool IsOriginalRecord { get; set; } = true;

        [Display(Name = "نقطة اتصال")]
        public bool IsConnectionPoint { get; set; } = false;

        [Display(Name = "مؤسس الشجرة")]
        public bool IsFounder { get; set; } = false;

        [Display(Name = "معرف الشجرة الأصلية")]
        public int? OriginalTreeId { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public string Gender { get; set; } = "Male";

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ الوفاة")]
        [DataType(DataType.Date)]
        public DateTime? DeathDate { get; set; }

        [Display(Name = "المدينة")]
        public string City { get; set; } = "غير محدد";

        [Display(Name = "الصورة")]
        public string Photo { get; set; } = "";

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; } = "لا يوجد";

        [Display(Name = "سبب الإضافة")]
        public string AdditionReason { get; set; } = "";

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }

        // العلاقات
        public int FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; }

        public int? OccupationId { get; set; }
        public Occupation? Occupation { get; set; }

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public int? FatherId { get; set; }
        public Person? Father { get; set; }

        public int? MotherId { get; set; }
        public Person? Mother { get; set; }

        public ICollection<Person> Children { get; set; } = new List<Person>();

        // منشئ للمعالجة الآمنة
        public Person()
        {
            // نضمن أن الحقول المطلوبة لها قيم افتراضية
            FirstName ??= "";
            Gender ??= "Male";
        }
    }
}