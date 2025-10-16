using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الدولة مطلوب")]
        [Display(Name = "اسم الدولة")]
        public string Name { get; set; }

        [Display(Name = "الرمز")]
        public string Code { get; set; } = string.Empty; // قيمة افتراضية

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedDate { get; set; }

        public virtual ICollection<Person> Persons { get; set; }

        public Country()
        {
            Persons = new HashSet<Person>();
            CreatedDate = DateTime.Now;
            Code = string.Empty; // تأكيد القيمة الافتراضية
        }
    }
}