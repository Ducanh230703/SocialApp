using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        [HttpPost("creategr")]
        public async Task<Models.ReponseModel.ApiReponseModel> CreateGroup([FromBody] Group group)
        {
            return await GroupService.CreateGroup(Cache.CacheEx.DataUser.ID, group.GroupName);
        }
    }
}
