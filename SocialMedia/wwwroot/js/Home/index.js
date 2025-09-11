function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
const type = input.getAttribute('type');

if (type === 'password') {
    input.setAttribute('type', 'text');
    } else {
    input.setAttribute('type', 'password');
    }
}
//function loginWithGoogle() {
//    const url = 'https://localhost:7024/api/User/login/google';
//    const newWindow = window.open(url, 'GoogleLogin', 'width=500,height=600');


//}