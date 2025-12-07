using Microsoft.AspNetCore.Mvc;

namespace SocialMedia.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult AccessDenied()
        {
            // Thiết lập mã trạng thái HTTP là 403 Forbidden 
            // Điều này quan trọng cho SEO và bảo mật
            Response.StatusCode = 403;

            // Có thể thêm ViewData để thay đổi tiêu đề trang nếu cần
            ViewData["Title"] = "Không có quyền truy cập";

            return View();
        }
    }
}
