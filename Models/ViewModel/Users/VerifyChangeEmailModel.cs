using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Users
{
    public class VerifyChangeEmailModel
    {
        public string NewEmail { get; set; }
        public string OtpCode { get; set; }
    }
}
