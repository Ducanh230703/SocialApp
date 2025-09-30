using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Group
    {
        public int ID { get; set; }
        public bool IsPrivate { get; set; }
        public string GroupName { get; set; }

        public string GroupPictureUrl { get; set; } = "user.png";
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    }
}
