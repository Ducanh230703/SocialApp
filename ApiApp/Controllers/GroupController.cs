using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
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
            group.CreatedByUserId = Cache.CacheEx.DataUser.ID;
            return await GroupService.CreateGroup(group);
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

        [HttpGet("listgr")] 
        public  async Task<ApiReponseModel<List<Group>>> GetListGroup()
        {
            return await GroupService.GetListGroup(Cache.CacheEx.DataUser.ID);

        }
    }
}
