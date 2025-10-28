using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models.ViewModel.Home;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController : ControllerBase

    {
        [HttpPost("likepost")]
        public async Task<ApiReponseModel> AddLikePost([FromBody] PostLikeVM postLikeVM)
        {
            var a = CacheEx.DataUser;
            var data = await LikeService.LikePost(postLikeVM.PostId, a.ID);
            return data;
        }

        [HttpPost("deletelikepost")]
        public async Task<ApiReponseModel> DeleteLikePost([FromBody] PostLikeVM postLikeVM)
        {
            var a = CacheEx.DataUser;
            var data = await LikeService.UnlikePost(postLikeVM.PostId, a.ID);
            return data;
        }
    }
}
