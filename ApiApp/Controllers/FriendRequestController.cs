using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models.ReponseModel;
using Models.ViewModel.Friend;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FriendRequestController : ControllerBase
    {
        [HttpGet("getstatus")]
        public async Task<ApiReponseModel<FriendRequestRP>> GetStatus([FromQuery]int loggedInUserId, [FromQuery] int profileUserId)
        {
            var data = await FriendRequestService.StatusRequest(loggedInUserId, profileUserId);
            return data;
        }

        [HttpPost("friendrequest")]
        public async Task<ApiReponseModel> FriendRequest([FromBody] FriendRequestVM friendRequestVM)
        {
            var sender = Cache.CacheEx.DataUser;
            var data = await FriendRequestService.SendRequest(friendRequestVM.Status, friendRequestVM.SenderId, friendRequestVM.ReceiverId);
            return data;
        }

        [HttpPost("friendanswer")]
        public async Task<ApiReponseModel> FriendAnswer([FromBody] FriendAnswer friendRequestVM)
        {
            var sender = Cache.CacheEx.DataUser;
            var data = await FriendRequestService.AnswerRequest (friendRequestVM.ID,friendRequestVM.Status);
            return data;
        }

        [HttpGet("search")]
        public async Task<ApiReponseModel<PaginatedResponse<SearchResult>>> Search(string stringSearch, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var data = await FriendRequestService.FriendSearch(stringSearch, pageNumber,pageSize);
            return data;

        }

        [HttpGet("friendindex")]
        public async Task<ApiReponseModel<FriendShipVM>> FriendIndex([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var reponse = new ApiReponseModel<FriendShipVM>();
            var user = CacheEx.DataUser;

            var friendShip = new FriendShipVM();
            var listFriend = await FriendRequestService.GetListFriend(user.ID, pageNumber, pageSize);
            if (listFriend.Status == 1)
            {
                friendShip.Friend = listFriend.Data;
            }

            var friendPend = await FriendRequestService.GetListPend(user.ID, pageNumber, pageSize);
            if (friendPend.Status == 1)
            {
                friendShip.FriendRequestsReceived = friendPend.Data;
            }

            var friendSend = await FriendRequestService.GetListSend(user.ID, pageNumber, pageSize);
            if (friendSend.Status == 1)
            {
                friendShip.FriendRequestsSent = friendSend.Data;
            }

            if (listFriend.Status == 0 || friendPend.Status == 0 || friendSend.Status == 0)
            {
                reponse.Status = 0;
                reponse.Mess = "Lấy danh sách bạn bè thất bại";
                reponse.Data = friendShip;

            }

            else
            {
                reponse.Status = 1;
                reponse.Mess = "Lấy danh sách bạn bè thành công";
                reponse.Data = friendShip;
            }

            return reponse;
                    
        }

        [HttpGet("loadmorefr")]
        public async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> LoadMoreFriend([FromQuery] int pageNumber = 2, [FromQuery] int pageSize = 5)
        {
            var user = CacheEx.DataUser;

            var data = await FriendRequestService.GetListFriend(user.ID, pageNumber, pageSize);

            return data;
        }


        [HttpGet("loadmoresend")]
        public async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> LoadMoreSend([FromQuery] int pageNumber = 2, [FromQuery] int pageSize = 5)
        {
            var user = CacheEx.DataUser;

            var data = await FriendRequestService.GetListSend(user.ID, pageNumber, pageSize);

            return data;
        }

        [HttpGet("loadmorepend")]
        public async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> LoadMorePend([FromQuery] int pageNumber = 2, [FromQuery] int pageSize = 5)
        {
            var user = CacheEx.DataUser;

            var data = await FriendRequestService.GetListPend(user.ID, pageNumber, pageSize);

            return data;
        }

        [HttpGet("friendonline")]
        public async Task<ApiReponseModel<List<FriendListVM>>> GetOnlineFriend()
        {
            var user = Cache.CacheEx.DataUser;
            var rs = await FriendRequestService.GetOnlineFriendList(user.ID);
            return rs;
        }



    }
}
