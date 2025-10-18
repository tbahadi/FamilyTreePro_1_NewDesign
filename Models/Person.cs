using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTreePro.Models
{
    public class Person
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "اسم الأب")]
        public string FatherName { get; set; }

        [Display(Name = "اسم الجد")]
        public string GrandFatherName { get; set; }

        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; }

        [Display(Name = "اللقب")]
        public string Nickname { get; set; }

        // خاصية محسوبة للاسم الكامل
        [NotMapped]
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

        [Display(Name = "هل هو سجل أصلي؟")]
        public bool IsOriginalRecord { get; set; } = true;

        [Display(Name = "نقطة اتصال؟")]
        public bool IsConnectionPoint { get; set; } = false;

        [Display(Name = "معرف الشجرة الأصلية")]
        public int? OriginalTreeId { get; set; } // الخاصية المضافة

        [Required]
        [Display(Name = "الجنس")]
        public string Gender { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ الوفاة")]
        [DataType(DataType.Date)]
        public DateTime? DeathDate { get; set; }

        [Display(Name = "المدينة")]
        public string City { get; set; }

        [Display(Name = "الصورة")]
        public string Photo { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        [Display(Name = "سبب الإضافة")]
        public string AdditionReason { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }

        // العلاقات
        public int FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; }

        public int? OccupationId { get; set; }
        public Occupation Occupation { get; set; }

        public int? CountryId { get; set; }
        public Country Country { get; set; }

        public int? FatherId { get; set; }
        public Person Father { get; set; }

        public int? MotherId { get; set; }
        public Person Mother { get; set; }

        public ICollection<Person> Children { get; set; }
    }
}