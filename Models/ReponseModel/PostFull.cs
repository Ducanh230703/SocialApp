using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class PostFull
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPrivate { get; set; }

        public string? Bio { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool IsDeleted { get; set; }

        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public string? UserProfilePictureUrl { get; set; }
        public int? GroupID { get; set; } = null;
        public int? GroupName { get; set; } = null;

        public List<int>? LikeUserIds { get; set; } = new List<int>();
        public List<CommentDetail>? Comments { get; set; } = new List<CommentDetail>();
    }

}
