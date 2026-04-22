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

        // Nếu không tìm thấy User (do tắt trình duyệt, hết hạn session)
        if (user == null)
        {
            // Đá người dùng về trang Login ngay lập tức
            return RedirectToPage("/Login");
        }
        if(user.Role != "Teacher")
        {
            return RedirectToPage("AccessDenied", new { namePage = "Trang xem điểm dành cho giáo viên"});
        }
        var query = _supabase.From<ExamResult>();

        if (!string.IsNullOrEmpty(filterClass))
        {
            query.Where(x => x.ClassName == filterClass);
        }

        if (!string.IsNullOrEmpty(filterTest))
        {
            // Mẹo: Nếu filterTest là một URL, ta chỉ lấy phần tên file cuối cùng
            string fileName = Path.GetFileName(System.Net.WebUtility.UrlDecode(filterTest));
            query.Where(x => x.TestName == fileName);
        }

        var result = await query.Get();
        ListResults = result.Models; // Gán danh sách kết quả vào biến hiển thị
        return Page();
    }
}