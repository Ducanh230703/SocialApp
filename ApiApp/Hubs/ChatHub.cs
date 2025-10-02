using Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Models;
using Models.ViewModel.Chat;
using Models.ViewModel.Friend;
using Models.ViewModel.Home;
using Services;
using System.Collections.Concurrent;
using System.Linq;
using Umbraco.Core.Collections;
using Umbraco.Core.Models.Membership;

namespace ApiApp.Hubs
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<int, ConcurrentHashSet<string>> UserConnections = new();
        public static List<FriendListVM> onlineFriends = new List<FriendListVM>();
        private readonly ILogger<ChatHub> _logger;

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
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
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
                _logger.LogWarning("[ChatHub] HttpContext = NULL for ConnectionId {conn}", Context?.ConnectionId);
                return 0;
            }

            string userIdString = httpContext.Request.Cookies["LoggedInUserId"];
            _logger.LogInformation("[ChatHub] Cookie LoggedInUserId = {cookie} for ConnectionId {conn}", userIdString ?? "NULL", Context?.ConnectionId);

            if (string.IsNullOrEmpty(userIdString)) return 0;
            if (int.TryParse(userIdString, out int userId))
            {
                _logger.LogInformation("[ChatHub] Parsed userId = {userId} for ConnectionId {conn}", userId, Context?.ConnectionId);
                return userId;
            }
            _logger.LogWarning("[ChatHub] Cookie LoggedInUserId parse FAILED: {cookie}", userIdString);
            return 0;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                _logger.LogWarning("[ChatHub] HttpContext = NULL on OnConnectedAsync. ConnectionId={conn}", Context.ConnectionId);
                Context.Abort();
                return;
            }

            _logger.LogInformation("[ChatHub] OnConnectedAsync START. ConnectionId={conn}", Context.ConnectionId);

            var userId = GetUserIdFromAuthToken(httpContext);
            _logger.LogInformation("[ChatHub] OnConnectedAsync resolved userId={userId} for ConnectionId={conn}", userId, Context.ConnectionId);

            if (userId != 0)
            {
                var connections = UserConnections.GetOrAdd(userId, new ConcurrentHashSet<string>());
                connections.Add(Context.ConnectionId);

                _logger.LogInformation("[ChatHub] Added connection {conn} for user {userId}. Total connections for this user={count}",
                    Context.ConnectionId, userId, connections.Count);

                if (connections.Count == 1)
                {
                    await UpdateUserOnlineStatusInDb(userId, true);
                    _logger.LogInformation("[ChatHub] User {userId} marked ONLINE in DB.", userId);

                    var friendIds = await GetFriendIdsFromDb(userId);
                    _logger.LogInformation("[ChatHub] User {userId} has {count} friends.", userId, friendIds.Count);

                    foreach (var friendId in friendIds)
                    {
                        if (UserConnections.TryGetValue(friendId, out var friendConnections))
                        {
                            foreach (var connectionId in friendConnections)
                            {
                                await Clients.Client(connectionId).SendAsync("UserStatusChanged", userId, true);
                                _logger.LogInformation("[ChatHub] Notified friend {friendId} (connection {connId}) that user {userId} is ONLINE.",
                                    friendId, connectionId, userId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("[ChatHub] Friend {friendId} of user {userId} has no online connections.", friendId, userId);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("[ChatHub] userId=0. Aborting connection {conn}", Context.ConnectionId);
                Context.Abort();
            }

            await base.OnConnectedAsync();
            _logger.LogInformation("[ChatHub] OnConnectedAsync END for ConnectionId={conn}", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("[ChatHub] OnDisconnectedAsync START. ConnectionId={conn}. Exception={ex}", Context.ConnectionId, exception?.Message);

            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                _logger.LogWarning("[ChatHub] HttpContext = NULL on OnDisconnectedAsync. ConnectionId={conn}", Context.ConnectionId);
                Context.Abort();
                return;
            }

            var userId = GetUserIdFromAuthToken(httpContext);
            _logger.LogInformation("[ChatHub] OnDisconnectedAsync resolved userId={userId} for ConnectionId={conn}", userId, Context.ConnectionId);

            if (userId != 0 && UserConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                _logger.LogInformation("[ChatHub] Removed connection {conn} for user {userId}. Remaining={count}",
                    Context.ConnectionId, userId, connections.Count);

                if (!connections.Any())
                {
                    UserConnections.TryRemove(userId, out _);
                    await UpdateUserOnlineStatusInDb(userId, false);
                    _logger.LogInformation("[ChatHub] User {userId} marked OFFLINE in DB.", userId);

                    var friendIds = await GetFriendIdsFromDb(userId);
                    foreach (var friendId in friendIds)
                    {
                        if (UserConnections.TryGetValue(friendId, out var friendConnections))
                        {
                            foreach (var connectionId in friendConnections)
                            {
                                await Clients.Client(connectionId).SendAsync("UserStatusChanged", userId, false);
                                _logger.LogInformation("[ChatHub] Notified friend {friendId} (connection {connId}) that user {userId} is OFFLINE.",
                                    friendId, connectionId, userId);
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("[ChatHub] Could not resolve valid userId for disconnected connection {conn}.", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
            _logger.LogInformation("[ChatHub] OnDisconnectedAsync END for ConnectionId={conn}", Context.ConnectionId);
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

        public async Task AddComment(int postId, string content)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                Context.Abort();
                return;
            }

            var userId = GetUserIdFromAuthToken(httpContext);
            if (userId == 0)
            {
                Console.WriteLine($"[ChatHub] Unauthenticated user {Context.ConnectionId} attempted to add a comment.");
                return;
            }

            var result = await PostService.AddComment(postId, userId, content);

            if (result.Status == 1)
            {
                var newComment = result.Data;
                await Clients.All.SendAsync("ReceiveNewComment", postId, newComment);
                Console.WriteLine($"[ChatHub] User {userId} added a comment to post {postId}. Broadcasting new comment to all clients.");
            }
            else
            {
                Console.WriteLine($"[ChatHub] Failed to add comment for user {userId} on post {postId}: {result.Mess}");
            }
        }
    }
}
