using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Learning.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public IndexModel(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public class ClassFolder
        {
            public string Subject { get; set; } = "";
            public string Teacher { get; set; } = "";
        }

        // Danh sách hiển thị chung cho cả 2 Role
        public List<ClassFolder> StudentLessons { get; set; } = new List<ClassFolder>();

        public string NameSchool { get; set; } = "";
        public string NameClass { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string? Subject { get; set; }

        public void OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Lấy thông tin từ Claim
                NameSchool = User.FindFirst("School")?.Value ?? "";
                NameClass = User.FindFirst("Class")?.Value ?? "";

                // Gọi hàm quét thư mục dựa trên Trường và Lớp của User
                LoadData(NameSchool, NameClass);
            }
        }

        private void LoadData(string school, string cls)
        {
            if (string.IsNullOrEmpty(school) || string.IsNullOrEmpty(cls)) return;

            // Đường dẫn: wwwroot/LearningData/TenTruong/TenLop
            string classPath = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school, cls);

            if (Directory.Exists(classPath))
            {
                var subjectDirs = Directory.GetDirectories(classPath);
                foreach (var subDir in subjectDirs)
                {
                    string subName = Path.GetFileName(subDir);

                    // Nếu có tìm kiếm (Subject) mà không khớp thì bỏ qua
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
            var teacherName = User.Identity?.Name; // Dùng Username làm folder định danh GV

            if (string.IsNullOrEmpty(school) || string.IsNullOrEmpty(cls) || string.IsNullOrEmpty(SubjectID))
            {
                // Nếu nó chạy vào đây, nghĩa là Claim của bạn đang bị NULL
                return RedirectToPage();
            }

            // Cấu trúc folder đồng bộ: LearningData -> Trường -> Lớp -> Môn -> TênGV
            var path = Path.Combine(_hostEnvironment.WebRootPath, "LearningData", school, cls, SubjectID, teacherName ?? "Unknown");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth"); // Phải khớp với tên Scheme bạn đặt trong Program.cs
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostFindFolder(string SubjectID)
        {
            return RedirectToPage(new { Subject = SubjectID });
        }
    }
}