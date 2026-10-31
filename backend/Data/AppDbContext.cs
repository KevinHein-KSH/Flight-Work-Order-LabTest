using LabTest.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LabTest.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Define DbSets here, e.g.:
        // public DbSet<MyEntity> MyEntities { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity properties and relationships here if needed
            modelBuilder.Entity<WorkOrder>()
                .ToTable(t => t.HasCheckConstraint("CK_WorkOrder_PbbAngle", "[PbbAngle] IN (0,90,180,270)"));
        }
    }
}