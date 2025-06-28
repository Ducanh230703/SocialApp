using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Users
{
    public class EditInfo
    {
        public string? ProfilePictureUrl { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? Bio { get; set; }
    }
}
