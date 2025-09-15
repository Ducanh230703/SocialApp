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

        [HttpDelete("deletegr")]
        public async Task<Models.ReponseModel.ApiReponseModel> DeleteGroup([FromBody] Group group)
        {
            return await GroupService.DeleteGroup(group.ID);
        }

        [HttpPatch("updateimage")]
        public async Task<Models.ReponseModel.ApiReponseModel> UpdateImage([FromBody] Group group)
        {
            return await GroupService.UpImageGroup(group.ID, group.GroupPictureUrl);
        }

        [HttpPatch("updatename")]
        public async Task<Models.ReponseModel.ApiReponseModel> UpdateName([FromBody] Group group)
        {
            return await GroupService.ChangeName(group.ID, group.GroupName);
        }
    }
}
