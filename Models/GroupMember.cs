using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GroupMember
    {
        public int GroupId { get; set; }
        public int UserID { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member; 
    }

    public enum GroupMemberRole
    {
        Member = 0,
        Admin = 1,
        Owner = 2
    }


}
