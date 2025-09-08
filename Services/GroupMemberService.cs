using Models.ReponseModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class GroupMemberService
    {
        public static async Task<ApiReponseModel> JoinGroup(int groupId, List<int> userId)
        {
            var apiResponse = new ApiReponseModel();
            if (userId == null || !userId.Any())
            {
                apiResponse.Mess = "Danh sách User ID không được để trống.";
                return apiResponse;
            }
            StringBuilder sql = new StringBuilder("INSERT INTO GroupMembers (GroupId, UserId) VALUES ");
            SortedList parameters = new SortedList();

            for (int i = 0; i < userId.Count; i++)
            {
                sql.Append($"(@GroupId{i}, @UserId{i})");
                parameters.Add($"@GroupId{i}", groupId);
                parameters.Add($"@UserId{i}", userId[i]);

                if (i < userId.Count - 1)
                {
                    sql.Append(", "); 
                }
            }

            try
            {
                int rowsAffected = await connectDB.Insert(sql.ToString(), parameters);

                if (rowsAffected > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = $"Đã thêm thành công {rowsAffected} thành viên vào nhóm.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không có thành viên nào được thêm. Có thể đã tồn tại.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Đã xảy ra lỗi khi thêm thành viên: {ex.Message}";
                Console.WriteLine($"Error in AddMember: {ex.Message}");
            }

            return apiResponse;
        }
        
        public static async Task<ApiReponseModel> DeleteMember(int groupId, int userId)
        {
            var apiResponse = new ApiReponseModel();

            var sql = $"DELETE FROM GroupMembers Where GroupId ={groupId} AND UserId = {userId}";

            try
            {
                int rowsAffected = await connectDB.Insert(sql);

                if (rowsAffected > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = $"Đã xóa thành công {rowsAffected} thành viên vào nhóm.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không có thành viên nào được xóa";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Đã xảy ra lỗi khi xóa thành viên: {ex.Message}";
                Console.WriteLine($"Error in AddMember: {ex.Message}");
            }

            return apiResponse;
        }

    }
}
