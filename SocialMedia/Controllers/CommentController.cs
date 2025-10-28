using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models.ViewModel.Home;
using SocialMedia.Helper;

namespace SocialMedia.Controllers
{
    public class CommentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddPostComment(PostCommentVM postCommentVM)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            try
            {
                var apiResponse = await ApiHelper.PostAsync<PostCommentVM, ApiReponseModel>($"/api/Comment/addcomment/{postCommentVM.PostId}", postCommentVM, token);

                if (apiResponse != null && apiResponse.Status == 1)
                {
                    return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = apiResponse?.Mess ?? "Không thể thêm bình luận."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> RemovePostComment(int CommentId)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Chưa xác thực" });
            }

            try
            {
                var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/Comment/deletecomment/{CommentId}", token);

                if (apiResponse != null && apiResponse.Status == 1)
                {
                    return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
                }
                else
                {
                    return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
                }
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bình luận: " + ex.Message });
            }
        }
    }
}
