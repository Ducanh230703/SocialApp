using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Group
{
    public class GroupSearchResult
    {
        public int ID { get; set; }
        public string GroupName { get; set; }
        public string GroupPictureUrl { get; set; }
        public bool IsPrivate { get; set; }
        public int MemberCount { get; set; }
    }
}

