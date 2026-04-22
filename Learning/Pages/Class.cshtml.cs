using Microsoft.AspNetCore.Identity;
using Supabase;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;

namespace Learning.Pages
{
    public class ClassModel : PageModel
    {
        private readonly Supabase.Client _supabase;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public ClassModel(UserManager<User> userManager, IConfiguration configuration, Supabase.Client supabase)
        {
            _userManager = userManager;
            _configuration = configuration;
            _supabase = supabase;
        }

        [BindProperty(SupportsGet = true)] public string sName { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string cName { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string subID { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string tID { get; set; } = "";

        public string CurrentUserRole { get; set; } = "";
        public string UserClass { get; set; } = "";
        public List<string> Files { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Đá người dùng về trang Login ngay lập tức
                return RedirectToPage("/Login");
            }
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name!);
                CurrentUserRole = user?.Role ?? "";
                UserClass = user?.Class ?? "";

                try
                {
                    string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}";
                    var result = await _supabase.Storage.From("learning-data").List(path);
                    if (result != null)
                    {
                        Files = result.Select(x => x.Name!)
                                      .Where(n => n != ".emptyFolderPlaceholder" && n != "info.txt" && !string.IsNullOrEmpty(n))
                                      .ToList();
                    }
                }
                catch { }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostUploadFile(List<IFormFile> UploadFiles)
        {
            if (UploadFiles == null || UploadFiles.Count == 0) return RedirectToPage(new { sName, cName, subID, tID });

            try
            {
                foreach (var file in UploadFiles)
                {
                    if (file.Length > 0)
                    {
                        string extension = Path.GetExtension(file.FileName).ToLower();
                        string fileNameOnly = Path.GetFileNameWithoutExtension(file.FileName);
                        // Làm sạch tên nhưng giữ lại đuôi
                        string safeFileName = RemoveDiacritics(fileNameOnly) + extension;

                        string remotePath = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{safeFileName}";

                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);

                        var options = new Supabase.Storage.FileOptions { Upsert = true, ContentType = "application/octet-stream" };
                        await _supabase.Storage.From("learning-data").Upload(ms.ToArray(), remotePath, options);
                    }
                }
                TempData["Message"] = "Tải lên thành công!";
            }
            catch (Exception ex) { TempData["Error"] = "Lỗi upload: " + ex.Message; }

            return RedirectToPage(new { sName, cName, subID, tID });
        }

        public async Task<IActionResult> OnPostDeletedFileAsync(string file)
        {
            if (string.IsNullOrEmpty(file)) return RedirectToPage(new { sName, cName, subID, tID });

            try
            {
                // Đường dẫn xóa đồng nhất hoàn toàn với lúc Upload
                string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{file}";

                await _supabase.Storage.From("learning-data").Remove(new List<string> { path });
                TempData["Message"] = "Đã xóa vĩnh viễn!";
            }
            catch (Exception ex) { TempData["Error"] = "Lỗi xóa: " + ex.Message; }

            return RedirectToPage(new { sName, cName, subID, tID });
        }

        public string GetFileUrl(string fileName)
        {
            var url = _configuration["Supabase:Url"];
            string path = $"{RemoveDiacritics(sName)}/{RemoveDiacritics(cName)}/{RemoveDiacritics(subID)}/{RemoveDiacritics(tID)}/{fileName}";
            return $"{url}/storage/v1/object/public/learning-data/{path}";
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "unknown";
            text = text.Replace("Đ", "D").Replace("đ", "d");
            string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in normalizedString)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    // Cho phép dấu chấm để không hỏng đuôi file
                    if (char.IsLetterOrDigit(c) || c == '.') sb.Append(c);
                    else sb.Append('_');
                }
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"_+", "_").Trim('_');
        }
    }
}