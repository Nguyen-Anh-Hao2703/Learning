// site.js
console.log("JS đã chạy!");

document.addEventListener("DOMContentLoaded", function () {
    const quizForm = document.querySelector('form');
    if (quizForm) {
        quizForm.addEventListener('submit', function () {
            const btn = document.getElementById('btnNext');
            const txt = document.getElementById('btnText');

            if (btn) {
                // Thêm class để kích hoạt CSS hiển thị spinner
                btn.classList.add('is-loading');
                // Vô hiệu hóa để tránh nhấn nhiều lần
                btn.disabled = true;
            }
            if (txt) {
                txt.innerText = "Đang nộp bài...";
            }
        });
    }
});