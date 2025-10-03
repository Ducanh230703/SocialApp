using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client.Extensions.Msal;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Users;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Cache;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    // Bỏ STATIC khỏi class
    public class UserSerivce
    {
        private readonly EmailService _emailService;

        // Constructor để nhận EmailService qua Dependency Injection

        public static string apiHost;
        public static string apiAvatar;

        // Giữ UserRegister là static để không làm thay đổi các đoạn code RegisterWithGoogle
        public static async Task<ApiReponseModel> UserRegister(string Email, string? Password, string FullName)
        {
            string PasswordHash = null;

            if (string.IsNullOrEmpty(Email))
            {
                return new ApiReponseModel
                {
                    Status = 400,
                    Mess = "Email cannot be null or empty.",
                };
            }
            else if (!IsValidEmail(Email))
            {
                return new ApiReponseModel
                {
                    Status = 400,
                    Mess = "Invalid email format.",
                };
            }

            if (string.IsNullOrEmpty(Password))
            {
                return new ApiReponseModel
                {
                    Status = 400,
                    Mess = "Password cannot be null or empty.",
                };
            }
            else
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
            }

            if (string.IsNullOrEmpty(FullName))
            {
                return new ApiReponseModel
                {
                    Status = 400,
                    Mess = "Full name cannot be null or empty.",
                };
            }

            var inParams = new SortedList
            {
                {"@Email", Email},
                {"@Password",(object) PasswordHash ?? DBNull.Value},
                {"@FullName", FullName }
            };

            string[] outParamNames = { "@Status", "@Message" };

            var spResult = await connectDB.ExecuteStoredProcedure(
                "usp_RegisterUser",
                inParams,
                outParamNames
            );

            int status = (int)spResult["Status"];
            string message = spResult["Message"]?.ToString();

            if (status == 1)
            {
                return new ApiReponseModel
                {
                    Status = status,
                    Mess = message,
                };
            }
            else if (status == -1)
            {
                return new ApiReponseModel
                {
                    Status = 409,
                    Mess = message,
                };
            }
            else
            {
                return new ApiReponseModel
                {
                    Status = 500,
                    Mess = message ?? "Đã xảy ra lỗi không xác định khi đăng ký.",
                };
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<ApiReponseModel> SendOtpForPasswordReset(string Email,EmailService emailService)
        {
            string sql = "SELECT ID FROM Users WHERE Email = @Email";
            var param = new SortedList
            {
                { "Email", Email }
            };

            DataTable userDt = await connectDB.Select(sql, param);
            if (userDt == null || userDt.Rows.Count == 0)
            {
                return new ApiReponseModel { Status = 404, Mess = "Email không tồn tại trong hệ thống." };
            }

            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            TimeSpan expiryDuration = TimeSpan.FromMinutes(5);
            bool isSet = Cache.CacheEx.SetOtp(Email, otp, expiryDuration);

            if (!isSet)
            {
                return new ApiReponseModel
                {
                    Status = 500,
                    Mess = "Lỗi hệ thống khi tạo OTP. Vui lòng thử lại."
                };
            }

            try
            {

                bool emailSent = await emailService.SendOtpEmail(Email, otp);

            }
            catch (Exception)
            {
                Cache.CacheEx.CleanUpOtp(Email);
                return new ApiReponseModel
                {
                    Status = 500,
                    Mess = "Lỗi hệ thống khi gửi OTP. Vui lòng thử lại."
                };
            }

            return new ApiReponseModel
            {
                Status = 1,
                Mess = $"Mã OTP đã được gửi đến {Email}. Mã này sẽ hết hạn sau 5 phút."
            };
        }

        public static async Task<ApiReponseModel> ResetPasswordWithOtp(string Email, string Otp, string NewPassword)
        {
            // BƯỚC 1: XÁC THỰC OTP
            string storedOtp = Cache.CacheEx.GetOtp(Email);

            if (storedOtp == null || storedOtp != Otp)
            {
                return new ApiReponseModel
                {
                    Status = 400,
                    Mess = "Mã OTP không hợp lệ hoặc đã hết hạn."
                };
            }

            // BƯỚC 2: CẬP NHẬT MẬT KHẨU
            // Mã hóa mật khẩu mới
            string PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);

            // *** THỰC HIỆN TRUY VẤN DB ĐỂ CẬP NHẬT MẬT KHẨU ***
            // Dùng cột 'Password' theo DB hiện tại của bạn
            string sqlUpdate = "UPDATE Users SET Password = @PasswordHash, DateUpdated = GETDATE() WHERE Email = @Email";
            var updateParam = new SortedList
            {
                { "@PasswordHash", PasswordHash },
                { "@Email", Email }
            };

            int rowsAffected = await connectDB.Update(sqlUpdate, updateParam);

            if (rowsAffected > 0)
            {
                // BƯỚC 3: XÓA OTP KHỎI CACHE SAU KHI CẬP NHẬT THÀNH CÔNG (quan trọng cho bảo mật)
                Cache.CacheEx.CleanUpOtp(Email);

                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Đặt lại mật khẩu thành công."
                };
            }
            else
            {
                return new ApiReponseModel
                {
                    Status = 500,
                    Mess = "Lỗi hệ thống khi cập nhật mật khẩu. Vui lòng thử lại."
                };
            }
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel<UserReponseModel>> Login(string Email, string Password, EmailService emailService)
        {
            User rs = new User();

            UserReponseModel userReponse = new UserReponseModel();

            string sql = "SELECT TOP 1 * FROM Users WHERE Email = @Email";
            var param = new System.Collections.SortedList
            {
                { "Email", Email }
            };

            DataTable user = await connectDB.Select(sql, param);
            if (user == null || user.Rows.Count == 0)
            {
                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 0,
                    Mess = "Email không tồn tại",
                    Data = null

                };
            }

            DataRow row = user.Rows[0];
            string hashedPassword = row["Password"].ToString();

            bool isPasswordMatch = BCrypt.Net.BCrypt.Verify(Password, hashedPassword);

            if (!isPasswordMatch)
            {
                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 0,
                    Mess = "Sai mật khẩu"
                };
            }

            bool isVerified = Convert.ToBoolean(row["IsVerified"]);
            if (!isVerified)
            {
                var otpCode = new Random().Next(100000, 999999).ToString();
                bool isCached = Cache.CacheEx.SetOtp(Email, otpCode, TimeSpan.FromMinutes(5));

                if (!isCached)
                {
                    Cache.CacheEx.CleanUpOtp(Email);
                    return new ApiReponseModel<UserReponseModel>
                    {
                        Status = 0,
                        Mess = "Lỗi hệ thống khi tạo mã xác thực.",
                        Data = null
                    };
                }

                // Gửi email OTP bằng service đã được inject
                bool emailSent = await emailService.SendOtpEmail(Email, otpCode);

                if (emailSent)
                {
                    return new ApiReponseModel<UserReponseModel>
                    {
                        Status = 2, // TRẢ VỀ STATUS 2 YÊU CẦU XÁC THỰC OTP
                        Mess = "Tài khoản của bạn chưa được xác thực. Mã OTP mới đã được gửi đến email.",
                        Data = null
                    };
                }
                else
                {
                    // Nếu gửi email thất bại, xóa OTP đã cache để tránh lỗi
                    Cache.CacheEx.CleanUpTokens(Email);
                    return new ApiReponseModel<UserReponseModel>
                    {
                        Status = 0,
                        Mess = "Lỗi khi gửi email xác thực. Vui lòng thử lại sau.",
                        Data = null
                    };
                }
            }

            // ĐĂNG NHẬP THÀNH CÔNG (Đã xác thực)
            rs.Email = row["Email"].ToString();
            rs.FullName = row["FullName"].ToString();
            rs.Bio = row["Bio"].ToString();
            rs.ID = Convert.ToInt16(row["ID"]);
            rs.ProfilePictureUrl = row["ProfilePictureUrl"].ToString();

            string token = Cache.CacheEx.SetTokenEx(rs);
            userReponse.ID = rs.ID;
            userReponse.Email = rs.Email;
            userReponse.FullName = rs.FullName;
            userReponse.Bio = rs.Bio;
            userReponse.ID = rs.ID;
            userReponse.ProfilePictureUrl = rs.ProfilePictureUrl;
            userReponse.Token = token;

            if (token != null)
            {
                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 1,
                    Mess = "Đăng nhập thành công",
                    Data = userReponse

                };
            }

            return new ApiReponseModel<UserReponseModel>
            {
                Status = 0,
                Mess = "Đăng nhập thất bại",
                Data = null
            };
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel> VerifyUserEmail(string email)
        {
            try
            {
                var sql = "UPDATE Users SET IsVerified = @isVerified WHERE Email = @email AND (IsVerified IS NULL OR IsVerified = 0)";

                var param = new SortedList
                {
                    {"@isVerified", true},
                    {"@email", email}
                };

                int updatedRows = await connectDB.Update(sql, param);

                if (updatedRows > 0)
                {
                    return new ApiReponseModel { Status = 1, Mess = "Xác thực email thành công. Tài khoản đã được kích hoạt." };
                }
                else
                {
                    return new ApiReponseModel { Status = 0, Mess = "Mã OTP hợp lệ nhưng không tìm thấy người dùng hoặc tài khoản đã được xác thực trước đó." };
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Lỗi khi xác thực email: {ex.Message}");
                return new ApiReponseModel { Status = 0, Mess = "Lỗi hệ thống khi xác thực email." };
            }
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel<EditInfo>> GetUserInfoEdit(int userId)
        {
            var sql = "SELECT * FROM Users WHERE ID = @userId";
            var param = new System.Collections.SortedList
            {
                {"userId",userId }
            };

            DataTable dt = await connectDB.Select(sql, param);
            var userdata = new EditInfo();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                userdata.ProfilePictureUrl = row["ProfilePictureUrl"] == DBNull.Value ? "" : apiAvatar + row["ProfilePictureUrl"].ToString();
                userdata.Email = row["Email"].ToString();
                userdata.FullName = row["FullName"].ToString();
                userdata.Bio = row["Bio"] == DBNull.Value ? null : row["Bio"].ToString();

                return new ApiReponseModel<EditInfo>
                {
                    Status = 1,
                    Mess = "Lấy thông tin người dùng thành công.",
                    Data = userdata
                };
            }
            else
            {
                return new ApiReponseModel<EditInfo>
                {
                    Status = 0,
                    Mess = "Không tìm thấy thông tin người dùng.",
                    Data = null
                };
            }

        }


        // Bỏ STATIC
        public static async Task<List<User>> GetAllUsers()
        {
            var list = new List<User>();
            string sql = "SELECT * FROM Users";
            DataTable dt = await connectDB.Select(sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new User
                {
                    ID = Convert.ToInt16(row["ID"]),
                    FullName = row["FullName"] + "",
                    Email = row["Email"].ToString(),
                    Password = row["Password"].ToString()
                });
            }

            return list;
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel<UserReponseModel>> GetUserInfo(int userId)
        {
            var sql = "SELECT * FROM Users WHERE ID = @userId";
            var param = new System.Collections.SortedList
            {
                {"userId",userId }
            };

            DataTable dt = await connectDB.Select(sql, param);
            var userdata = new UserReponseModel();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                userdata.ID = Convert.ToInt32(row["ID"]);
                userdata.Email = row["Email"].ToString();
                userdata.FullName = row["FullName"].ToString();
                userdata.Bio = row["Bio"] == DBNull.Value ? null : row["Bio"].ToString();
                userdata.ProfilePictureUrl = row["ProfilePictureUrl"] == DBNull.Value ? null : row["ProfilePictureUrl"].ToString();

                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 1,
                    Mess = "Lấy thông tin người dùng thành công.",
                    Data = userdata
                };
            }
            else
            {
                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 0,
                    Mess = "Không tìm thấy thông tin người dùng.",
                    Data = null
                };
            }

        }

        // Bỏ STATIC
        public static async Task<PaginatedResponse<PostFull>> GetPostById(int pageNumber, int pageSize, int UserId)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            string countSql = @"SELECT COUNT(p.ID) FROM Posts p JOIN Users u ON u.ID = p.UserId";
            int totalCount = 0;
            try
            {
                string countJson = await connectDB.SelectJS(countSql);

                if (!string.IsNullOrEmpty(countJson))
                {
                    using (JsonDocument doc = JsonDocument.Parse(countJson))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                        {
                            if (doc.RootElement[0].TryGetProperty("Column1", out JsonElement countElement))
                            {
                                countElement.TryGetInt32(out totalCount);
                            }
                            else if (doc.RootElement[0].EnumerateObject().Any() && doc.RootElement[0].EnumerateObject().First().Value.ValueKind == JsonValueKind.Number)
                            {
                                doc.RootElement[0].EnumerateObject().First().Value.TryGetInt32(out totalCount);
                            }
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Number)
                        {
                            doc.RootElement.TryGetInt32(out totalCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy tổng số bài viết: {ex.Message}");
                totalCount = 0;
            }

            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"SELECT
                                p.ID AS Id,
                                p.Content,
                                p.ImageUrl,
                                p.IsPrivate,
                                p.DateCreated,
                                p.DateUpdated,
                                p.IsDeleted,
                                p.UserId,
                                u.FullName AS UserFullName,
                                u.Bio,
                                ISNULL(u.ProfilePictureUrl, '') AS UserProfilePictureUrl,
                                (
                                    SELECT JSON_QUERY('[' + STRING_AGG(CAST(l.UserId AS NVARCHAR(MAX)), ',') + ']')
                                    FROM Likes l
                                    WHERE l.PostId = p.ID
                                ) AS LikeUserIds,
                                CAST(
                                (
                                    SELECT TOP 2 
                                        c.ID,
                                        c.Content,
                                        c.DateCreated,
                                        c.UserId,
                                        cu.FullName AS UserFullName,
                                        ISNULL(cu.ProfilePictureUrl, '') AS UserProfilePictureUrl
                                    FROM Comments c
                                    LEFT JOIN Users cu ON cu.ID = c.UserId
                                    WHERE c.PostId = p.ID
                                    ORDER BY c.DateCreated DESC
                                    FOR JSON PATH
                                ) AS NVARCHAR(MAX)
                                ) AS Comments
                            FROM Posts p
                            JOIN Users u ON u.ID = p.UserId
                            WHERE p.UserId = {UserId}
                            ORDER BY p.DateCreated DESC
                            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                            FOR JSON PATH";

            List<PostFull> posts = new List<PostFull>();
            string json = await connectDB.SelectJS(sql);

            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    posts = JsonSerializer.Deserialize<List<PostFull>>(json, options) ?? new List<PostFull>();
                    string apiHost = "https://localhost:7024";

                    foreach (var post in posts)
                    {
                        if (!string.IsNullOrEmpty(post.ImageUrl))
                        {
                            string[] fileNames = post.ImageUrl.Split(',');

                            var fullImageUrls = fileNames.Select(fileName =>
                            {
                                return $"{apiHost}/Media/ShowImage?fileName={Uri.EscapeDataString(fileName.Trim())}";
                            }).ToList();
                            post.ImageUrl = string.Join(",", fullImageUrls);
                        }
                        if (!string.IsNullOrEmpty(post.UserProfilePictureUrl))
                        {
                            post.UserProfilePictureUrl = apiAvatar + post.UserProfilePictureUrl;
                        }

                        if (post.Comments != null)
                        {
                            foreach (var comment in post.Comments)
                            {
                                comment.UserProfilePictureUrl = apiAvatar + comment.UserProfilePictureUrl;
                            }
                        }

                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Lỗi Deserialize JSON trong PostService.GetAllPosts (data): {ex.Message}");
                    Console.WriteLine($"JSON gây lỗi: {json}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi deserialize posts: {ex.Message}");
                }
            }

            return new PaginatedResponse<PostFull>
            {
                Data = posts,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // Bỏ STATIC
        public static async Task<List<ListFriend>> GetFriendByUserId(int UserId)
        {
            var sql = @"SELECT
                                CASE
                                    WHEN FR.SenderId = @UserId THEN U_Receiver.FullName
                                    ELSE U_Sender.FullName
                                END AS FriendFullName,
                        
                                CASE
                                    WHEN FR.SenderId = @UserId THEN U_Receiver.ProfilePictureUrl
                                    ELSE U_Sender.ProfilePictureUrl
                                END AS ProfilePictureUrl,

                                CASE
                                    WHEN FR.SenderId = @UserId THEN U_Receiver.ID
                                    ELSE U_Sender.ID
                                END AS ID
                                    
                            FROM
                                FriendRequests AS FR
                            JOIN
                                Users AS U_Sender ON FR.SenderId = U_Sender.ID
                            JOIN
                                Users AS U_Receiver ON FR.ReceiverId = U_Receiver.ID
                            WHERE
                                (FR.SenderId = @UserId OR FR.ReceiverId = @UserId) AND FR.Status = 1;";

            var param = new System.Collections.SortedList
            {
                {"UserId",UserId }
            };
            var list = new List<ListFriend>();
            DataTable data = await connectDB.Select(sql, param);
            if (data != null)
            {
                foreach (DataRow row in data.Rows)
                {
                    string profilePictureUrl = row["ProfilePictureUrl"] + "";
                    list.Add(new ListFriend
                    {
                        ID = Convert.ToInt16(row["ID"]),
                        FullName = row["FriendFullName"] + "",
                        ImageUrl = apiAvatar + profilePictureUrl
                    });
                }
            }
            return list;
        }


        // Bỏ STATIC
        public static async Task<ApiReponseModel<UserInfo>> GetUserInfoWithPaginatedPosts(int profileUserId, int pageNumber = 1, int pageSize = 10)
        {
            var userInfo = new UserInfo();

            var userDetails = await GetUserInfo(profileUserId);
            if (userDetails.Data != null)
            {
                userInfo.ID = userDetails.Data.ID;
                userInfo.FullName = userDetails.Data.FullName;
                userInfo.Email = userDetails.Data.Email;
                userInfo.ProfilePictureUrl = apiAvatar + userDetails.Data.ProfilePictureUrl;
                userInfo.Bio = userDetails.Data.Bio;
            }
            else
            {
                return new ApiReponseModel<UserInfo>
                {
                    Status = 0,
                    Mess = "Không tìm thấy thông tin người dùng."
                };
            }
            userInfo.ListPost = await GetPostById(pageNumber, pageSize, profileUserId);
            userInfo.ListFriend = await GetFriendByUserId(profileUserId);
            return new ApiReponseModel<UserInfo>
            {
                Status = 1,
                Mess = "Lấy thông tin profile và bài viết thành công.",
                Data = userInfo
            };
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel> UploadAvatar(int userId, string avatarUrl)
        {
            var sql = "UPDATE Users SET ProfilePictureUrl = @avatarurl WHERE ID = @userId";
            var param = new System.Collections.SortedList
            {
                {"avatarUrl",avatarUrl },
                {"userId" ,userId}
            };

            var rs = await connectDB.Update(sql, param);

            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Update avatar thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Update avatar thất bại"
                };
        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel> EditInfo(int userID, string email, string fullName, string? bio)
        {
            var sql = "UPDATE Users SET Email = @email,FullName = @fullName, Bio = @bio WHERE ID = @userId";
            var param = new System.Collections.SortedList
            {
                {"email",email },
                {"fullName",fullName },
                {"bio",(object)bio ?? DBNull.Value },
                {"userId",userID }
            };

            var rs = await connectDB.Update(sql, param);

            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Update info thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Update info thất bại"
                };


        }

        // Bỏ STATIC
        public static async Task<ApiReponseModel<UserOnline>> GetUserById(int userid)
        {
            var sql = $"SELECT ID,FullName,ProfilePictureUrl FROM Users WHERE ID = {userid}";

            DataTable dt = await connectDB.Select(sql);
            var userdata = new UserOnline();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                userdata.ProfilePictureUrl = row["ProfilePictureUrl"] == DBNull.Value ? "" : apiAvatar + row["ProfilePictureUrl"].ToString();
                userdata.FullName = row["FullName"].ToString();
                userdata.UserID = Convert.ToInt32(row["ID"]);

                return new ApiReponseModel<UserOnline>
                {
                    Status = 1,
                    Mess = "Lấy thông tin người dùng thành công.",
                    Data = userdata
                };
            }
            else
            {
                return new ApiReponseModel<UserOnline>
                {
                    Status = 0,
                    Mess = "Không tìm thấy thông tin người dùng.",
                    Data = null
                };
            }
        }

        // Giữ LoginOrRegisterWithGoogle là static để không làm thay đổi các đoạn code RegisterWithGoogle
        public static async Task<ApiReponseModel<UserReponseModel>> LoginOrRegisterWithGoogle(string email, string fullName)
        {
            var sql = "SELECT TOP 1 * FROM Users WHERE Email = @Email";
            var param = new SortedList { { "Email", email } };
            DataTable dt = await connectDB.Select(sql, param);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                var user = new User
                {
                    ID = Convert.ToInt32(row["ID"]),
                    Email = row["Email"].ToString(),
                    FullName = row["FullName"].ToString(),
                    Bio = row["Bio"].ToString(),
                    ProfilePictureUrl = row["ProfilePictureUrl"].ToString()
                };

                string token = Cache.CacheEx.SetTokenEx(user);

                var userResponse = new UserReponseModel
                {
                    ID = user.ID,
                    Email = user.Email,
                    FullName = user.FullName,
                    Bio = user.Bio,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Token = token
                };

                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 1,
                    Mess = "Đăng nhập Google thành công.",
                    Data = userResponse
                };
            }
            else
            {
                var registerResult = await UserRegister(email, null, fullName);
                if (registerResult.Status != 1)
                {
                    return new ApiReponseModel<UserReponseModel>
                    {
                        Status = 0,
                        Mess = $"Đăng ký Google thất bại: {registerResult.Mess}"
                    };
                }

                dt = await connectDB.Select(sql, param);
                if (dt.Rows.Count == 0)
                {
                    return new ApiReponseModel<UserReponseModel>
                    {
                        Status = 0,
                        Mess = "Đã xảy ra lỗi khi lấy thông tin user sau khi đăng ký."
                    };
                }

                DataRow newRow = dt.Rows[0];
                var newUser = new User
                {
                    ID = Convert.ToInt32(newRow["ID"]),
                    Email = newRow["Email"].ToString(),
                    FullName = newRow["FullName"].ToString(),
                    Bio = newRow["Bio"].ToString(),
                    ProfilePictureUrl = newRow["ProfilePictureUrl"].ToString()
                };

                string newToken = Cache.CacheEx.SetTokenEx(newUser);

                var newUserResponse = new UserReponseModel
                {
                    ID = newUser.ID,
                    Email = newUser.Email,
                    FullName = newUser.FullName,
                    Bio = newUser.Bio,
                    ProfilePictureUrl = newUser.ProfilePictureUrl,
                    Token = newToken
                };

                return new ApiReponseModel<UserReponseModel>
                {
                    Status = 1,
                    Mess = "Đăng ký và đăng nhập Google thành công.",
                    Data = newUserResponse
                };
            }
        }
    }
}