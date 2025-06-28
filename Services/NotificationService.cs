using Models;
using Models.ReponseModel;
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
    public class NotificationService
    {
        public static string apiAvatar;
        public static async Task<PaginatedResponse<Notification>> GetNotice(int receiverId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var param = new System.Collections.SortedList { { "receiverId", receiverId } };
            string countSql = @"SELECT COUNT(ID) FROM Notifications WHERE ReceiverId = @receiverId";
            int totalCount = 0;
            try
            {
                string countJson = await connectDB.SelectJS(countSql, param);

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
                Console.WriteLine($"Lỗi khi lấy tổng số thông báo: {ex.Message}");
                totalCount = 0;
            }
            int offset = (pageNumber - 1) * pageSize;

            string sql = @"Select n.*, u_sender.ProfilePictureUrl
                            From Notifications n
                            join Users u_sender on u_sender.ID = n.SenderId
                            Where ReceiverId = @receiverId
                            ORDER BY DateCreated DESC
                            OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY
                            FOR JSON PATH
";

            string json = await connectDB.SelectJS(sql, param);
            List <Notification> notices = new List<Notification>();
            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    notices = JsonSerializer.Deserialize<List<Notification>>(json, options) ?? new List<Notification>();
                    foreach (var notice in notices)
                    {
                        if (!string.IsNullOrEmpty(notice.ProfilePictureUrl))
                        {
                            notice.ProfilePictureUrl = apiAvatar + notice.ProfilePictureUrl;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Lỗi Deserialize JSON trong NotificationService (data): {ex.Message}");
                    Console.WriteLine($"JSON gây lỗi: {json}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi deserialize posts: {ex.Message}");
                }
            }
            return new PaginatedResponse<Notification>
                {
                    Data = notices,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount

                };
        }

        public static async Task<ApiReponseModel<int>> GetCountUnRead(int userId)
        {
            var sql = "SELECT Count(ID) as total FROM Notifications Where ReceiverId = @userId AND IsRead = 0";
            var param = new System.Collections.SortedList { { "userId", userId } };

            DataTable data = await connectDB.Select(sql, param);
            int total = 0;

            if (data != null && data.Rows.Count > 0)
            {
                DataRow row = data.Rows[0];
                total = Convert.ToInt32(row["total"]);
                return new ApiReponseModel<int>
                {
                    Status = 1,
                    Mess = "Lấy số lượng thông báo thành công",
                    Data = total,
                };
            }

            else
                return new ApiReponseModel<int>
                {
                    Status = 1,
                    Mess = "Lấy số lượng thông báo thất bại"
                };

        }

        public static async Task<ApiReponseModel> SetIsRead(int idNotice)
            {
            var sql = "UPDATE Notifications SET IsRead = 1 WHERE ID = @idNotice";
            var param = new System.Collections.SortedList { { "idNotice", idNotice } };

            var rs = await connectDB.Update(sql, param);

            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Cập nhật IsRead thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Cập nhật IsRead thất bại"
                };
        }
    }
}
