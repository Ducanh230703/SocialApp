using Models.ReponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class LikeService
    {
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
        public static async Task<ApiReponseModel> UnlikePost(int PostId, int UserId)
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
