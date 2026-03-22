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
                    // 1. Xử lý tên file cực sạch
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    string fileNameOnly = Path.GetFileNameWithoutExtension(file.FileName);

                    // Chỉ giữ lại chữ cái và số cho tên file
                    string safeFileName = RemoveDiacritics(fileNameOnly) + extension;

                    // 2. ÉP BUỘC làm sạch từng thành phần đường dẫn một lần nữa
                    string s = RemoveDiacritics(sName);
                    string c = RemoveDiacritics(cName);
                    string sub = RemoveDiacritics(subID);
                    string t = RemoveDiacritics(tID);

                    // 3. Ghép đường dẫn: Đảm bảo không có dấu chấm dư thừa
                    string remotePath = $"{s}/{c}/{sub}/{t}/{safeFileName}";

                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    var options = new Supabase.Storage.FileOptions
                    {
                        Upsert = true,
                        ContentType = "application/octet-stream"
                    };

                    // Ghi Log ra màn hình Console của Render để kiểm tra đường dẫn thực tế
                    Console.WriteLine($"Uploading to: {remotePath}");

                    await client.Storage.From("learning-data").Upload(ms.ToArray(), remotePath, options);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi upload: " + ex.Message;
            Console.WriteLine("Chi tiết lỗi: " + ex.StackTrace);
        }

        return RedirectToPage(new { sName, cName, subID, tID });
    }

    // Hàm sinh URL tải file trực tiếp từ mây Supabase
    // Hàm sinh URL tải file: Giữ nguyên fileName vì nó đã sạch sẵn rồi
    public string GetFileUrl(string fileName)
    {
        var url = _configuration["Supabase:Url"];
        string bucket = "learning-data";

        // Chỉ làm sạch các thư mục cha, còn fileName thì giữ nguyên để bảo vệ dấu chấm của đuôi file
        string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{fileName}";

        return $"{url}/storage/v1/object/public/{bucket}/{path}";
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";

        // 1. Chuyển về dạng không dấu
        string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (char c in normalizedString)
        {
            System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // 2. Chỉ giữ lại chữ cái và con số, biến tất cả ký tự lạ/dấu chấm/khoảng trắng thành '_'
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
        }

        // 3. Loại bỏ việc có nhiều dấu gạch dưới liên tiếp và trả về chuỗi sạch
        string result = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        return System.Text.RegularExpressions.Regex.Replace(result, @"_+", "_").Trim('_');
    }
}