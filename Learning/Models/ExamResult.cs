using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace Learning.Models
{
    [Table("ExamResults")]
    public class ExamResult : BaseModel
    {
        [PrimaryKey("id", false)] // Cột id là khóa chính tự tăng (int8), cần dùng thuộc tính PrimaryKey
        public int Id { get; set; }

        [Column("student_name")]
        public string? StudentName { get; set; }

        [Column("class_name")]
        public string? ClassName { get; set; }

        [Column("test_name")]
        public string? TestName { get; set; }

        // Đổi thành double (không nullable) để khi làm bài xong ghi điểm số thực (ví dụ: 9.5) không bị lỗi map kiểu float8
        [Column("point")]
        public double Point { get; set; }

        [Column("student_id")]
        public string Student_Id { get; set; } = "";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow; // Supabase dùng giờ chuẩn UTC
    }
}