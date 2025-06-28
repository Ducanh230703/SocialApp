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
    }

    public class FileModel
    {
        public int ID { get; set; } = 0;
        public string UrlMedia { get; set; }
        public IFormFile? Image { get; set; }
        public bool? IsDelete { get; set; } = false;
    }
}
