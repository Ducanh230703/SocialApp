using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Group
{
    public class CreateGroupForm
    {
        public bool IsPrivate { get; set; }
        public string GroupName { get; set; }
        public IFormFile? Image { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? GroupPictureUrl { get; set; }

    }
}
