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
    public class StoryService
    {
        /// <summary>
        /// Lấy tất cả các story
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Story>> GetAllStories()
        {
            var list = new List<Story>();
            string sql = @"
                        SELECT s.*, u.FullName, u.ProfilePictureUrl
                        FROM Storys s
                        JOIN Users u ON s.UserId = u.ID
                        WHERE s.ExpireAt IS NULL OR s.ExpireAt > GETUTCDATE()
                        ORDER BY s.DateCreated DESC";

            DataTable dt = await connectDB.Select(sql);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Story
                {
                    ID = Convert.ToInt32(row["ID"]),
                    ImageUrl = row["ImageUrl"] + "",
                    UserID = Convert.ToInt32(row["UserID"]),
                    IsDeleted = row["IsDeleted"] != DBNull.Value && (bool)row["IsDeleted"],
                    DateCreated = row["DateCreated"] != DBNull.Value ? (DateTime)row["DateCreated"] : DateTime.MinValue,
                    UserFullName = row["FullName"]?.ToString(),
                    ProfilePictureUrl = row["ProfilePictureUrl"]?.ToString()
                });
            }

            return list;
        }

        public static async Task<ApiReponseModel> Upstory(string ImageUrl,DateTime ExpireAt, int UserId)
        {
            var sql = "INSERT INTO Storys(ImageUrl,UserId,ExpireAt) VALUES (@ImageUrl,@UserId,@ExpireAt);";
            var param = new System.Collections.SortedList
            {
                {"ImageUrl", ImageUrl },
                {"ExpireAt",ExpireAt },
                {"UserId",UserId }
            };

            var rs = await connectDB.Insert(sql, param);
            if (rs > 0)
                return new ApiReponseModel
                {
                    Status = 1,
                    Mess = "Upstory thành công"
                };
            else
                return new ApiReponseModel
                {
                    Status = 0,
                    Mess = "Upstory thất bại"
                };
        }
    }
}
