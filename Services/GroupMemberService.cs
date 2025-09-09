using Models;
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
        public static async Task<ApiReponseModel> JoinGroup(int UserID, int GroupID)
        {
            var apiResponse = new ApiReponseModel();

            StringBuilder sql = new StringBuilder("INSERT INTO GroupMembers (GroupId, UserId) VALUES (@GroupId, @UserId)");
            var parameters = new SortedList()
            {
                { "GroupId", GroupID },
                { "UserId", UserID }
            };

            try
            {
                int rowsAffected = await connectDB.Insert(sql.ToString(), parameters);

                if (rowsAffected > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = "Tham gia nhóm thành công";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Tham gia nhóm thất bại";
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

        public static async Task<ApiReponseModel> DeleteMember(GroupMember groupMember)
        {
            var apiResponse = new ApiReponseModel();

            var sql = $"DELETE FROM GroupMembers Where GroupId ={groupMember.GroupId} AND UserId = {groupMember.UserID} AND (Role = 'Owner' OR Role = 'Admin'";

            try
            {
                int rowsAffected = await connectDB.Delete(sql);

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
