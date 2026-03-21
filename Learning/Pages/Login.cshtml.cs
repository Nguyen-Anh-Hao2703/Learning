using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Learning.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;

        public LoginModel(SignInManager<User> signInManager) => _signInManager = signInManager;

        [BindProperty]
        public string Username { get; set; } = "";
        [BindProperty]
        public string Password { get; set; } = "";
        public string Message { get; set; } = "";

        public async Task<IActionResult> OnPostAsync()
        {
            // SignInManager sẽ tự kiểm tra PasswordHash trong DB
            var result = await _signInManager.PasswordSignInAsync(Username, Password, false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToPage("/Index");
            }

            Message = "Sai tài khoản hoặc mật khẩu!";
            return Page();
        }
    }
}