// site.js
console.log("JS đã chạy!");

document.addEventListener("DOMContentLoaded", function () {
    const quizForm = document.querySelector('form');

    if (quizForm) {
        quizForm.addEventListener('submit', function (event) {
            // 1. Tạm dừng việc gửi form lên Server
            event.preventDefault();

            const spin = document.getElementById('loading');
            const txt = document.getElementById('btnText');
            const btn = document.getElementById('btnNext');

            // 2. Kích hoạt hiệu ứng ngay lập tức
            if (spin) spin.style.setProperty('display', 'inline-block', 'important');
            if (txt) txt.innerText = "Đang chấm điểm...";
            if (btn) btn.style.pointerEvents = 'none';

            console.log("JS đã chiếm quyền, đang chuẩn bị trả cho C#...");

            // 3. Đợi 100ms để trình duyệt kịp vẽ spinner rồi mới nộp form thật sự
            setTimeout(() => {
                quizForm.submit();
            }, 100);
        });
    }
});