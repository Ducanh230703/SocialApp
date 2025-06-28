using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Data;
using System.Threading.Tasks;

namespace Services
{
    public class connectDB
    {
        public static string conStr;

        public static async Task<DataTable> Select(string sql, SortedList param = null)
        {
            DataTable rs = new DataTable();
            try
            {
                if (string.IsNullOrEmpty(conStr))
                {
                    throw new InvalidOperationException("Connection string (conStr) is null or empty.");
                }

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


        public static async Task<int> Insert(string sql, SortedList param = null)
        {
            int rs = 0;
            try
            {
                // Thiếu kiểm tra và khởi tạo lại conStr ở đây
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
                rs = rows > 0 ? 1 : 0;
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

        public static async Task<int> Update(string sql, SortedList param = null)
        {
            int rs = 0;
            try
            {
                // Thiếu kiểm tra và khởi tạo lại conStr ở đây
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
                rs = rows > 0 ? 1 : 0;
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

        public static async Task<int> Delete(string sql, SortedList param = null)   
        {
            int rs = 0;
            try
            {
                // Thiếu kiểm tra và khởi tạo lại conStr ở đây
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
                rs = rows > 0 ? 1 : 0;
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

        public static async Task<string> SelectJS(string sql, SortedList param = null)
        {
            string json = null;
            try
            {
                if (string.IsNullOrEmpty(conStr))
                {
                    throw new InvalidOperationException("Connect string not found");
                }

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
                var rs = await cmd.ExecuteScalarAsync();
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
        public static async Task<Dictionary<string, object>> ExecuteStoredProcedure(string procedureName,SortedList inParams = null,string[] outParamNames = null)
        {
            var result = new Dictionary<string, object>();
            try
            {
                if (string.IsNullOrEmpty(conStr))
                {
                    throw new InvalidOperationException("Connection string (conStr) is null or empty.");
                }

                using var conn = new SqlConnection(conStr);
                using var cmd = new SqlCommand(procedureName, conn)
                {
                    CommandType = CommandType.StoredProcedure // Rất quan trọng: Chỉ định đây là Stored Procedure
                };

                if (inParams != null)
                {
                    foreach (DictionaryEntry item in inParams)
                    {
                        cmd.Parameters.AddWithValue(item.Key.ToString(), item.Value);
                    }
                }

                if (outParamNames != null)
                {
                    foreach (var paramName in outParamNames)
                    {

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

                if (outParamNames != null)
                {
                    foreach (var paramName in outParamNames)
                    {
                        if (cmd.Parameters[paramName].Value != DBNull.Value)
                        {
                            result[paramName.TrimStart('@')] = cmd.Parameters[paramName].Value;
                        }
                        else
                        {
                            result[paramName.TrimStart('@')] = null;
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[Connection Error - SP] {ex.Message}");
                // Trả về trạng thái lỗi mặc định
                result["Status"] = -99;
                result["Message"] = "Lỗi kết nối cơ sở dữ liệu.";
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[SQL Error - SP] {ex.Message}");
                result["Status"] = -98; // Mã lỗi SQL
                result["Message"] = $"Lỗi SQL: {ex.Message}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General Error - SP] {ex.Message}");
                result["Status"] = -97; // Mã lỗi chung
                result["Message"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return result;
        }

    }
}