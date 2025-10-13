using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ResponseModels;
using Models.ViewModel.Group;
using Models.ViewModel.Home;
using SocialMedia.Helper;
using System.Net.Http.Headers;

namespace SocialMedia.Controllers
{
    public class GroupController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Authentication");
            try
            {
                int loggedInUserId = 0;

                var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
                if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
                {
                    loggedInUserId = parsedUserId;
                }
                ViewBag.LoggedInUserId = loggedInUserId;


                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<List<Group>>>("/api/Group/listgr", token);
                
                 if (apiResponse != null && apiResponse.Status == 1)
                {
                    return View(apiResponse.Data);
                }
                else
                {
                    return View(new List<Group>());
                }
            }
            catch (Exception ex)
            {

                return View(new List<Group>());
            }
        }

        public async Task<IActionResult> CreateGroup(CreateGroupForm group)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Authentication");

            try
            {
                List<string> imageUrls = new();
                string uploadResponse = null;
                if (group.Image != null)
                {
                    var form = new MultipartFormDataContent();
                    var stream = group.Image.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(group.Image.ContentType);
                    form.Add(fileContent, "images", group.Image.FileName);
                    uploadResponse = await ApiHelper.PostFormAsync<string>("/api/Post/uploadimage", form, token);
                }
                var postData = new
                {
                    GroupName = group.GroupName,
                    GroupPictureUrl = uploadResponse,
                    IsPrivate = group.IsPrivate
                };

                var apiResponse = await ApiHelper.PostAsync<object, ApiReponseModel<int>>("/api/Group/creategr", postData, token);

                if (apiResponse != null && apiResponse.Status == 1)
                {
                    TempData["Success"] = "Tạo nhóm thành công!";
                }
                else
                {
                    TempData["Error"] = "Tạo nhóm thất bại";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
            }
            return RedirectToAction("Index", "Group");

        }

        public async Task<IActionResult> DeleteGroup ([FromBody] int id)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/Group/deletegr/{id}",token);
                return Json(apiResponse);
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, Mess = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }

        public async Task<IActionResult> Details(int id)    
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Authentication");
            }

            try
            {

                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<GroupDetailResponseModel>>($"/api/Group/detail/{id}", token);

                if (apiResponse != null && apiResponse.Status == 1)
                {
                    // Pass the data to the view
                    return View(apiResponse.Data);
                }
                else
                {
                    // Handle cases where the group is not found or API call fails
                    TempData["Error"] = apiResponse?.Mess ?? "Không tìm thấy nhóm hoặc có lỗi xảy ra.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions during the API call
                TempData["Error"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
    