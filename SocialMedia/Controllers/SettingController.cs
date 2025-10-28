using Microsoft.AspNetCore.Mvc;

namespace SocialMedia.Controllers
{
    public class SettingController : Controller
    {
        public IActionResult ChangePassword() => View();
        public IActionResult ChangeEmail() => View();
    }
}
