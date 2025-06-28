using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Chat
{
    public class ChatMessageVM
    {
            public int TargetUserId { get; set; }
            public string TargetUserFullName { get; set; } 
            public string TargetUserProfilePictureUrl { get; set; }
            public int CurrentLoggedInUserId { get; set; }
            public PaginatedResponse<Message> Messages { get; set; }
    }
}
