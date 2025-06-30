// SignalR.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

const openChatBoxes = {};
let currentMaxZIndex = 100;

// Global variables for WebRTC
let localStream;
let remoteStream;
let peerConnection;
let currentCallTargetUserId = null; // To keep track of who we are calling/being called by


connection.on("ReceiveMessage", function (messageId, senderId, targetId, content, sentDate, isRead) {
    console.log("ReceiveMessage:", { messageId, senderId, targetId, content, sentDate, isRead });

    const currentLoggedInUserId = parseInt(localStorage.getItem('loggedInUserId'));

    let chatBoxTargetId;
    if (senderId === currentLoggedInUserId) {
        chatBoxTargetId = targetId;
    } else if (targetId === currentLoggedInUserId) {
        chatBoxTargetId = senderId;
    } else {
        console.warn("Received message not relevant to current user:", senderId, targetId, currentLoggedInUserId);
        return;
    }

    const chatBoxElement = document.getElementById(`chat-box-${chatBoxTargetId}`);
    if (chatBoxElement) {
        const isSender = (senderId === currentLoggedInUserId);
        renderMessage(chatBoxElement, content, isSender);
    } else {
        console.log(`Chat box for user ${chatBoxTargetId} is not open. Showing notification.`);
        loadRecentMessengers(1, 10);
        UIkit.notification({
            message: `Bạn có tin nhắn mới từ ${senderId === currentLoggedInUserId ? 'người khác' : 'ai đó'}`,
            status: 'primary',
            pos: 'bottom-right'
        });
    }
});

connection.on("ReceiveCallOffer", function (fromUserId, fromUserName, signal) {
    console.log("ReceiveCallOffer from:", fromUserId, fromUserName, signal);
    handleIncomingCall(fromUserId, fromUserName, signal);
});

connection.on("ReceiveCallAnswer", function (fromUserId, signal) {
    console.log("ReceiveCallAnswer from:", fromUserId, signal);
    if (peerConnection && peerConnection.remoteDescription === null) {
        const answer = new RTCSessionDescription(JSON.parse(signal));
        peerConnection.setRemoteDescription(answer).catch(e => console.error("Error setting remote description from answer:", e));
        UIkit.notification({ message: 'Kết nối cuộc gọi thành công!', status: 'success', pos: 'bottom-center' });
        // Potentially show the video call modal here if not already shown
        // UIkit.modal('#video-call-modal').show();
    }
});

connection.on("ReceiveIceCandidate", function (fromUserId, candidate) {
    console.log("ReceiveIceCandidate from:", fromUserId, candidate);
    if (peerConnection && candidate) {
        peerConnection.addIceCandidate(new RTCIceCandidate(JSON.parse(candidate))).catch(e => console.error("Error adding ICE candidate:", e));
    }
});

connection.on("CallEnded", function (endingUserId) {
    console.log("CallEnded by:", endingUserId);
    handleCallEnded(endingUserId);
});

connection.on("CallDeclined", function (decliningUserId) {
    console.log("CallDeclined by:", decliningUserId);
    handleCallDeclined(decliningUserId);
});


async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        setTimeout(startConnection, 5000);
    }
}

startConnection();

connection.onclose(async () => {
    console.log("SignalR Disconnected. Attempting to reconnect...");
    await startConnection();
});


