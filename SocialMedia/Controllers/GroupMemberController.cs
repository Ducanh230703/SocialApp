using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using SocialMedia.Helper;

namespace SocialMedia.Controllers
{
    public class GroupMemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> LeaveGroup(int id)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return Unauthorized();

            try
            {
                var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/GroupMember/leavegroup/{id}", token);

                return Json(apiResponse);
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, Mess = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
    }
}
