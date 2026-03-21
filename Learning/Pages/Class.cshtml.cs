using Microsoft.AspNetCore.Identity;
using Supabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;

public class ClassModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration; // Thêm dòng này

    public ClassModel(IWebHostEnvironment environment, UserManager<User> userManager, IConfiguration configuration)
    {
        _environment = environment;
        _userManager = userManager;
        _configuration = configuration; // Thêm dòng này
    }

    [BindProperty(SupportsGet = true)] public string sName { get; set; }
    [BindProperty(SupportsGet = true)] public string cName { get; set; }
    [BindProperty(SupportsGet = true)] public string subID { get; set; }
    [BindProperty(SupportsGet = true)] public string tID { get; set; }
    public string CurrentUserRole { get; set; } = "";
    public List<string> Files { get; set; } = new List<string>();

    // Hàm dùng chung để tạo kết nối Supabase
    private async Task<Supabase.Client> GetSupabaseClient()
    {
        var url = _configuration["Supabase:Url"];
        var key = _configuration["Supabase:Key"];
        var client = new Supabase.Client(url, key);
        await client.InitializeAsync();
        return client;
    }

    public async Task OnGetAsync()
    {
        if (User.Identity.IsAuthenticated)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            CurrentUserRole = user?.Role ?? "";
        }

        try
        {
            var client = await GetSupabaseClient();
            var folderPath = $"{sName}/{cName}/{subID}/{tID}";
            var result = await client.Storage.From("learning-data").List(folderPath);

            if (result != null)
            {
                // Lọc bỏ tên folder, chỉ lấy tên file
                Files = result.Select(x => x.Name).Where(name => name != ".emptyFolderPlaceholder").ToList();
            }
        }
        catch { }
    }

    public async Task<IActionResult> OnPostUploadFile(List<IFormFile> UploadFiles)
    {
        var client = await GetSupabaseClient();

        foreach (var file in UploadFiles)
        {
            if (file.Length > 0)
            {
                var remotePath = $"{sName}/{cName}/{subID}/{tID}/{file.FileName}";

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                // Upload lên Supabase
                await client.Storage.From("learning-data").Upload(fileBytes, remotePath);
            }
        }
        return RedirectToPage(new { sName, cName, subID, tID });
    }
}