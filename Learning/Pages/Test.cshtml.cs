using Learning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public int currentPoint { get; set; }
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

        // Đã thêm async Task ở đây để chạy được Supabase
        public async Task<IActionResult> OnPostChoice(string path, int currentIndex, int currentPoint, string correctText)
        {
            // 1. Cộng điểm ngay lập tức
            if (SelectedAnswer == correctText)
            {
                currentPoint++;
            }

            // 2. Kiểm tra câu cuối
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

                    if (currentIndex >= listQuestions.Length - 1)
                    {
                        var user = await _userManager.GetUserAsync(User);
                        double finalScore = Math.Round(((double)10 / listQuestions.Length) * currentPoint, 2);

                        var finalResult = new ExamResult
                        {
                            StudentName = user?.FullName ?? "Học sinh ẩn danh",
                            ClassName = user?.Class ?? "Không rõ lớp",
                            TestName = Path.GetFileName(decodedPath),
                            Point = finalScore
                        };

                        await _supabase.From<ExamResult>().Insert(finalResult);
                        return RedirectToPage("/Result", new { score = finalScore });
                    }
                }
            }

            return RedirectToPage(new { path = path, index = currentIndex + 1, point = currentPoint });
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