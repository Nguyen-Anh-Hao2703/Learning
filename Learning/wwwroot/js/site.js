// site.js
console.log("JS đã chạy!");

document.addEventListener("DOMContentLoaded", function () {
    const quizForm = document.querySelector('form');
    if (quizForm) {
        quizForm.addEventListener('submit', function () {
            const spin = document.getElementById('loading');
            const txt = document.getElementById('btnText');
            const btn = document.getElementById('btnNext');

            if (spin) {
                // Thêm class active để hiện spinner
                spin.classList.remove('d-none');
                spin.classList.add('active');
            }
            if (txt) {
                txt.innerText = "Đang kiểm tra bài...";
            }
            if (btn) {
                // Khóa nút để không cho nhấn lần 2
                btn.classList.add('disabled');
                btn.style.pointerEvents = 'none';
            }

            console.log("Spinner đã được kích hoạt!");
        });
    }
});