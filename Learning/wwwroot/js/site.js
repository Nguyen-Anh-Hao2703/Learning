// site.js
console.log("JS đã chạy!");

document.addEventListener("DOMContentLoaded", function () {
    const quizForm = document.querySelector('form');

    if (quizForm) {
        quizForm.addEventListener('submit', function (event) {
            // 1. Hiển thị Spinner ngay lập tức
            const loading = document.getElementById('loading');
            const text = document.getElementById('btnText');

            if (loading) {
                loading.style.setProperty('display', 'inline-block', 'important');
                loading.classList.add('active'); // Thêm class nếu cần
            }
            if (text) text.innerText = "Đang nộp bài...";

            // 2. Vô hiệu hóa nút để tránh nhấn lần 2
            const btn = document.getElementById('btnNext');
            if (btn) {
                btn.style.opacity = '0.5';
                btn.style.pointerEvents = 'none';
            }

            // C# sẽ tự động chạy tiếp sau khi JS thực hiện xong các lệnh trên
            console.log("JS đã kích hoạt xong, giờ đợi C# xử lý!");
        });
    }
});