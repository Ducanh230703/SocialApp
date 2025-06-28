    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int ReceiverId { get; set; }
        public int SenderId { get; set; }

        public string Message { get; set; }
        public bool IsRead { get; set; }

        public string Type { get; set; }

        public string RelateId { get; set; }

        public DateTime NotificationDate { get; set; }
        public string ProfilePictureUrl { get; set; }

    }
}
