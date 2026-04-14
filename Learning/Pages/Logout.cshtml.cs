using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;

namespace Learning.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;

        public LogoutModel(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }
        public async  Task<IActionResult> OnGet()
        {
            await OnPostLogoutAsync();
            return RedirectToPage("Index");
        }
        public async Task OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
