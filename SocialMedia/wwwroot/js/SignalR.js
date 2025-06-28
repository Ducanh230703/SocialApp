let connection;
let totalClicks = 0;

document.addEventListener("DOMContentLoaded", async () => {
    if (!connection || connection.state === signalR.HubConnectionState.Disconnected) {
        console.log("SignalR.js: Initializing new HubConnection.");
        connection = new signalR.HubConnectionBuilder()
            .withUrl(`https://localhost:7024/chathub`)
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on("ReceiveClickCount", (amountOfClicksReceived) => {
            totalClicks += amountOfClicksReceived;
            const totalClicksElement = document.getElementById("totalClicks");
            if (totalClicksElement) {
                totalClicksElement.innerText = totalClicks;
            }
            
        });

        connection.on("UserStatusChanged", (userId, isOnline) => {
            const event = new CustomEvent('userStatusUpdate', { detail: { userId, isOnline } });
            document.dispatchEvent(event);
        });


        try {
            await connection.start();
            console.log("SignalR.js: SignalR connected. Connection ID:", connection.connectionId);
        } catch (err) {
            console.error("SignalR.js: SignalR connection error:", err);
        }
    } else {
        console.log("SignalR.js: Connection already established or being established.");
    }

    connection.on("ReceiveMessage", (sendMessageMD) => {
        console.log("SignalR.js: Received ReceiveMessage event.", sendMessageMD);
        ChatManager.receiveMessage(sendMessageMD.senderId, sendMessageMD.messageContent);

    });

    connection.on("MessageSent", (sendMessageMD) => {
        console.log("SignalR.js: Received MessageSent confirmation.", sendMessageMD);
        const chatBox = ChatManager.openChatBoxes.get(sendMessageMD.targetId);
        if (chatBox) {
            const messagesContainer = chatBox.querySelector(`#chat-messages-${sendMessageMD.targetId}`);
            ChatManager.addMessageToChat(sendMessageMD.senderId, sendMessageMD.messageContent, messagesContainer, true);
        }
    });

    connection.on("ReceiveCallOffer", async (callerId, offer) => {
        console.log(`[SignalR] Received call offer from ${callerId}`);
        await ChatManager.handleCallOffer(callerId, offer);
    });

    connection.on("ReceiveCallAnswer", async (calleeId, answer) => {
        console.log(`[SignalR] Received call answer from ${calleeId}`);
        await ChatManager.handleCallAnswer(calleeId, answer);
    });

    connection.on("ReceiveIceCandidate", async (senderId, candidate) => {
        console.log(`[SignalR] Received ICE candidate from ${senderId}`);
        await ChatManager.handleIceCandidate(senderId, candidate);
    });

    connection.on("CallEnded", (userId) => {
        console.log(`[SignalR] Call ended by ${userId}`);
        ChatManager.handleCallEnded(userId);
    });

    connection.on("CallDeclined", (callerId) => {
        console.log(`[SignalR] Call declined by ${callerId}`);
        ChatManager.handleCallDeclined(callerId);
    });
   

    window.signalRConnection = connection;

    window.sendClickCount = async (userId, amount) => {
        if (window.signalRConnection && window.signalRConnection.state === signalR.HubConnectionState.Connected) {
            try {
                await window.signalRConnection.invoke("SendClickCount", userId, amount);
                console.log(`SignalR.js: Sent SendClickCount to Hub for user ${userId} with amount ${amount}!`);
            } catch (err) {
            }
        } else {
            console.warn("SignalR.js: SignalR connection not established. Cannot send click.");
        }
    };

    const sendClickBtn = document.getElementById("sendClickBtn");
    if (sendClickBtn) {
        sendClickBtn.addEventListener("click", async () => {
            const clicksToSend = 1;

            await window.sendClickCount(4, clicksToSend);
        });
    } else {
        console.warn("SignalR.js: Element with ID 'sendClickBtn' not found.");
    }
});

