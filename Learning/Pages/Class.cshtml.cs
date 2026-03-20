using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ClassModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    public ClassModel(IWebHostEnvironment environment) => _environment = environment;

    [BindProperty(SupportsGet = true)] public string sName { get; set; }
    [BindProperty(SupportsGet = true)] public string cName { get; set; }
    [BindProperty(SupportsGet = true)] public string subID { get; set; }
    [BindProperty(SupportsGet = true)] public string tID { get; set; }

    public List<string> Files { get; set; } = new List<string>();

    public void OnGet()
    {
        // Quét các file hiện có trong thư mục
        var path = Path.Combine(_environment.WebRootPath, "LearningData", sName, cName, subID, tID);
        if (Directory.Exists(path))
        {
            Files = Directory.GetFiles(path).Select(Path.GetFileName).ToList()!;
        }
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
                {
                    await file.CopyToAsync(stream);
                }
            }
        }
        return RedirectToPage(new { sName, cName, subID, tID });
    }
}