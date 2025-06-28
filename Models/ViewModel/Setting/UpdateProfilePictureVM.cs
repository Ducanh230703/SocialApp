using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Setting
{
    public class UpdateProfilePictureVM
    {
        public IFormFile ProfilePictureImage { get; set; }
    }
}
