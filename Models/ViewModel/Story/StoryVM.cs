using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModel.Story
{
    public class StoryVM
    {
        public IFormFile? Image { get; set; }
        public string ImageUrl { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