const SidebarManager = (() => {
    const onlineFriendListContainer = document.getElementById('onlineFriendListContainer');
    const noOnlineFriendsMessage = document.getElementById('noOnlineFriendsMessage');
    const onlineFriendsMap = new Map();

    console.log("SidebarManager initialized. Container:", onlineFriendListContainer);

    const createOnlineFriendItemHtml = (friend) => {
        const friendItem = document.createElement('div');
        friendItem.className = 'online-friend-item';
        friendItem.dataset.userId = friend.userID; 
        const avatar = document.createElement('img');
        avatar.className = 'avatar';
        avatar.src = friend.profilePictureUrl || '/images/default_avatar.png';
        avatar.alt = friend.fullName;

        const friendName = document.createElement('span');
        friendName.className = 'friend-name';
        friendName.textContent = friend.fullName;

        const statusDot = document.createElement('span');
        statusDot.className = 'status-dot online-dot'; // Mặc định là online

        friendItem.appendChild(avatar);
        friendItem.appendChild(friendName);
        friendItem.appendChild(statusDot);
        return friendItem;
    };

    const updateNoFriendsMessage = () => {
        if (!onlineFriendListContainer) return;

        const actualChildren = Array.from(onlineFriendListContainer.children)
            .filter(child => !child.classList.contains('loading-message') && child.id !== 'noOnlineFriendsMessage');

        if (actualChildren.length === 0) {
            if (noOnlineFriendsMessage) {
                noOnlineFriendsMessage.style.display = 'block';
            } else {
                const msg = document.createElement('p');
                msg.className = 'uk-text-muted uk-text-small';
                msg.id = 'noOnlineFriendsMessage';
                msg.textContent = 'Không có bạn bè nào đang online.';
                onlineFriendListContainer.appendChild(msg);
            }
        } else {
            if (noOnlineFriendsMessage) {
                noOnlineFriendsMessage.style.display = 'none';
            }
        }
        console.log("[SidebarManager] updateNoFriendsMessage called. Current online friend count:", actualChildren.length);
    };

    const loadOnlineFriends = () => {
        if (!onlineFriendListContainer) {
            console.error("[SidebarManager] onlineFriendListContainer not found!");
            return;
        }

        onlineFriendListContainer.innerHTML = '<p class="uk-text-muted uk-text-small loading-message">Đang tải bạn bè online...</p>';
        onlineFriendsMap.clear(); // Clear the map as we are reloading everything

        console.log("[SidebarManager] Initiating loadOnlineFriends fetch.");

        fetch('/FriendRequest/LoadOnlineFriend')
            .then(response => {
                console.log("[SidebarManager] LoadOnlineFriend fetch response received.");
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(result => {
                console.log("[SidebarManager] LoadOnlineFriend fetch result:", result);
                onlineFriendListContainer.innerHTML = ''; // Clear all content including loading message

                if (result.status > 0 && result.data) {
                    result.data.forEach(friend => {
                        if (!onlineFriendsMap.has(friend.userID)) {
                            const friendItem = createOnlineFriendItemHtml(friend);
                            onlineFriendListContainer.appendChild(friendItem);
                            onlineFriendsMap.set(friend.userID, friendItem);
                            console.log(`[SidebarManager] Added initial online friend ${friend.userID}.`);
                        } else {
                            console.warn(`[SidebarManager] Initial load: Friend ${friend.userID} already in map, skipping.`);
                        }
                    });
                }
                updateNoFriendsMessage();
            })
            .catch(error => {
                console.error('Lỗi khi tải danh sách bạn bè online:', error);
                onlineFriendListContainer.innerHTML = '<p class="uk-text-danger uk-text-small">Có lỗi xảy ra khi kết nối.</p>';
                updateNoFriendsMessage();
            });
    };

    const handleUserStatusUpdate = async (event) => {
        if (!event || !event.detail) return;
        console.log("[SidebarManager] handleUserStatusUpdate fired. Event detail:", event.detail);
        const { userId, isOnline } = event.detail;

        let existingFriendItem = onlineFriendsMap.get(userId);

        if (isOnline) {
            if (!existingFriendItem) {
                console.log(`[SidebarManager] User ${userId} is online and not in list, fetching details.`);
                try {
                    const response = await fetch(`/User/GetUserOnline?userId=${userId}`);
                    console.log(`[SidebarManager] GetUserOnline response for ${userId}:`, response);
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    const data = await response.json();
                    if (data.status > 0 && data.data != null) {
                        const friend = data.data;
                        console.log(`[SidebarManager] Fetched user ${userId} details:`, friend);
                        if (!onlineFriendsMap.has(friend.userID)) {
                            const friendItem = createOnlineFriendItemHtml(friend);
                            onlineFriendListContainer.appendChild(friendItem);
                            onlineFriendsMap.set(friend.userID, friendItem);
                            updateNoFriendsMessage();
                            console.log(`[SidebarManager] Added new online user ${userId} to list.`);
                        } else {
                            console.log(`[SidebarManager] User ${userId} already added by another process.`);
                            onlineFriendsMap.get(userId).querySelector('.status-dot').classList.remove('offline-dot');
                            onlineFriendsMap.get(userId).querySelector('.status-dot').classList.add('online-dot');
                        }
                    } else {
                        console.error(`Could not fetch details for online user ${userId}:`, data.message);
                    }
                } catch (err) {
                    console.error(`Error fetching friend details for ${userId}:`, err);
                }
            } else {
                console.log(`[SidebarManager] User ${userId} is online, already in list. Updating status dot.`);
                existingFriendItem.querySelector('.status-dot').classList.remove('offline-dot');
                existingFriendItem.querySelector('.status-dot').classList.add('online-dot');
            }
        }
        else { // isOnline is false (offline)
            if (existingFriendItem) {
                console.log(`[SidebarManager] User ${userId} is offline, removing from list.`);
                existingFriendItem.remove();
                onlineFriendsMap.delete(userId); // Remove from map
                updateNoFriendsMessage();
            } else {
                console.log(`[SidebarManager] User ${userId} is offline, but not found in list (already removed or never added).`);
            }
        }
    };

    const setupOnlineFriendClick = () => {
        if (onlineFriendListContainer) {
            onlineFriendListContainer.addEventListener('click', function (event) {
                const friendItem = event.target.closest('.online-friend-item');
                if (friendItem) {
                    const userId = parseInt(friendItem.dataset.userId); // Ensure userId is integer
                    const userName = friendItem.querySelector('.friend-name').textContent;
                    const userAvatar = friendItem.querySelector('.avatar').src;
                    console.log(`Clicked on online friend: ${userName} (ID: ${userId})`);
                    ChatManager.openChatBox(userId, userName, userAvatar);
                }
            });
            console.log("[SidebarManager] Online friend click listener set up.");
        } else {
            console.warn("[SidebarManager] onlineFriendListContainer not found for click listener setup.");
        }
    };

    const setupCustomEventListeners = () => {
        document.addEventListener('userStatusUpdate', handleUserStatusUpdate);
        console.log("[SidebarManager] Custom event listener 'userStatusUpdate' set up.");
    };

    return {
        init: () => {
            console.log("[SidebarManager] Init called.");
            loadOnlineFriends();
            setupOnlineFriendClick();
            setupCustomEventListeners();
        }
    };
})();

// --- ChatManager ---
const ChatManager = (() => {
    const chatBoxesContainer = document.getElementById('chatBoxesContainer');
    const openChatBoxes = new Map();
    let localStream;
    let peerConnection;
    let currentCallingUserId = null;
    let isCallInProgress = false;
    const mediaConstraints = { video: true, audio: true };
    let callOfferSent = false;
    let bufferedIceCandidates = [];

    let remoteStream = null;

    const videoCallModal = document.getElementById('videoCallModal');
    if (!videoCallModal) {
        console.error("Lỗi: Không tìm thấy #videoCallModal trong DOM. Chức năng cuộc gọi video sẽ không hoạt động.");
        return {};
    }

    const localVideo = document.getElementById('localVideo');
    const remoteVideo = document.getElementById('remoteVideo');
    const toggleAudioBtn = document.getElementById('toggleAudioBtn');
    const toggleVideoBtn = document.getElementById('toggleVideoBtn');
    const endCallBtn = document.getElementById('endCallButton');
    const callModalTitle = videoCallModal.querySelector('.uk-modal-title');
    const callStatusMessage = document.getElementById('callStatusText');
    const incomingCallActions = document.getElementById('incomingCallModal').querySelector('#incoming-call-actions');
    const answerCallBtn = document.getElementById('acceptCallButton');
    const declineCallBtn = document.getElementById('rejectCallButton');

    let isAudioMuted = false;
    let isVideoMuted = false;

    const createChatBoxHtml = (userId, fullName, profilePictureUrl) => {
        if (openChatBoxes.has(userId)) {
            const existingBox = openChatBoxes.get(userId);
            existingBox.style.zIndex = getNextZIndex();
            return existingBox;
        }

        const chatBox = document.createElement('div');
        chatBox.className = 'chat-box';
        chatBox.dataset.userId = userId;
        chatBox.id = `chat-box-${userId}`;
        chatBox.style.zIndex = getNextZIndex();

        chatBox.innerHTML = `
                <div class="chat-box-header">
                    <div class="friend-info">
                        <img src="${profilePictureUrl || '/images/default_avatar.png'}" alt="${fullName}" class="avatar">
                        <span class="friend-name">${fullName}</span>
                    </div>
                    <div class="chat-box-actions">
                        <button class="minimize-btn" data-uk-icon="icon: minus"></button>
                        <button class="video-call-btn" data-uk-icon="icon: video-camera"></button>
                        <button class="close-btn" data-uk-icon="icon: close"></button>
                    </div>
                </div>
                <div class="chat-box-messages" id="chat-messages-${userId}">
                    <p class="uk-text-muted uk-text-small uk-text-center">Đang tải tin nhắn...</p>
                </div>
                <div class="chat-box-input">
                    <input type="text" placeholder="Nhập tin nhắn..." id="chat-input-${userId}">
                    <button id="send-button-${userId}" data-uk-icon="icon: paper-plane"></button>
                </div>
            `;

        chatBoxesContainer.appendChild(chatBox);
        openChatBoxes.set(userId, chatBox);

        setupChatBoxEvents(chatBox, userId, fullName);
        return chatBox;
    };

    const setupChatBoxEvents = (chatBox, userId, fullName) => {
        const closeBtn = chatBox.querySelector('.close-btn');
        closeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            closeChatBox(userId);
        });

        const minimizeBtn = chatBox.querySelector('.minimize-btn');
        minimizeBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            chatBox.classList.toggle('minimized');
            // Cuộn xuống cuối tin nhắn khi mở lại (nếu có)
            if (!chatBox.classList.contains('minimized')) {
                const messagesContainer = chatBox.querySelector(`#chat-messages-${userId}`);
                if (messagesContainer) {
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;
                }
            }
        });

        const videoCallBtn = chatBox.querySelector('.video-call-btn');
        videoCallBtn.addEventListener('click', async (e) => {
            e.stopPropagation();
            if (!isCallInProgress) {
                await initiateCall(userId, fullName);
            } else {
                Toastify({
                    text: "Bạn đang trong một cuộc gọi khác. Vui lòng kết thúc cuộc gọi hiện tại trước.",
                    duration: 3000,
                    backgroundColor: "#ffc107",
                    gravity: "top",
                    position: "right",
                }).showToast();
            }
        });


        const sendButton = chatBox.querySelector(`#send-button-${userId}`);
        const messageInput = chatBox.querySelector(`#chat-input-${userId}`);
        const messagesContainer = chatBox.querySelector(`#chat-messages-${userId}`);

        sendButton.addEventListener('click', () => sendMessage(userId, messageInput, messagesContainer));
        messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                sendMessage(userId, messageInput, messagesContainer);
            }
        });

        loadChatMessages(userId, messagesContainer);

        const chatBoxHeader = chatBox.querySelector('.chat-box-header');
        chatBoxHeader.addEventListener('click', () => {
            chatBox.style.zIndex = getNextZIndex();
            // Nếu box đang thu nhỏ, khôi phục nó khi click vào header
            if (chatBox.classList.contains('minimized')) {
                chatBox.classList.remove('minimized');
                const messagesContainer = chatBox.querySelector(`#chat-messages-${userId}`);
                if (messagesContainer) {
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;
                }
            }
        });
    };

    const getNextZIndex = () => {
        let maxZ = 99; // Z-index cơ sở
        document.querySelectorAll('.chat-box').forEach(box => {
            const z = parseInt(box.style.zIndex || 0);
            if (z > maxZ) maxZ = z;
        });
        return maxZ + 1;
    };

    const openChatBox = (userId, fullName, profilePictureUrl) => {
        console.log(`[ChatManager] Opening chat box for ${fullName} (ID: ${userId})`);
        const chatBox = createChatBoxHtml(userId, fullName, profilePictureUrl);
        // Đảm bảo chatbox không bị thu nhỏ khi mở
        chatBox.classList.remove('minimized');
        const messagesContainer = chatBox.querySelector(`#chat-messages-${userId}`);
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    };

    const closeChatBox = (userId) => {
        console.log(`[ChatManager] Closing chat box for ID: ${userId}`);
        const chatBox = openChatBoxes.get(userId);
        if (chatBox) {
            chatBox.remove();
            openChatBoxes.delete(userId);
        }
    };

    const loadChatMessages = (targetId, messagesContainer) => {
        messagesContainer.innerHTML = '<p class="uk-text-muted uk-text-small uk-text-center">Đang tải tin nhắn...</p>'; // Hiển thị lại loading message
        console.log(`[ChatManager] Loading messages for targetId: ${targetId}`);
        fetch(`/Message/GetAllMessage?targetId=${targetId}&pageNumber=1&pageSize=10`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(result => {
                console.log(`[ChatManager] Messages loaded for ${targetId}:`, result);
                messagesContainer.innerHTML = ''; // Xóa "Đang tải tin nhắn..."
                if (result.status > 0 && result.data && result.data.messages && result.data.messages.data.length > 0) {
                    const currentLoggedInUserId = result.data.currentLoggedInUserId;
                    result.data.messages.data.forEach(message => {
                        const messageElement = document.createElement('div');
                        messageElement.className = `message-item ${message.senderId == currentLoggedInUserId ? 'sent' : 'received'}`;
                        messageElement.innerHTML = `<div class="message-content">${message.content}</div>`;
                        messagesContainer.prepend(messageElement);
                    });
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;
                } else {
                    messagesContainer.innerHTML = '<p class="uk-text-muted uk-text-small uk-text-center">Chưa có tin nhắn nào.</p>';
                }
            })
            .catch(error => {
                console.error(`Error loading messages for ${targetId}:`, error);
                messagesContainer.innerHTML = '<p class="uk-text-danger uk-text-small uk-text-center">Lỗi khi tải tin nhắn.</p>';
            });
    };

    const sendMessage = async (targetUserId, messageInput, messagesContainer) => {
        const content = messageInput.value.trim();
        if (!content) return;

        const messagePayload = {
            TargetId: parseInt(targetUserId),
            Type: 0,
            MessageContent: content,
        };

        console.log("[ChatManager] Attempting to send message. Payload:", messagePayload);

        try {
            // Đảm bảo connection đã được khởi tạo và kết nối
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("SendMessage", messagePayload);
                console.log("[ChatManager] SignalR invoke 'SendMessage' successful.");
                messageInput.value = '';
            } else {
                console.error("[ChatManager] SignalR connection is not established or not connected. State:", connection ? connection.state : 'undefined');
                Toastify({
                    text: "Kết nối chat không khả dụng. Vui lòng thử lại sau.",
                    duration: 3000,
                    close: true,
                    gravity: "top",
                    position: "right",
                    backgroundColor: "#dc3545",
                }).showToast();
            }
        } catch (err) {
            console.error("Error sending message via SignalR invoke:", err);
            Toastify({
                text: "Gửi tin nhắn thất bại. Vui lòng thử lại.",
                duration: 3000,
                close: true,
                gravity: "top",
                position: "right",
                backgroundColor: "#dc3545",
            }).showToast();
        }
    };

    const addMessageToChat = (senderId, content, messagesContainer, isSent = false) => {
        console.log(`[ChatManager] addMessageToChat called. Sender ID: ${senderId}, Content: "${content}", Is Sent: ${isSent}`);
        if (!messagesContainer) {
            console.error("[ChatManager] addMessageToChat: messagesContainer is null or undefined! Cannot add message.");
            return;
        }
        const messageElement = document.createElement('div');
        messageElement.className = `message-item ${isSent ? 'sent' : 'received'}`;
        messageElement.innerHTML = `<div class="message-content">${content}</div>`;
        messagesContainer.prepend(messageElement);
        // messagesContainer.scrollTop = messagesContainer.scrollHeight; // Có thể bỏ nếu dùng flex-direction: column-reverse và tin nhắn mới nhất nằm ở cuối
    };

    const receiveMessage = (senderId, content) => {
        console.log(`[ChatManager] receiveMessage called from SignalR. SenderId: ${senderId}, Content: ${content}`);
        const chatBox = openChatBoxes.get(senderId); // senderId đã là số, không cần toString()
        if (chatBox) {
            console.log(`[ChatManager] Chat box found for senderId ${senderId}.`);
            // Đảm bảo chat box không ở trạng thái minimized khi nhận tin nhắn mới
            if (chatBox.classList.contains('minimized')) {
                chatBox.classList.remove('minimized');
                // Đưa chatbox lên trên cùng nếu nó đang được thu nhỏ và nhận tin nhắn mới
                chatBox.style.zIndex = getNextZIndex();
            }
            const messagesContainer = chatBox.querySelector(`#chat-messages-${senderId}`);
            if (messagesContainer) {
                addMessageToChat(senderId, content, messagesContainer, false); // false cho isSent
                messagesContainer.scrollTop = messagesContainer.scrollHeight; // Đảm bảo cuộn xuống tin nhắn mới
            } else {
                console.error(`[ChatManager] messagesContainer not found for senderId ${senderId} inside chatBox.`);
            }
        } else {
            console.warn(`[ChatManager] Chat box not open for senderId ${senderId}. Displaying toast notification.`);
            Toastify({
                text: `Tin nhắn mới từ ${senderId}: ${content}`, // Trong ứng dụng thực tế, hãy lấy tên người gửi
                duration: 5000,
                close: true,
                gravity: "top",
                position: "right",
                backgroundColor: "#007bff",
                onClick: () => {
                    console.log(`Notification clicked for user ${senderId}`);
                }
            }).showToast();
        }
    };

    const getLocalStream = async () => {
        try {
            localStream = await navigator.mediaDevices.getUserMedia(mediaConstraints);
            console.log("[WebRTC] Local stream obtained.");
            localStream.getTracks().forEach(track => {
                console.log(`[Debug] Track in localStream: ${track.kind}, ID: ${track.id}, Enabled: ${track.enabled}, ReadyState: ${track.readyState}`);
            });

            if (localVideo) {
                localVideo.srcObject = localStream;
                localVideo.play().catch(e => console.error("Error playing local video:", e));
            }
            return true;
        } catch (error) {
            console.error("[WebRTC] Error accessing media devices:", error);
            Toastify({
                text: "Không thể truy cập camera hoặc micro. Vui lòng kiểm tra quyền truy cập.",
                duration: 5000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
            return false;
        }
    };

    const setupPeerConnection = () => {
        const configuration = {
            'iceServers': [
                { 'urls': 'stun:stun.l.google.com:19302' },
            ]
        };
        peerConnection = new RTCPeerConnection(configuration);

        if (localStream) {
                localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, localStream);
                console.log(`[Debug] Adding local track to PeerConnection: ${track.kind}, ID: ${track.id}, Enabled: ${track.enabled}, ReadyState: ${track.readyState}`);
            });
        }

        peerConnection.ontrack = (event) => {
            console.log("[WebRTC] Remote track received.", event.streams[0]);
            const newRemoteStream = event.streams[0];

            if (remoteVideo && newRemoteStream) {

                if (remoteVideo.srcObject !== newRemoteStream) {
                    remoteVideo.srcObject = newRemoteStream;
                    remoteVideo.play().catch(e => {
                        console.error("Error playing remote video:", e);
                    });
                    console.log("[Debug] Remote video srcObject set and play attempted.");
                } else {
                    console.log("[Debug] Remote stream already assigned to remoteVideo.");
                }
            } else {
                console.warn("[Debug] No remote video element or new remote stream received.");
            }
        };

        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                console.log("[WebRTC] Sending ICE candidate to remote:", event.candidate);
                if (connection && connection.state === signalR.HubConnectionState.Connected && currentCallingUserId) {
                    connection.invoke("SendIceCandidate", currentCallingUserId, event.candidate)
                        .catch(err => console.error("SignalR: Error sending ICE candidate:", err));
                }
            }
        };

        // Event handler for ICE connection state changes (for debugging)
        peerConnection.oniceconnectionstatechange = () => {
            console.log(`[WebRTC] ICE connection state changed: ${peerConnection.iceConnectionState}`);
            callStatusMessage.textContent = `Trạng thái kết nối: ${getIceConnectionStateDisplay(peerConnection.iceConnectionState)}`;
        };

        // Event handler for signaling state changes (for debugging)
        peerConnection.onsignalingstatechange = () => {
            console.log(`[WebRTC] Signaling state changed: ${peerConnection.signalingState}`);
        };
    };

    const initiateCall = async (targetUserId, targetUserName) => {
        currentCallingUserId = targetUserId;
        isCallInProgress = true;
        callOfferSent = false;
        bufferedIceCandidates = [];

        callModalTitle.textContent = `Đang gọi ${targetUserName}...`;
        callStatusMessage.textContent = 'Đang chờ người nhận trả lời...';
        incomingCallActions.style.display = 'none';
        UIkit.modal(videoCallModal).show(); // Show modal

        const streamGranted = await getLocalStream();
        if (!streamGranted) {
            endCall(); // End the call if media access fails
            return;
        }

        setupPeerConnection();

        try {
            const offer = await peerConnection.createOffer();
            await peerConnection.setLocalDescription(offer);
            console.log("[WebRTC] Created and set local offer.");

            // Send offer to the other peer via SignalR
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("SendCallOffer", targetUserId, offer);
                callOfferSent = true;
                console.log(`[SignalR] Sent call offer to ${targetUserId}`);
            } else {
                console.error("SignalR connection not established. Cannot send call offer.");
                Toastify({
                    text: "Kết nối SignalR không ổn định. Không thể thực hiện cuộc gọi.",
                    duration: 3000,
                    backgroundColor: "#dc3545",
                    gravity: "top",
                    position: "center",
                }).showToast();
                endCall();
            }
        } catch (error) {
            console.error("[WebRTC] Error initiating call:", error);
            Toastify({
                text: "Lỗi khi bắt đầu cuộc gọi video. Vui lòng thử lại.",
                duration: 3000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
            endCall();
        }
    };

    const addBufferedIceCandidates = async () => {
        if (!peerConnection || !peerConnection.remoteDescription) {
            console.warn("[WebRTC] Cannot add buffered ICE candidates: PeerConnection or Remote Description not ready.");
            return;
        }
        console.log(`[WebRTC] Adding ${bufferedIceCandidates.length} buffered ICE candidates.`);
        for (const candidate of bufferedIceCandidates) {
            try {
                await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
                console.log("[WebRTC] Added buffered ICE candidate:", candidate);
            } catch (e) {
                console.error("[WebRTC] Error adding buffered ICE candidate:", e);
            }
        }
        bufferedIceCandidates = [];
    };

    const getIceConnectionStateDisplay = (state) => {
        switch (state) {
            case 'new': return 'Đang khởi tạo...';
            case 'checking': return 'Đang kiểm tra kết nối...';
            case 'connected': return 'Đã kết nối!';
            case 'completed': return 'Kết nối đã hoàn tất.';
            case 'failed': return 'Kết nối thất bại.';
            case 'disconnected': return 'Đã ngắt kết nối.';
            case 'closed': return 'Đã đóng kết nối.';
            default: return state;
        }
    };

    const handleCallOffer = async (callerId, offer) => {
        if (isCallInProgress) {
            console.warn(`[WebRTC] Already in a call or busy. Declining call from ${callerId}.`);
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("DeclineCall", callerId, "busy");
            }
            return;
        }

        currentCallingUserId = callerId;
        isCallInProgress = true;
        callOfferSent = false;
        bufferedIceCandidates = [];

        let callerName = `Người dùng ${callerId}`;

        callModalTitle.textContent = `Cuộc gọi đến từ ${callerName}!`;
        callStatusMessage.textContent = 'Bạn có muốn trả lời không?';
        incomingCallActions.style.display = 'block';
        UIkit.modal(videoCallModal).show();

        const streamGranted = await getLocalStream();
        if (!streamGranted) {
            endCall();
            return;
        }

        setupPeerConnection();

        try {
            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
            console.log("[WebRTC] Set remote description (offer) for incoming call.");

            // Đệm các ICE candidates đã nhận được trước khi setRemoteDescription
            await addBufferedIceCandidates();

            // DO NOT create and set local answer here.
            // Wait for the user to click the "Answer" button.

        } catch (error) {
            console.error("[WebRTC] Error handling incoming offer:", error);
            Toastify({
                text: "Lỗi khi xử lý cuộc gọi đến.",
                duration: 3000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
            endCall();
        }
    };

    const handleCallAnswer = async (calleeId, answer) => {
        if (calleeId !== currentCallingUserId || !peerConnection) {
            console.warn("[WebRTC] Received answer for an unknown or ended call. Ignoring.");
            return;
        }
        try {
            await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
            console.log("[WebRTC] Set remote description (answer). Call established.");
            callStatusMessage.textContent = 'Cuộc gọi đã kết nối!';
            // Hide incoming call actions if visible (in case the modal was showing incoming call UI)
            incomingCallActions.style.display = 'none';
            await addBufferedIceCandidates();
        } catch (error) {
            console.error("[WebRTC] Error setting remote answer:", error);
            Toastify({
                text: "Lỗi khi kết nối cuộc gọi.",
                duration: 3000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
            endCall();
        }
    };

    const handleIceCandidate = async (senderId, candidate) => {
        if (senderId !== currentCallingUserId || !peerConnection || !candidate) {
            console.warn("[WebRTC] Received ICE candidate for an unknown or ended call, or invalid candidate. Ignoring.");
            return;
        }
        try {
            if (peerConnection.remoteDescription) {
                await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
                console.log("[WebRTC] Added remote ICE candidate directly.");
            } else {
                // Nếu remoteDescription chưa được đặt, hãy thêm vào buffer
                bufferedIceCandidates.push(candidate);
                console.log("[WebRTC] Buffered ICE candidate, remote description not yet set.");
            }
        } catch (error) {
            console.error("[WebRTC] Error adding received ICE candidate:", error);
        }
    };

    // End the call
    const endCall = async () => {
        console.log("[WebRTC] Ending call.");
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            localStream = null;
        }
        localVideo.srcObject = null;
        remoteVideo.srcObject = null;
        const previousCallingUserId = currentCallingUserId;
        currentCallingUserId = null;
        isCallInProgress = false;
        callOfferSent = false;
        bufferedIceCandidates = [];
        isAudioMuted = false; // Reset mute states
        isVideoMuted = false;
        toggleAudioBtn.innerHTML = '<span uk-icon="icon: mic"></span>';
        toggleVideoBtn.innerHTML = '<span uk-icon="icon: video-camera"></span>';

        UIkit.modal(videoCallModal).hide(); // Hide modal
        callStatusMessage.textContent = '';
        incomingCallActions.style.display = 'none';

        // Notify the other peer that the call has ended, but only if we were actually in a call
        if (connection && connection.state === signalR.HubConnectionState.Connected && previousCallingUserId) {
            await connection.invoke("SendCallEnded", previousCallingUserId).catch(err => console.error("SignalR: Error sending EndCall:", err));
        }

        Toastify({
            text: "Cuộc gọi đã kết thúc.",
            duration: 2000,
            backgroundColor: "#dc3545",
            gravity: "top",
            position: "center",
        }).showToast();
    };

    // Handle Call Ended signal from remote peer
    const handleCallEnded = (userId) => {
        if (userId === currentCallingUserId && isCallInProgress) {
            console.log(`[WebRTC] Remote user ${userId} ended the call.`);
            // Only clean up, do not send SendCallEnded back to avoid loop
            if (peerConnection) {
                peerConnection.close();
                peerConnection = null;
            }
            if (localStream) {
                localStream.getTracks().forEach(track => track.stop());
                localStream = null;
            }
            localVideo.srcObject = null;
            remoteVideo.srcObject = null;
            currentCallingUserId = null;
            isCallInProgress = false;
            callOfferSent = false;
            bufferedIceCandidates = [];
            isAudioMuted = false;
            isVideoMuted = false;
            toggleAudioBtn.innerHTML = '<span uk-icon="icon: mic"></span>';
            toggleVideoBtn.innerHTML = '<span uk-icon="icon: video-camera"></span>';

            UIkit.modal(videoCallModal).hide();
            callStatusMessage.textContent = '';
            incomingCallActions.style.display = 'none';

            Toastify({
                text: "Người kia đã kết thúc cuộc gọi.",
                duration: 3000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
        }
    };

    // Handle Call Declined signal from remote peer
    const handleCallDeclined = (callerId) => {
        if (callerId === currentCallingUserId && isCallInProgress) {
            console.log(`[WebRTC] Call to ${callerId} was declined.`);
            endCall(); // Clean up local call state
            Toastify({
                text: "Người bạn đang gọi đã từ chối cuộc gọi.",
                duration: 3000,
                backgroundColor: "#dc3545",
                gravity: "top",
                position: "center",
            }).showToast();
        }
    };

    // Event listeners for call controls
    endCallBtn.addEventListener('click', endCall);

    toggleAudioBtn.addEventListener('click', () => {
        if (localStream) {
            const audioTrack = localStream.getAudioTracks()[0];
            if (audioTrack) {
                audioTrack.enabled = !audioTrack.enabled;
                isAudioMuted = !audioTrack.enabled;
                toggleAudioBtn.innerHTML = audioTrack.enabled ? '<span uk-icon="icon: mic"></span>' : '<span uk-icon="icon: mic-mute"></span>';
                Toastify({
                    text: audioTrack.enabled ? "Đã bật micro" : "Đã tắt micro",
                    duration: 1500,
                    backgroundColor: "#5cb85c",
                    gravity: "bottom",
                    position: "center",
                }).showToast();
            }
        }
    });

    toggleVideoBtn.addEventListener('click', () => {
        if (localStream) {
            const videoTrack = localStream.getVideoTracks()[0];
            if (videoTrack) {
                videoTrack.enabled = !videoTrack.enabled;
                isVideoMuted = !videoTrack.enabled;
                toggleVideoBtn.innerHTML = videoTrack.enabled ? '<span uk-icon="icon: video-camera"></span>' : '<span uk-icon="icon: video-camera-off"></span>';
                Toastify({
                    text: videoTrack.enabled ? "Đã bật camera" : "Đã tắt camera",
                    duration: 1500,
                    backgroundColor: "#5cb85c",
                    gravity: "bottom",
                    position: "center",
                }).showToast();
            }
        }
    });

    answerCallBtn.addEventListener('click', async () => {
        if (peerConnection && currentCallingUserId) {
            try {
                // Tạo và đặt local description (answer) CHỈ KHI người dùng chấp nhận
                const answer = await peerConnection.createAnswer();
                await peerConnection.setLocalDescription(answer);
                console.log("[WebRTC] Created and set local answer after user accepted.");

                // Gửi answer qua SignalR
                await connection.invoke("SendCallAnswer", currentCallingUserId, peerConnection.localDescription);
                console.log(`[SignalR] Sent call answer to ${currentCallingUserId}`);
                callStatusMessage.textContent = 'Cuộc gọi đã kết nối!';
                incomingCallActions.style.display = 'none';
                // Đảm bảo các ICE candidates đã được đệm được thêm vào sau khi đặt remote description (trong handleCallOffer)
                // và bây giờ sau khi setLocalDescription, mọi thứ đã sẵn sàng.
                // handleCallAnswer sẽ gọi addBufferedIceCandidates() sau khi setRemoteDescription(answer)
            } catch (error) {
                console.error("[WebRTC] Error sending answer:", error);
                Toastify({
                    text: "Không thể trả lời cuộc gọi. Vui lòng thử lại.",
                    duration: 3000,
                    backgroundColor: "#dc3545",
                    gravity: "top",
                    position: "center",
                }).showToast();
                endCall();
            }
        }
    });

    declineCallBtn.addEventListener('click', async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected && currentCallingUserId) {
            await connection.invoke("DeclineCall", currentCallingUserId, "user_declined")
                .catch(err => console.error("SignalR: Error sending DeclineCall:", err));
        }
        endCall(); // End the call locally
        Toastify({
            text: "Đã từ chối cuộc gọi.",
            duration: 2000,
            backgroundColor: "#ffc107",
            gravity: "top",
            position: "center",
        }).showToast();
    });

    videoCallModal.addEventListener('hide', () => {
        // Chỉ kết thúc cuộc gọi nếu nó đang trong quá trình (để tránh kết thúc nhầm)
        if (isCallInProgress) {
            console.log("[WebRTC] Video call modal hidden, ending call.");
            endCall();
        }
    });

    return {
        openChatBox: openChatBox,
        receiveMessage: receiveMessage,
        closeChatBox: closeChatBox,
        loadChatMessages: loadChatMessages,
        addMessageToChat: addMessageToChat,
        openChatBoxes: openChatBoxes,
        handleCallOffer: handleCallOffer,
        handleCallAnswer: handleCallAnswer,
        handleIceCandidate: handleIceCandidate,
        handleCallEnded: handleCallEnded,
        handleCallDeclined: handleCallDeclined
    };
})();

document.addEventListener("DOMContentLoaded", () => {
    console.log("DOMContentLoaded event fired in Layout Script. Initializing SidebarManager.");
    SidebarManager.init();
});

if (window.history && window.history.replaceState) {
    window.history.replaceState(null, document.title, window.location.href);
}

async function startSignalRConnection() {
    if (!connection || connection.state === signalR.HubConnectionState.Disconnected) {
        console.log("SignalR.js: Attempting to restart connection from startSignalRConnection.");
        try {
            await connection.start();
            console.log("SignalR.js: Connection re-established by startSignalRConnection.");
        } catch (err) {
            console.error("SignalR.js: Failed to restart connection from startSignalRConnection:", err);
        }
    }
}