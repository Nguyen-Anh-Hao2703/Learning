using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;
using Supabase;

public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public IndexModel(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    // --- CÁC BIẾN NÀY PHẢI CÓ ĐỂ HIỂN THỊ RA GIAO DIỆN ---
    public string NameSchool { get; set; } = "";
    public string NameClass { get; set; } = "";
    public string CurrentUserRole { get; set; } = "";
    [BindProperty] public string Subject { get; set; } = "";
    public List<LessonInfo> StudentLessons { get; set; } = new List<LessonInfo>();

    public class LessonInfo
    {
        public string Subject { get; set; } = "";
        public string Teacher { get; set; } = "";
    }

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
            if (user != null)
            {
                NameSchool = user.School;
                NameClass = user.Class;
                CurrentUserRole = user.Role;

                // Hàm này dùng để load danh sách bài tập từ Supabase hiện lên trang chủ
                await LoadLessons(user.School, user.Class);
            }
        }
    }

    private async Task LoadLessons(string school, string className)
    {
        try
        {
            var client = await GetSupabaseClient();
            string path = $"{RemoveDiacritics(school)}/{RemoveDiacritics(className)}";

            // Liệt kế các thư mục môn học
            var subjects = await client.Storage.From("learning-data").List(path);
            if (subjects != null)
            {
                foreach (var sub in subjects)
                {
                    if (sub.Name == ".emptyFolderPlaceholder") continue;

                    // Tiếp tục liệt kê giáo viên trong môn đó
                    string subPath = $"{path}/{sub.Name}";
                    var teachers = await client.Storage.From("learning-data").List(subPath);
                    if (teachers != null)
                    {
                        foreach (var t in teachers)
                        {
                            if (t.Name == ".emptyFolderPlaceholder") continue;
                            StudentLessons.Add(new LessonInfo { Subject = sub.Name, Teacher = t.Name });
                        }
                    }
                }
            }
        }
        catch { }
    }

    public async Task<IActionResult> OnPostCreateFolder(string SubjectID)
    {
        var user = await _userManager.FindByNameAsync(User.Identity.Name);
        if (user == null || string.IsNullOrEmpty(SubjectID)) return RedirectToPage();

        try
        {
            var client = await GetSupabaseClient();

            // --- SỬA CHỖ NÀY: Tạo nội dung file mồi "thật" để tránh lỗi 0 byte ---
            var contentString = "folder_initialized_at_" + DateTime.Now.ToString();
            var content = System.Text.Encoding.UTF8.GetBytes(contentString);

            // Chuẩn hóa đường dẫn
            string school = RemoveDiacritics(user.School);
            string @class = RemoveDiacritics(user.Class);
            string subject = RemoveDiacritics(SubjectID);
            string userName = RemoveDiacritics(user.UserName);

            // Đường dẫn file info.txt
            string path = $"{school}/{@class}/{subject}/{userName}/info.txt";

            // Upload với Upsert = true
            var options = new Supabase.Storage.FileOptions { Upsert = true };

            await client.Storage.From("learning-data").Upload(content, path, options);

            Console.WriteLine($"✅ Thành công: {path}");
        }
        catch (Exception ex)
        {
            // Ghi lỗi chi tiết để debug
            TempData["Error"] = ex.Message;
            Console.WriteLine($"❌ Lỗi: {ex.Message}");
        }

        return RedirectToPage();
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
        return new string(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray()).Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "_");
    }
}