using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Models;
using Services;
using Models.ViewModel.Friend;
using Umbraco.Core.Collections;
using Umbraco.Core.Models.Membership;
using System.Linq;
using Models.ViewModel.Chat;
using Cache;
using Microsoft.AspNetCore.Http;

namespace ApiApp.Hubs
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<int, ConcurrentHashSet<string>> UserConnections = new();
        public static List<FriendListVM> onlineFriends = new List<FriendListVM>();
        public async Task UpdateUserOnlineStatusInDb(int userId, bool isOnline)
        {
            var sql = $"UPDATE Users SET IsOnline = @IsOnline WHERE ID = @UserId";
            var param = new System.Collections.SortedList
            {
                { "IsOnline", isOnline ? 1 : 0 },
                { "UserId", userId }
            };
            await connectDB.Update(sql, param);
            Console.WriteLine($"[ChatHub] DB: User {userId} status updated to {(isOnline ? "online" : "offline")}");
        }

        public async Task<List<int>> GetFriendIdsFromDb(int userId)
        {
            var friendIds = new List<int>();
            var sql = @"SELECT
                            CASE
                                WHEN FS.SenderId = @UserId THEN FS.ReceiverId
                                ELSE FS.SenderId
                            END AS FriendId
                        FROM
                            FriendRequests AS FS
                        WHERE
                            (FS.SenderId = @UserId OR FS.ReceiverId = @UserId)
                            AND FS.Status = 1"; 

            var param = new System.Collections.SortedList { { "UserId", userId } };
            System.Data.DataTable data = await connectDB.Select(sql, param);

            if (data != null)
            {
                foreach (System.Data.DataRow row in data.Rows)
                {
                    friendIds.Add(Convert.ToInt32(row["FriendId"]));
                }
            }
            return friendIds;
        }
        private int GetUserIdFromAuthToken(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return 0;
            }

            string userIdString = httpContext.Request.Cookies["LoggedInUserId"];

            if (string.IsNullOrEmpty(userIdString))
            {
                return 0;
            }

            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            else
            {
                return 0;
            }
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                Context.Abort();
                return;
            }

            var userId = GetUserIdFromAuthToken(httpContext);
            if (userId != 0)
            {
                UserConnections.GetOrAdd(userId, new ConcurrentHashSet<string>()).Add(Context.ConnectionId);

                if (UserConnections[userId].Count == 1)
                {
                    await UpdateUserOnlineStatusInDb(userId, true);

                    var friendIds = await GetFriendIdsFromDb(userId);

                    foreach (var friendId in friendIds)
                    {
                        if (UserConnections.TryGetValue(friendId, out var friendConnections))
                        {
                            foreach (var connectionId in friendConnections)
                            {
                                await Clients.Client(connectionId).SendAsync("UserStatusChanged", userId, true);
                                Console.WriteLine($"[ChatHub] Đã gửi 'UserStatusChanged' (online) tới connection {connectionId} của bạn bè {friendId} về user {userId}.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ChatHub] Bạn bè {friendId} của user {userId} không có kết nối online nào để nhận thông báo online.");
                        }
                    }
                }
            }
            else
            {
                Context.Abort(); 
            }

            await base.OnConnectedAsync(); 
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                Context.Abort();
                return;
            }

            var userId = GetUserIdFromAuthToken(httpContext);

            if (userId != 0)
            {
                
                if (UserConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);

                    if (!connections.Any())
                    {
                            UserConnections.TryRemove(userId, out _);

                        await UpdateUserOnlineStatusInDb(userId, false);

                        var friendIds = await GetFriendIdsFromDb(userId);

                        foreach (var friendId in friendIds)
                        {
                            if (UserConnections.TryGetValue(friendId, out var friendConnections))
                            {
                                foreach (var connectionId in friendConnections)
                                {
                                    await Clients.Client(connectionId).SendAsync("UserStatusChanged", userId, false);
                                    Console.WriteLine($"[ChatHub] Đã gửi 'UserStatusChanged' (offline) tới connection {connectionId} của bạn bè {friendId} về user {userId}.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[ChatHub] Bạn bè {friendId} của user {userId} không có kết nối online nào để nhận thông báo offline.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ChatHub] User {userId} disconnected. ConnectionId: {Context.ConnectionId}. Remaining connections for user: {connections.Count}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[ChatHub] Connection {Context.ConnectionId} not found or unauthenticated upon disconnect.");
            }
            await base.OnDisconnectedAsync(exception);
        }


        public async Task SendClickCount(int receiverUserId, int totalClicks)
        {
            if (UserConnections.TryGetValue(receiverUserId, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveClickCount", totalClicks);
                }
                Console.WriteLine($"Sent {totalClicks} clicks to all {connections.Count} connections of user {receiverUserId}.");
            }
            else
            {
                Console.WriteLine($"ReceiverUserId {receiverUserId} not found in UserConnections (not online or no active connections).");
            }
        }

        public async Task SendMessage(SendMessageMD sendMessageMD)
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext == null)
                {
                    Context.Abort();
                    return;
                }

                var userId = GetUserIdFromAuthToken(httpContext);

                sendMessageMD.SenderId = userId;
                
                var rs = await  MessageService.SendMessage(sendMessageMD);
                if (rs.Status > 0)
                {
                    if (UserConnections.TryGetValue(sendMessageMD.TargetId, out var receiverConnections))
                    {
                        foreach (var connectionId in receiverConnections)
                        {
                            await Clients.Client(connectionId).SendAsync("ReceiveMessage", sendMessageMD);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ReceiverUserId {sendMessageMD.TargetId} not found in UserConnections (not online or no active connections).");
                    }

                    if (UserConnections.TryGetValue(userId, out var senderConnections))
                    {
                        foreach (var connectionId in senderConnections)
                        {
                            await Clients.Client(connectionId).SendAsync("MessageSent", sendMessageMD);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(rs.Mess);
                } 

            }

        public async Task SendCallOffer(int targetUserId, object offer)
        {
            var senderUserId = GetUserIdFromAuthToken(Context.GetHttpContext());
            if (senderUserId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to send offer.");
                return;
            }

            Console.WriteLine($"[ChatHub] User {senderUserId} sending call offer to {targetUserId}.");

            if (UserConnections.TryGetValue(targetUserId, out var targetConnections))
            {
                foreach (var connectionId in targetConnections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveCallOffer", senderUserId, offer);
                    Console.WriteLine($"[ChatHub] Sent call offer from {senderUserId} to connection {connectionId} of {targetUserId}.");
                }
            }
            else
            {
                // Inform the caller that the target user is not found or offline.
                await Clients.Caller.SendAsync("CallRejected", targetUserId, "Người dùng không online hoặc không tìm thấy.");
                Console.WriteLine($"[ChatHub] Target user {targetUserId} not found or offline. Offer not sent from {senderUserId}.");
            }
        }

        
        public async Task SendCallAnswer(int targetUserId, object answer)
        {
            var senderUserId = GetUserIdFromAuthToken(Context.GetHttpContext());
            if (senderUserId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to send answer.");
                return;
            }

            Console.WriteLine($"[ChatHub] User {senderUserId} sending call answer to {targetUserId}.");

            if (UserConnections.TryGetValue(targetUserId, out var targetConnections))
            {
                foreach (var connectionId in targetConnections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveCallAnswer", senderUserId, answer);
                    Console.WriteLine($"[ChatHub] Sent call answer from {senderUserId} to connection {connectionId} of {targetUserId}.");
                }
            }
            else
            {
                await Clients.Caller.SendAsync("CallRejected", targetUserId, "Người nhận cuộc gọi đã ngắt kết nối.");
                Console.WriteLine($"[ChatHub] Target user {targetUserId} (caller) not found or offline. Answer not sent from {senderUserId}.");
            }
        }

        
        public async Task SendIceCandidate(int targetUserId, object candidate)
        {
            var senderUserId = GetUserIdFromAuthToken(Context.GetHttpContext());
            if (senderUserId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to send ICE candidate.");
                return;
            }

            Console.WriteLine($"[ChatHub] User {senderUserId} sending ICE candidate to {targetUserId}.");

            if (UserConnections.TryGetValue(targetUserId, out var targetConnections))
            {
                foreach (var connectionId in targetConnections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveIceCandidate", senderUserId, candidate);
                    Console.WriteLine($"[ChatHub] Sent ICE candidate from {senderUserId} to connection {connectionId} of {targetUserId}.");
                }
            }
            else
            {
                // It's generally fine if candidate isn't sent if user is offline,
                // but you might log it or handle it based on your error strategy.
                Console.WriteLine($"[ChatHub] Target user {targetUserId} not found or offline. ICE candidate not sent from {senderUserId}.");
            }
        }

        public async Task SendCallRejected(int targetUserId, string reason)
        {
            var senderUserId = GetUserIdFromAuthToken(Context.GetHttpContext());
            if (senderUserId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to send call rejected status.");
                return;
            }

            Console.WriteLine($"[ChatHub] User {senderUserId} sending call rejected notification to {targetUserId} with reason: {reason}.");

            if (UserConnections.TryGetValue(targetUserId, out var targetConnections))
            {
                foreach (var connectionId in targetConnections)
                {
                    await Clients.Client(connectionId).SendAsync("CallRejected", senderUserId, reason);
                    Console.WriteLine($"[ChatHub] Sent CallRejected from {senderUserId} to connection {connectionId} of {targetUserId}.");
                }
            }
            else
            {
                Console.WriteLine($"[ChatHub] Target user {targetUserId} not found or offline. CallRejected not sent from {senderUserId}.");
            }
        }

        
        public async Task SendCallEnded(int targetUserId)
        {
            var senderUserId = GetUserIdFromAuthToken(Context.GetHttpContext());
            if (senderUserId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to send call ended status.");
                return;
            }

            Console.WriteLine($"[ChatHub] User {senderUserId} sending call ended notification to {targetUserId}.");

            if (UserConnections.TryGetValue(targetUserId, out var targetConnections))
            {
                foreach (var connectionId in targetConnections)
                {
                    await Clients.Client(connectionId).SendAsync("CallEnded", senderUserId);
                    Console.WriteLine($"[ChatHub] Sent CallEnded from {senderUserId} to connection {connectionId} of {targetUserId}.");
                }
            }
            else
            {
                Console.WriteLine($"[ChatHub] Target user {targetUserId} not found or offline. CallEnded not sent from {senderUserId}.");
            }
        }
    }
}
