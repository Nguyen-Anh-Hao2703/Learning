using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Learning.Models;
using System.Security.Claims;

namespace Learning.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<User> _userManager;

        public IndexModel(IWebHostEnvironment hostEnvironment, UserManager<User> userManager)
        {
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        public class ClassFolder
        {
            public string Subject { get; set; } = "";
            public string Teacher { get; set; } = "";
        }

        public List<ClassFolder> StudentLessons { get; set; } = new List<ClassFolder>();
        public string NameSchool { get; set; } = "";
        public string NameClass { get; set; } = "";
        public string CurrentUserRole { get; set; } = ""; // Biến quan trọng để check Role

        [BindProperty(SupportsGet = true)]
        public string? Subject { get; set; }

        public async Task OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Lấy thông tin User trực tiếp từ DB để biết Role là gì
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                if (user != null)
                {
                    CurrentUserRole = user.Role;
                    NameSchool = user.School ?? "";
                    NameClass = user.Class ?? "";

                    LoadData(NameSchool, NameClass);
                }
            }
        }

        private void LoadData(string school, string cls)
        {
            if (string.IsNullOrEmpty(school) || string.IsNullOrEmpty(cls)) return;
            string classPath = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school, cls);

            if (Directory.Exists(classPath))
            {
                var subjectDirs = Directory.GetDirectories(classPath);
                foreach (var subDir in subjectDirs)
                {
                    string subName = Path.GetFileName(subDir);
                    if (!string.IsNullOrEmpty(Subject) && !subName.Contains(Subject, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var teacherDirs = Directory.GetDirectories(subDir);
                    foreach (var tDir in teacherDirs)
                    {
                        StudentLessons.Add(new ClassFolder
                        {
                            Subject = subName,
                            Teacher = Path.GetFileName(tDir)
                        });
                    }
                }
            }
        }

        public IActionResult OnPostCreateFolder(string SubjectID)
        {
            var school = User.FindFirst("School")?.Value;
            var cls = User.FindFirst("Class")?.Value;
            var teacherName = User.Identity?.Name;

            if (string.IsNullOrEmpty(school) || string.IsNullOrEmpty(cls) || string.IsNullOrEmpty(SubjectID))
                return RedirectToPage();

            var path = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school, cls, SubjectID, teacherName ?? "Unknown");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostFindFolder(string SubjectID) => RedirectToPage(new { Subject = SubjectID });
    }
}