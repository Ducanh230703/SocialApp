using Microsoft.Data.SqlClient;
using Models;
using Models.ReponseModel;
using Models.ResponseModels;
using Models.ViewModel.Group;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class GroupService
    {
        public static string apiAvatar;
        public static async Task<ApiReponseModel<int>> CreateGroup(CreateGroupForm group)
        {
            try
            {
                var sql = @"
                    BEGIN TRANSACTION;

                    BEGIN TRY
                        INSERT INTO Groups (CreatedByUserId, GroupName, IsPrivate, GroupPictureUrl)
                        VALUES (@userId, @groupName, @IsPrivate, @GroupPictureUrl);
        
                        DECLARE @NewGroupID INT = SCOPE_IDENTITY();
        
                        INSERT INTO GroupMembers (GroupId, UserId, Role)
                        VALUES (@NewGroupID, @userId, 2);
        
                        COMMIT TRANSACTION;
        
                        SELECT @NewGroupID;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRANSACTION;
                        SELECT NULL;
                    END CATCH;
                ";

                var param = new System.Collections.SortedList
                {
                    { "userId", group.CreatedByUserId },
                    { "groupName", group.GroupName },
                    { "IsPrivate", group.IsPrivate },
                    { "GroupPictureUrl", (object)group.GroupPictureUrl??DBNull.Value }
                };

                var newGroupId = await connectDB.InsertAndGetId(sql, param);

                if (newGroupId != null)
                {
                    int groupId = Convert.ToInt32(newGroupId);

                    return new ApiReponseModel<int>
                    {
                        Status = 1,
                        Mess = "Tạo nhóm thành công.",
                        Data = groupId
                    };
                }
                else
                {
                    return new ApiReponseModel<int>
                    {
                        Status = 0,
                        Mess = "Tạo nhóm thất bại. Vui lòng thử lại.",
                        Data = 0
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiReponseModel<int>
                {
                    Status = -1,
                    Mess = $"Đã xảy ra lỗi: {ex.Message}",
                    Data = 0
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

        public static async Task<ApiReponseModel> DeleteGroup(int groupId, int userId)
        {
            if (!await CheckGroupOwner(groupId, userId))
                return new ApiReponseModel { Status = 0, Mess = "Bạn không có quyền xóa nhóm!" };
            var sql = @"BEGIN TRANSACTION;
                        BEGIN TRY
                            DELETE L
                            FROM Likes L
                            INNER JOIN Posts P ON L.PostId = P.ID
                            WHERE P.GroupId = @groupId;

                            DELETE C
                            FROM Comments C
                            INNER JOIN Posts P ON C.PostId = P.ID
                            WHERE P.GroupId = @groupId;

                            DELETE FROM Posts WHERE GroupId = @groupId;

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
                var groupList = new List<Group>();
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

        public static async Task<ApiReponseModel<GroupDetailResponseModel>> GetGroupDetail(int groupId, int loggedInUserId)
        {
            try
            {
                var sql = @"
                            SELECT 
                                g.ID AS GroupId,
                                g.GroupName,
                                g.IsPrivate AS GroupIsPrivate,
                                g.GroupPictureUrl,
                                g.CreatedByUserId,
                                g.CreatedDate AS GroupCreatedDate,
                                gu.FullName AS CreatedByUserName,
                                gu.ProfilePictureUrl AS CreatedByUserProfilePictureUrl,
                                p.ID AS PostId,
                                p.Content,
                                p.ImageUrl AS PostImageUrl,
                                p.IsPrivate AS PostIsPrivate,
                                p.DateCreated AS PostCreatedDate,
                                p.IsDeleted AS PostIsDeleted,
                                p.UserId AS PostUserId,
                                u.FullName AS PostUserFullName,
                                u.ProfilePictureUrl AS PostUserProfilePictureUrl,
                                u.Bio AS PostUserBio,
                                (SELECT COUNT(*) FROM Likes WHERE PostId = p.ID) AS LikeCount,
                                (SELECT STRING_AGG(CAST(UserId AS NVARCHAR(10)), ',') FROM Likes WHERE PostId = p.ID) AS LikeUserIds,
                                (SELECT COUNT(*) FROM GroupMembers WHERE GroupId = g.ID) AS MemberCount,
                                (CASE WHEN EXISTS (SELECT 1 FROM GroupMembers WHERE GroupId = g.ID AND UserId = @loggedInUserId) THEN 1 ELSE 0 END) AS IsMember ,
                                gm.Role AS CurrentUserRole
                            FROM Groups g
                            LEFT JOIN Posts p ON g.ID = p.GroupId
                            LEFT JOIN Users gu ON g.CreatedByUserId = gu.ID 
                            LEFT JOIN Users u ON p.UserId = u.ID
                            LEFT JOIN GroupMembers gm ON gm.GroupId = g.ID AND gm.UserId = @loggedInUserId
                            WHERE g.ID = @groupId
                            ORDER BY p.DateCreated DESC;
                        ";

                var param = new SortedList
            {
                { "groupId", groupId },
                 {"loggedInUserId",loggedInUserId }
            };

                DataTable dt = await connectDB.Select(sql, param);

                if (dt.Rows.Count == 0)
                {
                    return new ApiReponseModel<GroupDetailResponseModel>
                    {
                        Status = 0,
                        Mess = "Không tìm thấy nhóm.",
                        Data = null
                    };
                }
                var groupPicture = apiAvatar + dt.Rows[0]["GroupPictureUrl"].ToString;
                var userPicture = apiAvatar + dt.Rows[0]["CreatedByUserProfilePictureUrl"].ToString;

                var groupDetail = new GroupDetailResponseModel
                {
                    ID = Convert.ToInt32(dt.Rows[0]["GroupId"]),
                    IsPrivate = Convert.ToBoolean(dt.Rows[0]["GroupIsPrivate"]),
                    GroupName = dt.Rows[0]["GroupName"].ToString(),
                    GroupPictureUrl = groupPicture,
                    CreatedByUserId = Convert.ToInt32(dt.Rows[0]["CreatedByUserId"]),
                    CreatedDate = Convert.ToDateTime(dt.Rows[0]["GroupCreatedDate"]),
                    IsMember = Convert.ToBoolean(dt.Rows[0]["IsMember"]),
                    CreatedByUserName = dt.Rows[0]["CreatedByUserName"].ToString(),
                    CreatedByUserProfilePictureUrl = userPicture,
                    MemberCount = Convert.ToInt32(dt.Rows[0]["MemberCount"]),
                    CurrentUserRole = dt.Rows[0]["CurrentUserRole"] == DBNull.Value? null : Convert.ToInt32(dt.Rows[0]["CurrentUserRole"])
                };

                var posts = new List<PostFull>();
                foreach (DataRow row in dt.Rows)
                {
                    if (row["PostId"] == DBNull.Value)
                    {
                        continue;
                    }

                    var likeUserIdsString = row["LikeUserIds"] != DBNull.Value ? row["LikeUserIds"].ToString() : null;
                    var likeUserIds = string.IsNullOrEmpty(likeUserIdsString)
                        ? new List<int>()
                        : likeUserIdsString.Split(',').Select(int.Parse).ToList();

                    var userPic = apiAvatar + row["PostUserProfilePictureUrl"].ToString();
                    posts.Add(new PostFull
                    {
                        Id = Convert.ToInt32(row["PostId"]),
                        Content = row["Content"].ToString(),
                        ImageUrl = row["PostImageUrl"] != DBNull.Value ? row["PostImageUrl"].ToString() : null,
                        IsPrivate = Convert.ToBoolean(row["PostIsPrivate"]),
                        Bio = row["PostUserBio"] != DBNull.Value ? row["PostUserBio"].ToString() : null,
                        DateCreated = Convert.ToDateTime(row["PostCreatedDate"]),
                        UserId = Convert.ToInt32(row["PostUserId"]),
                        UserFullName = row["PostUserFullName"].ToString(),
                        UserProfilePictureUrl = userPic,
                        LikeUserIds = likeUserIds,
                        Comments = new List<CommentDetail>()
                    });
                }
                groupDetail.RecentPosts = posts;

                return new ApiReponseModel<GroupDetailResponseModel>
                {
                    Status = 1,
                    Mess = "Lấy chi tiết nhóm thành công.",
                    Data = groupDetail
                };
            }
            catch (Exception ex)
            {
                return new ApiReponseModel<GroupDetailResponseModel>
                {
                    Status = -2,
                    Mess = $"Đã xảy ra lỗi không xác định: {ex.Message}",
                    Data = null
                };
            }
        }
        public static async Task<ApiReponseModel<List<Group>>> SearchGroup(string keyword)
        {
            try
            {
                var sql = @"
                    SELECT g.*
                    FROM Groups g
                    WHERE g.GroupName LIKE '%' + @keyword + '%'
                    ORDER BY g.CreatedDate DESC;
                ";

                var param = new SortedList
                {
                    { "keyword", keyword }                };

                DataTable dt = await connectDB.Select(sql, param);
                var result = new List<Group>();

                foreach (DataRow row in dt.Rows)
                {
                    result.Add(new Group
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        GroupName = row["GroupName"].ToString(),
                        GroupPictureUrl = row["GroupPictureUrl"] != DBNull.Value ? row["GroupPictureUrl"].ToString() : null,
                        IsPrivate = Convert.ToBoolean(row["IsPrivate"]),
                        CreatedByUserId = Convert.ToInt32(row["CreatedByUserId"]),
                        CreatedDate = Convert.ToDateTime(row["CreatedDate"])
                    });
                }

                return new ApiReponseModel<List<Group>>
                {
                    Status = 1,
                    Mess = "Tìm kiếm nhóm thành công.",
                    Data = result
                };
            }
            catch (SqlException ex)
            {
                return new ApiReponseModel<List<Group>>
                {
                    Status = -1,
                    Mess = $"Lỗi SQL: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiReponseModel<List<Group>>
                {
                    Status = -2,
                    Mess = $"Lỗi không xác định: {ex.Message}",
                    Data = null
                };
            }
        }
        public static async Task<bool> CheckGroupOwner(int groupId, int userId)
        {
            var sql = "SELECT COUNT(*) FROM Groups WHERE ID = @groupId AND CreatedByUserId = @userId";
            var param = new System.Collections.SortedList { { "groupId", groupId }, { "userId", userId } };
            var dt = await connectDB.Select(sql, param);
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }
    }
}
