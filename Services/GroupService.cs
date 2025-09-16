using Microsoft.Data.SqlClient;
using Models;
using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
     public class GroupService
     {
        public static async Task<ApiReponseModel> CreateGroup(Group group )
        {
            try
            {
                var sql = @"
                BEGIN TRANSACTION;

                BEGIN TRY
                    DECLARE @NewGroupID INT;

                    INSERT INTO Groups (CreatedByUserId, GroupName,IsPrivate)
                    VALUES (@userId, @groupName,@IsPrivate);

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
                { "userId", group.CreatedByUserId },
                { "groupName", group.GroupName },
                {"IsPrivate",group.IsPrivate }
            };

                var result = await connectDB.Insert(sql, param);

                if (result != null)
                {

                    int status = (int)result;
                    if (status == 1)
                    {
                        return new ApiReponseModel { Status = 1, Mess = "Tạo nhóm thành công" };
                    }
                    else
                    {
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
            var sql = @"BEGIN TRANSACTION;
                        BEGIN TRY
                            DELETE FROM GroupMembers WHERE GroupId = @groupId;
                            DELETE FROM Groups WHERE ID = @groupId;
                            COMMIT TRANSACTION;
                        END TRY
                        BEGIN CATCH
                            ROLLBACK TRANSACTION;
                            DECLARE @ErrorMessage NVARCHAR(4000);
                            DECLARE @ErrorSeverity INT;
                            DECLARE @ErrorState INT;
                            SELECT 
                                @ErrorMessage = ERROR_MESSAGE(),
                                @ErrorSeverity = ERROR_SEVERITY(),
                                @ErrorState = ERROR_STATE();
                            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                        END CATCH";
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

        public static async Task<ApiReponseModel<List<Group>>> GetListGroup(int userId)
        {
            var sql = $@"Select g.* from Groups g
                        left join GroupMembers gm ON g.ID = gm.GroupId
                        Where gm.UserId = {userId};";

            try
            {
                DataTable dt = await connectDB.Select(sql);
                var groupList = new List<Group>() ;
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        groupList.Add(new Group
                        {
                            ID = Convert.ToInt32(row["ID"]),
                            IsPrivate = Convert.ToBoolean(row["IsPrivate"]),
                            GroupName = row["GroupName"].ToString(),
                            GroupPictureUrl = row["GroupPictureUrl"] != DBNull.Value ? row["GroupPictureUrl"].ToString() : null,
                            CreatedByUserId = Convert.ToInt32(row["CreatedByUserId"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"])
                        });
                    }
                }

                return new ApiReponseModel<List<Group>>
                {
                    Status = 1,
                    Mess = "Lấy danh sách nhóm thành công",
                    Data = groupList
                };
            }
            catch (SqlException sqlEx) 
            {
                return new ApiReponseModel<List<Group>>
                {
                    Status = -1,
                    Mess = $"Lỗi cơ sở dữ liệu: {sqlEx.Message}", 
                    Data = null
                };
            }
            catch (Exception ex) 
            {
                return new ApiReponseModel<List<Group>>
                {
                    Status = -2, 
                    Mess = $"Đã xảy ra lỗi không xác định: {ex.Message}",
                    Data = null
                };
            }



        }
     }
}
