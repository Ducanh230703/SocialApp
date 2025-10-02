using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class User
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public  string? Password { get; set; }
        public string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsVerified { get; set; }
        public bool IsOnline { get; set; } = false;
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
