using System.ComponentModel.DataAnnotations;

namespace FamilyTreePro.Models
{
    public class CombinedTree
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الشجرة المدمجة مطلوب")]
        [Display(Name = "اسم الشجرة المدمجة")]
        public string Name { get; set; }

        [Display(Name = "الوصف")]
        public string Description { get; set; }

        [Display(Name = "لون الشجرة")]
        public string Color { get; set; } = "#28a745"; // لون مختلف

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        // الشجرات المدمجة
        public virtual ICollection<CombinedTreeItem> CombinedTrees { get; set; }

        public CombinedTree()
        {
            CombinedTrees = new HashSet<CombinedTreeItem>();
            CreatedDate = DateTime.Now;
        }
    }

    public class CombinedTreeItem
    {
        public int Id { get; set; }

        public int CombinedTreeId { get; set; }
        public CombinedTree CombinedTree { get; set; }

        public int FamilyTreeId { get; set; }
        public FamilyTree FamilyTree { get; set; }

        [Display(Name = "شخص الربط")]
        public int? ConnectionPersonId { get; set; }
        public Person ConnectionPerson { get; set; }

        [Display(Name = "تاريخ الإضافة")]
        public DateTime AddedDate { get; set; }

        public CombinedTreeItem()
        {
            AddedDate = DateTime.Now;
        }
    }
}