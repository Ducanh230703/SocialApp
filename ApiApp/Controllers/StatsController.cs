using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Services;

[Route("api/[controller]")]
[ApiController]
public class StatsController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ApiReponseModel<StatsDashboardModel>> GetDashboardStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (Cache.CacheEx.DataUser.Role != "Admin")
            return new ApiReponseModel<StatsDashboardModel>
            {
                Status = 0,
                Mess = "Bạn không có quyền truy cập!"
            };
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return new ApiReponseModel<StatsDashboardModel>
            {
                Status = 0,
                Mess = "Ngày bắt đầu không được lớn hơn ngày kết thúc."
            };
        }

        return await StatsService.GetDashboardStats(startDate, endDate);
    }
}
