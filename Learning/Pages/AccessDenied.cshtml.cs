using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Learning.Pages
{
    public class AccessDeniedModel : PageModel
    {
        private string? name;
        public void OnGet(string namePage)
        {
            name = namePage;
        }
    }
}
