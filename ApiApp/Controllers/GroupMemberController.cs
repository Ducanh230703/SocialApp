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
            return await Services.GroupMemberService.JoinGroup(groupMember);
        }
    }
}
