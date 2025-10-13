using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.GroupMember;
using SocialMedia.Helper;
using System.Collections.Generic; // Cần thiết nếu bạn dùng GetMembers

namespace SocialMedia.Controllers
{
    public class GroupMemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup([FromBody] int groupId)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { Status = 0, Mess = "Chưa đăng nhập. Vui lòng đăng nhập lại." });
            }

            try
            {
                var postData = new { GroupId = groupId };

                var apiResponse = await ApiHelper.PostAsync<object, ApiReponseModel>($"/api/GroupMember/joingr", postData, token);

                return Json(apiResponse);
            }
            catch (Exception)
            {
                return Json(new { Status = 0, Mess = "Đã xảy ra lỗi khi tham gia nhóm. Vui lòng thử lại sau." });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> LeaveGroup([FromBody] int groupId)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            try
            {
                var apiResponse = await ApiHelper.DeleteAsync<ApiReponseModel>($"/api/GroupMember/leavegroup/{groupId}", token);

                return Json(apiResponse);
            }
            catch (Exception)
            {
                return Json(new { Status = 0, Mess = "Có lỗi xảy ra khi rời nhóm. Vui lòng thử lại sau." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ShowMembersModal(int groupId, int currentUserRole)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { Status = 0, Mess = "Chưa đăng nhập." });
            }

            try
            {
                var apiResponse = await ApiHelper.GetAsync<ApiReponseModel<List<GroupMemberList>>>(
                    $"/api/GroupMember/getmembers/{groupId}", token
                );

                if (apiResponse.Status == 1)
                {
                    var sorted = apiResponse.Data
                        .OrderByDescending(m => m.Role == GroupMemberRole.Owner)
                        .ThenByDescending(m => m.Role == GroupMemberRole.Admin)
                        .ThenBy(m => m.Role== GroupMemberRole.Member)
                        .ToList();
                    ViewData["CurrentUserRole"] = (GroupMemberRole)currentUserRole;
                    return PartialView("_GroupMembersModal", sorted);
                }
                else
                {
                    return Json(new { Status = 0, Mess = apiResponse.Mess });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Mess = $"Không thể tải danh sách thành viên: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin([FromBody] GroupMember model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            try
            {
                var apiResponse = await ApiHelper.PostAsync<GroupMember, ApiReponseModel>("/api/GroupMember/addadmin", model, token);
                return Json(apiResponse);
            }
            catch (Exception)
            {
                return Json(new { Status = 0, Mess = "Lỗi khi thêm quản trị viên." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAdmin([FromBody] GroupMember model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            try
            {
                var apiResponse = await ApiHelper.PostAsync<GroupMember, ApiReponseModel>("/api/GroupMember/removeadmin", model, token);
                return Json(apiResponse);
            }
            catch (Exception)
            {
                return Json(new { Status = 0, Mess = "Lỗi khi gỡ quyền quản trị viên." });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMember([FromBody] GroupMember model)
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            try
            {
                var apiResponse = await ApiHelper.PostAsync<GroupMember, ApiReponseModel>("/api/GroupMember/deletemember", model, token);
                return Json(apiResponse);
            }
            catch (Exception)
            {
                return Json(new { Status = 0, Mess = "Lỗi khi xoá thành viên khỏi nhóm." });
            }
        }

    }
}