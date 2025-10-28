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
    public class CommentController : ControllerBase
    {
        [HttpPost("addcomment/{postId}")]
        public async Task<ApiReponseModel<CommentDetail>> AddComment([FromBody] PostCommentVM postCommentVM)
        {
            var user = CacheEx.DataUser;
            var data = await CommentService.AddComment(postCommentVM.PostId, user.ID, postCommentVM.Content);
            return data;

        }

        [HttpDelete("deletecomment/{commentId}")]
        public async Task<ApiReponseModel> DeleteComment(int commentId)
        {
            var data = await CommentService.DeleteComment(commentId);
            return data;
        }
    }
}
