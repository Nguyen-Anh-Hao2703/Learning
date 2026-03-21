using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;

public class ClassModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<User> _userManager;

    public ClassModel(IWebHostEnvironment environment, UserManager<User> userManager)
    {
        _environment = environment;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public string sName { get; set; }
    [BindProperty(SupportsGet = true)] public string cName { get; set; }
    [BindProperty(SupportsGet = true)] public string subID { get; set; }
    [BindProperty(SupportsGet = true)] public string tID { get; set; }
    public string CurrentUserRole { get; set; } = "";
    public List<string> Files { get; set; } = new List<string>();

    public async Task OnGetAsync()
    {
        if (User.Identity.IsAuthenticated)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            CurrentUserRole = user?.Role ?? "";
        }

        var path = Path.Combine(_environment.WebRootPath, "LearningData", sName, cName, subID, tID);
        if (Directory.Exists(path))
            Files = Directory.GetFiles(path).Select(Path.GetFileName).ToList()!;
    }

    public async Task<IActionResult> OnPostUploadFile(List<IFormFile> UploadFiles)
    {
        var path = Path.Combine(_environment.WebRootPath, "LearningData", sName, cName, subID, tID);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        foreach (var file in UploadFiles)
        {
            if (file.Length > 0)
            {
                var filePath = Path.Combine(path, file.FileName);
                using (var stream = System.IO.File.Create(filePath))
                    await file.CopyToAsync(stream);
            }
        }
        return RedirectToPage(new { sName, cName, subID, tID });
    }
}