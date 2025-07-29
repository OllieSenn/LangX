using Microsoft.EntityFrameworkCore;
using LangX.Pages;
using LangX.Models;

namespace LangX.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
      
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<UserLike> UserLikes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserFollow> UserFollows { get; set; }

        public DbSet<UserBadge> UserBadges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserLike>()
                .HasIndex(ul => new { ul.UserId, ul.PostId })
                .IsUnique();
        }
    }
}
