using Microsoft.AspNetCore.Identity;
using Supabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;

public class ClassModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public ClassModel(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [BindProperty(SupportsGet = true)] public string sName { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string cName { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string subID { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string tID { get; set; } = "";

    public string CurrentUserRole { get; set; } = "";
    public List<string> Files { get; set; } = new List<string>();

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
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            CurrentUserRole = user?.Role ?? "";

            try
            {
                var client = await GetSupabaseClient();
                string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}";

                var result = await client.Storage.From("learning-data").List(path);
                if (result != null)
                {
                    // Lọc bỏ các file mồi hệ thống để danh sách sạch đẹp
                    Files = result.Select(x => x.Name)
                                  .Where(n => n != ".emptyFolderPlaceholder" && n != "info.txt" && n != "init.txt" && !string.IsNullOrEmpty(n))
                                  .ToList();
                }
            }
            catch { }
        }
    }

    public async Task<IActionResult> OnPostUploadFile(List<IFormFile> UploadFiles)
    {
        if (UploadFiles == null || UploadFiles.Count == 0) return RedirectToPage();

        try
        {
            var client = await GetSupabaseClient();
            foreach (var file in UploadFiles)
            {
                if (file.Length > 0)
                {
                    // Xử lý tên file: bỏ dấu và khoảng trắng để tránh lỗi 400/Invalid Key
                    string safeFileName = RemoveDiacritics(file.FileName);
                    string remotePath = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{safeFileName}";

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    // Upload lên Supabase với tùy chọn Upsert (ghi đè nếu trùng tên)
                    await client.Storage.From("learning-data").Upload(ms.ToArray(), remotePath, new Supabase.Storage.FileOptions { Upsert = true });
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi: " + ex.Message;
        }

        return RedirectToPage(new { sName, cName, subID, tID });
    }

    // Hàm sinh URL tải file trực tiếp từ mây Supabase
    public string GetFileUrl(string fileName)
    {
        var url = _configuration["Supabase:Url"];
        string bucket = "learning-data";
        string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{fileName}";

        return $"{url}/storage/v1/object/public/{bucket}/{path}";
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        return new string(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "_");
    }
}