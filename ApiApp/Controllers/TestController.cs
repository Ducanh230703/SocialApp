//using Microsoft.AspNetCore.Mvc;
//using static Umbraco.Core.Constants.Conventions;

//namespace ApiApp.Controllers
//{
//    public class TestController : Controller
//    {
//        public async Task AuthenticateWithGoogle(HttpContext httpContext, string code)
//        {
//            if (string.IsNullOrEmpty(code))
//            {
//                return (false, _localizer["Xin lỗi, không xác định được thông tin nhân viên !. Mời bạn thử lại!"], "/Account/Login");
//            }


//            var clientID = domainInfo?.ClientId;
//            var clientSecret = domainInfo?.GG_secret_clientId;
//            //var clientID = "64774674383-hhm717530pqsdkcr421hnp1o1l36k9ul.apps.googleusercontent.com";
//            //var clientSecret = "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";

//            if (string.IsNullOrEmpty(clientID) || string.IsNullOrEmpty(clientSecret))
//            {
//                return (false, _localizer["Xin lỗi, không xác định được thông tin nhân viên !. Mời bạn thử lại!"], "/Account/Login");
//            }

//            var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/GoogleResponse"; ;

//            using var client = new HttpClient();
//            var tokenRequestContent = new FormUrlEncodedContent(new[]
//            {
//                new KeyValuePair("code", code),
//                new KeyValuePair("client_id", clientID),
//                new KeyValuePair("client_secret", clientSecret),
//                new KeyValuePair("redirect_uri", redirectUri),
//                new KeyValuePair("grant_type", "authorization_code")
//            });

//            var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequestContent);
//            if (!tokenResponse.IsSuccessStatusCode)
//            {
//                return (false, _localizer["Xin lỗi, không xác định được thông tin nhân viên !. Mời bạn thử lại!"], "/Account/Login");
//            }

//            var tokenData = await tokenResponse.Content.ReadFromJsonAsync();
//            if (tokenData?.IdToken is null)
//            {
//                return (false, _localizer["Xin lỗi, không xác định được thông tin nhân viên !. Mời bạn thử lại!"], "/Account/Login");
//            }

//            var email = EncryptHelper.ExtractEmailFromToken(tokenData.IdToken);
//            if (string.IsNullOrEmpty(email))
//            {
//                return (false, _localizer["Xin lỗi, không xác định được thông tin nhân viên !. Mời bạn thử lại!"], "/Account/Login");
//            }

//            return (true, _localizer["Đăng nhập thành công"], "/Workplace/Home/Index");
//        }
//    }

//    public IActionResult LoginWithGoogle()
//    {

//        var clientID = domainInfo.ClientId;
//        if (!string.IsNullOrEmpty(clientID))
//        {
//            var redirectUri = Url.Action("GoogleResponse", "Account", null, Request.Scheme);
//            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/auth?client_id={clientID}&redirect_uri={redirectUri}&scope=https://www.googleapis.com/auth/userinfo.email&response_type=code&state=custom_state";
//            return Redirect(googleAuthUrl);
//        }

//        return RedirectToAction("Login", "Account");
//    }

//    [HttpGet]
//    public async Task GoogleResponse(string code, string state)
//    {
//        var result = await _authService.AuthenticateWithGoogle(HttpContext, code);
//        if (!result.IsSuccess)
//        {
//            return RedirectToAction("Login", "Account", new { msgErr = result.Message });
//        }

//        return RedirectToAction("Index", "Home", new { area = "" });
//    }

//}
