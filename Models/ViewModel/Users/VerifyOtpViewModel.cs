using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Users
{
    public class VerifyOtpViewModel
    {
        // Dùng để chứa email đã đăng ký thành công (ẩn trên form)
        public string Email { get; set; }

        // Dùng để nhận mã OTP người dùng nhập vào
        public string OtpCode { get; set; }

        // Dùng để hiển thị thông báo lỗi/thành công
        public string Message { get; set; }
    }
}
