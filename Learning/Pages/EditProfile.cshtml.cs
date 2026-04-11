using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class EditProfileModel : PageModel
{
    private readonly UserManager<User> _userManager;

    public EditProfileModel(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        public string? FullName { get; set; }
        public string? Class { get; set; }
        public string? School { get; set; }
    }

    // Bước 1: Lấy dữ liệu cũ đổ vào ô nhập
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User); // Lấy user đang đăng nhập
        if (user == null) return NotFound();

        Input = new InputModel
        {
            FullName = user.FullName,
            Class = user.Class,
            School = user.School
        };
        return Page();
    }

    // Bước 2: Lưu dữ liệu mới xuống Mumbai
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = Input.FullName!;
        user.Class = Input.Class;
        user.School = Input.School;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToPage("./Index"); // Xong thì về trang chủ
        }
        return Page();
    }
}