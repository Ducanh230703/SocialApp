using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.GroupMember
{
    public class GroupMemberList
    {
        public int GroupId { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; }
        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
        public string ProfilePictureUrl { get; set; }
    }
}
