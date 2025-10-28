    using System;
    using System.Collections.Generic;
    using Models.ReponseModel;

    namespace Models.ResponseModels;

    public class GroupDetailResponseModel
    {
        public int ID { get; set; }
        public bool IsPrivate { get; set; }
        public string GroupName { get; set; }
        public string GroupPictureUrl { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }

        public string CreatedByUserName { get; set; }
        public string? CreatedByUserProfilePictureUrl { get; set; }

        public int MemberCount { get; set; }
        public bool IsMember { get; set; }
        public int? CurrentUserRole { get; set; }
    
        public List<PostFull> RecentPosts { get; set; } = new List<PostFull>();
    }
