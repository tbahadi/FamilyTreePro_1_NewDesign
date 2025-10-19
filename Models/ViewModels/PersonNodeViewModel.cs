using FamilyTreePro.Models;

namespace FamilyTreePro.ViewModels
{
    public class PersonNodeViewModel
    {
        public Person Person { get; set; }
        public List<Person> AllPersons { get; set; }
        public int Level { get; set; }
    }
}