using Microsoft.AspNetCore.Mvc;
using Models;
using SocialMedia.Helper;

namespace SocialMedia.ViewComponents
{
    public class StoriesViewComponent:ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return View();

            var allStories = await ApiHelper.GetAsync<List<Story>>("/api/Story/getall",token);
            return View(allStories);
        }
    }
}
