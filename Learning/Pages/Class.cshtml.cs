using Microsoft.AspNetCore.Identity;
using Supabase;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;
using Learning.Pages;

public class ClassModel : PageModel
{
    private readonly Supabase.Client _supabase;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public ClassModel(UserManager<User> userManager, IConfiguration configuration, Supabase.Client supabase)
    {
        _userManager = userManager;
        _configuration = configuration;
        _supabase = supabase; // Phải gán giá trị này thì mới dùng được ở hàm Xóa!
    }

    [BindProperty(SupportsGet = true)] public string sName { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string cName { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string subID { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string tID { get; set; } = "";

    public string CurrentUserRole { get; set; } = "";
    public string UserClass { get; set; } = "";
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
            UserClass = user?.Class ?? "";
            try
            {
                var client = await GetSupabaseClient();
                // Làm sạch đường dẫn trước khi List file
                string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}";

                var result = await client.Storage.From("learning-data").List(path);
                if (result != null)
                {
                    Files = result.Select(x => x.Name)
                                  .Where(n => n != ".emptyFolderPlaceholder" && n != "info.txt" && !string.IsNullOrEmpty(n))
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
                    // 1. Làm sạch tên file: Giữ lại đuôi file, làm sạch phần tên
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    string fileNameOnly = Path.GetFileNameWithoutExtension(file.FileName);
                    string safeFileName = RemoveDiacritics(fileNameOnly) + extension;

                    // 2. Xây dựng đường dẫn cực sạch
                    string remotePath = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{safeFileName}";

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    // 3. Ép kiểu ContentType để Supabase chấp nhận file lạ .qs
                    var options = new Supabase.Storage.FileOptions
                    {
                        Upsert = true,
                        ContentType = "application/octet-stream"
                    };

                    await client.Storage.From("learning-data").Upload(ms.ToArray(), remotePath, options);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi: " + ex.Message;
        }

        return RedirectToPage(new { sName, cName, subID, tID });
    }

    public string GetFileUrl(string fileName)
    {
        var url = _configuration["Supabase:Url"];
        string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{fileName}";
        return $"{url}/storage/v1/object/public/learning-data/{path}";
    }
    public async Task<IActionResult> OnPostDeletedFileAsync(string file)
    {
        if (string.IsNullOrEmpty(file)) return RedirectToPage(new { sName, cName, subID, tID });

        try
        {
            // 1. Dùng GetSupabaseClient() để đảm bảo client đã sẵn sàng nếu _supabase chưa inject
            var client = await GetSupabaseClient();

            // 2. Đường dẫn PHẢI khớp tuyệt đối với cấu trúc lúc upload
            string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{file}";

            // 3. Thực hiện xóa
            await client.Storage
                .From("learning-data")
                .Remove(new List<string> { path });

            TempData["Message"] = "Đã xóa file vĩnh viễn!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
        }

        // 4. Quan trọng: Phải truyền lại tham số để quay về đúng thư mục bài tập
        return RedirectToPage(new { sName, cName, subID, tID });
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        // Fix chữ Đ/đ và xóa dấu
        text = text.Replace("Đ", "D").Replace("đ", "d");
        text = text.Replace("Ô", "O").Replace("ô", "o");
        text = text.Replace("Ơ", "O").Replace("ơ", "o");
        string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in normalizedString)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) sb.Append(c);
                else sb.Append('_');
            }
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"_+", "_").Trim('_');
    }
}