async function sendMessage(targetUserId, messageContent) {
    if (connection.state === signalR.HubConnectionState.Connected) {
        try {
            const senderId = parseInt(localStorage.getItem('loggedInUserId'));
            if (isNaN(senderId)) {
                console.error("SenderId is not a valid number. Cannot send message.");
                UIkit.notification({ message: 'Lỗi: Không tìm thấy ID người dùng đăng nhập.', status: 'danger', pos: 'bottom-center' });
                return;
            }

            const messageType = "text";

            await connection.invoke("SendMessage", senderId, targetUserId, messageContent, messageType)
                .then(response => {
                    console.log("Message sent response:", response);
                    if (response.status === 1) {
                        console.log("Message successfully sent to API and saved.");
                        const chatInput = document.getElementById(`chat-input-${targetUserId}`);
                        if (chatInput) {
                            chatInput.value = '';
                            chatInput.focus();
                        }
                    } else {
                        console.error("Failed to send message via API:", response.mess);
                        UIkit.notification({ message: `Gửi tin nhắn thất bại: ${response.mess}`, status: 'danger', pos: 'bottom-center' });
                    }
                })
                .catch(err => {
                    console.error("Error invoking SendMessage on hub:", err);
                    UIkit.notification({ message: `Lỗi khi gửi tin nhắn: ${err.message}`, status: 'danger', pos: 'bottom-center' });
                });
        } catch (err) {
            console.error("Error sending message:", err);
            UIkit.notification({ message: `Lỗi kết nối khi gửi tin nhắn: ${err.message}`, status: 'danger', pos: 'bottom-center' });
        }
    } else {
        console.warn("SignalR connection not established. Message not sent.");
        UIkit.notification({ message: 'Chưa kết nối đến máy chủ chat. Vui lòng thử lại.', status: 'warning', pos: 'bottom-center' });
    }
}

