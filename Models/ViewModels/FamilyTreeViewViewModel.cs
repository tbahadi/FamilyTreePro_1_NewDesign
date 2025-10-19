using System.Collections.Generic;
using FamilyTreePro.Models;

namespace FamilyTreePro.ViewModels
{
    public class FamilyTreeViewViewModel
    {
        public int FamilyTreeId { get; set; }
        public string FamilyTreeName { get; set; }
        public List<Person> RootPersons { get; set; }
        public List<Person> AllPersons { get; set; }

        // يمكن إضافة خصائص إضافية حسب الحاجة
        public int TotalPersons => AllPersons?.Count ?? 0;
        public int MaleCount => AllPersons?.Count(p => p.Gender == "Male") ?? 0;
        public int FemaleCount => AllPersons?.Count(p => p.Gender == "Female") ?? 0;
        public int RootsCount => RootPersons?.Count ?? 0;
    }
}