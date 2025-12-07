using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        [HttpGet("getnotification")]
        public async Task<ApiReponseModel<PaginatedResponse<Notification>>> GetNotificationById([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var user = CacheEx.DataUser;
            var dataReponse = await NotificationService.GetNotice(user.ID, pageNumber, pageSize);
                if (dataReponse.Data != null)
                return new ApiReponseModel<PaginatedResponse<Notification>>
                {
                    Status = 1,
                    Mess = "Lấy thông báo thành công",
                    Data = dataReponse
                };
            else
                return new ApiReponseModel<PaginatedResponse<Notification>>
                {
                    Status = 0,
                    Mess = "Lấy thông báo thất bại",
                    Data = null
                };
        }

        [HttpGet("getcount")]
        public async Task<ApiReponseModel<int>> GetCount()
        {
            var user = CacheEx.DataUser;
            var dataReponse = await NotificationService.GetCountUnRead(user.ID);
            return dataReponse;
        }

        [HttpPost("setisread")]
        public async Task<ApiReponseModel> SetRead([FromBody] SetReadRequest setReadRequest)
        {
            var dataReponse = await NotificationService.SetIsRead(setReadRequest.NoticeId);
            return dataReponse;

        }
    }
}