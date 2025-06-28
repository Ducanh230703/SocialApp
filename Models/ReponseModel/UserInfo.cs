using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class UserInfo
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? Bio { get; set; }
        public string ProfilePictureUrl { get; set; }
        public PaginatedResponse<PostFull>? ListPost { get; set; }

        public List<ListFriend> ListFriend { get; set; } = new List<ListFriend>();

    }
}
