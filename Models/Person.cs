using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTreePro.Models
{
    public class Person
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "حقل الاسم الأول مطلوب")]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "حقل اسم الأب مطلوب")]
        [Display(Name = "اسم الأب")]
        public string FatherName { get; set; }

        [Required(ErrorMessage = "حقل اسم الجد مطلوب")]
        [Display(Name = "اسم الجد")]
        public string GrandFatherName { get; set; }

        [Required(ErrorMessage = "حقل اسم العائلة مطلوب")]
        [Display(Name = "اسم العائلة")]
        public string LastName { get; set; }

        [Display(Name = "اللقب")]
        public string Nickname { get; set; }

        [Display(Name = "الاسم الكامل")]
        public string FullName => $"{FirstName} {FatherName} {GrandFatherName} {LastName} {(!string.IsNullOrEmpty(Nickname) ? "(" + Nickname + ")" : "")}";

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
        public string City { get; set; }

        [Display(Name = "صورة")]
        public string Photo { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedDate { get; set; }

        // العلاقات
        [Display(Name = "المهنة")]
        public int? OccupationId { get; set; }
        public Occupation Occupation { get; set; }

        [Display(Name = "الدولة")]
        public int? CountryId { get; set; }
        public Country Country { get; set; }

        [Display(Name = "الشجرة العائلية")]
        public int FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; }

        // العلاقات العائلية
        [Display(Name = "الأب")]
        public int? FatherId { get; set; }
        [ForeignKey("FatherId")]
        public virtual Person Father { get; set; }

        [Display(Name = "الأم")]
        public int? MotherId { get; set; }
        [ForeignKey("MotherId")]
        public virtual Person Mother { get; set; }

        public virtual ICollection<Person> Children { get; set; }

        // خاصية للإشارة إلى أن هذا الشخص يمثل نقطة الربط بين الشجرات
        [Display(Name = "يمثل نقطة ربط بين الشجرات")]
        public bool IsConnectionPoint { get; set; }

        public Person()
        {
            Children = new HashSet<Person>();
            CreatedDate = DateTime.Now;
        }
    }
}