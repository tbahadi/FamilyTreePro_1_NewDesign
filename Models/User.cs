using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyTreePro.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [Display(Name = "كلمة المرور")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        public string Email { get; set; }

        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }

        [Display(Name = "تاريخ التسجيل")]
        public DateTime CreatedDate { get; set; }

        // الحقول الجديدة - نضيفها فقط
        [Display(Name = "مفعل")]
        public bool IsActive { get; set; }

        [Display(Name = "مدير النظام")]
        public bool IsAdmin { get; set; }

        // العلاقات
        public virtual ICollection<FamilyTree> FamilyTrees { get; set; }

        public User()
        {
            FamilyTrees = new HashSet<FamilyTree>();
            CreatedDate = DateTime.Now;
            // قيم افتراضية بسيطة
            IsActive = true;
            IsAdmin = false;
        }
    }
}