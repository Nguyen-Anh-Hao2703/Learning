using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Learning.Models
{
    public class User : IdentityUser
    {
        [Key] // Khóa chính tự tăng
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; } = string.Empty;

        // Phân quyền: "Teacher" hoặc "Student"
        public string Role { get; set; } = "";
        public string? School { get; set; }
        public string? Class { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}