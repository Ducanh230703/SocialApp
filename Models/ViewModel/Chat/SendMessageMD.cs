using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Chat
{
    public class SendMessageMD
    {
        public int SenderId { get; set; }
        public int TargetId { get; set; }
        public int Type { get; set; } = 0;
        public string MessageContent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
