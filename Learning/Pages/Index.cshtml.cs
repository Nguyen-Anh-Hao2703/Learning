using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;
using Supabase;

namespace Learning.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public IndexModel(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public class ClassFolder
        {
            public string Subject { get; set; } = "";
            public string Teacher { get; set; } = "";
        }

        public List<ClassFolder> StudentLessons { get; set; } = new List<ClassFolder>();
        public string NameSchool { get; set; } = "";
        public string NameClass { get; set; } = "";
        public string CurrentUserRole { get; set; } = "";

        // Dòng này cực kỳ quan trọng để hết lỗi CS1061 trong ảnh của cậu
        [BindProperty(SupportsGet = true)]
        public string? Subject { get; set; }

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
                    CurrentUserRole = user.Role;
                    NameSchool = user.School ?? "";
                    NameClass = user.Class ?? "";
                    await LoadDataFromSupabase();
                }
            }
        }

        private async Task LoadDataFromSupabase()
        {
            try
            {
                var client = await GetSupabaseClient();
                string cleanPath = $"{RemoveDiacritics(NameSchool)}/{RemoveDiacritics(NameClass)}";
                var subjects = await client.Storage.From("learning-data").List(cleanPath);

                if (subjects != null)
                {
                    foreach (var sub in subjects)
                    {
                        if (sub.Name == ".emptyFolderPlaceholder") continue;
                        // Nếu có lọc môn học thì kiểm tra ở đây
                        if (!string.IsNullOrEmpty(Subject) && !sub.Name.Contains(Subject, StringComparison.OrdinalIgnoreCase)) continue;

                        var teachers = await client.Storage.From("learning-data").List($"{cleanPath}/{sub.Name}");
                        if (teachers != null)
                        {
                            foreach (var t in teachers)
                            {
                                if (t.Name == ".emptyFolderPlaceholder") continue;
                                StudentLessons.Add(new ClassFolder { Subject = sub.Name, Teacher = t.Name });
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

            var client = await GetSupabaseClient();
            string path = $"{RemoveDiacritics(user.School)}/{RemoveDiacritics(user.Class)}/{RemoveDiacritics(SubjectID)}/{RemoveDiacritics(user.UserName)}/.emptyFolderPlaceholder";

            await client.Storage.From("learning-data").Upload(new byte[] { 0 }, path);
            return RedirectToPage();
        }

        public IActionResult OnPostFindFolder(string SubjectID)
        {
            return RedirectToPage(new { Subject = SubjectID });
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "unknown";
            return new string(text.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray()).Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "_");
        }
    }
}