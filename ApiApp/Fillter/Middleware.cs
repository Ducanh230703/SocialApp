using Cache;
using Models.ReponseModel;

namespace ApiApp.Fillter
{
    public class Middleware
    {
        private readonly RequestDelegate _next;

        public Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();

            if (path.StartsWith("/swagger") || path.StartsWith("/favicon") || path.Contains("swagger") || path.Contains("media/") || path.Contains("/chathub")||path.Contains("verify-otp")|| path.Contains("send-otp")|| path.Contains("/forgot-password") || path.Contains("/reset-password-with-otp"))
            {
                await _next(context);
                return;
            }

            if (path.Contains("/signin-google")
                || path.Contains("/signin-google-callback")
                || path.Contains("/GoogleLoginCallback")
                || path.Contains("/GoogleCallback"))
            {
                await _next(context);
                return;
            }


            if (path.Contains("/login") || path.Contains("/register"))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault();

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }


            if (!string.IsNullOrEmpty(token))
            {
                if (token.StartsWith("Authorization ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring("Authorization ".Length).Trim();
                }

                if (!CacheEx.CheckTokenEx(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    var apiResponse = new ApiReponseModel<object>
                    {
                        Status = 0,
                        Mess = "Invalid Token",
                    };
                    await context.Response.WriteAsJsonAsync(apiResponse);
                    return;
                }

                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var noTokenResponse = new ApiReponseModel<object>
            {
                Status = 0,
                Mess = "Missing Authorization Token",
            };
            await context.Response.WriteAsJsonAsync(noTokenResponse);
        }
    }
}