function renderMessage(chatBoxElement, content, isSender) {
    const messagesContainer = chatBoxElement.querySelector('.chat-box-messages');
    if (!messagesContainer) return;

    const noMessagesParagraph = messagesContainer.querySelector('.no-messages-yet');
    if (noMessagesParagraph) {
        noMessagesParagraph.remove();
    }

    const messageItem = document.createElement('div');
    messageItem.className = `message-item ${isSender ? 'sent' : 'received'}`;
    messageItem.innerHTML = `<div class="message-content">${content}</div>`;

    messagesContainer.prepend(messageItem);

    if (messagesContainer.scrollHeight - messagesContainer.clientHeight <= messagesContainer.scrollTop + 20 || isSender) {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
}

async function openChatBox(targetUserId, targetFullName, targetProfilePictureUrl) {
    if (openChatBoxes[targetUserId]) {
        console.log(`Chat box for user ${targetFullName} (${targetUserId}) is already open.`);
        const existingChatBox = openChatBoxes[targetUserId].element;
        updateChatBoxZIndex(existingChatBox);
        existingChatBox.classList.remove('minimized');
        openChatBoxes[targetUserId].isMinimized = false;
        positionAllChatBoxes();
        const chatInput = document.getElementById(`chat-input-${targetUserId}`);
        if (chatInput) {
            chatInput.focus();
        }
        return;
    }

    try {
        const response = await fetch(`/Message/GetChatBoxPartial?targetId=${targetUserId}&targetFullName=${encodeURIComponent(targetFullName)}&targetProfilePictureUrl=${encodeURIComponent(targetProfilePictureUrl)}`);
        if (response.ok) {
            const partialHtml = await response.text();
            const chatBoxContainer = document.getElementById('chat-boxes-container');
            if (!chatBoxContainer) {
                console.error("Chat boxes container not found.");
                return;
            }

            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = partialHtml;
            const newChatBoxElement = tempDiv.firstElementChild;
            chatBoxContainer.appendChild(newChatBoxElement);

            openChatBoxes[targetUserId] = { element: newChatBoxElement, zIndex: 0, isMinimized: false };
            updateChatBoxZIndex(newChatBoxElement);

            attachChatBoxEventListeners(newChatBoxElement, targetUserId);

            positionAllChatBoxes();

            const messagesContainer = newChatBoxElement.querySelector('.chat-box-messages');
            if (messagesContainer) {
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }
            const chatInput = document.getElementById(`chat-input-${targetUserId}`);
            if (chatInput) {
                chatInput.focus();
            }

        } else {
            console.error('Failed to load chat box partial:', response.statusText);
            UIkit.notification({ message: 'Không thể mở hộp chat.', status: 'danger', pos: 'bottom-center' });
        }
    } catch (error) {
        console.error('Error fetching chat box partial:', error);
        UIkit.notification({ message: 'Đã xảy ra lỗi khi mở hộp chat.', status: 'danger', pos: 'bottom-center' });
    }
}

function attachChatBoxEventListeners(chatBoxElement, targetUserId) {
    const sendButton = chatBoxElement.querySelector(`#send-button-${targetUserId}`);
    const chatInput = chatBoxElement.querySelector(`#chat-input-${targetUserId}`);
    if (sendButton && chatInput) {
        sendButton.addEventListener('click', () => {
            const messageContent = chatInput.value.trim();
            if (messageContent) {
                sendMessage(targetUserId, messageContent);
            }
        });

        chatInput.addEventListener('keypress', (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
                sendButton.click();
            }
        });
    }

    const minimizeBtn = chatBoxElement.querySelector('.minimize-btn');
    if (minimizeBtn) {
        minimizeBtn.addEventListener('click', () => {
            chatBoxElement.classList.toggle('minimized');
            openChatBoxes[targetUserId].isMinimized = chatBoxElement.classList.contains('minimized');
            positionAllChatBoxes();
        });
    }

    const closeBtn = chatBoxElement.querySelector('.close-btn');
    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            chatBoxElement.remove();
            delete openChatBoxes[targetUserId];
            positionAllChatBoxes();
            // If a call is active in this chat box, end it
            if (currentCallTargetUserId === targetUserId) {
                endCall();
            }
        });
    }

    const chatBoxHeader = chatBoxElement.querySelector('.chat-box-header');
    if (chatBoxHeader) {
        chatBoxHeader.addEventListener('click', (event) => {
            if (!event.target.closest('.chat-box-actions button')) {
                updateChatBoxZIndex(chatBoxElement);
                if (chatBoxElement.classList.contains('minimized')) {
                    chatBoxElement.classList.remove('minimized');
                    openChatBoxes[targetUserId].isMinimized = false;
                    positionAllChatBoxes();
                }
                const chatInput = document.getElementById(`chat-input-${targetUserId}`);
                if (chatInput) {
                    chatInput.focus();
                }
            }
        });
    }

    const loadMoreBtnContainer = chatBoxElement.querySelector('.load-more-messages-btn-container');
    const loadMoreBtn = chatBoxElement.querySelector('.load-more-messages-btn');
    const messagesContainer = chatBoxElement.querySelector('.chat-box-messages');

    if (loadMoreBtn && messagesContainer) {
        loadMoreBtn.addEventListener('click', async () => {
            let currentPage = parseInt(messagesContainer.dataset.currentPage || 1);
            const totalPages = parseInt(chatBoxElement.dataset.totalPages || 1);
            const pageSize = parseInt(chatBoxElement.dataset.pageSize || 10);

            if (currentPage < totalPages) {
                currentPage++;
                loadMoreBtn.disabled = true;
                loadMoreBtn.textContent = "Đang tải...";

                try {
                    const response = await fetch(`/api/Message/GetAllMessage?targetUserId=${targetUserId}&pageNumber=${currentPage}&pageSize=${pageSize}`);
                    if (response.ok) {
                        const apiResponse = await response.json();
                        if (apiResponse.status === 1 && apiResponse.data && apiResponse.data.messages && apiResponse.data.messages.data) {
                            const oldScrollHeight = messagesContainer.scrollHeight;

                            apiResponse.data.messages.data.reverse().forEach(message => {
                                const isSender = (message.senderId === parseInt(localStorage.getItem('loggedInUserId')));
                                const messageItem = document.createElement('div');
                                messageItem.className = `message-item ${isSender ? 'sent' : 'received'}`;
                                messageItem.innerHTML = `<div class="message-content">${message.Content}</div>`;
                                messagesContainer.insertBefore(messageItem, messagesContainer.firstChild);
                            });

                            messagesContainer.dataset.currentPage = currentPage;
                            chatBoxElement.dataset.currentPage = currentPage;
                            chatBoxElement.dataset.totalPages = apiResponse.data.messages.totalPages;

                            messagesContainer.scrollTop = messagesContainer.scrollHeight - oldScrollHeight;

                            if (currentPage >= apiResponse.data.messages.totalPages) {
                                if (loadMoreBtnContainer) loadMoreBtnContainer.style.display = 'none';
                            }
                        } else {
                            UIkit.notification({ message: apiResponse.mess || 'Không thể tải thêm tin nhắn.', status: 'warning', pos: 'bottom-center' });
                        }
                    } else {
                        UIkit.notification({ message: 'Lỗi khi tải thêm tin nhắn.', status: 'danger', pos: 'bottom-center' });
                    }
                } catch (error) {
                    console.error('Error loading more messages:', error);
                    UIkit.notification({ message: 'Đã xảy ra lỗi khi tải thêm tin nhắn.', status: 'danger', pos: 'bottom-center' });
                } finally {
                    loadMoreBtn.disabled = false;
                    loadMoreBtn.textContent = "Tải thêm tin nhắn cũ hơn";
                }
            } else {
                if (loadMoreBtnContainer) loadMoreBtnContainer.style.display = 'none';
            }
        });

        const initialPage = parseInt(messagesContainer.dataset.currentPage || 1);
        const initialTotalPages = parseInt(chatBoxElement.dataset.totalPages || 1);
        if (initialPage >= initialTotalPages) {
            if (loadMoreBtnContainer) loadMoreBtnContainer.style.display = 'none';
        }
    }

    const videoCallBtn = chatBoxElement.querySelector('.video-call-btn');
    if (videoCallBtn) {
        videoCallBtn.addEventListener('click', () => {
            initiateCall(targetUserId, chatBoxElement.dataset.targetFullName);
        });
    }
}


