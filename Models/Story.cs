using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Story
    {
        public int ID { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsDeleted { get; set; }

        public int UserID { get; set; }

        public string? UserFullName { get; set; }
        public string? ProfilePictureUrl { get; set; }

    }
}
