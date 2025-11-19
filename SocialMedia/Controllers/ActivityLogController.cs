using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using SocialMedia.Helper;

namespace SocialMedia.Controllers
{
    public class ActivityLogController : Controller
    {


        public async Task<IActionResult> Index([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Authentication");

            // Không cần lấy LoggedInUserId vì GetAllLogs API thường không yêu cầu ID ở URL query
            // Nhưng nếu API của bạn yêu cầu, cần phải chỉnh sửa logic gọi API.

            try
            {
                // Giả định ApiHelper được sử dụng để gọi API Controller
                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<ActivityLogResponse>>>(
                    $"/api/ActivityLog/getall?pageNumber={pageNumber}&pageSize={pageSize}", token);

                // Truyền kết quả phản hồi API vào View
                return View(apiResponse);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống: Không thể tải Activity Log. {ex.Message}";
                return View(new ApiReponseModel<PaginatedResponse<ActivityLogResponse>>
                {
                    Status = 0,
                    Mess = "Không thể kết nối đến máy chủ API.",
                    Data = new PaginatedResponse<ActivityLogResponse>()
                });
            }
        }
    }
}