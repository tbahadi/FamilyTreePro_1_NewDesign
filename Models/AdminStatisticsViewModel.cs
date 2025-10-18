using System.Collections.Generic;
using FamilyTreePro.Models;

namespace FamilyTreePro.Models
{
    public class AdminStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int TotalTrees { get; set; }
        public int TotalPersons { get; set; }
        public int RecentRegistrations { get; set; }
        public List<User> LatestUsers { get; set; }
    }
}