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
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user != null)
            {
                NameSchool = user.School;
                NameClass = user.Class;
                CurrentUserRole = user.Role;
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
            var subjects = await client.Storage.From("learning-data").List(path);

            if (subjects != null)
            {
                foreach (var sub in subjects)
                {
                    if (sub.Name.Contains(".emptyFolder")) continue;
                    var teachers = await client.Storage.From("learning-data").List($"{path}/{sub.Name}");
                    if (teachers != null)
                    {
                        foreach (var t in teachers)
                        {
                            if (t.Name.Contains(".emptyFolder")) continue;
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
            var content = System.Text.Encoding.UTF8.GetBytes("init_" + DateTime.Now.Ticks);

            // Dùng chung hàm RemoveDiacritics để đường dẫn luôn khớp nhau
            string path = $"{RemoveDiacritics(user.School)}/{RemoveDiacritics(user.Class)}/{RemoveDiacritics(SubjectID)}/{RemoveDiacritics(user.UserName)}/info.txt";

            await client.Storage.From("learning-data").Upload(content, path, new Supabase.Storage.FileOptions { Upsert = true });
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }

        return RedirectToPage();
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "unknown";
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