using Microsoft.EntityFrameworkCore;

namespace FamilyTreePro.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FamilyTree> FamilyTrees { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Occupation> Occupations { get; set; }
        public DbSet<Country> Countries { get; set; }

        public DbSet<CombinedTree> CombinedTrees { get; set; }
        public DbSet<CombinedTreeItem> CombinedTreeItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // تكوين العلاقات فقط، لا تجعل الحقول مطلوبة هنا
            modelBuilder.Entity<FamilyTree>()
                .HasOne(ft => ft.User)
                .WithMany(u => u.FamilyTrees)
                .HasForeignKey(ft => ft.UserId);

            modelBuilder.Entity<FamilyTree>()
                .HasOne(ft => ft.ParentTree)
                .WithMany(ft => ft.ChildTrees)
                .HasForeignKey(ft => ft.ParentTreeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyTree>()
                .HasOne(ft => ft.ConnectionPerson)
                .WithMany()
                .HasForeignKey(ft => ft.ConnectionPersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.FamilyTree)
                .WithMany(ft => ft.Persons)
                .HasForeignKey(p => p.FamilyTreeId);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Occupation)
                .WithMany(o => o.Persons)
                .HasForeignKey(p => p.OccupationId);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Country)
                .WithMany(c => c.Persons)
                .HasForeignKey(p => p.CountryId);

            // العلاقات العائلية
            modelBuilder.Entity<Person>()
                .HasOne(p => p.Father)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.FatherId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Mother)
                .WithMany()
                .HasForeignKey(p => p.MotherId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<CombinedTreeItem>()
                .HasOne(cti => cti.FamilyTree)
                .WithMany()
                .HasForeignKey(cti => cti.FamilyTreeId);

            modelBuilder.Entity<CombinedTreeItem>()
                .HasOne(cti => cti.ConnectionPerson)
                .WithMany()
                .HasForeignKey(cti => cti.ConnectionPersonId);

            base.OnModelCreating(modelBuilder);
        }
    }
}