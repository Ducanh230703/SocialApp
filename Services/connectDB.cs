using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Data;
using System.Threading.Tasks;

namespace Services
{
    public class connectDB
    {
        // Chuỗi kết nối toàn cục, được thiết lập ở tầng khởi tạo ứng dụng.
        public static string conStr;

        // Hàm kiểm tra chuỗi kết nối bị lặp lại trong nhiều hàm
        private static void CheckConnectionStatus()
        {
            if (string.IsNullOrEmpty(conStr))
            {
                throw new InvalidOperationException("Connection string (conStr) is null or empty. Please set connectDB.conStr.");
            }
        }

        // --- SELECT & READ DATA ---

        /// <summary>
        /// Thực thi truy vấn SELECT và trả về DataTable (Dùng cho nhiều bản ghi)
        /// </summary>
        public static async Task<DataTable> Select(string sql, SortedList param = null)
        {
            DataTable rs = new DataTable();
            try
            {
                CheckConnectionStatus();

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(sql, conn);

                if (param != null)
                {
                    foreach (DictionaryEntry item in param)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();
                rs.Load(reader);
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

            return rs;
        }

        /// <summary>
        /// Thực thi truy vấn SELECT và trả về JSON string (Dùng cho truy vấn FOR JSON)
        /// </summary>
        public static async Task<string> SelectJS(string sql, SortedList param = null)
        {
            string json = null;
            try
            {
                CheckConnectionStatus();

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(sql, conn);

                if (param != null)
                {
                    foreach (DictionaryEntry item in param)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                await conn.OpenAsync();
                var rs = await cmd.ExecuteScalarAsync(); // Dùng ExecuteScalar vì FOR JSON trả về 1 dòng 1 cột
                if (rs != null && rs != DBNull.Value)
                {
                    json = rs.ToString();
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

            return json;
        }

        /// <summary>
        /// Thực thi truy vấn và trả về giá trị đơn (ví dụ: COUNT, SUM, MAX)
        /// Đây là hàm cần thiết cho GetTotalPostsCount()
        /// </summary>
        public static async Task<T> ExecuteScalar<T>(string sql, SortedList param = null)
        {
            try
            {
                CheckConnectionStatus();

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(sql, conn);

                if (param != null)
                {
                    foreach (DictionaryEntry item in param)
                    {
                        // Kiểm tra nếu Key có @ thì bỏ qua việc thêm @
                        string key = item.Key.ToString().StartsWith("@") ? item.Key.ToString() : "@" + item.Key.ToString();
                        cmd.Parameters.AddWithValue(key, item.Value);
                    }
                }

                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    return (T)Convert.ChangeType(result, typeof(T));
                }

                // Trả về giá trị mặc định của T (0 cho int, null cho object, etc.)
                return default(T);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error - Scalar] {ex.Message}");
                return default(T);
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error - Scalar] {ex.Message}");
                return default(T);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error - Scalar] {ex.Message}");
                return default(T);
            }
        }

        // --- INSERT, UPDATE, DELETE ---

        /// <summary>
        /// Thực thi truy vấn INSERT, trả về 1 nếu thành công, 0 nếu thất bại.
        /// </summary>
        public static async Task<int> Insert(string sql, SortedList param = null)
        {
            // **Đã thêm kiểm tra conStr**
            return await ExecuteNonQueryInternal(sql, param);
        }

        /// <summary>
        /// Thực thi truy vấn UPDATE, trả về 1 nếu thành công, 0 nếu thất bại.
        /// </summary>
        public static async Task<int> Update(string sql, SortedList param = null)
        {
            // **Đã thêm kiểm tra conStr**
            return await ExecuteNonQueryInternal(sql, param);
        }

        /// <summary>
        /// Thực thi truy vấn DELETE, trả về 1 nếu thành công, 0 nếu thất bại.
        /// </summary>
        public static async Task<int> Delete(string sql, SortedList param = null)
        {
            // **Đã thêm kiểm tra conStr**
            return await ExecuteNonQueryInternal(sql, param);
        }

        /// <summary>
        /// Hàm nội bộ cho INSERT/UPDATE/DELETE để tránh lặp lại mã.
        /// </summary>
        private static async Task<int> ExecuteNonQueryInternal(string sql, SortedList param = null)
        {
            try
            {
                CheckConnectionStatus();

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(sql, conn);

                if (param != null)
                {
                    foreach (DictionaryEntry item in param)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                await conn.OpenAsync();
                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0 ? 1 : 0;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error - NonQuery] {ex.Message}");
                return 0;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error - NonQuery] {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error - NonQuery] {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Thực thi truy vấn INSERT và lấy ID của bản ghi vừa chèn (dùng cho SCOPE_IDENTITY() hoặc OUTPUT)
        /// </summary>
        public static async Task<int> InsertAndGetId(string sql, SortedList param = null)
        {
            try
            {
                CheckConnectionStatus(); // **Đã thêm kiểm tra conStr**

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(sql, conn);

                if (param != null)
                {
                    foreach (DictionaryEntry item in param)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                await conn.OpenAsync();

                // Sử dụng ExecuteScalarAsync để lấy ID của bản ghi vừa được chèn
                // Điều này yêu cầu SQL phải kết thúc bằng ";SELECT SCOPE_IDENTITY();"
                var result = await cmd.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }

                return -1; // Trả về -1 nếu không lấy được ID
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error - GetId] {ex.Message}");
                return -2;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error - GetId] {ex.Message}");
                return -3;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error - GetId] {ex.Message}");
                return -4;
            }
        }

