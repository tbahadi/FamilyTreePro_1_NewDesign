using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class CreatePersonViewModel
    {
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

        [Required(ErrorMessage = "حقل الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public string Gender { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "المهنة")]
        public int? OccupationId { get; set; }

        [Display(Name = "الدولة")]
        public int? CountryId { get; set; }

        [Display(Name = "المدينة")]
        public string City { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        [Required]
        public int FamilyTreeId { get; set; }

        [Display(Name = "الأب")]
        public int? FatherId { get; set; }
    }
}