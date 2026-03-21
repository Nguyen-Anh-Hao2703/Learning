using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Learning.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public LoginModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public string Username { get; set; } = "";
        [BindProperty]
        public string Password { get; set; } = "";
        public string Message { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            // Tìm user trong SQL Server
            var user = _context.Users.FirstOrDefault(u => u.UserName == Username && u.Password == Password);

            if (user != null)
            {
                // Tạo danh tính cho người dùng
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role),
                    // QUAN TRỌNG: Phải nạp 2 dòng này từ Database vào Cookie
                    new Claim("School", user.School ?? ""),
                    new Claim("Class", user.Class ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                ClaimsPrincipal principal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);

                return RedirectToPage("/Index");
            }

            Message = "Sai tài khoản hoặc mật khẩu!";
            return Page();
        }
    }
}