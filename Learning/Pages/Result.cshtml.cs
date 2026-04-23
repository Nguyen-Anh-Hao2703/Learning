using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Learning.Pages
{
    public class ResultModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly HttpClient _httpClient = new HttpClient();
        public double Point { get; set; } // Nên dùng Property để Razor dễ truy cập
        public string UserName = string.Empty;
        public string Class = string.Empty;
        public ResultModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> OnGetAsync(double score) // Đổi từ void sang Task
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserName = user.FullName ?? "N/A";
                Class = user.Class ?? "N/A";
            }
            Point = score;
            return Page();
        }
    }
}
