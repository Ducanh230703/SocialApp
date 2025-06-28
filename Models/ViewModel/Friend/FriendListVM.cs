using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Friend
{
    public class FriendListVM
    {
        public int StatusID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
