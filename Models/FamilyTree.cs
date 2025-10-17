using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTreePro.Models
{
    public class FamilyTree
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الشجرة مطلوب")]
        [Display(Name = "اسم الشجرة")]
        [StringLength(100, ErrorMessage = "اسم الشجرة يجب ألا يتجاوز 100 حرف")]
        public string Name { get; set; }

        [Display(Name = "الوصف")]
        [StringLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز 500 حرف")]
        public string Description { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "لون الشجرة")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "اللون يجب أن يكون بصيغة HEX (مثل: #007bff)")]
        public string Color { get; set; } = "#007bff";

        [Display(Name = "المستخدم")]
        public int UserId { get; set; }
        public User User { get; set; }

        // العلاقة مع الأشخاص في هذه الشجرة
        public virtual ICollection<Person> Persons { get; set; }

        // العلاقات مع الشجرات الأخرى
        [Display(Name = "الشجرة الأم")]
        public int? ParentTreeId { get; set; }

        [ForeignKey("ParentTreeId")]
        public virtual FamilyTree ParentTree { get; set; }

        public virtual ICollection<FamilyTree> ChildTrees { get; set; }

        // شخص الربط مع الشجرة الأم
        [Display(Name = "شخص الربط مع الشجرة الأم")]
        public int? ConnectionPersonId { get; set; }

        [ForeignKey("ConnectionPersonId")]
        public virtual Person ConnectionPerson { get; set; }

        // خاصية جديدة: هل البيانات مستقلة؟
        [Display(Name = "البيانات مستقلة")]
        public bool IsDataIndependent { get; set; }

        public FamilyTree()
        {
            Persons = new HashSet<Person>();
            ChildTrees = new HashSet<FamilyTree>();
            CreatedDate = DateTime.Now;
            Description = string.Empty;
            IsDataIndependent = true;
        }
    }
}