using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Home
{
    public class PostVM
    {
        public string? Content { get; set; }
        public List<IFormFile>? Image { get; set; }
        public string? ImageUrls { get; set; }
        public int? GroupID { get; set; } = null;
    }

}
