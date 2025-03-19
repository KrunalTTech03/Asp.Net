using Microsoft.EntityFrameworkCore;
using StudentCoreWebApi.Model;

namespace StudentCoreWebApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Userrole> UsersRoles { get; set; }

    }
}