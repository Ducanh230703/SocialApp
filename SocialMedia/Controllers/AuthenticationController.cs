    using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ReponseModel;
using SocialMedia.Helper;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SocialMedia.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpGet]
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult Login()
        {
            var token = Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var userInfo = ApiHelper.GetAsync<ApiReponseModel>("/api/User/checkback", token).Result;
                    if (userInfo.Status == 1)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.Error.WriteLine($"Error in Login (GET): {ex}");
                    Response.Cookies.Delete("AuthToken");
                }
            }
            return View(new LoginModel());
        }

        [HttpPost]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await ApiHelper.PostAsync<LoginModel, ApiReponseModel<UserReponseModel>>("/api/User/login", model);

                if (result != null)
                {
                    if (result.Status == 1)
                    {
                        if (result.Data != null && !string.IsNullOrEmpty(result.Data.Token))
                        {
                            Response.Cookies.Append("AuthToken", result.Data.Token, new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.UtcNow.AddHours(1)
                            });

                            Response.Cookies.Append("LoggedInUserId", result.Data.ID.ToString(), new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true, 
                                SameSite = SameSiteMode.None,
                                Expires = DateTimeOffset.UtcNow.AddHours(1)

                            });
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Đăng nhập thất bại: Token không hợp lệ hoặc thiếu.");
                        }
                    }
                    else if (result.Status == 2) 
                    {
                        TempData["UserEmail"] = model.Email;
                        TempData["OTPMess"] = result.Mess;

                        return RedirectToAction("VerifyOtpRegistration", "Authentication");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Mess ?? "Đăng nhập thất bại. Vui lòng thử lại.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in Login (POST): {ex}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau.");
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["AuthToken"];
            var apiReponse = new ApiReponseModel();
            if (token != null)
            {
                apiReponse = await ApiHelper.GetAsync<ApiReponseModel>("/api/User/logout", token);
                if (apiReponse != null && apiReponse.Status == 1)
                {
                    Response.Cookies.Delete("LoggedInUserId");
                    Response.Cookies.Delete("AuthToken");
                    return RedirectToAction("Login", "Authentication");
                }
                else
                    return BadRequest("Lỗi Api");
            }

            else return BadRequest("Đăng xuất thất b");
        }

        public async Task<IActionResult> Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel registerModel)
        {
            var rs = await ApiHelper.PostAsync<RegisterModel, ApiReponseModel>("/api/User/register", registerModel);

            if (rs != null)
            {
                if (rs.Status == 1)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Authentication");
                }
                else if (rs.Status == 2)
                {
                    TempData["UserEmail"] = registerModel.Email;
                    TempData["OTPMess"] = rs.Mess;
                    return RedirectToAction("VerifyOtpRegistration", "Authentication");
                }
                else
                {
                    ModelState.AddModelError("", rs.Mess ?? "Đăng ký thất bại. Vui lòng thử lại.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng thử lại.");
            }

            return View(registerModel);
        }

        public IActionResult LoginWithGoogle()
        {
            return Redirect("https://apiapp20250930133943-a3ewemhsd2egfgeq.canadacentral-01.azurewebsites.net/api/User/login/google");
        }


        [HttpGet]
        public IActionResult GoogleLoginCallback(string token, int id)
        {
            if (!string.IsNullOrEmpty(token))
            {
                Response.Cookies.Append("AuthToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });


                Response.Cookies.Append("LoggedInUserId", id.ToString(), new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)

                });

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Login", "Authentication", new { error = "google_failed" });
        }
        [HttpGet]
        public IActionResult VerifyOtpRegistration()
        {
            if (TempData["UserEmail"] == null)
            {
                return RedirectToAction("Register");
            }
            var model = new OtpVerificationModel { Email = TempData["UserEmail"]?.ToString() };
            TempData.Keep("UserEmail");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtpRegistration(OtpVerificationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await ApiHelper.PostAsync<OtpVerificationModel, ApiReponseModel>("/api/User/verify-otp", model);

                if (result != null && result.Status == 1)
                {
                    TempData.Remove("UserEmail");
                    TempData["SuccessMessage"] = "Xác thực email thành công! Bạn có thể đăng nhập.";
                    return RedirectToAction("Login", "Authentication");
                }
                else
                {
                    ModelState.AddModelError("", result?.Mess ?? "Xác thực OTP thất bại.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in VerifyOtpRegistration (POST): {ex}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xác thực. Vui lòng thử lại sau.");
            }

            TempData.Keep("UserEmail");
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Gọi API Backend để gửi OTP
                var result = await ApiHelper.PostAsync<ForgotPasswordModel, ApiReponseModel>("/api/User/forgot-password", model);

                if (result != null && result.Status == 1)
                {
                    // Lưu email vào TempData để chuyển sang bước ResetPassword
                    TempData["ResetPasswordEmail"] = model.Email;
                    TempData["ResetPasswordMessage"] = result.Mess;

                    return RedirectToAction("ResetPassword");
                }
                else
                {
                    ModelState.AddModelError("", result?.Mess ?? "Yêu cầu OTP thất bại. Vui lòng kiểm tra email.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in ForgotPassword (POST): {ex}");
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            // Kiểm tra xem Email đã được gửi từ ForgotPassword chưa
            var email = TempData["ResetPasswordEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                // Nếu không có email, quay lại trang ForgotPassword
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordModel { Email = email };

            // Giữ lại email và thông báo để dùng cho POST và hiển thị View
            TempData.Keep("ResetPasswordEmail");
            TempData.Keep("ResetPasswordMessage");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            // Giữ lại email cho trường hợp ModelState không hợp lệ
            TempData.Keep("ResetPasswordEmail");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Đảm bảo Email trong model luôn có
            if (string.IsNullOrEmpty(model.Email))
            {
                model.Email = TempData["ResetPasswordEmail"]?.ToString();
                if (string.IsNullOrEmpty(model.Email))
                {
                    ModelState.AddModelError("", "Phiên đặt lại mật khẩu đã hết hạn hoặc không hợp lệ.");
                    return View(model);
                }
            }

            try
            {
                // Gọi API Backend để xác thực OTP và cập nhật mật khẩu
                var result = await ApiHelper.PostAsync<ResetPasswordModel, ApiReponseModel>("/api/User/reset-password-with-otp", model);

                if (result != null && result.Status == 1)
                {
                    TempData.Remove("ResetPasswordEmail");
                    TempData.Remove("ResetPasswordMessage");
                    TempData["SuccessMessage"] = result.Mess;

                    // Chuyển về trang đăng nhập sau khi thành công
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError("", result?.Mess ?? "Đặt lại mật khẩu thất bại. Vui lòng kiểm tra OTP và thử lại.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in ResetPassword (POST): {ex}");
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            TempData.Keep("ResetPasswordEmail"); // Giữ lại email nếu bị lỗi và cần hiển thị lại form
            return View(model);
        }

    }
}
