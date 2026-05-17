using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Learning.Models
{
    public class User : IdentityUser
    {
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