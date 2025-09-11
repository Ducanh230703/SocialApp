using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

                    if (result != null && result.Status == 1)
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
                        });

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Đăng nhập thất bại: Token không hợp lệ hoặc thiếu.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", result?.Mess ?? "Đăng nhập thất bại. Vui lòng thử lại.");
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
            var rs = await ApiHelper.PostAsync < RegisterModel, ApiReponseModel>("/api/User/register", registerModel);

            if (rs != null)
            {
                if (rs.Status == 1)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Authentication");
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
            // Chuyển hướng tới API backend (Google auth)
            return Redirect("https://localhost:7024/api/User/login/google");
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

                Response.Cookies.Append("LoggedInUserId", id.ToString(), new CookieOptions { });

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Login", "Authentication", new { error = "google_failed" });
        }

    }
}
