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

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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
        private static Random rng = new Random();
        public List<AnswerOption> ShuffledAnswers { get; set; } = new();

        // Inject cả userManager và supabase vào
        public TestModel(UserManager<User> userManager, Supabase.Client supabase)
        {
            _userManager = userManager;
            _supabase = supabase;
        }
        public async Task<IActionResult> OnGetAsync(string path, int index, int? point)
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
                        //Ép kiểu tường minh (Chắc chắn nhất)
                        double score = ((double)10 / totalQuestions) * currentPoint;
                        // Làm tròn đến 2 chữ số thập phân (ví dụ: 6.67)
                        double finalScore = Math.Round(score, 2);
                        var finalResult = new ExamResult
                        {
                            StudentName = user?.FullName ?? "Học sinh ẩn danh", // Không được để null
                            ClassName = user?.Class ?? "Không rõ lớp",
                            TestName = Path.GetFileName(decodedPath), // Chỉ lấy tên file cho ngắn gọn
                            Point = finalScore
                        };
                        await _supabase.From<ExamResult>().Insert(finalResult);
                        return RedirectToPage("/Result", new { score = finalScore });
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
            using (StreamReader reader = new StreamReader(nameFileSLQ!.Open()))
            {
                string content = await reader.ReadToEndAsync();
                string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Lưu nội dung câu hỏi
                Content_Test = lines[1];

                // Tạo danh sách đáp án để xáo trộn
                var options = new List<AnswerOption>
                {
                    new AnswerOption { Key = "A", Value = lines[2] },
                    new AnswerOption { Key = "B", Value = lines[3] },
                    new AnswerOption { Key = "C", Value = lines[4] },
                    new AnswerOption { Key = "D", Value = lines[5] }
                };

                // Lưu đáp án đúng thực tế (Nội dung văn bản)
                string correctKey = lines[6]; // Ví dụ: "A"
                int correctIdx = correctKey[0] - 'A' + 2; // Chuyển A->2, B->3...
                current_Answer = lines[correctIdx]; // Đây là nội dung text của câu đúng

                // Xáo trộn
                options.Shuffle();
                ShuffledAnswers = options;

                // Xử lý hình ảnh
                if (lines.Length >= 8) Picture = lines[7];
            }
        }
        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IActionResult> OnPostChoiceAsync(string path, int currentIndex, int currentPoint, string correctText)
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Giải thích: Hào truyền 'correctText' (nội dung câu đúng) từ Form thay vì so sánh A, B, C
            if (SelectedAnswer == correctText)
            {
                currentPoint++;
            }

            return RedirectToPage(new { path = path, index = currentIndex + 1, point = currentPoint });
        }
    }
    public class AnswerOption
    {
        public string? Key { get; set; }    // A, B, C, hoặc D
        public string? Value { get; set; }  // Nội dung thực tế: "Con bò", "Con gà"...
    }

    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
