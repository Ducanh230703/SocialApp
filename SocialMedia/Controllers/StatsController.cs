using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using System;
using System.Threading.Tasks;
using SocialMedia.Helper;
using System.Text.Json;
using System.Collections.Generic;

namespace SocialMedia.Controllers
{
    public class StatsController : Controller
    {
        public async Task<IActionResult> Index([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Authentication");

            if (!startDate.HasValue && !endDate.HasValue)
            {
                endDate = DateTime.Today;
                startDate = DateTime.Today.AddDays(-6);
            }
            else if (!startDate.HasValue)
            {
                startDate = endDate.Value.AddDays(-6);
            }
            else if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddDays(6);
            }
            var response = await ApiGetDashboardStats(startDate, endDate);
            if (response.Status == -1)
            {
                return RedirectToAction("AccessDenied", "Error");
            }
                
            ViewData["TotalPosts"] = response.Data.TotalPostsCount.ToString();
            ViewData["DailyRegistrationsJson"] = response.Data.DailyRegistrations.Count.ToString();
            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            return View();
        }


        // Trong SocialMedia.Controllers.StatsController

        [HttpGet]
        public async Task<ApiReponseModel<StatsDashboardModel>> ApiGetDashboardStats(DateTime? startDate, DateTime? endDate)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                // Trả về mô hình lỗi để Frontend xử lý
                return new ApiReponseModel<StatsDashboardModel> { Status = -1, Mess = "Unauthorized" };
            }

            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                // Trả về mô hình lỗi ngày tháng
                return new ApiReponseModel<StatsDashboardModel>
                {
                    Status = 0,
                    Mess = "Ngày bắt đầu không được lớn hơn ngày kết thúc."
                };
            }

            try
            {
                var url = "/api/Stats/dashboard";
                var query = new Dictionary<string, string>();

                if (startDate.HasValue)
                    query.Add("startDate", startDate.Value.ToString("yyyy-MM-dd"));

                if (endDate.HasValue)
                    query.Add("endDate", endDate.Value.ToString("yyyy-MM-dd"));

                url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, query);

                // 🚨 NHẬN TOÀN BỘ PHẢN HỒI TỪ API BACKEND
                var response = await ApiHelper.GetAsync<ApiReponseModel<StatsDashboardModel>>(url, token);


                return response;            }
            catch
            {
                return new ApiReponseModel<StatsDashboardModel> { Status = 0, Mess = "Lỗi hệ thống khi gọi API." };
            }
        }
    }
}
