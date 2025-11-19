using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{

    public class CommentService
    {
        public static string apiAvatar;

        /// <summary>
        /// Thêm bình luận
        /// </summary>
        /// <param name="PostId"></param>
        /// <param name="UserId"></param>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel<CommentDetail>> AddComment(int PostId, int UserId, string Content)
        {
            var insertSql = "INSERT INTO Comments (PostId, UserId, Content, DateCreated) VALUES (@PostId, @UserId, @Content, GETDATE()); SELECT SCOPE_IDENTITY();";
            var insertParam = new System.Collections.SortedList
        {
            {"PostId", PostId},
            {"UserId", UserId},
            {"Content", Content}
        };

            try
            {
                var result = await connectDB.InsertAndGetId(insertSql, insertParam);
                if (result == null || result <= 0)
                {
                    return new ApiReponseModel<CommentDetail>
                    {
                        Status = 0,
                        Mess = "Bình luận thất bại",
                        Data = null
                    };
                }

                int newCommentId = (int)result;

                var selectSql = @"
                                    SELECT
                                        c.ID,
                                        c.Content,
                                        c.DateCreated,
                                        c.UserId,
                                        cu.FullName AS UserFullName,
                                        ISNULL(cu.ProfilePictureUrl, '') AS UserProfilePictureUrl
                                    FROM [socialapp].[dbo].Comments c
                                    JOIN [socialapp].[dbo].Users cu ON cu.ID = c.UserId
                                    WHERE c.ID = @CommentId";

                var selectParam = new System.Collections.SortedList
                    {
                        {"CommentId", newCommentId}
                    };

                DataTable dt = await connectDB.Select(selectSql, selectParam);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var commentDetail = new CommentDetail
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        Content = row["Content"].ToString(),
                        DateCreated = Convert.ToDateTime(row["DateCreated"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        UserFullName = row["UserFullName"].ToString(),
                        UserProfilePictureUrl = apiAvatar + row["UserProfilePictureUrl"].ToString()
                    };

                    return new ApiReponseModel<CommentDetail>
                    {
                        Status = 1,
                        Mess = "Bình luận thành công",
                        Data = commentDetail
                    };
                }
                else
                {
                    return new ApiReponseModel<CommentDetail>
                    {
                        Status = 0,
                        Mess = "Bình luận được thêm nhưng không thể lấy thông tin chi tiết",
                        Data = null
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thêm bình luận: {ex.Message}");
                return new ApiReponseModel<CommentDetail>
                {
                    Status = 0,
                    Mess = $"Lỗi hệ thống: {ex.Message}",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Xóa bình luân
        /// </summary>
        /// <param name="CommentId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> DeleteComment(int commentId, int userId)
        {
            if (!await CheckCommentOwner(commentId, userId))
                return new ApiReponseModel { Status = 0, Mess = "Bạn không có quyền xóa bình luận này!" };

            var sql = "DELETE FROM Comments WHERE ID = @commentId";
            var param = new System.Collections.SortedList { { "commentId", commentId } };
            var rs = await connectDB.Delete(sql, param);

            return rs > 0 ?
                new ApiReponseModel { Status = 1, Mess = "Xóa bình luận thành công" } :
                new ApiReponseModel { Status = 0, Mess = "Xóa bình luận thất bại" };
        }

        public static async Task<bool> CheckCommentOwner(int commentId, int userId)
        {
            var sql = "SELECT COUNT(*) FROM Comments WHERE ID = @commentId AND UserId = @userId";
            var param = new System.Collections.SortedList
    {
        {"commentId", commentId },
        {"userId", userId }
    };
            var dt = await connectDB.Select(sql, param);
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

    }
}
