using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Users;
using Newtonsoft.Json;
using SocialMedia.Helper;
using System.Net.Http.Headers;

namespace SocialMedia.Controllers
{
    public class UserController : Controller
    {

        [HttpGet]
        public async Task<IActionResult> Details(int userId, int pageNumber = 1, int pageSize = 2) 
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Authentication");
            }

            int loggedInUserId = 0;
            var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
            if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
            {
                loggedInUserId = parsedUserId;
            }         

            if (loggedInUserId != userId)
            {
                var statusfriend = await ApiHelper.GetAsync<ApiReponseModel<FriendRequestRP>>($"api/FriendRequest/getstatus?loggedInUserId={loggedInUserId}&profileUserId={userId}", token);
                if (statusfriend.Data != null)
                {
                    ViewBag.FriendshipStatus = statusfriend.Data.Status;
                    ViewBag.SenderId = statusfriend.Data.SenderID;
                    ViewBag.ID = statusfriend.Data.ID;
                }
                else
                {
                    ViewBag.FriendshipStatus = -1;
                    ViewBag.SenderId = null;
                }
            }

            ViewBag.LoggedInUserId = loggedInUserId;
            ViewBag.ProfileUserId = userId;

            var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<UserInfo>>($"/api/User/Details?userId={userId}&pageNumber={pageNumber}&pageSize={pageSize}", token);

            if (apiResponse != null && apiResponse.Status == 1 && apiResponse.Data != null)
            {
                return View(apiResponse.Data);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
 
        [HttpGet]
        public async Task<IActionResult> GetMoreUserPosts(int userId, int pageNumber = 1, int pageSize = 3)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<UserInfo>>($"/api/User/Details?userId={userId}&pageNumber={pageNumber}&pageSize={pageSize}", token);

            if (apiResponse != null && apiResponse.Status == 1 && apiResponse.Data?.ListPost != null)
            {
                return PartialView("_PostList", apiResponse.Data.ListPost.Data);
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetMoreUserPostsIndex(int pageNumber = 1, int pageSize = 3)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            int loggedInUserId = 0;
            var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
            if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
            {
                loggedInUserId = parsedUserId;
            }

            var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<PostFull>>>($"/api/User/getmorepostindex?pageNumber={pageNumber}&pageSize={pageSize}", token);

            if (apiResponse != null && apiResponse.Status == 1)
            {
                ViewBag.LoggedInUserId = loggedInUserId;
                return PartialView("_PostList", apiResponse.Data.Data);
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetInfo()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            var apiReponse = await ApiHelper.GetAsync<ApiReponseModel<EditInfo>>("/api/User/getedit", token);

            return PartialView("MyAcc", apiReponse.Data);
        }

        [HttpPost]
        public async Task<IActionResult> UpAvatar(IFormCollection avaFile)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }
            var image = avaFile.Files.GetFile("image");
            string removeUrlFromForm = avaFile["removeUrl"].ToString();

            if (image == null)
            {
                return BadRequest(new ApiReponseModel { Status = 0, Mess = "Không có tệp ảnh nào được tải lên." });
            }

            using (var formData = new MultipartFormDataContent())
            {
                var fileContent = new StreamContent(image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                formData.Add(fileContent, "image", image.FileName);

                formData.Add(new StringContent(removeUrlFromForm ?? ""), "removeUrl");

                try
                {
                    var response = await ApiHelper.PostFormAsync<ApiReponseModel>("/api/User/upavatar", formData, token);

                    if (response != null)
                        return Json(response);
                    else
                        return BadRequest("Lỗi Api");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new ApiReponseModel { Status = 0, Mess = $"Lỗi nội bộ khi gọi API: {ex.Message}" });
                }
            } 
        }

        [HttpPost]
        public async Task<IActionResult> EditInfo(EditInfo editInfo)
        {
            try
            {
                var token = Request.Cookies["AuthToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized();
                }

                    var apiReponse = await ApiHelper.PostAsync<EditInfo,ApiReponseModel>("/api/User/editinfo",editInfo, token);

                if (apiReponse.Status == 1)
                {
                    return Ok(apiReponse);
                }
                else
                {
                    return Ok(apiReponse);

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in EditInfo POST action: {ex.Message}");
                Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Đã xảy ra lỗi nội bộ khi xử lý yêu cầu: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOnline(int userId)
        {
            var token = Request.Cookies["AuthToken"];

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            try
            {
                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<UserOnline>>($"/api/User/getuser/{userId}", token);

                if (apiResponse == null)
                {
                    return StatusCode(500, new { Status = -1, Message = "Không nhận được phản hồi từ dịch vụ API nội bộ." });
                }

                return Json(apiResponse);
            }
            catch (HttpRequestException httpEx)
            {
                Console.Error.WriteLine($"[GetUserOnline] HTTP Request Exception: {httpEx.Message}");
                Console.Error.WriteLine($"[GetUserOnline] Stack Trace: {httpEx.StackTrace}");
                return StatusCode(503, new { Status = -2, Message = "Không thể kết nối đến dịch vụ người dùng. Vui lòng thử lại sau." });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GetUserOnline] General Exception: {ex.Message}"); 
                Console.Error.WriteLine($"[GetUserOnline] Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { Status = -3, Message = $"Đã xảy ra lỗi nội bộ khi xử lý yêu cầu: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordModel model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return RedirectToAction("Login", "Authentication");
            }

            var rs = await ApiHelper.PostAsync<ChangePasswordModel, ApiReponseModel>("/api/User/change-password", model, token);

            return Json(rs);
        }


        [HttpPost]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailModel model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Message"] = "Bạn cần đăng nhập để thay đổi email.";
                return RedirectToAction("Login", "Authentication");
            }

            var rs = await ApiHelper.PostAsync<ChangeEmailModel, ApiReponseModel>("/api/User/change-email", model, token);
            return Json(rs);

        }

        [HttpPost]
        public async Task<IActionResult> VerifyChangeEmail([FromBody] VerifyChangeEmailModel model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            var rs = await ApiHelper.PostAsync<VerifyChangeEmailModel, ApiReponseModel>("/api/User/verify-change-email", model, token);
            return Json(rs);
        }
    }
};