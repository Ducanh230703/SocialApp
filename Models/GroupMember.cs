using System;

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
        Pending = -1, 
        Member = 0,
        Admin = 1,
        Owner = 2
    }
}