        // --- STORED PROCEDURE ---

        /// <summary>
        /// Thực thi Stored Procedure và lấy giá trị của các tham số OUT.
        /// </summary>
        public static async Task<Dictionary<string, object>> ExecuteStoredProcedure(string procedureName, SortedList inParams = null, string[] outParamNames = null)
        {
            var result = new Dictionary<string, object>();
            try
            {
                CheckConnectionStatus(); // **Đã thêm kiểm tra conStr**

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(procedureName, conn)
                {
                    CommandType = CommandType.StoredProcedure // Rất quan trọng
                };

                // Thêm tham số đầu vào (IN)
                if (inParams != null)
                {
                    foreach (DictionaryEntry item in inParams)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                // Thêm tham số đầu ra (OUT)
                if (outParamNames != null)
                {
                    foreach (var paramName in outParamNames)
                    {
                        // Giả định kiểu dữ liệu dựa trên tên (cần cẩn thận hơn trong thực tế)
                        if (paramName.Contains("Message", StringComparison.OrdinalIgnoreCase))
                        {
                            cmd.Parameters.Add(paramName, SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
                        }
                        else if (paramName.Contains("Status", StringComparison.OrdinalIgnoreCase))
                        {
                            cmd.Parameters.Add(paramName, SqlDbType.Int).Direction = ParameterDirection.Output;
                        }
                        else
                        {
                            cmd.Parameters.Add(paramName, SqlDbType.NVarChar, 255).Direction = ParameterDirection.Output;
                        }
                    }
                }

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                // Lấy giá trị của tham số đầu ra (OUT)
                if (outParamNames != null)
                {
                    foreach (var paramName in outParamNames)
                    {
                        var paramValue = cmd.Parameters[paramName].Value;
                        if (paramValue != DBNull.Value)
                        {
                            // Lưu vào Dictionary, loại bỏ ký tự @
                            result[paramName.TrimStart('@')] = paramValue;
                        }
                        else
                        {
                            result[paramName.TrimStart('@')] = null;
                        }
                    }
                }
            }
            // Khối catch được giữ nguyên như code gốc của bạn
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error - SP] {ex.Message}");
                result["Status"] = -99;
                result["Message"] = "Lỗi kết nối cơ sở dữ liệu.";
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error - SP] {ex.Message}");
                result["Status"] = -98;
                result["Message"] = $"Lỗi SQL: {ex.Message}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error - SP] {ex.Message}");
                result["Status"] = -97;
                result["Message"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return result;
        }

    }
}