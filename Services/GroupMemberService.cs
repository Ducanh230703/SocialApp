using Models;
using Models.ReponseModel;
using Models.ViewModel.GroupMember;
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
        public static string apiAvatar;
        public static async Task<ApiReponseModel> JoinGroup(int UserID, int GroupID,int Role)
        {
            var apiResponse = new ApiReponseModel();

            StringBuilder sql = new StringBuilder("INSERT INTO GroupMembers (GroupId, UserId,Role) VALUES (@GroupId, @UserId,@Role)");
            var parameters = new SortedList()
            {
                { "GroupId", GroupID },
                { "UserId", UserID },
                { "RoleId", Role },
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

            var sql = $"DELETE FROM GroupMembers Where GroupId ={groupMember.GroupId} AND UserId = {groupMember.UserID} AND (Role = 2 OR Role = 1)";

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

        public static async Task<ApiReponseModel> LeaveGroup(int UserID, int GroupID)
        {
            var apiResponse = new ApiReponseModel();
            var sql = "DELETE FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId";
            var parameters = new SortedList()
            {
                { "GroupId", GroupID },
                { "UserId", UserID }
            };

            try
            {
                var rs = await connectDB.Delete(sql, parameters);
                if (rs > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = $"Người dùng rời nhóm thành công";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không có thành viên vào rời nhóm thành công";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Đã xảy ra lỗi khi rời nhóm: {ex.Message}";
                Console.WriteLine($"Error in AddMember: {ex.Message}");
            }

            return apiResponse;
        }

        public static async Task<ApiReponseModel<List<GroupMemberList>>> GetMembersInGroup(int GroupID, int CallerID)
        {
            var apiResponse = new ApiReponseModel<List<GroupMemberList>>();

            var sql = @"
                        SELECT 
                            GM.UserId, 
                            GM.Role, 
                            U.FullName,  
                            U.ProfilePictureUrl     
                        FROM GroupMembers GM
                        INNER JOIN Users U ON GM.UserId = U.ID
                        WHERE GM.GroupId = @GroupId
                    ";

            var parameters = new SortedList()
            {
                { "GroupId", GroupID }
            };

            try
            {
                // Giả định connectDB.Select trả về DataTable
                var membersTable = await connectDB.Select(sql, parameters);

                // KHAI BÁO DANH SÁCH THÀNH VIÊN ĐỂ LƯU KẾT QUẢ
                var memberList = new List<GroupMemberList>();

                if (membersTable != null && membersTable.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in membersTable.Rows)
                    {
                        memberList.Add(new GroupMemberList
                        {
                            UserID = Convert.ToInt32(row["UserId"]),
                            GroupId = GroupID, 
                            Role = (GroupMemberRole)Convert.ToInt32(row["Role"]),
                            ProfilePictureUrl = apiAvatar + row["ProfilePictureUrl"].ToString(),
                            FullName = row["FullName"].ToString()
                        });
                    }

                    apiResponse.Status = 1;
                    apiResponse.Mess = $"Lấy danh sách {memberList.Count} thành viên thành công.";
                    apiResponse.Data = memberList; // Trả về List<GroupMember>
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không tìm thấy thành viên nào trong nhóm này.";
                    apiResponse.Data = memberList; // Trả về danh sách rỗng
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Đã xảy ra lỗi khi lấy danh sách thành viên: {ex.Message}";
                Console.WriteLine($"Error in GetMembersInGroup: {ex.Message}");
            }

            return apiResponse;
        }
        public static async Task<ApiReponseModel> AddAdmin(int callerID, int groupID, int targetUserID)
        {
            var apiResponse = new ApiReponseModel();

            try
            {
                // 1️⃣ Kiểm tra caller có phải là chủ nhóm không
                var checkSql = "SELECT Role FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId";
                var checkParams = new SortedList()
                {
                    { "GroupId", groupID },
                    { "UserId", callerID }
                };

                var callerTable = await connectDB.Select(checkSql, checkParams);
                if (callerTable.Rows.Count == 0 || Convert.ToInt32(callerTable.Rows[0]["Role"]) != (int)GroupMemberRole.Owner)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không có quyền thêm admin.";
                    return apiResponse;
                }

                // 2️⃣ Cập nhật quyền của người được chỉ định thành Admin
                var sql = "UPDATE GroupMembers SET Role = @Role WHERE GroupId = @GroupId AND UserId = @UserId";
                var parameters = new SortedList()
                {
                    { "Role", (int)GroupMemberRole.Admin },
                    { "GroupId", groupID },
                    { "UserId", targetUserID }
                };

                var rows = await connectDB.Update(sql, parameters);

                if (rows > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = "Đã thêm admin thành công.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không tìm thấy người dùng trong nhóm.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Lỗi khi thêm admin: {ex.Message}";
            }

            return apiResponse;
        }


        public static async Task<ApiReponseModel> RemoveAdmin(int callerID, int groupID, int targetUserID)
        {
            var apiResponse = new ApiReponseModel();

            try
            {
                // 1️⃣ Kiểm tra caller có phải là chủ nhóm không
                var checkSql = "SELECT Role FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId";
                var checkParams = new SortedList()
                {
                    { "GroupId", groupID },
                    { "UserId", callerID }
                };

                var callerTable = await connectDB.Select(checkSql, checkParams);
                if (callerTable.Rows.Count == 0 || Convert.ToInt32(callerTable.Rows[0]["Role"]) != (int)GroupMemberRole.Owner)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không có quyền xóa admin.";
                    return apiResponse;
                }

                // 2️⃣ Cập nhật quyền của người được chỉ định về Member
                var sql = "UPDATE GroupMembers SET Role = @Role WHERE GroupId = @GroupId AND UserId = @UserId AND Role = @OldRole";
                var parameters = new SortedList()
                {
                    { "Role", (int)GroupMemberRole.Member },
                    { "OldRole", (int)GroupMemberRole.Admin },
                    { "GroupId", groupID },
                    { "UserId", targetUserID }
                };

                var rows = await connectDB.Update(sql, parameters);

                if (rows > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = "Đã xóa quyền admin thành công.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không tìm thấy admin cần xóa hoặc người đó không phải admin.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Lỗi khi xóa admin: {ex.Message}";
            }

            return apiResponse;
        }

    }

}

