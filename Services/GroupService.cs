using Microsoft.Data.SqlClient;
using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
     public class GroupService
     {
        public static async Task<ApiReponseModel> CreateGroup(int userId, string groupName)
        {
            try
            {
                var sql = @"
                BEGIN TRANSACTION;

                BEGIN TRY
                    DECLARE @NewGroupID INT;

                    INSERT INTO Groups (CreatedByUserId, GroupName)
                    VALUES (@userId, @groupName);

                    SET @NewGroupID = SCOPE_IDENTITY();

                    INSERT INTO GroupMembers (GroupId, UserId, Role)
                    VALUES (@NewGroupID, @userId, 'Owner');

                    COMMIT TRANSACTION;

                    SELECT 1 AS Status, N'Tạo nhóm thành công' AS Mess, @NewGroupID AS GroupId;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;

                    -- Trả về lỗi
                    SELECT -1 AS Status, N'Lỗi SQL: ' + ERROR_MESSAGE() AS Mess;
                END CATCH;
            ";

                var param = new System.Collections.SortedList
            {
                { "userId", userId },
                { "groupName", groupName }
            };

                var result = await connectDB.Insert(sql, param);

                if (result != null)
                {
                    // This part depends on how your `ExecuteScalar` method returns a single value or a row.
                    // Assuming it returns a simple value for status.
                    int status = (int)result;
                    if (status == 1)
                    {
                        return new ApiReponseModel { Status = 1, Mess = "Tạo nhóm thành công" };
                    }
                    else
                    {
                        // This is a simplified error handling. You might need to adjust based on the SQL result.
                        return new ApiReponseModel { Status = -1, Mess = "Lỗi SQL: Không thể tạo nhóm." };
                    }
                }
                else
                {
                    return new ApiReponseModel { Status = 0, Mess = "Tạo nhóm thất bại" };
                }
            }
            catch (Exception ex)
            {
                return new ApiReponseModel
                {
                    Status = -1,
                    Mess = $"Đã xảy ra lỗi: {ex.Message}"
                };
            }
        }

        public static async Task<ApiReponseModel> UpImageGroup(int groupId, string imageUrl)
        {
            var sql = "UPDATE SET GroupPictureUrl = @imageUrl WHERE GroupId = @groupId";

            var param = new System.Collections.SortedList
            {
                {"imageUrl",imageUrl },
                {"groupId",groupId }
            };

            try
            {
                var rs = await connectDB.Update(sql, param);
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Tải ảnh thành công"
                    };
                }
                else
                {
                    return new ApiReponseModel
                    {
                        Status = 0,
                        Mess = "Tải ảnh thất bại"
                    };
                }
            }
            catch (SqlException ex)
            {
                return new ApiReponseModel
                {
                    Status = -1,
                    Mess = "Lỗi Sql"
                };

            }
        }

        public static async Task<ApiReponseModel> ChangeName(int groupId, string groupName)
        {
            var sql = "UPDATE SET GroupName = @groupName WHERE GroupId = @groupId";
            var param = new System.Collections.SortedList
            {
                {"groupName",groupName },
                {"groupId",groupId }
            };

            try
            {
                var rs = await connectDB.Update(sql, param);
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Tải ảnh thành công"
                    };
                }
                else
                {
                    return new ApiReponseModel
                    {
                        Status = 0,
                        Mess = "Tải ảnh thất bại"
                    };
                }
            }
            catch (SqlException ex)
            {
                return new ApiReponseModel
                {
                    Status = -1,
                    Mess = "Lỗi Sql"
                };

            }
        }

        public static async Task<ApiReponseModel> DeleteGroup(int groupId)
        {
            var sql = @"DELETE FROM Groups WHERE GroupId = @groupId;
                        DELETE FROM GroupMembers WHERE GroupId = @groupId ";
            var param = new System.Collections.SortedList
            {
                {"groupId",groupId }
            };

            try
            {
                var rs = await connectDB.Delete(sql, param);
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Xóa nhóm thành công"
                    };
                }
                else
                {
                    return new ApiReponseModel
                    {
                        Status = 0,
                        Mess = "Xóa nhóm thất bại"
                    };
                }
            }
            catch (SqlException ex)
            {
                return new ApiReponseModel
                {
                    Status = -1,
                    Mess = "Lỗi Sql"
                };

            }
        }
    }
}
