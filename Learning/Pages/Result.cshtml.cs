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
        public ResultModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public async void OnGet(int score)
        {
            var user = await _userManager.GetUserAsync(User);
            UserName = user!.FullName;
            Class = user.Class!;
            point = score;
        }
    }
}
