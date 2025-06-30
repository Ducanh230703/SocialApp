using Cache;
using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models.ViewModel.Chat;
using Services;
using SocialMedia.Helper;

namespace SocialMedia.Controllers
{
    public class MessageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> GetAllMessage(int targetId,int pageNumber,int pageSize)
            {
                var token = Request.Cookies["AuthToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Chưa xác thực" });
                }

                try
                {
                    var apiReponse = await ApiHelper.GetAsync<ApiReponseModel<ChatMessageVM>>($"/api/Message/getallMessage?targetUserId={targetId}&pageNumber={pageNumber}&pageSize={pageSize}", token);

                    if (apiReponse.Status == 1 && apiReponse.Data != null)
                    {
                            return Json(apiReponse);
                    }

                    else
                    {

                        string errorMessage = apiReponse.Mess ?? "Không thể tải tin nhắn. Có lỗi từ API.";
                        return Json(new { success = false, message = errorMessage });
                    }
                }
                catch (HttpRequestException ex) 
                {
                    Console.WriteLine($"Lỗi HttpRequestException khi tải tin nhắn: {ex.Message}");
                    return Json(new { success = false, message = "Không thể kết nối đến máy chủ API. Vui lòng thử lại sau." });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi tải tin nhắn: {ex.Message}");
                    return Json(new { success = false, message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại." });
                }
            }

        public async Task<IActionResult> GetMessengerList(int pageNumber,int pageSize)
        {

            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return PartialView("_RecentMessagesPartial", new PaginatedResponse<MessengerList> { Data = new List<MessengerList>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize });
            }



            try
            {
                // Gọi API Backend với các tham số phân trang
                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<MessengerList>>>($"/api/Message/messengerlist?pageNumber={pageNumber}&pageSize={pageSize}", token);

                if (apiResponse != null && apiResponse.Status == 1 && apiResponse.Data != null)
                {
                    return PartialView("_RecentMessagesPartial", apiResponse.Data);
                }
                else
                {
                    Console.WriteLine($"API returned error for GetMessengerList: {apiResponse?.Mess}");
                    return PartialView("_RecentMessagesPartial", new PaginatedResponse<MessengerList> { Data = new List<MessengerList>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling API /api/Message/messengerlist: {ex.Message}");
                return PartialView("_RecentMessagesPartial", new PaginatedResponse<MessengerList> { Data = new List<MessengerList>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize });
            }
        }
    }
}
