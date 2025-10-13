using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.GroupMember;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupMemberController : ControllerBase
    {
        [HttpPost("joingr")]
        public async Task<ApiReponseModel> JoinGroup([FromBody] JoinVM joinVM)
        {
            return await Services.GroupMemberService.JoinGroup(Cache.CacheEx.DataUser.ID, joinVM.GroupId,joinVM.Role);
        }

        [HttpDelete("deletemember")]
        public async Task<ApiReponseModel> DeleteMember([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.DeleteMember(groupMember);
        }

        [HttpDelete("leavegroup/{groupId}")]
        public async Task<ApiReponseModel> LeaveGroup(int groupId)
        {
            return await Services.GroupMemberService.LeaveGroup(Cache.CacheEx.DataUser.ID, groupId);
        }

        [HttpGet("getmembers/{groupId}")]
        public async Task<ApiReponseModel<List<GroupMemberList>>> GetMembersInGroup(int groupId)
        {
            return await Services.GroupMemberService.GetMembersInGroup(groupId, Cache.CacheEx.DataUser.ID);
        }

        [HttpPost("addadmin")]
        public async Task<ApiReponseModel> AddAdmin([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.AddAdmin(Cache.CacheEx.DataUser.ID, groupMember.GroupId, groupMember.UserID);
        }

        [HttpPost("removeadmin")]
        public async Task<ApiReponseModel> RemoveAdmin([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.RemoveAdmin(Cache.CacheEx.DataUser.ID, groupMember.GroupId, groupMember.UserID);
        }


    }
}
