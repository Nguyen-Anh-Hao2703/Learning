using Learning.Data;
using Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Learning.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User NewUser { get; set; } = default!;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Kiểm tra xem Username đã tồn tại chưa
            if (_context.Users.Any(u => u.Username == NewUser.Username))
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại!");
                return Page();
            }

            var newUser = new User
            {
                Username = NewUser.Username,
                Password = NewUser.Password,
                Role = NewUser.Role,
                School = NewUser.Role == "" ? NewUser.School : null,
                Class = NewUser.Role == "" ? NewUser.Class : null
            };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, newUser.Username),
                new Claim(ClaimTypes.Role, newUser.Role),
                // THÊM 2 DÒNG NÀY: Lưu thông tin vào Identity
                new Claim("School", newUser.School ?? ""),
                new Claim("Class", newUser.Class ?? "")
            };

            _context.Users.Add(NewUser);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index"); // Đăng ký xong thì về trang chủ
        }
    }
}