function positionAllChatBoxes() {
    const chatBoxWidth = 320;
    const gap = 20;
    let currentRightOffset = 20;

    const unminimizedBoxes = [];
    const minimizedBoxes = [];

    Object.values(openChatBoxes).forEach(boxInfo => {
        if (boxInfo.element && boxInfo.element.parentNode) {
            if (boxInfo.isMinimized) {
                minimizedBoxes.push(boxInfo);
            } else {
                unminimizedBoxes.push(boxInfo);
            }
        } else {
            delete openChatBoxes[boxInfo.element.dataset.userId];
        }
    });

    unminimizedBoxes.sort((a, b) => a.zIndex - b.zIndex);

    unminimizedBoxes.forEach(boxInfo => {
        const boxElement = boxInfo.element;
        boxElement.style.right = `${currentRightOffset}px`;
        boxElement.style.bottom = '20px';
        currentRightOffset += chatBoxWidth + gap;
    });
}


function updateChatBoxZIndex(chatBoxElement) {
    currentMaxZIndex++;
    chatBoxElement.style.zIndex = currentMaxZIndex;

    const targetUserId = parseInt(chatBoxElement.dataset.userId);
    if (openChatBoxes[targetUserId]) {
        openChatBoxes[targetUserId].zIndex = currentMaxZIndex;
    }

    Object.values(openChatBoxes).forEach(boxInfo => {
        if (boxInfo.element) {
            boxInfo.element.style.zIndex = boxInfo.zIndex;
        }
    });
}


const getLocalStream = async () => {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        localStream = stream; // Store it globally
        return stream;
    } catch (error) {
        console.error("Error getting local stream:", error);
        return null;
    }
};

const setupPeerConnection = (targetUserId) => {
    const pc = new RTCPeerConnection({
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
        ]
    });

    pc.onicecandidate = (event) => {
        if (event.candidate) {
            connection.invoke("SendIceCandidate", targetUserId, JSON.stringify(event.candidate));
        }
    };

    pc.ontrack = (event) => {
        remoteStream = event.streams[0];
        // You'll need a <video> element with a specific ID (e.g., 'remoteVideo') in your HTML
        // to display this stream. For example: document.getElementById('remoteVideo').srcObject = remoteStream;
        console.log("Remote stream received.");
    };

    if (localStream) {
        localStream.getTracks().forEach(track => pc.addTrack(track, localStream));
    } else {
        console.warn("No local stream available when setting up peer connection.");
    }

    peerConnection = pc; // Store globally
    return pc;
};

