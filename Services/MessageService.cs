using Cache;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Chat;
using Models.ViewModel.Home;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public class MessageService
    {
        public static string apiAvatar;
        public static async Task<ApiReponseModel> SendMessage(SendMessageMD sendMessageMD)
        {
            var apiResponse = new ApiReponseModel();

            var sql = "INSERT INTO Messages (SenderId,TargetId,Content,Type) VALUES (@senderId,@targetId,@content,@type);";

            var param = new System.Collections.SortedList
            {
                {"senderId",sendMessageMD.SenderId},
                { "targetId",sendMessageMD.TargetId},
                {"content",sendMessageMD.MessageContent },
                {"type",sendMessageMD.Type}
            };
            try
            {
                int rowsAffected = await connectDB.Insert(sql,param);

                if (rowsAffected > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = $"Đã thêm thành công {rowsAffected} tin nhắn";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không lưu tin nhăn thành công";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Đã xảy ra lỗi khi lưu tin nhắn {ex.Message}";
            }

            return apiResponse;
        }

        public static async Task<ApiReponseModel<ChatMessageVM>> GetChatMessageHistory(
        int currentLoggedInUserId,
        int targetUserId,
        int pageNumber,
        int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            string countSql = $@"SELECT COUNT(M.ID)
                             FROM Messages M
                             WHERE (M.SenderId = {currentLoggedInUserId} AND M.TargetId = {targetUserId})
                             OR (M.SenderId = {targetUserId} AND M.TargetId = {currentLoggedInUserId})";

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
                Console.WriteLine($"Lỗi khi lấy tổng số tin nhắn: {ex.Message}");
                totalCount = 0;
            }

            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
            SELECT
                M.Id,
                M.SenderId,
                M.TargetId,
                M.Type,
                M.Content,
                M.SentDate,
                M.IsRead
            FROM
                Messages AS M
            WHERE
                (M.SenderId = {currentLoggedInUserId} AND M.TargetId = {targetUserId})
                OR
                (M.SenderId = {targetUserId} AND M.TargetId = {currentLoggedInUserId})
            ORDER BY
                M.SentDate DESC
            OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
            FOR JSON PATH";

            List<Message> messages = new List<Message>();
            string json = await connectDB.SelectJS(sql);

            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    messages = JsonSerializer.Deserialize<List<Message>>(json, options) ?? new List<Message>();
                    messages.Reverse();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Lỗi Deserialize JSON trong ChatService.GetChatMessageHistory (messages): {ex.Message}");
                    Console.WriteLine($"JSON gây lỗi: {json}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi deserialize messages: {ex.Message}");
                }
            }

            var paginatedMessages = new PaginatedResponse<Message>
            {
                Data = messages,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            var chatMessageVM = new ChatMessageVM
            {
                TargetUserId = targetUserId,
                CurrentLoggedInUserId = currentLoggedInUserId,
                Messages = paginatedMessages
            };

            return new ApiReponseModel<ChatMessageVM>
            {
                Status = 1,
                Mess = "Chat history retrieved successfully.",
                Data = chatMessageVM
            };
        }


        public static async Task<ApiReponseModel<PaginatedResponse<MessengerList>>> GetMessengerList(int pageNumber,int pageSize)
        {
            var loggedInUserId = CacheEx.DataUser.ID;
            int totalCount = 0;


            var param = new System.Collections.SortedList
            {
                {"LoggedInUserId",loggedInUserId }
            };

            string countSql = @"WITH RankedMessages AS (
                                SELECT
                                    CASE
                                        WHEN M.SenderId = @LoggedInUserId THEN M.TargetId
                                        ELSE M.SenderId
                                    END AS OtherUserId,
                                    ROW_NUMBER() OVER (PARTITION BY
                                        CASE
                                            WHEN M.SenderId = @LoggedInUserId THEN M.TargetId
                                            ELSE M.SenderId
                                        END
                                        ORDER BY M.SentDate DESC, M.Id DESC) as rn
                                FROM
                                    [socialapp].[dbo].[Messages] AS M
                                WHERE
                                    M.SenderId = @LoggedInUserId OR M.TargetId = @LoggedInUserId
                            )
                            SELECT COUNT(DISTINCT OtherUserId) AS TotalMessagedFriends
                            FROM RankedMessages
                            WHERE rn = 1 AND OtherUserId <> @LoggedInUserId;";
            try
            {
                string countJson = await connectDB.SelectJS(countSql,param);

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
                Console.WriteLine($"Lỗi khi lấy tổng số người nhắn tin gần đây: {ex.Message}");
                return new ApiReponseModel<PaginatedResponse<MessengerList>>
                {
                    Status = -1,
                    Mess = "Lỗi khi lấy tổng số tin nhăn:",
                    Data = null
                };
            }

            if (totalCount == 0)
            {
                return new ApiReponseModel<PaginatedResponse<MessengerList>>
                {
                    Status = 0,
                    Mess = "Không có người dùng nhắn tin gần đây",
                    Data = null
                };
            }


            string sql = @"
                                WITH RankedMessages AS (
                                    SELECT
                                        M.Id AS MessageId,
                                        M.SenderId,
                                        M.TargetId,
                                        M.Content AS LastMessageContent,
                                        M.SentDate AS LastMessageSentDate,
                                        CASE
                                            WHEN M.SenderId = @LoggedInUserId THEN M.TargetId
                                            ELSE M.SenderId
                                        END AS OtherUserId,
                                        ROW_NUMBER() OVER (PARTITION BY
                                            CASE
                                                WHEN M.SenderId = @LoggedInUserId THEN M.TargetId
                                                ELSE M.SenderId
                                            END
                                            ORDER BY M.SentDate DESC, M.Id DESC) as rn
                                    FROM
                                        [socialapp].[dbo].[Messages] AS M
                                    WHERE
                                        M.SenderId = @LoggedInUserId OR M.TargetId = @LoggedInUserId
                                )
                                SELECT
                                    U.[ID] as UserId,
                                    U.[FullName],
                                    U.[ProfilePictureUrl],
                                    RM.LastMessageContent,
                                    RM.LastMessageSentDate,
                                    RM.SenderId AS LastMessageSenderId
                                FROM
                                    [socialapp].[dbo].[Users] AS U
                                INNER JOIN
                                    RankedMessages AS RM
                                    ON U.ID = RM.OtherUserId
                                WHERE
                                    RM.rn = 1
                                    AND U.ID <> @LoggedInUserId
                                ORDER BY
                                    U.[ID]
                                OFFSET ((@PageNumber - 1) * @PageSize) ROWS
                                FETCH NEXT @PageSize ROWS ONLY
                                FOR JSON PATH;";

            param = new System.Collections.SortedList
                {
                    {"LoggedInUserId", loggedInUserId },
                    {"PageNumber", pageNumber }, 
                    {"PageSize", pageSize } 
                }; 
            string json = await connectDB.SelectJS(sql, param);

            List<MessengerList> lists = new List<MessengerList>();
            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    lists = JsonSerializer.Deserialize<List<MessengerList>>(json, options) ?? new List<MessengerList>();
                    foreach (var item in lists)
                    {
                        item.ProfilePictureUrl = apiAvatar + item.ProfilePictureUrl;
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Lỗi Deserialize JSON trong MessageService.GetMessengerList (data): {ex.Message}");
                    Console.WriteLine($"JSON gây lỗi: {json}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi deserialize messenger list: {ex.Message}");
                }
            }

            return new ApiReponseModel<PaginatedResponse<MessengerList>>
            {
                Status = 1,
                Mess = "Lấy danh sách người dùng nhắn tin gần đây thành công",
                Data = new PaginatedResponse<MessengerList>
                {
                    Data = lists,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                }
            };
        }
    }


}
