using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public class ActivityLogService
    {
        // ... (Phương thức AddLog nếu có) ...

        /// <summary>
        /// Lấy tất cả các bản ghi hoạt động với phân trang và bọc trong ApiReponseModel.
        /// </summary>
        public static async Task<ApiReponseModel<PaginatedResponse<ActivityLogResponse>>> GetAllLogs(int loggedInUserId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 5;

            int totalCount = 0;
            var param = new System.Collections.SortedList();

            // 1. Lấy tổng số bản ghi (ĐÃ SỬA: Thêm điều kiện WHERE và đặt tên cột COUNT)
            string countSql = $@"
                SELECT 
                    COUNT(ID) AS TotalCount 
                FROM 
                    ActivityLogs
                WHERE 
                    UserId = {loggedInUserId}
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;";

            try
            {
                string countJson = await connectDB.SelectJS(countSql, param);

                if (!string.IsNullOrEmpty(countJson) && countJson != "[]")
                {
                    using (JsonDocument doc = JsonDocument.Parse(countJson))
                    {
                        JsonElement root = doc.RootElement;

                        // Xử lý cả hai trường hợp: Object đơn hoặc Array chứa Object
                        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                        {
                            root = root[0];
                        }

                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            // Ưu tiên lấy giá trị từ cột TotalCount đã đặt tên
                            if (root.TryGetProperty("TotalCount", out JsonElement countElement) && countElement.ValueKind == JsonValueKind.Number)
                            {
                                // GÁN totalCount
                                countElement.TryGetInt32(out totalCount);
                            }
                            // Dự phòng cho tên cột mặc định (Column1)
                            else if (root.TryGetProperty("Column1", out JsonElement defaultCountElement) && defaultCountElement.ValueKind == JsonValueKind.Number)
                            {
                                defaultCountElement.TryGetInt32(out totalCount);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Vẫn cho phép hàm tiếp tục nếu lỗi đếm, nhưng ghi log lỗi
                Console.WriteLine($"Lỗi khi lấy tổng số ActivityLogs: {ex.Message}");
            }

            // --- LẤY DỮ LIỆU PHÂN TRANG ---

            int offset = (pageNumber - 1) * pageSize;

            // ĐÃ SỬA: Ép kiểu DateCreated thành định dạng ISO 8601 (127) để tránh lỗi Deserialize
            string sql = $@"
                SELECT 
                    a.ID, a.UserId, 
                    u.FullName AS UserFullName,
                    a.ActionType, a.TargetType, a.TargetId, a.Description, 
                    CONVERT(NVARCHAR(30), a.DateCreated, 127) AS DateCreated 
                FROM 
                    ActivityLogs a
                INNER JOIN 
                    Users u ON a.UserId = u.ID
                WHERE 
                    a.UserId = {loggedInUserId}
                ORDER BY 
                    a.DateCreated DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                FOR JSON PATH;";

            List<ActivityLogResponse> logs = new List<ActivityLogResponse>();
            string json;

            try
            {
                json = await connectDB.SelectJS(sql, param);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi truy vấn dữ liệu ActivityLogs: {ex.Message}");
                return new ApiReponseModel<PaginatedResponse<ActivityLogResponse>>
                {
                    Status = 0,
                    Mess = $"Lỗi hệ thống khi truy vấn dữ liệu: {ex.Message}",
                    Data = null
                };
            }


            if (!string.IsNullOrEmpty(json) && json != "[]")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    logs = JsonSerializer.Deserialize<List<ActivityLogResponse>>(json, options) ?? new List<ActivityLogResponse>();
                }
                catch (JsonException ex)
                {
                    // Trả về lỗi chi tiết khi deserialize (bao gồm lỗi cắt chuỗi nếu có)
                    Console.WriteLine($"Lỗi Deserialize JSON trong ActivityLogService.GetAllLogs: {ex.Message}");
                    return new ApiReponseModel<PaginatedResponse<ActivityLogResponse>>
                    {
                        Status = 0,
                        Mess = $"Lỗi định dạng dữ liệu (JSON Deserialize): {ex.Message}",
                        Data = null
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi không mong muốn khi deserialize logs: {ex.Message}");
                    return new ApiReponseModel<PaginatedResponse<ActivityLogResponse>>
                    {
                        Status = 0,
                        Mess = $"Lỗi không mong muốn: {ex.Message}",
                        Data = null
                    };
                }
            }

            // 4. Trả về thành công
            var paginatedData = new PaginatedResponse<ActivityLogResponse>
            {
                Data = logs,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return new ApiReponseModel<PaginatedResponse<ActivityLogResponse>>
            {
                Status = 1,
                Mess = "Lấy danh sách log hoạt động thành công.",
                Data = paginatedData
            };
        }
    }
}