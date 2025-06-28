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
        public async Task<IActionResult> SearchResult(string stringSearch, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (string.IsNullOrEmpty(token))
            {
                if (isAjaxRequest)
                {
                    return PartialView("~/Views/FriendRequest/_SearchResult.cshtml", new PaginatedResponse<SearchResult> { Data = new List<SearchResult>(), TotalCount = 0 });
                }
                else
                {
                    ViewData["SearchQuery"] = stringSearch;
                    ViewData["PageNumber"] = 1;
                    ViewData["PageSize"] = 5;
                    TempData["ErrorMessage"] = "Bạn cần đăng nhập để sử dụng tính năng này.";
                    return View("~/Views/FriendRequest/SearchResult.cshtml", new PaginatedResponse<SearchResult> { Data = new List<SearchResult>(), TotalCount = 0 });
                }
            }

            ViewData["SearchQuery"] = stringSearch;
            ViewData["PageNumber"] = pageNumber;
            ViewData["PageSize"] = pageSize;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            PaginatedResponse<SearchResult> results = new PaginatedResponse<SearchResult> { Data = new List<SearchResult>(), TotalCount = 0 };

            if (!string.IsNullOrEmpty(stringSearch))
            {
                try
                {
                    var apiReponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<SearchResult>>>($"/api/FriendRequest/search?stringSearch={stringSearch}&pageNumber={pageNumber}&pageSize={pageSize}", token);

                    if (apiReponse != null && apiReponse.Status == 1 && apiReponse.Data != null)
                    {
                        results = apiReponse.Data;
                    }
                    else
                    {
                        Console.WriteLine($"API error or no data for search. Status: {apiReponse?.Status}, Message: {apiReponse?.Mess}");
                        TempData["ErrorMessage"] = apiReponse?.Mess ?? "Không thể tải kết quả tìm kiếm.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception when calling search API: {ex.Message}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tìm kiếm. Vui lòng thử lại.";
                }
            }

            if (isAjaxRequest)
            {
                return PartialView("~/Views/FriendRequest/_SearchResult.cshtml", results);
            }
            else
            {
                return View("~/Views/FriendRequest/SearchResult.cshtml", results);
            }
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
