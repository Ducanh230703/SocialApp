using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ActivityLogController : ControllerBase
    {

        [HttpGet("getall")]
        public async Task<ApiReponseModel<PaginatedResponse<ActivityLogResponse>>> GetAllLogs([FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            var data = await ActivityLogService.GetAllLogs(Cache.CacheEx.DataUser.ID, pageNumber, pageSize);
            return data;
        }
    }
}
