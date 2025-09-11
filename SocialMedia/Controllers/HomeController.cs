using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Home;
using SocialMedia.Helper;
using SocialMedia.Models;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Models.ViewModel.Story;
using System.Net.Http;
using System;
using System.Net.Http.Headers;

namespace SocialMedia.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Action này dùng để tải trang ban đầu (full HTML) hoặc xử lý việc chỉnh sửa bài viết.
    /// </summary>
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 3)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("AuthToken is missing or empty. Redirecting to login.");
                return RedirectToAction("Login", "Authentication");
            }

            int loggedInUserId = 0;
            var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
            if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
            {
                loggedInUserId = parsedUserId;
            }

            ViewBag.LoggedInUserId = loggedInUserId;

            try
            {
                var allPosts = await ApiHelper.GetAsync<PaginatedResponse<PostFull>>($"/api/Post/getall?pageNumber={pageNumber}&pageSize={pageSize}", token);
                ViewBag.CurrentPage = pageNumber;
                ViewBag.PageSize = pageSize;
                return View(allPosts);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API getall để hiển thị trang Home ban đầu.");
                TempData["Error"] = "Không thể tải bài viết. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Authentication");
            }
        }

    [HttpGet]
    public async Task<IActionResult> GetMorePosts(int pageNumber, int pageSize) 
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            Response.StatusCode = 401;
            return Json(new { success = false, message = "Unauthorized: Authentication token missing. Please log in again." });
        }

        try
        {
            int loggedInUserId = 0;
            var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
            if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
            {
                loggedInUserId = parsedUserId;
            }

            ViewBag.LoggedInUserId = loggedInUserId;
            var allPosts = await ApiHelper.GetAsync<PaginatedResponse<PostFull>>($"/api/Post/getall?pageNumber={pageNumber}&pageSize={pageSize}", token);
            return PartialView("_PostList", allPosts.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Lỗi HttpRequest khi tải thêm bài viết từ API trong GetMorePosts: {ex.Message}");
            Response.StatusCode = 500; 
            return Json(new { success = false, message = "Server error: Could not load more posts due to API request issue." });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, $"Lỗi không xác định khi tải thêm bài viết trong GetMorePosts: {ex.Message}");
            Response.StatusCode = 500;
            return Json(new { success = false, message = "An unexpected error occurred while loading more posts." });
        }
    }

    [HttpGet]
     public async Task<IActionResult> Details(int postId)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Authentication");

            try
            {
                var postResponse = await ApiHelper.GetAsync<ApiReponseModel<PostFull>>($"/api/Post/getpostbyid/{postId}", token);

                    if (postResponse == null || postResponse.Data == null)
                {
                    TempData["Error"] = "Bài viết không tồn tại.";
                    return RedirectToAction("Index");
                }
                ViewData["ShowAllCommentsPostId"] = postResponse.Data.Id;
                int loggedInUserId = 0;
                var loggedInUserIdCookie = Request.Cookies["LoggedInUserId"];
                if (!string.IsNullOrEmpty(loggedInUserIdCookie) && int.TryParse(loggedInUserIdCookie, out int parsedUserId))
                {
                    loggedInUserId = parsedUserId;
                }

            ViewBag.LoggedInUserId = loggedInUserId;
            return View("Details", postResponse.Data);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Lỗi khi gọi API getbyidfull cho bài viết ID: {postId}");
                TempData["Error"] = "Không thể tải chi tiết bài viết.";
                return RedirectToAction("Index");
            }
        }


    //[HttpPost]
    //public async Task<IActionResult> CreatePost(PostVM postVM)
    //        {
    //        var token = Request.Cookies["AuthToken"];
    //        if (string.IsNullOrEmpty(token))
    //            return RedirectToAction("Login", "Authentication");
    //        string imageUrlList = null;
    //        try
    //        {
    //            var imageUrls = new List<string>();

    //            if (postVM.Image != null && postVM.Image.Count > 0)
    //            {
    //                foreach (var image in postVM.Image)
    //                {
    //                    if (image != null && image.Length > 0)
    //                    {
    //                        var fileName = Path.GetFileName(image.FileName);
    //                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploaded", fileName);

    //                        using (var fileStream = new FileStream(filePath, FileMode.Create))
    //                        {
    //                            await image.CopyToAsync(fileStream);
    //                        }
    //                        imageUrls.Add("/images/uploaded/" + fileName);
    //                    }
    //                }
    //                if (imageUrls.Count > 0)
    //                {
    //                    imageUrlList = string.Join(",", imageUrls);
    //                }
    //            }


    //        var postData = new
    //            {
    //                Content = postVM.Content,
    //                ImageUrls = imageUrlList,
    //            };

    //            var apiResponse = await ApiHelper.PostAsync<object, ApiReponseModel>("/api/Post/newpost", postData, token);

    //            if (apiResponse != null && apiResponse.Status == 1)
    //            {
    //                TempData["Success"] = "Đăng bài thành công!";
    //            }
    //            else
    //            {
    //                TempData["Error"] = "Đăng bài không thành công.";
    //            }
    //            return RedirectToAction("Index", "Home");
    //        }
    //        catch (HttpRequestException ex)
    //        {
    //            _logger.LogError(ex, "Error creating post");
    //            TempData["Error"] = "Không thể kết nối đến máy chủ. Vui lòng thử lại sau.";
    //            return RedirectToAction("Index", "Home");
    //        }
    //    }

    //[HttpGet]
    //public async Task<IActionResult> GetPostById(int id)
    //{
    //    var token = Request.Cookies["AuthToken"];

    //    var post = await ApiHelper.GetAsync<ApiReponseModel<Post>>($"/api/Post/getbyid/{id}",token);

    //    if (post == null)
    //    {
    //        return NotFound();
    //    }

    //    var postEditVM = new PostEditVM
    //    {
    //        PostId = post.Data.ID,
    //        ImageUrls = post.Data.ImageUrl,
    //        Content = post.Data.Content
    //    };

    //    return PartialView("_EditStatus", postEditVM);
    //}
    [HttpPost]
    public async Task<IActionResult> CreatePost(PostVM postVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Authentication");

        try
        {
            List<string> imageUrls = new();
            string uploadResponse = null;
            if (postVM.Image != null && postVM.Image.Count > 0)
            {
                var form = new MultipartFormDataContent();
                foreach (var image in postVM.Image)
                {
                    var stream = image.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    form.Add(fileContent, "images", image.FileName);
                }

                uploadResponse = await ApiHelper.PostFormAsync<string>("/api/Post/uploadimage", form, token);
            }



            var postData = new
            {
                Content = postVM.Content,
                ImageUrls = uploadResponse
            };

            var apiResponse = await ApiHelper.PostAsync<object, ApiReponseModel>("/api/Post/newpost", postData, token);

            if (apiResponse != null && apiResponse.Status == 1)
            {
                TempData["Success"] = "Đăng bài thành công!";
            }
            else
            {
                TempData["Error"] = "Đăng bài không thành công.";
            }

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo bài viết");
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditPost(PostEditVM postEditVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Authentication");
        }

        string uploadResponse = null;
        if (postEditVM.Image != null && postEditVM.Image.Count > 0)
        {
            var form = new MultipartFormDataContent();
            foreach (var image in postEditVM.Image)
            {
                var stream = image.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                form.Add(fileContent, "images", image.FileName);
            }

                uploadResponse = await ApiHelper.PostFormAsync<string>("/api/Post/uploadimage", form, token);
        }


        var imageUrls = new List<string>();

        if (!string.IsNullOrEmpty(postEditVM.ImageUrls))
        {
            imageUrls.AddRange(postEditVM.ImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        if (!string.IsNullOrEmpty(uploadResponse))
        {
            imageUrls.AddRange(uploadResponse.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        string finalImageUrls = string.Join(",", imageUrls);

        // Gửi dữ liệu chỉnh sửa bài viết
        var editData = new PostEditVM
        {
            Content = postEditVM.Content,
            ImageUrls = finalImageUrls,
            PostId = postEditVM.PostId,
            RemovedImageUrls = postEditVM.RemovedImageUrls
        };

        var apiResponse = await ApiHelper.PostAsync<object, ApiReponseModel>("/api/Post/editpost", editData, token);

        if (apiResponse != null && apiResponse.Status == 1)
        {
            return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
        }
        else
        {
            return Json(new { status = apiResponse.Status, message = apiResponse.Mess ?? "Lỗi không xác định" });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeletePost(int PostId)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return Json(new { success = false, message = "Chưa xác thực" });
        }

        try
        {
            var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/Post/deletebyid/{PostId}", token);

            if (apiResponse != null && apiResponse.Status == 1)
            {
                return Json(new { status = apiResponse.Status,message = apiResponse.Mess });
            }
            else
            {
                Console.Error.WriteLine($"Failed to delete post {PostId}.  API Response Status: {apiResponse?.Status}, Message: {apiResponse?.Mess}");
                return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
            }
        }
        catch (Exception ex) 
        {

            Console.Error.WriteLine($"Exception in DeletePost: {ex.Message}");
            return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bài viết: " + ex.Message });
        }
    }


    [HttpPost]
    public async Task<IActionResult> AddPostComment(PostCommentVM postCommentVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return Json(new { success = false, message = "Chưa xác thực" });
        }

        try
        {
            var apiResponse = await ApiHelper.PostAsync<PostCommentVM, ApiReponseModel>($"/api/Post/addcomment/{postCommentVM.PostId}",postCommentVM,token);

            if (apiResponse != null && apiResponse.Status == 1)
            {
                return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = apiResponse?.Mess ?? "Không thể thêm bình luận."
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> RemovePostComment(int CommentId)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return Json(new { success = false, message = "Chưa xác thực" });
        }

        try
        {
            var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/Post/deletecomment/{CommentId}", token);

            if (apiResponse != null && apiResponse.Status == 1)
            {
                return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
            }
            else
            {
                return Json(new { status = apiResponse.Status, message = apiResponse.Mess });
            }
        }
        catch (Exception ex)
        {

            Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
            return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bình luận: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> LikePost(PostLikeVM postLikeVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return Json(new { success = false, message = "Chưa xác thực" });
        }

        try
        {
            var apiReponse = await ApiHelper.PostAsync<PostLikeVM, ApiReponseModel>("/api/Post/likepost", postLikeVM, token);
            if (apiReponse != null && apiReponse.Status == 1)
            {
                return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
            }
            else
            {
                return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
            }
        }
        catch (Exception ex)
        {

            Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
            return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa bình luận: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Unlike(PostLikeVM postLikeVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            return Json(new { success = false, message = "Chưa xác thực" });
        }

        try
        {
            var apiReponse = await ApiHelper.PostAsync<PostLikeVM, ApiReponseModel>("/api/Post/deletelikepost", postLikeVM, token);
            if (apiReponse != null && apiReponse.Status == 1)
            {
                return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
            }
            else
            {
                return Json(new { status = apiReponse.Status, message = apiReponse.Mess });
            }
        }
        catch (Exception ex)
        {

            Console.Error.WriteLine($"Exception in DeleteComment: {ex.Message}");
            return Json(new { success = false, message = "Đã xảy ra lỗi khi hủy like: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Upstory(StoryVM storyVM)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Authentication");

        try
        {
            string imageUrl = null;

            if (storyVM.Image != null)
            {
                var fileName = Path.GetFileName(storyVM.Image.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploaded", fileName);

                imageUrl = "/images/uploaded/" + fileName;
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await storyVM.Image.CopyToAsync(fileStream);
                }
                storyVM.ImageUrl = imageUrl;

                var storyData = new
                {
                    ImageUrl = storyVM.ImageUrl,
                    ExpireAt = DateTime.UtcNow.AddHours(24)
                };

                var apiReponse = await ApiHelper.PostAsync<object, ApiReponseModel>("/api/Story/upstory", storyData, token);
                if (apiReponse.Status == 1)
                {
                    return RedirectToAction("Index", "Home");
                }
                return Json(new { status = apiReponse?.Status ?? 0, message = apiReponse?.Mess ?? "Lỗi không xác định" });
            }
            return Json(new { status = 0, message = "Vui lòng chọn ảnh!" });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error creating post");
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPostById(int postId)
    {
        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Authentication");

        var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<PostFull>>($"/api/Post/getpostbyid/{postId}", token);

        if (apiResponse.Data != null)
        {
            var model = new PostEditVM
            {
                PostId = apiResponse.Data.Id,
                Content = apiResponse.Data.Content,
                ImageUrls = apiResponse.Data.ImageUrl,
            };

            return Ok(model);
        }

        return BadRequest("failed");
    }

}

