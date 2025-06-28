using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Friend
{
    class UserWithFriendsCountVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int FriendsCount { get; set; }
        public string FriendsCountDisplay =>
            FriendsCount == 0 ? "No followers" :
            FriendsCount == 1 ? "1 follower" :
            $"{FriendsCount} followers";
    }
}
