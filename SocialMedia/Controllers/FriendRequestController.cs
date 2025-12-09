using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Friend;
using SocialMedia.Helper;
using System.Threading.Tasks;

namespace SocialMedia.Controllers
{
    public class FriendRequestController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            try
            {
                var apiRepone = await ApiHelper.GetAsync<ApiReponseModel<FriendShipVM>>("/api/FriendRequest/friendindex?pageNumber=1&pageSize=5", token);
                if (apiRepone.Status == 1)
                    return View(apiRepone.Data);
                else return BadRequest("Lỗi call APi");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gọi API: "+ex);
            }
            return BadRequest("Lỗi API");
        }

        [HttpPost]
        public async Task<IActionResult> FriendRequest([FromBody]FriendRequestVM friendRequest)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            var apiReponse = await ApiHelper.PostAsync<FriendRequestVM, ApiReponseModel>("api/FriendRequest/friendrequest",friendRequest, token);

            if(apiReponse != null)
            {
                return Json(apiReponse);
            }
            else
            {
                return BadRequest("Lỗi nào đó");
            }
        }

        [HttpPost]
        public async Task<IActionResult> FriendAnswer([FromBody] FriendAnswer friendRequest)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            var apiReponse = await ApiHelper.PostAsync<FriendAnswer, ApiReponseModel>("api/FriendRequest/friendanswer", friendRequest, token);

            if (apiReponse != null)
            {
                return Json(apiReponse);
            }
            else
            {
                return BadRequest("Lỗi Api");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchResult(string stringSearch)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            ViewData["SearchQuery"] = stringSearch;
            var results = new List<SearchResult>();

            if (!string.IsNullOrEmpty(stringSearch))
            {
                try
                {
                    var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<List<SearchResult>>>(
                        $"/api/FriendRequest/search?stringSearch={stringSearch}", token);

                    if (apiResponse != null && apiResponse.Status == 1 && apiResponse.Data != null)
                    {
                        results = apiResponse.Data;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = apiResponse?.Mess ?? "Không thể tải kết quả tìm kiếm.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tìm kiếm.";
                }
            }

            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjaxRequest)
                return PartialView("~/Views/FriendRequest/_SearchResult.cshtml", results);
            else
                return View("~/Views/FriendRequest/SearchResult.cshtml", results);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreFriend([FromQuery] int pageNumber = 2, [FromQuery] int pageSize = 5)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { status = 0, message = "Chưa xác thực" });
            }

            var apiReponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<FriendListVM>>>($"/api/FriendRequest/loadmorefr?pageNumber={pageNumber}&pageSize={pageSize}", token);

            if (apiReponse != null && apiReponse.Data != null)
            {
                return Json(apiReponse.Data.Data);
            }
            else
            {
                return BadRequest(new { status = -1, message = "Lỗi Api khi tải thêm bạn bè." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadOnlineFriend()
        {
            ApiReponseModel<List<FriendListVM>> apiReponse = null;
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { status = 0, message = "Chưa xác thực" });
            }

            try
            {
                apiReponse = await ApiHelper.GetAsync<ApiReponseModel<List<FriendListVM>>>("/api/FriendRequest/friendonline", token);

                if (apiReponse.Status > 0)
                    return Json(apiReponse);
                else return Json(apiReponse);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error {ex}");
                return Json(apiReponse);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendsListForModal([FromQuery] int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { status = 0, message = "Bạn cần đăng nhập để xem danh sách bạn bè." });
            }

            try
            {
                // Gửi yêu cầu đến API để lấy danh sách bạn bè
                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<FriendListVM>>>(
                    $"/api/FriendRequest/loadmorefr?pageNumber={pageNumber}&pageSize={pageSize}", token);

                if (apiResponse.Status == 1 && apiResponse.Data != null)
                {
                    // Truyền userId của profile vào ViewBag để nút Tải thêm có thể sử dụng
                    ViewBag.ProfileUserId = userId;

                    // Trả về Partial View chứa danh sách bạn bè
                    return PartialView("~/Views/User/_FriendsListContent.cshtml", apiResponse.Data);
                }
                else
                {
                    // Trả về dữ liệu rỗng nếu có lỗi hoặc không có dữ liệu
                    var emptyData = new PaginatedResponse<FriendListVM> { Data = new List<FriendListVM>(), PageNumber = pageNumber, PageSize = pageSize};
                    return PartialView("~/Views/User/_FriendsListContent.cshtml", emptyData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tải danh sách bạn bè: {ex}");
                var emptyData = new PaginatedResponse<FriendListVM> { Data = new List<FriendListVM>(), PageNumber = pageNumber, PageSize = pageSize};
                return PartialView("~/Views/User/_FriendsListContent.cshtml", emptyData);
            }
        }

    }
}
