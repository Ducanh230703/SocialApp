using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{

    public class Message
    {
        public int Id { get; set; }

        public int SenderId { get; set; }

        public int TargetId { get; set; }

        public int Type { get; set; }

        public string Content  { get; set; }

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
