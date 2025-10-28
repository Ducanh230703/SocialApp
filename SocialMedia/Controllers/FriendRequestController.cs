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

    }
}
