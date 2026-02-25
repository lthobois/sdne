using System.Data.Entity;
using WebFormsNet48Basics.Models;

namespace WebFormsNet48Basics.Data
{
    public class AdventureWorksContext : DbContext
    {
        public AdventureWorksContext()
            : base("name=AdventureWorks2014Connection")
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .ToTable("Employee", "HumanResources")
                .HasKey(e => e.BusinessEntityID);

            modelBuilder.Entity<Employee>().Property(e => e.BusinessEntityID).HasColumnName("BusinessEntityID");
            modelBuilder.Entity<Employee>().Property(e => e.NationalIDNumber).HasColumnName("NationalIDNumber").IsRequired().HasMaxLength(15);
            modelBuilder.Entity<Employee>().Property(e => e.LoginID).HasColumnName("LoginID").IsRequired().HasMaxLength(256);
            modelBuilder.Entity<Employee>().Property(e => e.JobTitle).HasColumnName("JobTitle").IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Employee>().Property(e => e.BirthDate).HasColumnName("BirthDate");
            modelBuilder.Entity<Employee>().Property(e => e.MaritalStatus).HasColumnName("MaritalStatus").IsRequired().HasMaxLength(1).IsFixedLength();
            modelBuilder.Entity<Employee>().Property(e => e.Gender).HasColumnName("Gender").IsRequired().HasMaxLength(1).IsFixedLength();
            modelBuilder.Entity<Employee>().Property(e => e.HireDate).HasColumnName("HireDate");
            modelBuilder.Entity<Employee>().Property(e => e.SalariedFlag).HasColumnName("SalariedFlag");
            modelBuilder.Entity<Employee>().Property(e => e.VacationHours).HasColumnName("VacationHours");
            modelBuilder.Entity<Employee>().Property(e => e.SickLeaveHours).HasColumnName("SickLeaveHours");
            modelBuilder.Entity<Employee>().Property(e => e.CurrentFlag).HasColumnName("CurrentFlag");
            modelBuilder.Entity<Employee>().Property(e => e.rowguid).HasColumnName("rowguid");
            modelBuilder.Entity<Employee>().Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");

            base.OnModelCreating(modelBuilder);
        }
    }
}
