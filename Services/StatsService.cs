using Models.ReponseModel; // Chứa ApiReponseModel, StatsDashboardModel, DailyRegistrationStats
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Collections; // Dùng cho SortedList
using System.Reflection; // Cần thiết cho ConvertDataTableToList

namespace Services
{
    // GIẢ ĐỊNH: Lớp connectDB được định nghĩa ở nơi khác và có sẵn:
    // public static class connectDB { 
    //     public static Task<T> ExecuteScalar<T>(string sql, SortedList param = null) where T : IConvertible; 
    //     public static Task<DataTable> Select(string sql, SortedList param = null); 
    // }


    public class StatsService
    {
        /// <summary>
        /// Hàm tiện ích để chuyển đổi DataTable thành List<T> (Sử dụng Reflection)
        /// </summary>
        private static List<T> ConvertDataTableToList<T>(DataTable dt) where T : new()
        {
            var results = new List<T>();
            if (dt == null || dt.Rows.Count == 0) return results;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (DataRow row in dt.Rows)
            {
                T item = new T();
                foreach (var prop in properties)
                {
                    // Đảm bảo cột tồn tại và giá trị không phải DBNull
                    if (dt.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    {
                        try
                        {
                            // Chuyển đổi kiểu dữ liệu tương thích
                            prop.SetValue(item, Convert.ChangeType(row[prop.Name], prop.PropertyType));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Convert Error] Không thể chuyển đổi giá trị cho thuộc tính {prop.Name}: {ex.Message}");
                        }
                    }
                }
                results.Add(item);
            }
            return results;
        }


        /// <summary>
        /// Lấy Tổng số bài đăng trong khoảng thời gian được chọn
        /// </summary>
        /// <param name="startDate">Ngày bắt đầu (bao gồm)</param>
        /// <param name="endDate">Ngày kết thúc (bao gồm)</param>
        private static async Task<int> GetTotalPostsCount(DateTime? startDate, DateTime? endDate)
        {
            var sql = new StringBuilder();
            var param = new SortedList();

            sql.Append("SELECT COUNT(ID) FROM Posts WHERE 1=1 ");

            // Lọc theo ngày bắt đầu (>= startDate 00:00:00)
            if (startDate.HasValue)
            {
                sql.Append(" AND DateCreated >= @StartDate ");
                param.Add("StartDate", startDate.Value.Date);
            }

            // Lọc theo ngày kết thúc (< endDate + 1 ngày 00:00:00)
            if (endDate.HasValue)
            {
                sql.Append(" AND DateCreated < @EndDateNextDay ");
                // Lấy ngày tiếp theo để đảm bảo bao gồm toàn bộ ngày endDate
                param.Add("EndDateNextDay", endDate.Value.Date.AddDays(1));
            }
            return await connectDB.ExecuteScalar<int>(sql.ToString(), param);

        }

        /// <summary>
        /// Lấy thống kê lượt đăng ký theo ngày trong khoảng thời gian được chọn
        /// </summary>
        private static async Task<List<DailyRegistrationStats>> GetDailyRegistrationStats(DateTime? startDate, DateTime? endDate)
        {
            var sql = new StringBuilder();
            var param = new SortedList();

            sql.Append(@"
                SELECT 
                    CONVERT(nvarchar, DateCreated, 23) AS Date, 
                    COUNT(ID) AS Count
                FROM 
                    Users 
                WHERE 1=1 ");

            if (startDate.HasValue)
            {
                sql.Append(" AND DateCreated >= @StartDate ");
                param.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                sql.Append(" AND DateCreated < @EndDateNextDay ");
                param.Add("EndDateNextDay", endDate.Value.Date.AddDays(1));
            }

            sql.Append(@"
                GROUP BY 
                    CONVERT(nvarchar, DateCreated, 23)
                ORDER BY 
                    Date ASC;");

            var dataTable = await connectDB.Select(sql.ToString(), param);
            return ConvertDataTableToList<DailyRegistrationStats>(dataTable);

        }

        /// <summary>
        /// Lấy thống kê số lượng bài đăng theo ngày trong khoảng thời gian được chọn
        /// </summary>
        private static async Task<List<DailyRegistrationStats>> GetDailyPostsStats(DateTime? startDate, DateTime? endDate)
        {
            var sql = new StringBuilder();
            var param = new SortedList();

            sql.Append(@"
                SELECT 
                    CONVERT(nvarchar, DateCreated, 23) AS Date, 
                    COUNT(ID) AS Count
                FROM 
                    Posts 
                WHERE 1=1 ");

            if (startDate.HasValue)
            {
                sql.Append(" AND DateCreated >= @StartDate ");
                param.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                sql.Append(" AND DateCreated < @EndDateNextDay ");
                param.Add("EndDateNextDay", endDate.Value.Date.AddDays(1));
            }

            sql.Append(@"
                GROUP BY 
                    CONVERT(nvarchar, DateCreated, 23)
                ORDER BY 
                    Date ASC;");

            // Dùng connectDB.Select để lấy DataTable
            var dataTable = await connectDB.Select(sql.ToString(), param);
            return ConvertDataTableToList<DailyRegistrationStats>(dataTable);

        }

        /// <summary>
        /// Tổng hợp Dashboard Stats - Chấp nhận tham số lọc ngày
        /// </summary>
        public static async Task<ApiReponseModel<StatsDashboardModel>> GetDashboardStats(DateTime? startDate, DateTime? endDate)
        {
            var model = new StatsDashboardModel();

            // Lấy Tổng số bài đăng (dùng connectDB)
            model.TotalPostsCount = await GetTotalPostsCount(startDate, endDate);

            // Lấy thống kê đăng ký theo ngày (dùng connectDB)
            model.DailyRegistrations = await GetDailyRegistrationStats(startDate, endDate);

            // Lấy thống kê bài đăng theo ngày (dùng connectDB)
            model.DailyPosts = await GetDailyPostsStats(startDate, endDate);

            return new ApiReponseModel<StatsDashboardModel>
            {
                Status = 1,
                Mess = "Tải dữ liệu thống kê thành công",
                Data = model
            };
        }
    }
}