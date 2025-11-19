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
        public static async Task<ApiReponseModel> JoinGroup(int userId, int groupId, int role)
        {
            var apiResponse = new ApiReponseModel();

            try
            {
                string checkSql = "SELECT IsPrivate FROM Groups WHERE ID = @GroupId";
                var checkParams = new SortedList() { { "GroupId", groupId } };
                var groupTable = await connectDB.Select(checkSql, checkParams);

                bool isPrivate = groupTable.Rows.Count > 0 && Convert.ToBoolean(groupTable.Rows[0]["IsPrivate"]);

                int finalRole = isPrivate ? (int)GroupMemberRole.Pending : role;

                StringBuilder sql = new("INSERT INTO GroupMembers (GroupId, UserId, Role) VALUES (@GroupId, @UserId, @Role)");
                var parameters = new SortedList()
                {
                    { "GroupId", groupId },
                    { "UserId", userId },
                    { "Role", finalRole }
                };

                int rowsAffected = await connectDB.Insert(sql.ToString(), parameters);

                if (rowsAffected > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = isPrivate
                        ? "Yêu cầu tham gia nhóm đã được gửi, vui lòng chờ duyệt."
                        : "Tham gia nhóm thành công.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không thể tham gia nhóm.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Lỗi khi tham gia nhóm: {ex.Message}";
            }

            return apiResponse;
        }

        public static async Task<ApiReponseModel> DeleteMember(int groupId, int memberId,int callerId)
        {
            var apiResponse = new ApiReponseModel();
            int callerRole = await GetUserRoleInGroup(groupId, callerId);
            int memberRole = await GetUserRoleInGroup(groupId, memberId);

            if (callerRole < (int)GroupMemberRole.Admin)
                return new ApiReponseModel { Status = 0, Mess = "Bạn không có quyền thực hiện thao tác này!" };

            if (memberRole == (int)GroupMemberRole.Owner)
                return new ApiReponseModel { Status = 0, Mess = "Không thể xóa chủ nhóm!" };
            var sql = $"DELETE FROM GroupMembers Where GroupId ={groupId} AND UserId = {memberId} AND (Role = 0 OR Role = 1)";

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

        public static async Task<ApiReponseModel> ApproveMember(int approverId, int groupId, int userId)
        {
            var apiResponse = new ApiReponseModel();
            try
            {
                // 🔹 Kiểm tra người duyệt có phải là admin hoặc owner
                string checkSql = "SELECT Role FROM GroupMembers WHERE GroupId=@GroupId AND UserId=@UserId";
                var checkParams = new SortedList()
        {
            { "GroupId", groupId },
            { "UserId", approverId }
        };
                var table = await connectDB.Select(checkSql, checkParams);
                if (table.Rows.Count == 0)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không ở trong nhóm này.";
                    return apiResponse;
                }

                int role = Convert.ToInt32(table.Rows[0]["Role"]);
                if (role < (int)GroupMemberRole.Admin)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không có quyền duyệt thành viên.";
                    return apiResponse;
                }

                string sql = "UPDATE GroupMembers SET Role=@Role WHERE GroupId=@GroupId AND UserId=@UserId AND Role=@Pending";
                var parameters = new SortedList()
                {
                    { "Role", (int)GroupMemberRole.Member },
                    { "Pending", (int)GroupMemberRole.Pending },
                    { "GroupId", groupId },
                    { "UserId", userId }
                };
                int rows = await connectDB.Update(sql, parameters);

                if (rows > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = "Thành viên đã được duyệt vào nhóm.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không tìm thấy yêu cầu cần duyệt.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Lỗi khi duyệt thành viên: {ex.Message}";
            }

            return apiResponse;
        }

        public static async Task<ApiReponseModel> RejectJoinRequest(int approverId, int groupId, int userId)
        {
            var apiResponse = new ApiReponseModel();
            try
            {
                // 1️⃣ Kiểm tra quyền của người thực hiện
                string checkSql = "SELECT Role FROM GroupMembers WHERE GroupId=@GroupId AND UserId=@UserId";
                var checkParams = new SortedList()
        {
            { "GroupId", groupId },
            { "UserId", approverId }
        };

                var roleTable = await connectDB.Select(checkSql, checkParams);
                if (roleTable.Rows.Count == 0)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không phải là thành viên nhóm.";
                    return apiResponse;
                }

                int approverRole = Convert.ToInt32(roleTable.Rows[0]["Role"]);
                if (approverRole < (int)GroupMemberRole.Admin)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Bạn không có quyền từ chối yêu cầu tham gia.";
                    return apiResponse;
                }

                // 2️⃣ Kiểm tra người bị từ chối có đang ở trạng thái Pending không
                string checkPendingSql = "SELECT * FROM GroupMembers WHERE GroupId=@GroupId AND UserId=@UserId AND Role=@Pending";
                var pendingParams = new SortedList()
        {
            { "GroupId", groupId },
            { "UserId", userId },
            { "Pending", (int)GroupMemberRole.Pending }
        };
                var pendingTable = await connectDB.Select(checkPendingSql, pendingParams);

                if (pendingTable.Rows.Count == 0)
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không tìm thấy yêu cầu đang chờ để từ chối.";
                    return apiResponse;
                }

                string deleteSql = "DELETE FROM GroupMembers WHERE GroupId=@GroupId AND UserId=@UserId AND Role=@Pending";
                int rowsDeleted = await connectDB.Delete(deleteSql, pendingParams);

                if (rowsDeleted > 0)
                {
                    apiResponse.Status = 1;
                    apiResponse.Mess = "Đã từ chối yêu cầu tham gia nhóm.";
                }
                else
                {
                    apiResponse.Status = 0;
                    apiResponse.Mess = "Không thể từ chối yêu cầu.";
                }
            }
            catch (Exception ex)
            {
                apiResponse.Status = -1;
                apiResponse.Mess = $"Lỗi khi từ chối yêu cầu: {ex.Message}";
            }

            return apiResponse;
        }

        public static async Task<int> GetUserRoleInGroup(int groupId, int userId)
        {
            var sql = "SELECT Role FROM GroupMembers WHERE GroupId=@groupId AND UserId=@userId";
            var param = new SortedList { { "groupId", groupId }, { "userId", userId } };
            var dt = await connectDB.Select(sql, param);

            return dt.Rows.Count == 0 ? -1 : Convert.ToInt32(dt.Rows[0]["Role"]);
        }

    }

}

