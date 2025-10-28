using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models.ViewModel.Home;
using SocialMedia.Helper;

namespace SocialMedia.Controllers
{
    public class LikeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(PostLikeVM postLikeVM)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            try
            {
                var apiReponse = await ApiHelper.PostAsync<PostLikeVM, ApiReponseModel>("/api/Like/likepost", postLikeVM, token);
                if (apiReponse != null && apiReponse.Status == 1)
                {
                    return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
                }
                else
                {
                    return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
                }
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bình luận: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Unlike(PostLikeVM postLikeVM)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            try
            {
                var apiReponse = await ApiHelper.PostAsync<PostLikeVM, ApiReponseModel>("/api/Like/deletelikepost", postLikeVM, token);
                if (apiReponse != null && apiReponse.Status == 1)
                {
                    return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
                }
                else
                {
                    return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
                }
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi hủy like: " + ex.Message });
            }
        }
    }
}
