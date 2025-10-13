using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiApp.Controllers
{
    public class MediaController : Controller
    {
        //public IActionResult ShowImage([FromQuery] string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName))
        //    {
        //        return BadRequest("FileName null");
        //    }

        //    var imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Image", "Upload");
        //    var filePath = Path.Combine(imageUploadPath, fileName);
        //    if (!System.IO.File.Exists(filePath))
        //    {
        //        return NotFound($"Không tìm thấy file ảnh: {fileName}");
        //    }

        //    string contentType;
        //    var fileExtension = Path.GetExtension(fileName).ToLowerInvariant(); 
        //    switch (fileExtension)
        //    {
        //        case ".jpg":
        //        case ".jpeg":
        //            contentType = "image/jpeg";
        //            break;
        //        case ".png":
        //            contentType = "image/png";
        //            break;
        //        case ".gif":
        //            contentType = "image/gif";
        //            break;
        //        case ".bmp":
        //            contentType = "image/bmp";
        //            break;
        //        case ".webp":
        //            contentType = "image/webp";
        //            break;
        //        default:
        //            contentType = "application/octet-stream";
        //            break;
        //    }
        //    return File(System.IO.File.OpenRead(filePath), contentType);
        //}

            public IActionResult ShowAvatar([FromQuery] string fileName)
            {
            var defaultAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "Image","Upload", "user.png");

            if (string.IsNullOrEmpty(fileName))
            {
                if (System.IO.File.Exists(defaultAvatarPath))
                {
                    return File(System.IO.File.OpenRead(defaultAvatarPath), "image/png");
                }
                else
                {
                    return NotFound("FileName is null or empty, and default avatar 'user.png' was not found.");
                }
            }

            var imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Image", "Upload");
                var filePath = Path.Combine(imageUploadPath, fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Không tìm thấy file ảnh: {fileName}");
                }

                string contentType;
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                switch (fileExtension)
                {
                    case ".jpg":
                    case ".jpeg":
                        contentType = "image/jpeg";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    case ".gif":
                        contentType = "image/gif";
                        break;
                    case ".bmp":
                        contentType = "image/bmp";
                        break;
                    case ".webp":
                        contentType = "image/webp";
                        break;
                    default:
                        contentType = "application/octet-stream";
                        break;
                }
                return File(System.IO.File.OpenRead(filePath), contentType);
            }
    }
}
