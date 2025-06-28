using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class CommentDetail
    {
        public int ID { get; set; }
        public int PostId { get; set; }
        public string Content { get; set; }
        public DateTime DateCreated { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string? UserProfilePictureUrl { get; set; }
    }

}
