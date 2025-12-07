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

            var json = await ApiGetDashboardStats(startDate, endDate) as JsonResult;
            var stats = json?.Value as StatsDashboardModel ?? new StatsDashboardModel();

            ViewData["TotalPosts"] = stats.TotalPostsCount.ToString();
            ViewData["DailyRegistrationsJson"] =
                JsonSerializer.Serialize(stats.DailyRegistrations ?? new List<DailyRegistrationStats>());

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ApiGetDashboardStats(DateTime? startDate, DateTime? endDate)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return Json(new ApiReponseModel { Status = 0, Mess = "Unauthorized" });

            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return Json(new StatsDashboardModel());
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

                var response = await ApiHelper.GetAsync<ApiReponseModel<StatsDashboardModel>>(url, token);

                if (response.Status == 1 && response.Data != null)
                    return Json(response.Data);

                return Json(new StatsDashboardModel());
            }
            catch
            {
                return Json(new StatsDashboardModel());
            }
        }
    }
}
