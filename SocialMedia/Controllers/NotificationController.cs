using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models;
using SocialMedia.Helper;
using Models.ViewModel;

namespace SocialMedia.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNoticeAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "Unauthorized: Authentication token missing. Please log in again." });
            }

            var apiPath = $"/api/Notification/getnotification?pageNumber={pageNumber}&pageSize={pageSize}";
            var apireponse = await ApiHelper.GetAsync<ApiReponseModel<PaginatedResponse<Notification>>>(apiPath, token);

            if (apireponse.Status == 1)
                return PartialView("_Notification", apireponse.Data);
            else
                return BadRequest("Lỗi Api");
        }

        [HttpGet]
        public async Task<IActionResult> GetCount() 
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "Unauthorized: Authentication token missing. Please log in again." });
            }

            var apireponse = await ApiHelper.GetAsync<ApiReponseModel<int>>("/api/Notification/getcount", token);

            if (apireponse.Status == 1)
                return Json(apireponse.Data);
            else
                return Json(0);
        }

        [HttpPost]
        public async Task<IActionResult> SetRead(SetReadRequest setReadRequest)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                Response.StatusCode = 401;
                return Json(new { Status = -1, message = "Unauthorized: Authentication token missing. Please log in again." });
            }


            var apireponse = await ApiHelper.PostAsync<SetReadRequest, ApiReponseModel>("/api/Notification/setisread", setReadRequest, token);

            if (apireponse != null)
            {
                return Ok(apireponse);
            }

            else
            {
                return BadRequest("Lỗi Api");
            }

        }
    }
}
