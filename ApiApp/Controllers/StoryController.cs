
using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Home;
using Models.ViewModel.Story;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoryController : ControllerBase
    {
        [HttpGet("getall")]
        public async Task<List<Story>> GetAll()
        {
            var data = await StoryService.GetAllStories();
            return data;
        }

        [HttpPost("upstory")]
        public async Task<ApiReponseModel> UserUpstory([FromBody] StoryVM storyVM)
        {
            var a = CacheEx.DataUser;
            var data = await StoryService.Upstory(storyVM.ImageUrl,storyVM.ExpireAt, a.ID);
            return data;
        }
    }
}
