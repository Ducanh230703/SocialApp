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
            var sql = "INSERT INTO Groups (CreatedByUserId,GroupName) VALUES (@userId,@groupName)";
            var param = new System.Collections.SortedList
            {
                {"userId",userId },
                {"groupName",groupName }
            };

            try
            {
                var rs = await connectDB.Insert(sql, param);
                if (rs > 0)
                {
                    return new ApiReponseModel
                    {
                        Status = 1,
                        Mess = "Tạo nhóm thành công"
                    };
                }
                else
                {
                    return new ApiReponseModel
                    {
                        Status = 0,
                        Mess = "Tạo nhóm thất bại"
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
