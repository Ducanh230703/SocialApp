using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class MessengerList
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string LastMessageContent { get; set; }
        public DateTime LastMessageSentdate { get; set; }
        public int LastMessageSenderId { get; set; }
    }
}
