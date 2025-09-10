function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
const type = input.getAttribute('type');

if (type === 'password') {
    input.setAttribute('type', 'text');
    } else {
    input.setAttribute('type', 'password');
    }
}
function loginWithGoogle() {
    const url = 'https://localhost:7024/api/User/login/google';
    const newWindow = window.open(url, 'GoogleLogin', 'width=500,height=600');

    window.addEventListener('message', (event) => {
        // Kiểm tra nguồn gốc của message và đảm bảo có token
        if (event.origin === 'https://localhost:70́́80' && event.data.token) {
            document.cookie = `AuthToken=${event.data.token}; path=/;max-age=3600;`;

            // Chuyển hướng người dùng đến trang chính
            window.location.href = '/Home/Index';
        }
    });
}