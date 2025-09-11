using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Users;
using Services;
using System.Runtime;
using System.Security.Claims;
using Umbraco.Core.Models.Membership;
using User = Models.User;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ApiReponseModel> Register([FromBody] RegisterModel rmd)
        {
            var rs = await UserSerivce.UserRegister(rmd.Email, rmd.Password, rmd.FullName);
            return rs;

        }

        [HttpPost("login")]
        public async Task<ApiReponseModel<UserReponseModel>> Login([FromBody] LoginModel loginModel)
        {
            var rs = await UserSerivce.Login(loginModel.Email, loginModel.Password);

            return rs;
        }

        [HttpGet("logout")]
        public async Task<ApiReponseModel> Logout()
        {
            var rs = new ApiReponseModel
            {
                Status = 1,
                Mess = "Đăng xuất thành công"
            };
            var token = Request.Headers["Authorization"].ToString();
            if (token.StartsWith("Authorization ", StringComparison.OrdinalIgnoreCase) && token.Length > "Authorization ".Length)
            {
                token = token.Substring("Authorization ".Length);
            }
            if (Cache.CacheEx.CleanUpTokens(token) == false)
            {
                rs.Status = 0;
                rs.Mess = "Đăng xuất thấy bại";
            }  
            return rs;
        }


        [HttpGet("getall")]
        public async Task<List<User>> GetAll()
        {
            var data = await UserSerivce.GetAllUsers();
            return data;
        }

        [HttpGet("checkback")]
        public async Task<ApiReponseModel> CheckBack()
        {
            var user = Cache.CacheEx.DataUser;
            if (user != null)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Còn phiên"
                };
            else
                return new ApiReponseModel
                    {
                        Status = 0,
                        Mess = "Hết phiên"
                    };
        }

        [HttpGet("checklog")]
        public async Task<ApiReponseModel<User>> CheckLog()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Authorization ", "");
            var user = CheckLoggedService.CheckLogged(token);
            return user;
        }

        [HttpGet("details")]

        public async Task<ApiReponseModel<UserInfo>> Details(int UserID,[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5) 
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var data = await UserSerivce.GetUserInfoWithPaginatedPosts(UserID,pageNumber,pageSize);
            return data;
        }

        [HttpPost("upavatar")]
        [Consumes("multipart/form-data")]
        public async Task<ApiReponseModel> UploadAvatar([FromForm] IFormFile image, [FromForm] string removeUrl)
        {
            var user = Cache.CacheEx.DataUser;
            ApiReponseModel data = null;
            string uniqueFileName = null;
            if (image!= null)
            {
                uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Avatar", uniqueFileName);

                data = await UserSerivce.UploadAvatar(user.ID, uniqueFileName);
                if (data.Status == 1)
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    if (Uri.TryCreate(removeUrl, UriKind.Absolute, out Uri uriResult))
                    {

                        var queryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uriResult.Query);
                        if (queryString.TryGetValue("fileName", out Microsoft.Extensions.Primitives.StringValues fileNameValues))
                        {
                            removeUrl = fileNameValues.FirstOrDefault();
                        }
                    }

                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Avatar", removeUrl);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                } 


            }
            
            return data;
        }

        [HttpGet("getedit")]
        public async Task<ApiReponseModel<EditInfo>> GetEdit()
        {
            var user = Cache.CacheEx.DataUser;
            var data = await UserSerivce.GetUserInfoEdit(user.ID);
            return data;
        }

        [HttpPost("editinfo")]
        public async Task<ApiReponseModel> EditInfoUser([FromBody] EditInfo editInfo)
        {
            var user = Cache.CacheEx.DataUser;
            var rs = await UserSerivce.EditInfo(user.ID, editInfo.Email, editInfo.FullName, editInfo.Bio);
            return rs;
        }

        [HttpGet("getuser/{userId}")]
        public async Task<ApiReponseModel<UserOnline>> GetUserOnline(int userId)
        {
            var rs = await UserSerivce.GetUserById(userId);
            return rs;
        }

        [HttpGet("login/google")]
        public IActionResult LoginGoogle()
        {   
            var properties = new AuthenticationProperties
            {
                RedirectUri = "https://localhost:7024/api/User/signin-google    "
            };
            return Challenge(properties, "Google");
        }

        [HttpGet("signin-google")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync("Google");

            if (result.Succeeded)
            {
                var claims = result.Principal.Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest("Đăng nhập Google thất bại: Không lấy được email.");
                }

                var loginResult = await UserSerivce.LoginOrRegisterWithGoogle(email, fullName);

                if (loginResult.Status == 1 && loginResult.Data != null)
                {
                    var token = loginResult.Data.Token;
                    return Content($@"
                <script>
                    if (window.opener) {{
                        window.opener.postMessage({{ token: '{token}' }}, '{Request.Scheme}://{Request.Host}');
                        window.close();
                    }}
                </script>", "text/html");
                }
                else
                {
                    return BadRequest(loginResult.Mess);
                }
            }

            return BadRequest("Đăng nhập Google thất bại.");
        }
    }
}
