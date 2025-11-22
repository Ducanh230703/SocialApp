using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Home;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        [HttpGet("getall")]
        public async Task<PaginatedResponse<PostFull>> GetAll(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var data = await PostService.GetAllPosts(pageNumber, pageSize,CacheEx.DataUser.ID);
            return data; 
        }

        [HttpPost("newpost")]
        public async Task<ApiReponseModel<int>> AddNewPost([FromBody] PostVM postVM)
        {

            var a = CacheEx.DataUser;
            var data = await PostService.NewPost(postVM.Content, postVM.ImageUrls, a.ID,postVM.GroupID,postVM.IsAnnoy);
            return data;
        }


        [HttpPost("uploadimage")]
        [Consumes("multipart/form-data")]
        public async Task<string> UploadImages([FromForm] List<IFormFile> images)
        {
            var imageUrls = new List<string>();
            string UrlList = null;

            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Upload", uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    imageUrls.Add(uniqueFileName);
                }
            }

            if (imageUrls.Count > 0)
            {
                UrlList = string.Join(",", imageUrls);
            }

            return UrlList;
        }

        [HttpPost("editpost")]
        public async Task<ApiReponseModel> EditPost([FromBody] PostEditVM postEditVM)
        {
            var userId = CacheEx.DataUser.ID;

            var data = await PostService.UserEditPost(postEditVM.PostId, postEditVM.Content, postEditVM.ImageUrls,userId);
            if (data.Status == 1)
            {
                if (postEditVM.RemovedImageUrls != null && postEditVM.RemovedImageUrls.Any())
                {
                    foreach (var imageName in postEditVM.RemovedImageUrls)
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Upload", imageName);
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }
            }
            return data;
        }

        [HttpGet("getpostbyid/{postId}")]
        public async Task<ApiReponseModel<PostFull>> GetById(int postId)
        {
            var data = await PostService.GetPostByIdAsync(postId);
            return data;
        }

        [HttpDelete("deletebyid/{postId}")]
        public async Task<ApiReponseModel> DeleteById(int postId)
        {
            var userId = CacheEx.DataUser.ID;
            var data = await PostService.UserDeletePost(postId, userId);
            return data;
        }   


        //[HttpPost("addcomment/{postId}")]
        //public async Task<ApiReponseModel<CommentDetail>> AddComment([FromBody] PostCommentVM postCommentVM)
        //{
        //    var user = CacheEx.DataUser;
        //    var data = await PostService.AddComment(postCommentVM.PostId, user.ID, postCommentVM.Content);
        //    return data;

        //}

        //[HttpDelete("deletecomment/{commentId}")]
        //public async Task<ApiReponseModel> DeleteComment(int commentId)
        //{
        //    var data = await PostService.DeleteComment(commentId);
        //    return data;
        //}


        //[HttpPost("likepost")]
        //public async Task<ApiReponseModel> AddLikePost([FromBody] PostLikeVM postLikeVM)
        //{
        //    var a = CacheEx.DataUser;
        //    var data = await PostService.LikePost(postLikeVM.PostId, a.ID);
        //    return data;
        //}

        //[HttpPost("deletelikepost")]
        //public async Task<ApiReponseModel> DeleteLikePost([FromBody] PostLikeVM postLikeVM)
        //{
        //    var a = CacheEx.DataUser;
        //    var data = await PostService.UnlikePost(postLikeVM.PostId,a.ID);
        //    return data;
        //}
    }
}
