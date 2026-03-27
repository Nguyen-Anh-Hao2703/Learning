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
    {private readonly UserManager<User> _userManager;
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
                currentUserName = user.UserName;
                FullName = user.FullName;
                studentClass = user.Class; // Lấy cột Class trực tiếp từ model User của cậu
            }
            if (string.IsNullOrEmpty(path)) return RedirectToPage("/Index");

            // GIẢI MÃ URL: Biến các ký tự %3A, %2F thành : và /
            string decodedPath = System.Net.WebUtility.UrlDecode(path);
            currentIndex = index;
            currentPoint = point ?? 0;
            url = path; // Lấy cái link từ Supabase
            string extension = Path.GetExtension(url);
            string[] listQuestions = Array.Empty<string>();
            int totalQuestions = listQuestions.Length;
            if(extension == ".qs")
            {
                if (string.IsNullOrEmpty(decodedPath)) return RedirectToPage();

                // 1. Tải file từ URL về bộ nhớ đệm (Byte Array)
                byte[] fileData = await _httpClient.GetByteArrayAsync(decodedPath);

                // 2. Mở "luồng" bộ nhớ để đọc dữ liệu
                using (MemoryStream ms = new MemoryStream(fileData))
                {
                    // 3. Sử dụng ZipArchive để mở file .qs ngay trong RAM
                    using (ZipArchive archive = new ZipArchive(ms))
                    {
                        // 4. Tìm file "name.txt" bên trong file nén
                        ZipArchiveEntry? nameFileEntry = archive.GetEntry("name.txt");
                        if (nameFileEntry != null)
                        {
                            // Đọc nội dung file name.txt
                            using (StreamReader reader = new StreamReader(nameFileEntry.Open()))
                            {
                                string content = await reader.ReadToEndAsync();
                                // Cắt ra danh sách các file câu hỏi (.slq)
                                listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                                // Giờ cậu có thể dùng listQuestions[0] để đi tải tiếp file câu hỏi rồi!
                                Console.WriteLine("Tìm thấy " + listQuestions.Length + " câu hỏi.");
                            }
                        }
                        ZipArchiveEntry? nameFileSLQ = archive.GetEntry(listQuestions[currentIndex]);
                        if (nameFileSLQ != null)
                        {
                             await Load(nameFileSLQ);
                        }
                    }
                }
            }
            if (currentIndex >= totalQuestions)
            {
                // 1. Tạo đối tượng kết quả từ model ExamResult mình vừa sửa
                var finalResult = new ExamResult
                {
                    StudentName = FullName,    // Tên học sinh lấy từ Identity
                    ClassName = studentClass,  // Lớp lấy từ Identity
                    TestName = path,           // Tên file bài tập (.qs)
                    Point = currentPoint       // Số điểm vừa tính được
                };

                try
                {
                    // 2. Lệnh Insert thần thánh vào Supabase
                    await _supabase.From<ExamResult>().Insert(finalResult);

                    // 3. Xong thì cho các em sang trang chúc mừng
                    return RedirectToPage("/Result", new { score = currentPoint });
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi (ví dụ chưa tạo bảng trên web), nó sẽ hiện ở đây
                    Console.WriteLine("Lỗi lưu điểm: " + ex.Message);
                }
            }
            return Page();
        }
        public async Task Load(ZipArchiveEntry? nameFileSLQ)
        {
            // Tải toàn bộ nội dung file về dưới dạng chuỗi (string)
            using (StreamReader reader = new StreamReader(nameFileSLQ.Open()))
            {
                string content = await reader.ReadToEndAsync();
                // Cắt ra danh sách các file câu hỏi (.slq)
                string [] listQuestions = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
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
        public async Task<IActionResult> OnPostChoiceAsync(int currentIndex)
        {
            // 1. Lưu đáp án câu hiện tại (currentIndex) vào đâu đó (Session/Database)
            string userChoice = SelectedAnswer;
            // Lưu tạm vào TempData để trang sau có thể lấy ra tính điểm
            if(userChoice == current_Answer)
            {
                currentPoint++;
            }

            // 3. Quay lại trang Get với chỉ số mới và cái link file cũ
            // name ở đây chính là cái URL file .qs mà Hào nhận được lúc đầu
            return RedirectToPage(new { name = Request.Query["url"], index = currentIndex++ , point = currentPoint});
        }
    }
}