const initiateCall = async (targetUserId, targetUserName) => {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        UIkit.notification({ message: 'Chưa kết nối đến máy chủ chat. Không thể gọi.', status: 'warning', pos: 'bottom-center' });
        return;
    }

    currentCallTargetUserId = targetUserId;
    try {
        const stream = await getLocalStream();
        if (!stream) {
            UIkit.notification({ message: 'Không thể truy cập camera/micro. Vui lòng kiểm tra quyền.', status: 'danger', pos: 'bottom-center' });
            return;
        }

        const pc = setupPeerConnection(targetUserId);
        if (!pc) {
            UIkit.notification({ message: 'Lỗi khởi tạo kết nối P2P.', status: 'danger', pos: 'bottom-center' });
            return;
        }

        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        await connection.invoke("SendCallOffer", targetUserId, JSON.stringify(offer), targetUserName);
        console.log(`Call initiated to ${targetUserName} (${targetUserId})`);
        UIkit.notification({ message: `Đang gọi ${targetUserName}...`, status: 'primary', pos: 'top-center', timeout: 0 });

        // Example: Show a video call modal or UI
        // UIkit.modal('#video-call-modal').show();
    } catch (error) {
        console.error("Error initiating call:", error);
        UIkit.notification({ message: `Lỗi khi gọi: ${error.message}`, status: 'danger', pos: 'bottom-center' });
    }
};

const acceptCall = async (callerId, offerSignal) => {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        UIkit.notification({ message: 'Chưa kết nối đến máy chủ chat. Không thể chấp nhận cuộc gọi.', status: 'warning', pos: 'bottom-center' });
        return;
    }

    currentCallTargetUserId = callerId;
    try {
        const stream = await getLocalStream();
        if (!stream) {
            UIkit.notification({ message: 'Không thể truy cập camera/micro. Vui lòng kiểm tra quyền.', status: 'danger', pos: 'bottom-center' });
            return;
        }

        const pc = setupPeerConnection(callerId);
        if (!pc) {
            UIkit.notification({ message: 'Lỗi khởi tạo kết nối P2P.', status: 'danger', pos: 'bottom-center' });
            return;
        }

        const offer = new RTCSessionDescription(JSON.parse(offerSignal));
        await pc.setRemoteDescription(offer);

        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);

        await connection.invoke("SendCallAnswer", callerId, JSON.stringify(answer));
        console.log("Call accepted and answer sent.");
        UIkit.notification.closeAll(); // Close the incoming call notification
        UIkit.notification({ message: 'Đang kết nối cuộc gọi...', status: 'success', pos: 'bottom-center' });

        // Example: Show video call UI
        // UIkit.modal('#video-call-modal').show();
    } catch (error) {
        console.error("Error accepting call:", error);
        UIkit.notification({ message: `Lỗi khi chấp nhận cuộc gọi: ${error.message}`, status: 'danger', pos: 'bottom-center' });
    }
};

const declineCall = async (callerId) => {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        UIkit.notification({ message: 'Chưa kết nối đến máy chủ chat. Không thể từ chối cuộc gọi.', status: 'warning', pos: 'bottom-center' });
        return;
    }
    try {
        await connection.invoke("DeclineCall", callerId);
        console.log("Call declined.");
        UIkit.notification.closeAll();
        UIkit.notification({ message: 'Đã từ chối cuộc gọi.', status: 'info', pos: 'bottom-center' });
    } catch (error) {
        console.error("Error declining call:", error);
        UIkit.notification({ message: `Lỗi khi từ chối cuộc gọi: ${error.message}`, status: 'danger', pos: 'bottom-center' });
    }
};

const endCall = async () => {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        console.warn("Cannot end call: SignalR connection not established.");
        return;
    }
    if (currentCallTargetUserId) {
        try {
            await connection.invoke("EndCall", currentCallTargetUserId);
            console.log(`Call with ${currentCallTargetUserId} ended by local user.`);
        } catch (error) {
            console.error("Error invoking EndCall on hub:", error);
        }
    }

    if (localStream) {
        localStream.getTracks().forEach(track => track.stop());
        localStream = null;
    }
    if (remoteStream) {
        remoteStream.getTracks().forEach(track => track.stop());
        remoteStream = null;
    }
    if (peerConnection) {
        peerConnection.close();
        peerConnection = null;
    }
    currentCallTargetUserId = null;

    // Example: Hide any video call UI/modal
    // UIkit.modal('#video-call-modal').hide();
    console.log("Call resources released.");
    UIkit.notification.closeAll();
    UIkit.notification({ message: 'Cuộc gọi đã kết thúc.', status: 'info', pos: 'bottom-center' });
};


