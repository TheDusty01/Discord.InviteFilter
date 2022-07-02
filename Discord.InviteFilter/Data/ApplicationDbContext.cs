using Discord.InviteFilter.Models;
using Microsoft.EntityFrameworkCore;

namespace Discord.InviteFilter.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<GuildInviteSettings> GuildInviteSettings => Set<GuildInviteSettings>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContextOptions) : base(dbContextOptions)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GuildInviteSettings>(entity =>
            {
                entity.HasKey(e => e.GuildId);
            });
        }
    }
}
