using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class ActivityLogResponse
    {
        public int ID { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string ActionType { get; set; }
        public string TargetType { get; set; }
        public int TargetId { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
