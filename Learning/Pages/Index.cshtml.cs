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

            // Đảm bảo đường dẫn chuẩn xác kể cả khi có khoảng trắng
            string classPath = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school.Trim(), cls.Trim());

            if (!Directory.Exists(classPath))
            {
                Directory.CreateDirectory(classPath);
                return; // Thư mục mới tạo thì chắc chắn chưa có môn học
            }

            var subjectDirs = Directory.GetDirectories(classPath);
            foreach (var subDir in subjectDirs)
            {
                string subName = Path.GetFileName(subDir);

                if (!string.IsNullOrEmpty(Subject) && !subName.Contains(Subject, StringComparison.OrdinalIgnoreCase))
                    continue;

                var teacherDirs = Directory.GetDirectories(subDir);

                if (teacherDirs.Length > 0)
                {
                    foreach (var tDir in teacherDirs)
                    {
                        StudentLessons.Add(new ClassFolder
                        {
                            Subject = subName,
                            Teacher = Path.GetFileName(tDir)
                        });
                    }
                }
                else
                {
                    // Nếu môn học mới tạo, chưa có folder GV bên trong, vẫn cho hiện lên danh sách
                    StudentLessons.Add(new ClassFolder
                    {
                        Subject = subName,
                        Teacher = "Chưa có tài liệu"
                    });
                }
            }
        }

        public IActionResult OnPostCreateFolder(string SubjectID)
        {
            // Lấy trực tiếp từ database cho chắc ăn thay vì Claim (vì Claim có thể bị cũ)
            var user = _userManager.FindByNameAsync(User.Identity.Name).Result;

            if (user == null || string.IsNullOrEmpty(SubjectID)) return RedirectToPage();

            var school = user.School?.Trim();
            var cls = user.Class?.Trim();
            var teacherName = user.UserName;

            var path = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school, cls, SubjectID.Trim(), teacherName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

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