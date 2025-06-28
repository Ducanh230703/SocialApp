using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Users
{
    public class GetUserProfileVM
    {
        public User user { get; set; }
        public List<Post> Posts { get; set; }
    }
}
