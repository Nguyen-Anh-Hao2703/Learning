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
        public int point;
        public string UserName;
        public string Class;
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ResultModel(UserManager<User> userManager)
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            _userManager = userManager;
        }
        public async Task OnGetAsync(int score) // Đổi từ void sang Task
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserName = user.FullName ?? "N/A";
                Class = user.Class ?? "N/A";
            }
            point = score;
        }
    }
}
