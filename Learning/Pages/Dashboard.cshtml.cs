using Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DashboardModel : PageModel
{
    private readonly Supabase.Client _supabase;
    public List<ExamResult> ListResults { get; set; } = new();

    // Khai báo BindProperty để giữ giá trị trên ô nhập liệu (nếu muốn)
    [BindProperty(SupportsGet = true)]
    public string FilterClass { get; set; }

    [BindProperty(SupportsGet = true)]
    public string FilterTest { get; set; }

    public DashboardModel(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task OnGetAsync(string filterClass, string filterTest)
    {
        FilterClass = filterClass;
        FilterTest = filterTest;

        // 1. Khởi tạo query ban đầu
        var query = _supabase.From<ExamResult>();

        // 2. Kiểm tra và nối điều kiện (Không dùng dấu =)
        if (!string.IsNullOrEmpty(FilterClass))
        {
            query.Where(x => x.ClassName == FilterClass);
        }

        if (!string.IsNullOrEmpty(FilterTest))
        {
            query.Where(x => x.TestName == FilterTest);
        }

        // 3. Thực thi lấy dữ liệu
        var response = await query
            .Order(x => x.Point, Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        ListResults = response.Models;
    }
}