    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ActivityLog
    {
        public int ID { get; set; }

        public int UserId { get; set; }        
        public string ActionType { get; set; }
        public string TargetType { get; set; } 
        public int TargetId { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }

    }
}
