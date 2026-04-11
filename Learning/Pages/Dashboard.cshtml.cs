using Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DashboardModel : PageModel
{
    private readonly Supabase.Client _supabase;
    public List<ExamResult> ListResults { get; set; } = new();

    // Khai báo BindProperty để giữ giá trị trên ô nhập liệu (nếu muốn)
    [BindProperty(SupportsGet = true)]
    public string? FilterClass { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterTest { get; set; }

    public DashboardModel(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task OnGetAsync(string filterClass, string filterTest)
    {
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
    }
}