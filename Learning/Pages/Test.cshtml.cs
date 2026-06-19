using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase.Postgrest;
using System.IO.Compression;

namespace Learning.Pages
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class TestModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly Supabase.Client _supabase;
        private readonly HttpClient _httpClient = new HttpClient();

        [BindProperty]
        public string? SelectedAnswer { get; set; }

        public int currentIndex { get; set; }
        public int currentPoint { get; set; } // Xóa khởi tạo = 0 ở đây để nhận từ tham số
        public string? url { get; set; }
        public string? Content_Test { get; set; }
        public string? Picture { get; set; }
        public string? current_Answer { get; set; }
        public List<AnswerOption> ShuffledAnswers { get; set; } = new();

        public TestModel(UserManager<User> userManager, Supabase.Client supabase)
        {
            _userManager = userManager;
            _supabase = supabase;
        }

        public async Task<IActionResult> OnGetAsync(string path, int index, int? point)
        {
            var user_Test = await _userManager.GetUserAsync(User);
            string decodedPath_Test = System.Net.WebUtility.UrlDecode(path);
            string currentTestName = Path.GetFileName(decodedPath_Test);

            // Truy vấn nhanh lên Supabase để check trùng bằng .Match
            var checkExist = await _supabase.From<ExamResult>().Match(new Dictionary<string, string>
            {
                { "student_id", user_Test?.Id ?? "" },
                { "test_name", currentTestName }
            }).Get();

            // Nếu đã tồn tại dữ liệu điểm của bài này rồi -> Đá thẳng họ ra trang kết quả hoặc trang chủ, không cho tính điểm tiếp!
            if (checkExist?.Models != null && checkExist.Models.Count > 0)
            {
                // Có thể lấy điểm cũ gửi qua hoặc chuyển thẳng về trang chủ công bố bài đã nộp
                return RedirectToPage("/Result", new { score = checkExist.Models[0].Point });
            }

            if (string.IsNullOrEmpty(path)) return RedirectToPage("/Index");

            url = path;
            currentIndex = index;
            currentPoint = point ?? 0;
            string decodedPath = System.Net.WebUtility.UrlDecode(path);

            byte[] fileData = await _httpClient.GetByteArrayAsync(decodedPath);
            using (MemoryStream ms = new MemoryStream(fileData))
            using (ZipArchive archive = new ZipArchive(ms))
            {
                ZipArchiveEntry? nameFileEntry = archive.GetEntry("name.txt");
                if (nameFileEntry == null) return Page();

                string[] listQuestions;
                using (StreamReader reader = new StreamReader(nameFileEntry.Open()))
                {
                    string content = await reader.ReadToEndAsync();
                    listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }

                if (currentIndex >= listQuestions.Length)
                    return RedirectToPage("/Result", new { score = currentPoint });

                ZipArchiveEntry? currentQS = archive.GetEntry(listQuestions[currentIndex]);
                if (currentQS != null) await LoadQuestionData(currentQS);
            }
            return Page();
        }

        private async Task LoadQuestionData(ZipArchiveEntry entry)
        {
            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                string content = await reader.ReadToEndAsync();
                string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                Content_Test = lines[1];

                var options = new List<AnswerOption>
                {
                    new AnswerOption { Value = lines[2] },
                    new AnswerOption { Value = lines[3] },
                    new AnswerOption { Value = lines[4] },
                    new AnswerOption { Value = lines[5] }
                };

                string correctKey = lines[6].Trim().ToUpper();
                int correctIdx = correctKey[0] - 'A' + 2;
                current_Answer = lines[correctIdx];

                options.Shuffle();
                ShuffledAnswers = options;

                if (lines.Length >= 8) Picture = lines[7];
            }
        }

        public async Task<IActionResult> OnPostChoice(string path, int currentIndex, int currentPoint, string correctText)
        {

            // 1. Kiểm tra đáp án câu hiện tại và cộng điểm vào biến local
            int updatedPoint = currentPoint;
            if (SelectedAnswer == correctText)
            {
                updatedPoint++;
            }

            // 2. Load lại file để kiểm tra xem có phải câu cuối không
            string decodedPath = System.Net.WebUtility.UrlDecode(path);
            byte[] fileData = await _httpClient.GetByteArrayAsync(decodedPath);
            using (MemoryStream ms = new MemoryStream(fileData))
            using (ZipArchive archive = new ZipArchive(ms))
            {
                ZipArchiveEntry? nameFileEntry = archive.GetEntry("name.txt");
                if (nameFileEntry != null)
                {
                    using StreamReader reader = new StreamReader(nameFileEntry.Open());
                    string content = await reader.ReadToEndAsync();
                    var listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    // Nếu là câu cuối cùng
                    if (currentIndex >= listQuestions.Length - 1)
                    {
                        var user = await _userManager.GetUserAsync(User);
                        double finalScore = Math.Round(((double)10 / listQuestions.Length) * updatedPoint, 2);

                        // TẠO ĐỐI TƯỢNG BẰNG CÁCH KHỞI TẠO MỚI TINH ĐỂ TRÁNH XUNG ĐỘT BASEMODEL
                        var finalResult = new ExamResult
                        {
                            StudentName = user?.FullName ?? "Học sinh ẩn danh",
                            ClassName = user?.Class ?? "Không rõ lớp",
                            TestName = Path.GetFileName(decodedPath),
                            Student_Id = user?.Id ?? "Không rõ ID", // Đã sửa chữ H viết thường
                            Point = finalScore
                        };

                        // Ghi điểm bằng QueryOptions và ReturnType.Representation cực kỳ chuẩn bài
                        await _supabase.From<ExamResult>().Insert(finalResult, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

                        return RedirectToPage("/Result", new { score = finalScore });
                    }
                }
            }

            // Chưa hết thì sang câu tiếp theo với số điểm đã cập nhật
            return RedirectToPage(new { path = path, index = currentIndex + 1, point = updatedPoint });
        }
    }

    public class AnswerOption
    {
        public string? Value { get; set; }
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