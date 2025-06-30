using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Chat;
using Services;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        [HttpPost("sendmessage")]
        public async Task <ApiReponseModel> SendMessage([FromBody] SendMessageMD message)
        {
            var data = await MessageService.SendMessage(message);

            return data;

        }

        [HttpGet("getallmessage")]

        public  async Task <ApiReponseModel<ChatMessageVM>> GetChatMessageHistory (int targetUserId,[FromQuery]int pageNumber = 1, [FromQuery] int pageSize =10)
        {
            var data = await MessageService.GetChatMessageHistory(CacheEx.DataUser.ID, targetUserId, pageNumber, pageSize);
            return data;
        }

        [HttpGet("messengerlist")]
        public async Task <ApiReponseModel<PaginatedResponse<MessengerList>>> GetMessengerList([FromQuery] int pageNumber, [FromQuery] int pageSize)
        {
            var data = await MessageService.GetMessengerList(pageNumber, pageSize);
            return data;
        }
    }
}
