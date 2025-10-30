using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class EditFamilyTreeViewModel
    {
        // خصائص الإدخال (يتم التحقق منها)
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الشجرة العائلية مطلوب")]
        [Display(Name = "اسم الشجرة العائلية")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "اسم الشجرة يجب أن يكون بين {2} و {1} حرف")]
        public string Name { get; set; }

        [Display(Name = "وصف الشجرة")]
        [StringLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز {1} حرف")]
        public string Description { get; set; }

        [Required(ErrorMessage = "لون الشجرة مطلوب")]
        [Display(Name = "لون الشجرة")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "اللون يجب أن يكون بصيغة HEX صحيحة (مثل: #2c5aa0)")]
        public string Color { get; set; } = "#2c5aa0";

        [Required(ErrorMessage = "نوع الرؤية مطلوب")]
        [Display(Name = "رؤية الشجرة")]
        [Range(0, 2, ErrorMessage = "نوع الرؤية غير صالح. 0=خاص، 1=عام، 2=مشترك")]
        public int Visibility { get; set; } = 0;

        [Display(Name = "الدولة")]
        [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار دولة صحيحة")]
        public int? CountryID { get; set; } // جعلها nullable لتكون اختيارية

        [Display(Name = "المدينة")]
        [StringLength(100, ErrorMessage = "اسم المدينة يجب ألا يتجاوز {1} حرف")]
        public string City { get; set; }

        [Display(Name = "ملاحظات إضافية")]
        [StringLength(1000, ErrorMessage = "الملاحظات يجب ألا تتجاوز {1} حرف")]
        public string AdditionalNotes { get; set; }

        [Display(Name = "رمز العائلة")]
        [StringLength(50, ErrorMessage = "رمز العائلة يجب ألا يتجاوز {1} حرف")]
        public string FamilyCode { get; set; }

        [Display(Name = "تاريخ تأسيس العائلة")]
        [DataType(DataType.Date)]
        public DateTime? EstablishmentDate { get; set; }

        [Display(Name = "تفعيل إشعارات التحديثات")]
        public bool EnableUpdateNotifications { get; set; } = true;

        [Display(Name = "السماح بإضافة أفراد جدد")]
        public bool AllowNewMembers { get; set; } = true;

        // خصائص العرض فقط (لا توجد سمات تحقق)
        public DateTime? CreatedDate { get; set; }
        public int? PersonCount { get; set; }
        public string CountryName { get; set; }
        public string FounderName { get; set; }
        public int? FounderId { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string TreeSize { get; set; }
        public int? GenerationCount { get; set; }
    }
}