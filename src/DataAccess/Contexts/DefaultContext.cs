using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Contexts
{
    public class DefaultContext : DbContext
    {
        public DefaultContext(DbContextOptions<DefaultContext> options) : base(options) { }

        public DbSet<Default> Defaults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Default>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Defaults");
            });
        }
    }
}
