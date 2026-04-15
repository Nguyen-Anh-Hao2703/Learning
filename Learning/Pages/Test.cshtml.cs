using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;

namespace Learning.Pages
{

    public class TestModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        [BindProperty]
        public string? SelectedAnswer { get; set; } // Biến này sẽ lưu đáp án cuối cùng
        private readonly HttpClient _httpClient = new HttpClient();
        public int currentIndex = 0;
        public string? Picture;
        public string? Content_Test;
        public string? Answer_A;
        public string? Answer_B;
        public string? Answer_C;
        public string? Answer_D;
        public string? url;
        public string? User_Answer;
        public string? content;
        public string? current_Answer;
        public int currentPoint;
        public string? studentClass;
        public string? currentUserName;
        public string? FullName;
        public string[]? data;
        public string[]? data_list_question;
        private readonly Supabase.Client _supabase; // Khai báo ở đây

        // Inject cả userManager và supabase vào
        public TestModel(UserManager<User> userManager, Supabase.Client supabase)
        {
            _userManager = userManager;
            _supabase = supabase;
        }
        public async Task<IActionResult> OnGet(string path, int index, int? point)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                currentUserName = user.UserName ?? "Tài khoản không xác định";
                FullName = user.FullName ?? "Học sinh ẩn danh";
                studentClass = user.Class ?? "Không rõ lớp";
            }

            if (string.IsNullOrEmpty(path)) return RedirectToPage("/Index");

            string decodedPath = System.Net.WebUtility.UrlDecode(path);
            currentIndex = index;
            currentPoint = point ?? 0;
            url = path;

            string[] listQuestions = Array.Empty<string>();

            if (Path.GetExtension(decodedPath).ToLower() == ".qs")
            {
                byte[] fileData = await _httpClient.GetByteArrayAsync(decodedPath);
                using (MemoryStream ms = new MemoryStream(fileData))
                using (ZipArchive archive = new ZipArchive(ms))
                {
                    ZipArchiveEntry? nameFileEntry = archive.GetEntry("name.txt");
                    if (nameFileEntry != null)
                    {
                        using (StreamReader reader = new StreamReader(nameFileEntry.Open()))
                        {
                            string content = await reader.ReadToEndAsync();
                            listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }

                    // Cập nhật totalQuestions Ở ĐÂY sau khi đã đọc xong file
                    int totalQuestions = listQuestions.Length;

                    // KIỂM TRA: Đã hết câu hỏi chưa?
                    if (currentIndex >= totalQuestions && totalQuestions > 0)
                    {
                        // Đảm bảo lấy lại thông tin user trước khi insert
                        user = await _userManager.GetUserAsync(User);
                        var finalResult = new ExamResult
                        {
                            StudentName = user?.FullName ?? "Học sinh ẩn danh", // Không được để null
                            ClassName = user?.Class ?? "Không rõ lớp",
                            TestName = Path.GetFileName(decodedPath), // Chỉ lấy tên file cho ngắn gọn
                            Point = currentPoint
                        };
                        await _supabase.From<ExamResult>().Insert(finalResult);
                        return RedirectToPage("/Result", new { score = currentPoint });
                    }

                    // Nếu còn câu hỏi, load câu hỏi hiện tại
                    if (currentIndex < totalQuestions && currentIndex >= 0)
                    {
                        ZipArchiveEntry? nameFileSLQ = archive.GetEntry(listQuestions[currentIndex]);
                        if (nameFileSLQ != null) await Load(nameFileSLQ);
                    }
                }
            }
            return Page();
        }
        public async Task Load(ZipArchiveEntry? nameFileSLQ)
        {
            // Tải toàn bộ nội dung file về dưới dạng chuỗi (string)
            using (StreamReader reader = new StreamReader(nameFileSLQ!.Open()))
            {
                string content = await reader.ReadToEndAsync();
                // Cắt ra danh sách các file câu hỏi (.slq)
                string[] listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                Content_Test = listQuestions[1] ?? "Bạn đã hoàn thành bài";
                Answer_A = listQuestions[2] ?? "";
                Answer_B = listQuestions[3] ?? "";
                Answer_C = listQuestions[4] ?? "";
                Answer_D = listQuestions[5] ?? "";
                current_Answer = listQuestions[6] ?? "";
                if (listQuestions.Length == 8)
                {
                    string path = listQuestions[7] ?? "iVBORw0KGgoAAAANSUhEUgAAAZAAAABLAQMAAACAYf7kAAAABlBMVEUAAAD///+l2Z/dAAAAAXRSTlMAQObYZgAAAFRJREFUeAFjYBgFo2AUjIJRMApGwSigP6ChvYGBmYGB8QCG9v8P6P//f4CB+QCG9v8f0P///wEG5gMY2v9/QP///wcYmA9gaP9/QP///x9gYBgFAwYA6CEp96B79mYAAAAASUVORK5CYII=";
                    if (path != null)
                    {
                        Picture = path;
                    }
                }
            }
        }
        public async Task<IActionResult> OnPostChoiceAsync(string path, int currentIndex, int currentPoint)
        {
            if (string.IsNullOrEmpty(path)) return RedirectToPage("/Index");

            // 1. Tải lại file để lấy đáp án đúng (Vì HTTP là không trạng thái)
            string decodedPath = System.Net.WebUtility.UrlDecode(path);
            byte[] fileData = await _httpClient.GetByteArrayAsync(decodedPath);
            using (MemoryStream ms = new MemoryStream(fileData))
            using (ZipArchive archive = new ZipArchive(ms))
            {
                ZipArchiveEntry? nameFileEntry = archive.GetEntry("name.txt");
                if (nameFileEntry != null)
                {
                    using (StreamReader reader = new StreamReader(nameFileEntry.Open()))
                    {
                        string content = await reader.ReadToEndAsync();
                        var listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // Lấy file câu hỏi hiện tại để so đáp án
                        ZipArchiveEntry? currentQS = archive.GetEntry(listQuestions[currentIndex]);
                        if (currentQS != null)
                        {
                            using (StreamReader qsReader = new StreamReader(currentQS.Open()))
                            {
                                string qsContent = await qsReader.ReadToEndAsync();
                                var lines = qsContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                                string correctAnswer = lines[6]; // Đáp án đúng nằm ở dòng 7

                                if (SelectedAnswer == correctAnswer)
                                {
                                    currentPoint++;
                                }
                            }
                        }
                    }
                }
            }

            // 2. Chuyển sang câu tiếp theo
            return RedirectToPage(new { path = path, index = currentIndex + 1, point = currentPoint });
        }
    }
}
