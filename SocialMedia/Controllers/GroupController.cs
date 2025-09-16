using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using SocialMedia.Helper;

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
    }
}
