using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class Occupation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المهنة مطلوب")]
        [Display(Name = "اسم المهنة")]
        public string Name { get; set; }

        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty; // قيمة افتراضية

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Person> Persons { get; set; }

        public Occupation()
        {
            Persons = new HashSet<Person>();
            CreatedDate = DateTime.Now;
            Description = string.Empty; // تأكيد القيمة الافتراضية
        }
    }
}