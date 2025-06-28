using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Chat
{
    public class OnlineUser
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
