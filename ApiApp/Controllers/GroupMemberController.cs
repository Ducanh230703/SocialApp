using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupMemberController : ControllerBase
    {
        [HttpPost("joingr")]
        public async Task<ApiReponseModel> JoinGroup([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.JoinGroup(Cache.CacheEx.DataUser.ID, groupMember.GroupId);
        }

        [HttpDelete("deletemember")]
        public async Task<ApiReponseModel> DeleteMember([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.DeleteMember(groupMember);
        }

        [HttpDelete("leavegroup")]
        public async Task<ApiReponseModel> LeaveGroup([FromBody] GroupMember groupMember)
        {
            return await Services.GroupMemberService.LeaveGroup(Cache.CacheEx.DataUser.ID, groupMember.GroupId);
        }
    }
}
