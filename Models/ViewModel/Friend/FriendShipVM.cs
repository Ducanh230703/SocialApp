using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Friend
{
    public class FriendShipVM
    {
        public PaginatedResponse<FriendListVM>? Friend { get; set; }
        public PaginatedResponse<FriendListVM>? FriendRequestsSent { get; set; }
        public PaginatedResponse<FriendListVM>? FriendRequestsReceived { get; set; }
    }
}