const handleIncomingCall = (fromUserId, fromUserName, signal) => {
    console.log(`Incoming call from ${fromUserName} (${fromUserId})`);
    UIkit.notification({
        message: `Cuộc gọi đến từ ${fromUserName}. <button class="uk-button uk-button-small uk-button-primary accept-call-btn" data-caller-id="${fromUserId}" data-signal='${signal}'>Chấp nhận</button> <button class="uk-button uk-button-small uk-button-danger decline-call-btn" data-caller-id="${fromUserId}">Từ chối</button>`,
        status: 'primary',
        pos: 'top-center',
        timeout: 0
    });

    document.querySelectorAll('.accept-call-btn').forEach(btn => {
        btn.onclick = async () => {
            const callerId = parseInt(btn.dataset.callerId);
            const offerSignal = btn.dataset.signal;
            await acceptCall(callerId, offerSignal);
            UIkit.notification.closeAll();
        };
    });

    document.querySelectorAll('.decline-call-btn').forEach(btn => {
        btn.onclick = async () => {
            const callerId = parseInt(btn.dataset.callerId);
            await declineCall(callerId);
            UIkit.notification.closeAll();
        };
    });
};

const handleCallEnded = (endingUserId) => {
    console.log(`Call with ${endingUserId} has ended.`);
    if (currentCallTargetUserId === endingUserId) {
        endCall(); // Clean up local resources
        UIkit.notification({ message: 'Cuộc gọi đã kết thúc từ phía bên kia.', status: 'info', pos: 'bottom-center' });
    }
};

const handleCallDeclined = (decliningUserId) => {
    console.log(`Call to ${decliningUserId} was declined.`);
    if (currentCallTargetUserId === decliningUserId) {
        endCall(); // Clean up local resources
        UIkit.notification({ message: 'Cuộc gọi bị từ chối.', status: 'warning', pos: 'bottom-center' });
    }
};


document.addEventListener('DOMContentLoaded', () => {
    let chatBoxesContainer = document.getElementById('chat-boxes-container');
    if (!chatBoxesContainer) {
        chatBoxesContainer = document.createElement('div');
        chatBoxesContainer.id = 'chat-boxes-container';
        document.body.appendChild(chatBoxesContainer);
    }

    document.body.addEventListener('click', (event) => {
        const chatTrigger = event.target.closest('.open-chat-trigger');
        if (chatTrigger) {
            const targetUserId = parseInt(chatTrigger.dataset.userId);
            const targetFullName = chatTrigger.dataset.fullName;
            const targetProfilePictureUrl = chatTrigger.dataset.profilePictureUrl;
            if (!isNaN(targetUserId)) {
                openChatBox(targetUserId, targetFullName, targetProfilePictureUrl);
            } else {
                console.error("Invalid targetUserId for chat trigger.");
            }
        }
    });

    loadRecentMessengers(1, 10);
});


async function loadRecentMessengers(pageNumber, pageSize) {
    try {
        const response = await fetch(`/Message/GetMessengerList?pageNumber=${pageNumber}&pageSize=${pageSize}`);
        if (response.ok) {
            const partialHtml = await response.text();
            const recentMessagesContainer = document.getElementById('recent-messages-container');
            if (recentMessagesContainer) {
                recentMessagesContainer.innerHTML = partialHtml;
            } else {
                console.warn("Recent messages container not found. Appending to body for demonstration.");
                document.body.insertAdjacentHTML('beforeend', partialHtml);
            }
        } else {
            console.error('Failed to load recent messengers partial:', response.statusText);
        }
    } catch (error) {
        console.error('Error fetching recent messengers partial:', error);
    }
}