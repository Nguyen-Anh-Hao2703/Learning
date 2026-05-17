using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase.Gotrue;
using System.IO;

public class DashboardModel : PageModel
{
    private readonly Supabase.Client _supabase;
    private readonly UserManager<Learning.Models.User> _userManager;
    private readonly IConfiguration _configuration;
    public List<ExamResult> ListResults { get; set; } = new();

    // Khai báo BindProperty để giữ giá trị trên ô nhập liệu (nếu muốn)
    [BindProperty(SupportsGet = true)]
    public string? FilterClass { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterTest { get; set; }

    public DashboardModel(UserManager<Learning.Models.User> userManager, IConfiguration configuration, Supabase.Client supabase)
    {
        _userManager = userManager;
        _configuration = configuration;
        _supabase = supabase;
    }

    public async Task<IActionResult> OnGetAsync(string filterClass, string filterTest)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user!.Role != "Teacher")
        {
            return RedirectToPage("AccessDenied", new { namePage = "Trang xem điểm dành cho giáo viên" });
        }

        // 1. Khởi tạo query chuẩn tầng Postgrest để không bị lệch kiểu dữ liệu khi dùng .Where()
        Supabase.Postgrest.Interfaces.IPostgrestTable<ExamResult> query = _supabase.From<ExamResult>();

        // 2. Bây giờ gán đè thoải mái, hết sạch lỗi convert type
        if (!string.IsNullOrEmpty(filterClass))
        {
            query = query.Where(x => x.ClassName == filterClass);
        }

        if (!string.IsNullOrEmpty(filterTest))
        {
            string fileName = Path.GetFileName(System.Net.WebUtility.UrlDecode(filterTest));
            query = query.Where(x => x.TestName == fileName);
        }

        // 3. Thực hiện lấy dữ liệu an toàn
        var result = await query.Get();
        ListResults = result?.Models ?? new List<ExamResult>();

        return Page();
    }
}