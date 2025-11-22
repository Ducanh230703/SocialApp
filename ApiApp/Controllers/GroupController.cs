using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ResponseModels;
using Models.ViewModel.Group;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        [HttpPost("creategr")]
        public async Task<Models.ReponseModel.ApiReponseModel<int>> CreateGroup([FromBody] CreateGroupForm group)
        {
            var user = Cache.CacheEx.DataUser;
            group.CreatedByUserId = user.ID;
            return await GroupService.CreateGroup(group);
        }

        [HttpDelete("deletegr/{id}")]
        public async Task<Models.ReponseModel.ApiReponseModel> DeleteGroup(int id)
        {
            var userId = CacheEx.DataUser.ID;

            return await GroupService.DeleteGroup(id, userId);
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
        public async Task<ApiReponseModel<List<Group>>> GetListGroup()
        {
            return await GroupService.GetListGroup(Cache.CacheEx.DataUser.ID);

        }

        [HttpGet("detail/{groupID}")]
        public async Task<ApiReponseModel<GroupDetailResponseModel>> Detail(int groupID)
        {
            var data = await GroupService.GetGroupDetail(groupID, Cache.CacheEx.DataUser.ID);
            return data;
        }

        [HttpGet("search")]
        public async Task<ApiReponseModel<List<Group>>> SearchGroup([FromQuery] string query)
        {
            var user = Cache.CacheEx.DataUser;
            return await GroupService.SearchGroup(query);
        }
    }
}
