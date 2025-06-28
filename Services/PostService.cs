using Cache;
using Microsoft.AspNetCore.Http;
using Models;
using Models.ReponseModel;
using Models.ViewModel.Home;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{ 
    public class PostService
    {
        public static string apiHost = "https://localhost:7024";
        public static string apiAvatar;

        /// <summary>
        /// Lấy tất cả bài viết
        /// </summary>
        /// <returns></returns>
        public static async Task<PaginatedResponse<PostFull>> GetAllPosts(int pageNumber, int pageSize)
        {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                string countSql = @"SELECT COUNT(p.ID) FROM Posts p JOIN Users u ON u.ID = p.UserId";
                int totalCount = 0;
                try
                {
                    string countJson = await connectDB.SelectJS(countSql);

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
                    Console.WriteLine($"Lỗi khi lấy tổng số bài viết: {ex.Message}");
                    totalCount = 0;
                }

                int offset = (pageNumber - 1) * pageSize;

                string sql = $@"SELECT
                                    p.ID AS Id,
                                    p.Content,
                                    p.ImageUrl,
                                    p.IsPrivate,
                                    p.DateCreated,
                                    p.DateUpdated,
                                    p.IsDeleted,
                                    p.UserId,
                                    u.FullName AS UserFullName,
                                    ISNULL(u.ProfilePictureUrl, '') AS UserProfilePictureUrl,
                                    (
                                        SELECT JSON_QUERY('[' + STRING_AGG(CAST(l.UserId AS NVARCHAR(MAX)), ',') + ']')
                                        FROM Likes l
                                        WHERE l.PostId = p.ID
                                    ) AS LikeUserIds,
                                    CAST(
                                        (
                                            SELECT TOP 2 
                                                c.ID,
                                                c.Content,
                                                c.DateCreated,
                                                c.UserId,
                                                cu.FullName AS UserFullName,
                                                ISNULL(cu.ProfilePictureUrl, '') AS UserProfilePictureUrl
                                            FROM Comment c
                                            LEFT JOIN Users cu ON cu.ID = c.UserId
                                            WHERE c.PostId = p.ID
                                            ORDER BY c.DateCreated ASC
                                            FOR JSON PATH
                                        ) AS NVARCHAR(MAX)
                                        ) AS Comments
                                FROM Posts p
                                JOIN Users u ON u.ID = p.UserId
                                ORDER BY p.DateCreated DESC
                                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY
                                FOR JSON PATH";

                List<PostFull> posts = new List<PostFull>();
                string json = await connectDB.SelectJS(sql);

                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        posts = JsonSerializer.Deserialize<List<PostFull>>(json, options) ?? new List<PostFull>();

                        foreach (var post in posts)
                        {
                            if (!string.IsNullOrEmpty(post.ImageUrl))
                            {
                                string[] fileNames = post.ImageUrl.Split(',');

                                var fullImageUrls = fileNames.Select(fileName =>
                                {
                                    return $"{apiHost}/Media/ShowImage?fileName={Uri.EscapeDataString(fileName.Trim())}";
                                }).ToList();
                                post.ImageUrl = string.Join(",", fullImageUrls);
                            }
                            if (!string.IsNullOrEmpty(post.UserProfilePictureUrl))
                            {
                                post.UserProfilePictureUrl = apiAvatar + post.UserProfilePictureUrl;

                            }

                            foreach (var cmt in post.Comments)
                            {
                                if (!string.IsNullOrEmpty(cmt.UserProfilePictureUrl))
                                {
                                cmt.UserProfilePictureUrl = apiAvatar + cmt.UserProfilePictureUrl;

                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Lỗi Deserialize JSON trong PostService.GetAllPosts (data): {ex.Message}");
                        Console.WriteLine($"JSON gây lỗi: {json}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi không mong muốn khi deserialize posts: {ex.Message}");
                    }
                }

                return new PaginatedResponse<PostFull>
                {
                    Data = posts,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
        }
        /// <summary>
        /// Đăng bài viết mới
        /// </summary>
        /// <param name="Content"></param>
        /// <param name="ImageUrls"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> NewPost(string Content, string ImageUrls, int UserId)
        {
            //var InfoUser = CacheEx.DataUser;
            var sql = "INSERT INTO Posts (Content, ImageUrl,UserId) VALUES (@Content,@ImageUrl,@UserId);";
            var param = new System.Collections.SortedList
            {
                {"Content",(object)Content ?? DBNull.Value},
                {"ImageUrl", (object)ImageUrls ?? DBNull.Value },
                {"UserId",UserId }
            };
            var rs = await connectDB.Insert(sql, param);

            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Đăng bài thành công",
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Đăng bài thất bại",
                };
        }

        /// <summary>
        /// Người dùng thích bài viết
        /// </summary>
        /// <param name="PostId"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> UserLikePost(int PostId,int UserId)
        {
            var sql = "INSERT INTO Likes (PostId, UserId) VALUES (@PostId,@UserId);";
            var param = new System.Collections.SortedList
            {
                {"PostId",PostId },
                {"UserId",UserId }
            };

            var rs = await connectDB.Insert(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Like thành công",
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Like thất bại",
                };

        }

        /// <summary>
        /// Người dùng sửa bài viết
        /// </summary>
        /// <param name="PostId"></param>
        /// <param name="Content"></param>
        /// <param name="ImageUrls"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> UserEditPost(int PostId, string Content, string ImageUrls)
        {
            var sql = @"UPDATE Posts 
                      SET Content = @Content,
                          ImageUrl = @ImageUrls
                      Where ID = @PostId;";
                var param = new System.Collections.SortedList
            {
                {"PostId",PostId },
                {"Content",(object)Content ?? DBNull.Value },
                {"ImageUrls",(object)ImageUrls ?? DBNull.Value }
            };
            var rs = await connectDB.Update(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Update thanh cong",
                };
            else
                return new ApiReponseModel { 
                    Status = 0,
                    Mess = "Update that bai",
                };
        }

        /// <summary>
        /// Lấy bài viết với ID người dùng
        /// </summary>
        /// <param name="PostId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel<PostFull>> GetPostByIdAsync(int PostId)
        {
            string sql = @"SELECT
                        p.ID AS PostID,
                        p.Content,
                        p.ImageUrl,
                        p.IsPrivate,
                        p.DateCreated,
                        p.DateUpdated,
                        p.IsDeleted,
                        p.UserId,
                        u.FullName AS UserFullName,
                        u.ProfilePictureUrl AS UserProfilePictureUrl,
                        l.UserId AS LikeUserId,
                        c.ID AS CommentID,
                        c.Content AS CommentContent,
                        c.DateCreated AS CommentDateCreated,
                        c.DateUpdated AS CommentDateUpdated,
                        c.UserId AS CommentUserId,
                        cu.FullName AS CommentUserFullName,
                        cu.ProfilePictureUrl AS CommentUserProfilePictureUrl
                    FROM Posts p
                    INNER JOIN Users u ON p.UserId = u.ID
                    LEFT JOIN Likes l ON l.PostId = p.ID
                    LEFT JOIN Comment c ON c.PostId = p.ID
                    LEFT JOIN Users cu ON c.UserId = cu.ID
                    WHERE p.ID = @PostId
                    ORDER BY c.DateCreated;";

            var param = new System.Collections.SortedList
            {
                { "PostId", PostId }
            };

            DataTable dt = await connectDB.Select(sql, param);

            if (dt.Rows.Count == 0)
            {
                return new ApiReponseModel<PostFull>
                {
                    Status = 0,
                    Mess = "Không tìm thấy bài viết",
                    Data = null
                };
            }

                PostFull post = null;
            foreach (DataRow row in dt.Rows)
            {
                if (post == null)
                {
                    post = new PostFull
                    {
                        Id = Convert.ToInt32(row["PostID"]),
                        Content = row["Content"] + "",
                        ImageUrl = row["ImageUrl"] + "",
                        IsPrivate = Convert.ToBoolean(row["IsPrivate"]),
                        DateCreated = Convert.ToDateTime(row["DateCreated"]),
                        DateUpdated = Convert.ToDateTime(row["DateUpdated"]),
                        IsDeleted = Convert.ToBoolean(row["IsDeleted"]),
                        UserId = Convert.ToInt32(row["UserId"]),
                        UserFullName = row["UserFullName"] + "",
                        UserProfilePictureUrl = row["UserProfilePictureUrl"] + "",
                        LikeUserIds = new List<int>(),
                        Comments = new List<CommentDetail>()
                    };
                }

                if (row["LikeUserId"] != DBNull.Value)
                {
                    int likeUserId = Convert.ToInt32(row["LikeUserId"]);
                    if (!post.LikeUserIds.Contains(likeUserId))
                        post.LikeUserIds.Add(likeUserId);
                }

                if (row["CommentID"] != DBNull.Value)
                {
                    var comment = new CommentDetail
                    {
                        ID = Convert.ToInt32(row["CommentID"]),
                        Content = row["CommentContent"] + "",
                        DateCreated = Convert.ToDateTime(row["CommentDateCreated"]),
                        UserId = Convert.ToInt32(row["CommentUserId"]),
                        UserFullName = row["CommentUserFullName"] + "",
                        UserProfilePictureUrl = apiAvatar + row["CommentUserProfilePictureUrl"].ToString()
                    };

                    if (!post.Comments.Any(c => c.ID == comment.ID))
                    {
                        post.Comments.Add(comment);
                    }
                }


            }

            return new ApiReponseModel<PostFull>
            {
                Status = 1,
                Mess = "Lấy bài viết thành công",
                Data = post
            };
        }

        /// <summary>
        /// Người dùng xóa bài viết
        /// </summary>
        /// <param name="PostId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> UserDeletePost(int PostId)
        {
            var sql = "DELETE FROM Comment WHERE PostId = @PostId;" +
                    "DELETE FROM Likes WHERE PostId = @PostId;"+
                    "DELETE FROM Posts WHERE ID = @PostId;";
            var param = new System.Collections.SortedList
            {
                {"PostId",PostId }
            };

            var rs = await connectDB.Delete(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Xóa bài viết thành công",
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Xóa bài viết không thành công",
                };
        }

        /// <summary>
        /// Thêm bình luận
        /// </summary>
        /// <param name="PostId"></param>
        /// <param name="UserId"></param>
        /// <param name="Content"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> AddComment(int PostId, int UserId, string Content)
        {
            var sql = "INSERT INTO Comment (PostId, UserId,Content) VALUES (@PostId,@UserId,@Content);";
            var param = new System.Collections.SortedList
            {
                {"PostId",PostId },
                {"UserId", UserId },
                {"Content",Content }
            };

            var rs = await connectDB.Insert(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Bình luận thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Bình luận thất bại"
                };
        }

        /// <summary>
        /// Xóa bình luân
        /// </summary>
        /// <param name="CommentId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> DeleteComment(int CommentId)
            {
            var sql = "DELETE FROM Comment Where ID = @ID";
            var param = new System.Collections.SortedList
            {
                {"ID",CommentId },
            };

            var rs = await connectDB.Delete(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Xóa bình luận thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Xóa bình luận thất bại"
                };
        }
        /// <summary>
        /// Like bài viết
        /// </summary>
        /// <param name="PostId"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> LikePost(int PostId, int UserId)
        {
            var sql = "INSERT INTO Likes (PostId, UserId) VALUES (@PostId,@UserId);";
            var param = new System.Collections.SortedList
            {
                {"PostId",PostId },
                {"UserId", UserId },
            };

            var rs = await connectDB.Insert(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Like thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Like thất bại"
                };
        }  
        /// <summary>
        /// Hủy like bài viết
        /// </summary>
        /// <param name="LikeId"></param>
        /// <returns></returns>
        public static async Task<ApiReponseModel> UnlikePost (int PostId,int UserId)
        {
            var sql = "DELETE FROM Likes Where PostId = @PostId AND UserId = @UserId";
            var param = new System.Collections.SortedList
            {
                {"PostId",PostId },
                {"UserId",UserId }
            };

            var rs = await connectDB.Delete(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = " Hủy like thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Hủy like thất bại"
                };
        }

    }
}
