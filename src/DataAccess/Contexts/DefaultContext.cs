using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Contexts
{
    public class DefaultContext : DbContext
    {
        public DefaultContext(DbContextOptions<DefaultContext> options) : base(options) { }

        public DbSet<Candidate> Candidate { get; set; }
        public DbSet<CandidateStage> CandidateStage { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<City> City { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<Country> Country { get; set; }
        public DbSet<Position> Position { get; set; }
        public DbSet<PositionType> PositionType { get; set; }
        public DbSet<Resume> Resume { get; set; }
        public DbSet<State> State { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
#endif
        }
    }
}
