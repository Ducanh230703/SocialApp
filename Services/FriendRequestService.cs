using Azure;
using Microsoft.Data.SqlClient;
using Models.ReponseModel;
using Models.ViewModel.Friend;
using System.Collections;
using System.Data;
using System.Text.Json;

namespace Services
{
    public class FriendRequestService
    {
        public static string apiAvatar;
        public static async Task<ApiReponseModel<FriendRequestRP>> StatusRequest(int loggedInUserId, int profileUserId)
        {
            var sql = @"
                        SELECT ID, Status, SenderID, ReceiverID
                        FROM FriendRequests
                        WHERE (SenderID = @loggedInUserId AND ReceiverID = @profileUserId)
                           OR (SenderID = @profileUserId AND ReceiverID = @loggedInUserId);";

            var param = new SortedList
        {
            {"loggedInUserId", loggedInUserId },
            {"profileUserId", profileUserId }
        };

            try
            {
                DataTable dt = await connectDB.Select(sql, param);
                FriendRequestRP friendRequest = null;
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        friendRequest = new FriendRequestRP
                        {
                            ID = Convert.ToInt32(row["ID"]),
                            Status = Convert.ToInt32(row["Status"]),
                            SenderID = Convert.ToInt32(row["SenderID"]),
                            ReceiverID = Convert.ToInt32(row["ReceiverID"]),
                        };
                    }
                    return new ApiReponseModel<FriendRequestRP>
                    {
                        Status = 1,
                        Mess = "Lấy status thành công",
                        Data = friendRequest
                    };


                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error] {ex.Message}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error] {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error] {ex.Message}");
            }

            return new ApiReponseModel<FriendRequestRP>
            {
                Status = 0,
                Mess = "Lấy status thất bại",
                Data = null
            };
        }

        public static async Task<ApiReponseModel> SendRequest(int status, int senderId, int receiverId)
        {
            var sql = "INSERT INTO FriendRequests (Status,SenderID,ReceiverID) VALUES (@Status,@SenderID,@ReceiverID)";
            var param = new System.Collections.SortedList
            {
                {"Status", status },
                {"SenderID",senderId },
                {"ReceiverID",receiverId }
            };

            try
            {
                var rs = await connectDB.Insert(sql, param);
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Gửi yêu cầu thành công",
                    };


                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error] {ex.Message}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error] {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error] {ex.Message}");
            }

            return new ApiReponseModel
            {
                Status = 0,
                Mess = "Gửi yêu cầu thất bại",
            };
        }

        public static async Task<ApiReponseModel> AnswerRequest(int id, int status)
        {
            string sql = null;
            var param = new System.Collections.SortedList { };
            var rs = 0;
            if (status == 1)
            {
                sql = @"UPDATE FriendRequests SET Status = @status WHERE ID =@id";
                param = new System.Collections.SortedList
                {
                    {"Status", status },
                    {"id",id },
                };
                rs = await connectDB.Update(sql, param);

            }

            else
            {
                sql = "DELETE FROM FriendRequests WHERE ID=@id ;";
                param = new System.Collections.SortedList
                {
                    {"id",id }
                };
                rs = await connectDB.Delete(sql, param);

            }

            try
            {
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Gửi yêu cầu thành công",
                    };


                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error] {ex.Message}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error] {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error] {ex.Message}");
            }

            return new ApiReponseModel
            {
                Status = 0,
                Mess = "Gửi yêu cầu thất bại",
            };
        }
        public static async Task<ApiReponseModel<List<SearchResult>>> FriendSearch(string FullName)
        {
            var response = new ApiReponseModel<List<SearchResult>>
            {
                Status = 500,
                Mess = "Lỗi không xác định."
            };

            try
            {

                string searchKeyword = $"N'%{FullName.Replace("'", "''")}%'"; 

                if (string.IsNullOrWhiteSpace(FullName))
                {
                    response.Status = 1;
                    response.Mess = "Thành công. Không có từ khóa tìm kiếm.";
                    return response;
                }

                string sql = $@"
                                SELECT
                                    u.ID,
                                    u.FullName,
                                    u.ProfilePictureUrl
                                FROM
                                    [socialapp].[dbo].[Users] AS u
                                WHERE
                                    u.FullName LIKE {searchKeyword}
                                ORDER BY
                                    u.FullName ASC  -- Sắp xếp theo tên
                                FOR JSON PATH;
                            ";

                List<SearchResult> friends = new List<SearchResult>();
                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        friends = JsonSerializer.Deserialize<List<SearchResult>>(json, options) ?? new List<SearchResult>();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong FriendService.FriendSearch (data - LIKE): {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");
                        response.Mess = "Lỗi xử lý dữ liệu JSON.";
                        return response;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize friends (LIKE): {ex.Message}");
                        response.Mess = "Lỗi xử lý dữ liệu.";
                        return response;
                    }
                }

                response.Status = 1;
                response.Mess = "Tìm kiếm bạn bè thành công.";
                response.Data = friends;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tổng quát trong FriendService.FriendSearch: {ex.Message}");
                response.Mess = $"Lỗi hệ thống: {ex.Message}";
            }

            return response;
        }

        public static async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> GetListFriend(int loggedInUser, int pageNumber, int pageSize)
        {
            var response = new ApiReponseModel<PaginatedResponse<FriendListVM>>
            {
                Status = 500,
                Mess = "Lỗi không xác định."
            };

            string countSql = $@"SELECT COUNT(FR.ID) FROM
                                        FriendRequests AS FR
                                    JOIN
                                        [socialapp].[dbo].[Users] AS U ON
                                        (FR.SenderID = U.ID AND FR.ReceiverID = {loggedInUser})
                                        OR
                                        (FR.ReceiverID = U.ID AND FR.SenderID = {loggedInUser})
                                    WHERE
                                        FR.Status = 1";
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
                Console.WriteLine($"Lỗi khi lấy tổng số bạn bè: {ex.Message}");
                totalCount = 0;
            }
            List<FriendListVM> friends = new List<FriendListVM>();
            if (totalCount == 0)
            {
                response.Status = 1;
                response.Mess = "Lấy dữ liệu thành công";
                response.Data = new PaginatedResponse<FriendListVM>
                {
                    Data = null,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return response;
            }

            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                int offset = (pageNumber - 1) * pageSize;

                string sql = $@"
                                SELECT
                                        FR.ID AS StatusID,
                                        U.ID AS UserID,
                                        U.FullName,
                                        U.ProfilePictureUrl,
                                        FR.DateCreated AS DateCreated
                                    FROM
                                        FriendRequests AS FR
                                    JOIN
                                        [socialapp].[dbo].[Users] AS U ON
                                        (FR.SenderID = U.ID AND FR.ReceiverID = {loggedInUser})
                                        OR
                                        (FR.ReceiverID = U.ID AND FR.SenderID = {loggedInUser})
                                    WHERE
                                        FR.Status = 1
                                    ORDER BY
                                        FR.DateCreated DESC
                                    OFFSET {offset} ROWS
                                    FETCH NEXT {pageSize} ROWS ONLY
                                    FOR JSON PATH;
                            ";
                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        friends = JsonSerializer.Deserialize<List<FriendListVM>>(json, options) ?? new List<FriendListVM>();
                        foreach (var friend in friends)
                        {
                            friend.ProfilePictureUrl = apiAvatar + friend.ProfilePictureUrl;
                        }
                        response.Status = 1;
                        response.Mess = "Lấy dữ liệu thành công";
                        response.Data = new PaginatedResponse<FriendListVM>
                        {
                            Data = friends,
                            TotalCount = totalCount,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        };
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong FriendService.GetListFriend: {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize friends: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tổng quát trong FriendService.GetListFriend: {ex.Message}");
            }

            return response;
        }

        public static async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> GetListPend(int loggedInUser, int pageNumber, int pageSize)
        {
            var response = new ApiReponseModel<PaginatedResponse<FriendListVM>>
            {
                Status = 500,
                Mess = "Lỗi không xác định."
            };
            string countSql = $@"SELECT COUNT(FR.ID) FROM
                                                        FriendRequests AS FR
                                                    JOIN
                                                        Users AS U ON FR.SenderID = U.ID
                                                    WHERE
                                                        FR.ReceiverID = {loggedInUser}
                                                        AND FR.Status = 0 ";
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
                Console.WriteLine($"Lỗi khi lấy tổng số danh sách chờ: {ex.Message}");
                totalCount = 0;
            }
            List<FriendListVM> friends = new List<FriendListVM>();
            if (totalCount == 0)
            {
                response.Status = 1;
                response.Mess = "Lấy dữ liệu thành công";
                response.Data = new PaginatedResponse<FriendListVM>
                {
                    Data = null,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return response;
            }
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                int offset = (pageNumber - 1) * pageSize;

                string sql = $@"
                                SELECT
                                        FR.ID AS StatusID,
                                        U.ID AS UserID,
                                        U.FullName,
                                        U.ProfilePictureUrl,
                                        FR.DateCreated AS DateCreated
                                    FROM
                                       FriendRequests AS FR
                                    JOIN
                                        Users AS U ON FR.SenderID = U.ID
                                    WHERE
                                        FR.ReceiverID = {loggedInUser}
                                        AND FR.Status = 0
                                    ORDER BY
                                        FR.DateCreated DESC
                                    OFFSET {offset} ROWS
                                    FETCH NEXT {pageSize} ROWS ONLY
                                    FOR JSON PATH;
                            ";
                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        friends = JsonSerializer.Deserialize<List<FriendListVM>>(json, options) ?? new List<FriendListVM>();
                        foreach (var friend in friends)
                        {
                            friend.ProfilePictureUrl = apiAvatar + friend.ProfilePictureUrl;
                        }
                        response.Status = 1;
                        response.Mess = "Lấy dữ liệu thành công";
                        response.Data = new PaginatedResponse<FriendListVM>
                        {
                            Data = friends,
                            TotalCount = totalCount,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        };
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong FriendService.GetListSend: {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize friends (LIKE): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tổng quát trong FriendService.GetListSend: {ex.Message}");
            }

            return response;
        }

        public static async Task<ApiReponseModel<PaginatedResponse<FriendListVM>>> GetListSend(int loggedInUser, int pageNumber, int pageSize)
        {
            var response = new ApiReponseModel<PaginatedResponse<FriendListVM>>
            {
                Status = 500,
                Mess = "Lỗi không xác định."
            };
            string countSql = $@"SELECT COUNT(FR.ID) FROM
                                                        FriendRequests AS FR
                                                    JOIN
                                                        Users AS U ON FR.ReceiverID = U.ID
                                                    WHERE
                                                        FR.SenderID = {loggedInUser}
                                                        AND FR.Status = 0 ";
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
                Console.WriteLine($"Lỗi khi lấy tổng số danh sách đã gửi: {ex.Message}");
                totalCount = 0;
            }
            List<FriendListVM> friends = new List<FriendListVM>();
            if (totalCount == 0)
            {
                response.Status = 1;
                response.Mess = "Lấy dữ liệu thành công";
                response.Data = new PaginatedResponse<FriendListVM>
                {
                    Data = null,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return response;
            }

            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                int offset = (pageNumber - 1) * pageSize;

                string sql = $@"
                                SELECT
                                        FR.ID AS StatusID,
                                        U.ID AS UserID,
                                        U.FullName,
                                        U.ProfilePictureUrl,
                                        FR.DateCreated AS DateCreated
                                    FROM
                                       FriendRequests AS FR
                                    JOIN
                                        Users AS U ON FR.ReceiverID = U.ID
                                    WHERE
                                        FR.SenderID = {loggedInUser}
                                        AND FR.Status = 0
                                    ORDER BY
                                        FR.DateCreated DESC
                                    OFFSET {offset} ROWS
                                    FETCH NEXT {pageSize} ROWS ONLY
                                    FOR JSON PATH;
                            ";
                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        friends = JsonSerializer.Deserialize<List<FriendListVM>>(json, options) ?? new List<FriendListVM>();
                        foreach (var friend in friends)
                        {
                            friend.ProfilePictureUrl = apiAvatar + friend.ProfilePictureUrl;
                        }
                        response.Status = 1;
                        response.Mess = "Lấy dữ liệu thành công";
                        response.Data = new PaginatedResponse<FriendListVM>
                        {
                            Data = friends,
                            TotalCount = totalCount,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        };
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong FriendService.GetListSend: {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize friends (LIKE): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tổng quát trong FriendService.GetListSend: {ex.Message}");
            }

            return response;
        }

        public static async Task<ApiReponseModel<List<FriendListVM>>> GetOnlineFriendList(int loggedInUser)
        {
            var response = new ApiReponseModel<List<FriendListVM>>
            {
                Status = 500,
                Mess = "Lỗi không xác định."
            };

            List<FriendListVM> friends = new List<FriendListVM>();

            try
            {
                string sql = $@"
                        SELECT TOP 20
                                FR.ID AS StatusID,
                                U.ID AS UserID,
                                U.FullName,
                                U.ProfilePictureUrl,
                                FR.DateCreated AS DateCreated
                            FROM
                                FriendRequests AS FR
                            JOIN
                                [socialapp].[dbo].[Users] AS U ON
                                (FR.SenderID = U.ID AND FR.ReceiverID = {loggedInUser})
                                OR
                                (FR.ReceiverID = U.ID AND FR.SenderID = {loggedInUser})
                            WHERE
                                FR.Status = 1 AND U.IsOnline = 1
                            ORDER BY
                                FR.DateCreated DESC
                            FOR JSON PATH;
                        ";

                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        friends = JsonSerializer.Deserialize<List<FriendListVM>>(json, options) ?? new List<FriendListVM>();

                        foreach (var friend in friends)
                        {
                            friend.ProfilePictureUrl = apiAvatar + friend.ProfilePictureUrl;
                        }

                        response.Status = 1;
                        response.Mess = "Lấy dữ liệu thành công";
                        response.Data = friends; 
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong FriendService.GetOnlineFriendList: {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");
                        response.Mess = "Lỗi xử lý dữ liệu từ máy chủ.";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize friends: {ex.Message}");
                        response.Mess = "Lỗi không mong muốn khi xử lý dữ liệu.";
                    }
                }
                else
                {
                    response.Status = 1;
                    response.Mess = "Không có bạn bè nào đang online.";
                    response.Data = new List<FriendListVM>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tổng quát trong FriendService.GetOnlineFriendList: {ex.Message}");
                response.Mess = "Lỗi trong quá trình truy vấn dữ liệu bạn bè online.";
            }

            return response;
        }
    }
}
