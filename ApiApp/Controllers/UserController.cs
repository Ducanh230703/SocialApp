using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Users;
using NPoco.fastJSON;
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
        private readonly EmailService _emailService;

        public UserController(EmailService emailService)
        {
            _emailService = emailService;
        }
        [HttpPost("register")]
        public async Task<ApiReponseModel> Register([FromBody] RegisterModel rmd)
        {
            // Giả định UserSerivce.UserRegister trả về Status = 1 nếu đăng ký thành công
            var rs = await UserSerivce.UserRegister(rmd.Email, rmd.Password, rmd.FullName);

            if (rs.Status == 1)
            {
                // 1. Tạo và lưu OTP
                var otpCode = new Random().Next(100000, 999999).ToString();
                bool isCached = Cache.CacheEx.SetOtp(rmd.Email, otpCode, TimeSpan.FromMinutes(5));

                if (!isCached)
                {
                    // Nếu cache lỗi, trả về lỗi hệ thống
                    rs.Status = 0;
                    rs.Mess = "Đăng ký thành công nhưng lỗi hệ thống khi chuẩn bị gửi xác thực.";
                    return rs;
                }

                // 2. Gửi Email chứa mã OTP
                bool emailSent = await _emailService.SendOtpEmail(rmd.Email, otpCode);

                if (emailSent)
                {
                    rs.Mess = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.";
                    // Trả về Status = 2 để Frontend biết là cần chuyển sang màn hình xác thực OTP
                    rs.Status = 2;
                }
                else
                {
                    // Nếu gửi email lỗi, nên xóa OTP khỏi cache
                    Cache.CacheEx.CleanUpTokens(rmd.Email);
                    rs.Status = 0;
                    rs.Mess = "Đăng ký thành công nhưng lỗi khi gửi email xác thực. Vui lòng thử lại sau.";
                }
            }

            return rs;
        }

        [HttpPost("login")]
        public async Task<ApiReponseModel<UserReponseModel>> Login([FromBody] LoginModel loginModel)
        {
            var rs = await UserSerivce.Login(loginModel.Email, loginModel.Password,_emailService);

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
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Upload", uniqueFileName);

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

                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Image/Upload", removeUrl);
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
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "User", null, Request.Scheme)
            };

            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return Redirect("https://socialmedia20250930142855-gegwd5esgrcvczdz.canadacentral-01.azurewebsites.net/Authentication/Login?error=google_failed");
            }

            var claims = result.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Redirect("https://socialmedia20250930142855-gegwd5esgrcvczdz.canadacentral-01.azurewebsites.net/Authentication/Login?error=no_email");
            }

            var loginResult = await UserSerivce.LoginOrRegisterWithGoogle(email, fullName);

            if (loginResult.Status == 1 && loginResult.Data != null)
            {
                return Redirect($"https://socialmedia20250930142855-gegwd5esgrcvczdz.canadacentral-01.azurewebsites.net/Authentication/GoogleLoginCallback?token={loginResult.Data.Token}&id={loginResult.Data.ID}");
            }

            return Redirect("https://socialmedia20250930142855-gegwd5esgrcvczdz.canadacentral-01.azurewebsites.net/Authentication/Login?error=google_failed");
        }

        [HttpPost("send-otp")]
        public async Task<ApiReponseModel> SendOtp([FromBody] OtpRequestModel request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return new ApiReponseModel { Status = 0, Mess = "Email không được để trống." };
            }

            var otpCode = new Random().Next(100000, 999999).ToString();

            bool isCached = Cache.CacheEx.SetOtp(request.Email, otpCode, TimeSpan.FromMinutes(5));

            if (!isCached)
            {
                return new ApiReponseModel { Status = 0, Mess = "Lỗi hệ thống khi lưu OTP." };
            }

            bool emailSent = await _emailService.SendOtpEmail(request.Email, otpCode);

            if (emailSent)
            {
                return new ApiReponseModel { Status = 1, Mess = "Mã OTP đã được gửi đến email của bạn." };
            }
            else
            {
                Cache.CacheEx.CleanUpTokens(request.Email);
                return new ApiReponseModel { Status = 0, Mess = "Lỗi khi gửi email OTP. Vui lòng thử lại." };
            }
        }

        [HttpPost("verify-otp")]
        public async Task<ApiReponseModel> VerifyOtp([FromBody] OtpVerificationModel model)
        {
            var storedOtp = Cache.CacheEx.GetOtp(model.Email);

            if (string.IsNullOrEmpty(storedOtp))
            {
                return new ApiReponseModel { Status = 0, Mess = "Mã OTP đã hết hạn hoặc không tồn tại." };
            }

            if (storedOtp == model.OtpCode)
            {
                var verifyResult = await UserSerivce.VerifyUserEmail(model.Email);

                Cache.CacheEx.CleanUpTokens(model.Email);

                return verifyResult;
            }
            else
            {
                return new ApiReponseModel { Status = 0, Mess = "Mã OTP không đúng." };
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ApiReponseModel> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return new ApiReponseModel { Status = 400, Mess = "Email không hợp lệ." };
            }

            var rs = await UserSerivce.SendOtpForPasswordReset(model.Email,_emailService);
            return rs;
        }

        [HttpPost("reset-password-with-otp")]
        public async Task<ApiReponseModel> ResetPasswordWithOtp([FromBody] VerifyOtpAndResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return new ApiReponseModel { Status = 400, Mess = "Dữ liệu nhập vào không hợp lệ." };
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return new ApiReponseModel { Status = 400, Mess = "Mật khẩu mới và mật khẩu xác nhận không khớp." };
            }

            var rs = await UserSerivce.ResetPasswordWithOtp(model.Email, model.Otp, model.NewPassword);

            return rs;
        }


        [HttpPost("change-password")]
        public async Task<ApiReponseModel> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var user = Cache.CacheEx.DataUser;
            if (user == null)
                return new ApiReponseModel { Status = 401, Mess = "Người dùng chưa đăng nhập." };

            if (string.IsNullOrEmpty(model.OldPassword) || string.IsNullOrEmpty(model.NewPassword))
                return new ApiReponseModel { Status = 400, Mess = "Vui lòng nhập đầy đủ thông tin." };

            if (model.NewPassword != model.ConfirmPassword)
                return new ApiReponseModel { Status = 400, Mess = "Mật khẩu xác nhận không khớp." };

            var rs = await UserSerivce.ChangePassword(user.ID, model.OldPassword, model.NewPassword);
            return rs;
        }


        [HttpPost("change-email")]
        public async Task<ApiReponseModel> ChangeEmail([FromBody] ChangeEmailModel model)
        {
            var user = Cache.CacheEx.DataUser;
            if (user == null)
                return new ApiReponseModel { Status = 401, Mess = "Người dùng chưa đăng nhập." };

            if (string.IsNullOrEmpty(model.NewEmail))
                return new ApiReponseModel { Status = 400, Mess = "Email mới không được để trống." };

            // Gửi OTP đến email mới
            var otp = new Random().Next(100000, 999999).ToString();
            Cache.CacheEx.SetOtp(model.NewEmail, otp, TimeSpan.FromMinutes(5));

            bool sent = await _emailService.SendOtpEmail(model.NewEmail, otp);
            if (!sent)
                return new ApiReponseModel { Status = 0, Mess = "Lỗi khi gửi mã OTP. Vui lòng thử lại." };

            return new ApiReponseModel { Status = 1, Mess = "Mã OTP đã được gửi đến email mới. Vui lòng xác nhận." };
        }


        [HttpPost("verify-change-email")]
        public async Task<ApiReponseModel> VerifyChangeEmail([FromBody] VerifyChangeEmailModel model)
        {
            var user = Cache.CacheEx.DataUser;
            if (user == null)
                return new ApiReponseModel { Status = 401, Mess = "Người dùng chưa đăng nhập." };

            var storedOtp = Cache.CacheEx.GetOtp(model.NewEmail);
            if (string.IsNullOrEmpty(storedOtp))
                return new ApiReponseModel { Status = 0, Mess = "Mã OTP đã hết hạn hoặc không tồn tại." };

            if (storedOtp != model.OtpCode)
                return new ApiReponseModel { Status = 0, Mess = "Mã OTP không chính xác." };

            var rs = await UserSerivce.ChangeEmail(user.ID, model.NewEmail);
            Cache.CacheEx.CleanUpOtp(model.NewEmail);

            return rs;
        }

        [HttpGet("getmorepost")]
        public async Task<ApiReponseModel<PaginatedResponse<PostFull>>> getmorepost([FromQuery] int profileId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var data = await UserSerivce.GetPostById(pageNumber, pageSize, profileId);
            if (data != null)
            {
                return new ApiReponseModel<PaginatedResponse<PostFull>>
                {
                    Status = 1,
                    Mess = "Tải bài viết thành công",
                    Data = data
                };
            }
            else
            {
                return new ApiReponseModel<PaginatedResponse<PostFull>>
                {
                    Status = 0,
                    Mess = "Tải bài viết thất bại",
                };
            }
        }

        [HttpGet("getmorepostindex")]
        public async Task<ApiReponseModel<PaginatedResponse<PostFull>>> getmorepostindex([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var data = await UserSerivce.GetPostByIdIndex(pageNumber, pageSize, Cache.CacheEx.DataUser.ID);
            if (data != null)
            {
                return new ApiReponseModel<PaginatedResponse<PostFull>>
                {
                    Status = 1,
                    Mess = "Tải bài viết thành công",
                    Data = data
                };
            }
            else
            {
                return new ApiReponseModel<PaginatedResponse<PostFull>>
                {
                    Status = 0,
                    Mess = "Tải bài viết thất bại",
                };
            }
        }

    }
}
