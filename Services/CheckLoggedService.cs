using Azure.Core;
using Cache;
using Models;
using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class CheckLoggedService
    {
        public static ApiReponseModel<User> CheckLogged(string token)
        {
            if (!CacheEx.CheckTokenEx(token))
            {
                return new ApiReponseModel<User>
                {
                    Status = 0,
                    Mess = "Token không hợp lệ hoặc đã hết hạn",
                    Data = null
                };
            }
            else
            {
                var user = CacheEx.GetUserFromToken(token);

                if (user != null)
                {
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl) &&
                        !user.ProfilePictureUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !user.ProfilePictureUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        user.ProfilePictureUrl = "https://localhost:7024/Media/ShowAvatar?fileName=" + user.ProfilePictureUrl;
                    }
                    else if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        user.ProfilePictureUrl = "https://localhost:7024/Media/ShowAvatar?fileName=user.png";
                    }
                }

                return new ApiReponseModel<User>
                {
                    Status = 1,
                    Mess = "Token hợp lệ",
                    Data = user
                };
            }
        }
    }
}
