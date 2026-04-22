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
    public InputModel? Input { get; set; }

    public class InputModel
    {
        public string? FullName { get; set; }
        public string? Class { get; set; }
        public string? School { get; set; }
        public string? Password { get; set; }
    }

    // Bước 1: Lấy dữ liệu cũ đổ vào ô nhập
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User); // Lấy user đang đăng nhập
        if (user == null) return RedirectToPage("/Login");

        Input = new InputModel
        {
            FullName = user.FullName,
            Class = user.Class,
            School = user.School,
            Password = user.Password
        };
        return Page();
    }

    // Bước 2: Lưu dữ liệu mới xuống Mumbai
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("/Login");

        user.FullName = Input!.FullName!;
        user.Class = Input.Class;
        user.School = Input.School;
        // user.Password = Input.Password!; // Đừng gán trực tiếp thế này nữa Hào nhé

        // 1. Cập nhật các thông tin cơ bản trước
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            // 2. Kiểm tra nếu Hào có nhập mật khẩu mới thì mới đổi
            if (!string.IsNullOrEmpty(Input.Password))
            {
                // Xóa mã băm cũ và đặt mã băm mới (Hash) dựa trên mật khẩu mới nhập
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, Input.Password);

                if (!passwordResult.Succeeded)
                {
                    // Xử lý lỗi nếu mật khẩu mới không đủ mạnh (ví dụ: thiếu chữ hoa, số...)
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }
        return Page();
    }
}