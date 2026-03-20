using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Learning.Data;
using Learning.Models;

namespace Learning.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Nếu cậu có DbSet cho các bảng khác thì để bên dưới này
        public DbSet<User> Users { get; set; }
    }
}