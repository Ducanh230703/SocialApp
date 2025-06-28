using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Friend
{
    public class FriendRequestVM
    {
        public int Status { get; set; }

        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
    }
}
