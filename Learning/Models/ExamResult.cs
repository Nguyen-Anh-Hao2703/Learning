using Supabase.Postgrest.Attributes; // Thêm dòng này
using Supabase.Postgrest.Models;     // Thêm dòng này để hết lỗi BaseModel
using Supabase.Postgrest;
using System;

namespace Learning.Models // Đảm bảo có namespace để bên trang Test gọi được
{
    [Table("ExamResults")]
    public class ExamResult : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("student_name")]
        public string? StudentName { get; set; }

        [Column("class_name")]
        public string? ClassName { get; set; }

        [Column("test_name")]
        public string? TestName { get; set; }

        [Column("point")]
        public double Point { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}