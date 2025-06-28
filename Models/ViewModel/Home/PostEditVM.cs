using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Home
{
    public class PostEditVM
    {
        public int PostId { get; set; }
        public string? Content { get; set; }
        public List<IFormFile>? Image { get; set; }
        public List<string>? RemovedImageUrls { get; set; }

        public string? ImageUrls { get; set; }
    }
}
