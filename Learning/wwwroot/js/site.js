// site.js
console.log("JS đã chạy!");

document.addEventListener("DOMContentLoaded", function () {
    const quizForm = document.querySelector('form');
    if (quizForm) {
        quizForm.addEventListener('submit', function () {
            const spin = document.getElementById('loading');
            const txt = document.getElementById('btnText');

            if (spin) {
                // Ép hiện bằng mọi giá
                spin.style.setProperty('display', 'inline-block', 'important');
            }
            if (txt) txt.innerText = "Đang kiểm tra...";

            // Khóa nút để tránh spam
            document.getElementById('btnNext').style.pointerEvents = 'none';
        });
    }
});