using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class FriendRequestRP
    {
        public int ID { get; set; }
        public int Status { get; set; }
        public int SenderID { get; set; }
        public int ReceiverID { get; set; }
    }
}
