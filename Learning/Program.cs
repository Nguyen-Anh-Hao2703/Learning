using Supabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Learning.Data;
using Learning.Models;

internal class Program
{
    private static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var builder = WebApplication.CreateBuilder(args);

        // Thay đoạn UseSqlServer cũ bằng đoạn này
        // 1. Cấu hình Database PostgreSQL
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => {
                npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null); // Tự động thử lại nếu nghẽn mạng
            }));
        var supabaseUrl = builder.Configuration["Supabase:Url"];
        var supabaseKey = builder.Configuration["Supabase:Key"];

        // 3. Đăng ký Supabase Client làm "Singleton" để dùng chung cho toàn bộ ứng dụng
        builder.Services.AddSingleton(provider =>
            new Supabase.Client(supabaseUrl!, supabaseKey));
        // 2. Thêm Identity chuẩn (Dùng ApplicationUser để có FullName, Class, School)
        builder.Services.AddDefaultIdentity<User>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false; // Tắt bớt ép buộc mật khẩu cho dễ test
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            // 1. Cấu hình thời gian sống của Cookie (ví dụ: 7 ngày)
            options.ExpireTimeSpan = TimeSpan.FromDays(30);

            // 2. Nếu người dùng tắt trình duyệt rồi mở lại, Cookie vẫn còn (Ghi nhớ)
            options.SlidingExpiration = true;

            // 3. Đường dẫn đến trang Login nếu User chưa đăng nhập mà đòi vào xem điểm
            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/AccessDenied";
        });

        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles(); // Thêm dòng này nếu chưa có để nhận CSS/JS

        app.UseRouting();

        app.UseAuthentication(); // BẮT BUỘC: Xác nhận danh tính người dùng
        app.UseAuthorization();  // BẮT BUỘC: Kiểm tra quyền truy cập (Chỉ giữ 1 dòng)

        app.MapRazorPages().WithStaticAssets(); // Thêm dòng này để Identity tìm được các trang Register/Login
        app.Run();
    }
}