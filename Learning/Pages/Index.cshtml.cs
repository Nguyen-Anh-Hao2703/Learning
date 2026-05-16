using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public IndexModel(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }
    public string? Danh_hiệu;
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
        var client = new Supabase.Client(url!, key);
        await client.InitializeAsync();
        return client;
    }

    public async Task OnGetAsync(bool? nguoi_moi)
    {
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        if (User.Identity?.IsAuthenticated == true)
        {
            if (user != null)
            {
                NameSchool = user.School!;
                NameClass = user.Class!;
                CurrentUserRole = user.Role;
                await LoadLessons(user.School!, user.Class!);
            }
            if (nguoi_moi == true) Danh_hiệu = "Người mới";
        }
        string id = user!.Id;
        await GetStudentTitle(id);
    }

    public async Task<IActionResult> OnGetDownloadCertificateAsync()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated || string.IsNullOrEmpty(User.Identity.Name))
        {
            // Nếu chưa đăng nhập, tự động chuyển hướng sang trang Đăng nhập (Identity)
            return RedirectToPage("/Login", new { area = "Identity" });
        }
        // 1. Lấy thông tin người dùng đang đăng nhập
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        if (user == null) return NotFound();

        string name = user.FullName ?? "Người dùng";
        string templateName = "";
        string fileDisplayName = "";

        // 2. Định cấu hình File Mẫu và Tên File Tải về dựa vào Role và Danh Hiệu
        if (user.Role == "Student")
        {
            templateName = (Danh_hiệu == "Xuất sắc") ? "Chung_Chi_Xuat_Sac.docx" : "Chung_Chi_Gioi.docx";
            fileDisplayName = $"Chứng chỉ Học {Danh_hiệu} - {name}.docx";
        }
        else if (user.Role == "Teacher")
        {
            templateName = (Danh_hiệu == "Xuất sắc") ? "Chung_Chi_Xuat_Sac.docx" : "Chung_Chi_Gioi.docx";
            fileDisplayName = $"Chứng chỉ Dạy {Danh_hiệu} - {name}.docx";
        }
        else
        {
            return Page(); // Role không hợp lệ
        }

        try
        {
            // 3. Gọi hàm dùng chung để xử lý file và lấy mảng byte dữ liệu về
            byte[] fileBytes = await TaoFileChungChiTuTemplateAsync(templateName, name, user.Role);

            // 4. Trả file về cho trình duyệt (Đầy đủ tiếng Việt có dấu)
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileDisplayName);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo chứng chỉ: " + ex.Message);
            return Page();
        }
    }

    // 🛠️ HÀM DÙNG CHUNG: Nơi xử lý ma thuật nén/giải nén và thay thế từ
    private async Task<byte[]> TaoFileChungChiTuTemplateAsync(string templateName, string userName, string role)
    {
        // Tạo đường dẫn thư mục tạm thời duy nhất
        string uniqueId = Guid.NewGuid().ToString();
        string tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp_Cert_{uniqueId}");
        string pathTemplateGoc = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", templateName);

        // Giải nén file mẫu
        ZipFile.ExtractToDirectory(pathTemplateGoc, tempFolder);

        // Tìm và đọc file XML cấu trúc nội dung của file Word
        string xmlPath = Path.Combine(tempFolder, "word", "document.xml");
        string body = await System.IO.File.ReadAllTextAsync(xmlPath);

        // Tiến hành thay thế chuỗi văn bản
        body = body.Replace("[Tên]", userName)
                   .Replace("[Ngày tháng]", DateTime.Now.ToString("dd/MM/yyyy"));

        // Nếu là Giáo viên thì tự động biến chữ "Học" thành "Dạy" trong file mẫu
        if (role == "Teacher")
        {
            body = body.Replace("Học", "Dạy");
        }

        // Ghi đè lại nội dung mới vào file XML
        await System.IO.File.WriteAllTextAsync(xmlPath, body);

        // Đóng gói ngược lại thành file .docx tạm thời
        string tempZipOutput = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp_Out_{uniqueId}.docx");
        ZipFile.CreateFromDirectory(tempFolder, tempZipOutput, CompressionLevel.Optimal, false);

        // Đọc toàn bộ file vào bộ nhớ dưới dạng mảng byte
        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(tempZipOutput);

        // DỌN DẸP SẠCH SẼ SERVER (Không để lại file rác)
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        if (System.IO.File.Exists(tempZipOutput)) System.IO.File.Delete(tempZipOutput);

        return fileBytes;
    }

    public async Task<string> GetStudentTitle(string userId)
    {
        var client = await GetSupabaseClient();
        int Id = Convert.ToInt32(userId);
        // 1. Lấy danh sách điểm từ Supabase nơi điểm >= 9
        var results1 = await client.From<ExamResult>()
            .Where(x => x.Id == Id)
            .Where(x => x.Point >= 9)
            .Get();
        var results2 = await client.From<ExamResult>()
            .Where(x => x.Id == Id)
            .Where(x => x.Point >= 8)
            .Get();
        var results3 = await client.From<ExamResult>()
            .Where(x => x.Id == Id)
            .Where(x => x.Point >= 7)
            .Get();
        var results4 = await client.From<ExamResult>()
            .Where(x => x.Id == Id)
            .Where(x => x.Point >= 5)
            .Get();
        var results5 = await client.From<ExamResult>()
            .Where(x => x.Id == Id)
            .Where(x => x.Point < 5)
            .Get();

        int count = results1.Models.Count;
        int count2 = results2.Models.Count;
        int count3 = results3.Models.Count;
        int count4 = results4.Models.Count;
        int count5 = results5.Models.Count;

        // 2. Xét danh hiệu
        if (count >= 7) return Danh_hiệu = "Xuất sắc";
        if (count2 > 2) return Danh_hiệu = "Giỏi";
        if (count3 > 2) return Danh_hiệu = "Khá";
        if (count4 > 2) return Danh_hiệu = "Trung bình";
        if (count5 > 2) return Danh_hiệu = "Chưa đạt";

        return "Thành viên mới";
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
                    if (sub.Name!.Contains(".emptyFolder")) continue;
                    var teachers = await client.Storage.From("learning-data").List($"{path}/{sub.Name}");
                    if (teachers != null)
                    {
                        foreach (var t in teachers)
                        {
                            if (t.Name!.Contains(".emptyFolder")) continue;
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
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        if (user == null || string.IsNullOrEmpty(SubjectID)) return RedirectToPage();

        try
        {
            var client = await GetSupabaseClient();
            var content = System.Text.Encoding.UTF8.GetBytes("init_" + DateTime.Now.Ticks);

            // Dùng chung hàm RemoveDiacritics để đường dẫn luôn khớp nhau
            string path = $"{RemoveDiacritics(user.School!)}/{RemoveDiacritics(user.Class!)}/{RemoveDiacritics(SubjectID)}/{RemoveDiacritics(user.UserName!)}/info.txt";

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