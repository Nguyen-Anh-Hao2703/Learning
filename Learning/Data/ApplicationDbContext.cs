using Microsoft.EntityFrameworkCore;
using Learning.Models; // Kết nối tới thư mục Models ở trên

namespace Learning.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo bảng Users sẽ xuất hiện trong SQL Server
        public DbSet<User> Users { get; set; }
    }
}