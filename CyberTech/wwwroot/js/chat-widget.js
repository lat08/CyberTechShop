let isChatOpen = false;
let chatHistory = JSON.parse(localStorage.getItem('chatHistory')) || [];

document.addEventListener('DOMContentLoaded', () => {
    const chatWidget = document.getElementById('chat-widget');
    const chatBubble = document.getElementById('chat-bubble');
    const chatInput = document.getElementById('chat-input');
    const sendButton = document.getElementById('send-message');
    const minimizeButton = document.getElementById('chat-minimize');
    const maximizeButton = document.getElementById('chat-maximize');
    const closeButton = document.getElementById('chat-close');
    const messagesContainer = document.getElementById('chat-messages');
    const scrollBottomBtn = document.getElementById('scroll-bottom-btn');
    
    let isMaximized = false;

    // Load chat history from localStorage
    function loadChatHistory() {
        const savedHistory = localStorage.getItem('chatHistory');
        if (savedHistory) {
            chatHistory = JSON.parse(savedHistory);
            chatHistory.forEach(msg => {
                appendMessage(msg.content, msg.role === 'user' ? 'user-message' : 'bot-message', msg.role !== 'user');
            });
        }
        scrollToBottom();
    }

    // Save chat history to localStorage
    function saveChatHistory() {
        if (chatHistory.length > 50) {
            chatHistory = chatHistory.slice(-50); // Keep only last 50 messages
        }
        localStorage.setItem('chatHistory', JSON.stringify(chatHistory));
    }

    // Toggle chat widget visibility
    function toggleChat() {
        isChatOpen = !isChatOpen;
        chatWidget.classList.toggle('visible', isChatOpen);
        chatBubble.classList.toggle('hidden', isChatOpen);
        
        if (isChatOpen) {
            chatInput.focus();
            scrollToBottom();
        }
    }

    // Scroll to bottom functionality
    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
        updateScrollButtonVisibility();
    }

    // Update scroll button visibility
    function updateScrollButtonVisibility() {
        const isNearBottom = messagesContainer.scrollHeight - messagesContainer.scrollTop - messagesContainer.clientHeight < 100;
        scrollBottomBtn.classList.toggle('visible', !isNearBottom && messagesContainer.scrollHeight > messagesContainer.clientHeight);
    }

    // Show/hide scroll button based on scroll position
    messagesContainer.addEventListener('scroll', updateScrollButtonVisibility);

    // Scroll to bottom button click handler
    scrollBottomBtn.addEventListener('click', scrollToBottom);

    // Event listeners for chat bubble and close button
    chatBubble.addEventListener('click', toggleChat);
    closeButton.addEventListener('click', () => {
        toggleChat();
    });

    // Minimize functionality
    minimizeButton.addEventListener('click', () => {
        toggleChat();
    });

    // Maximize functionality
    maximizeButton.addEventListener('click', () => {
        isMaximized = !isMaximized;
        chatWidget.classList.toggle('maximized');
        maximizeButton.innerHTML = isMaximized ? 
            '<i class="fas fa-compress"></i>' : 
            '<i class="fas fa-expand"></i>';
    });

    // Chat functionality
    async function sendMessage() {
        const message = chatInput.value.trim();
        if (!message) return;

        // Add user message
        appendMessage(message, 'user-message');
        chatHistory.push({ role: 'user', content: message });
        saveChatHistory();

        // Clear input and disable
        chatInput.value = '';
        chatInput.disabled = true;
        sendButton.disabled = true;

        // Show typing indicator and scroll
        showTypingIndicator(true);
        scrollToBottom();

        try {
            const response = await fetch('/api/Chat/GeminiChat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify({ userInput: message })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            // Hide typing indicator
            showTypingIndicator(false);

            // Enable input
            chatInput.disabled = false;
            sendButton.disabled = false;
            chatInput.focus();

            if (result.success) {
                // Add bot response - result.html contains the formatted HTML response
                appendMessage(result.html, 'bot-message', true);
                chatHistory.push({ role: 'bot', content: result.html });
                saveChatHistory();
                scrollToBottom();
            } else {
                throw new Error(result.message || 'Không thể xử lý yêu cầu');
            }

        } catch (error) {
            console.error('Chat error:', error);
            showTypingIndicator(false);
            chatInput.disabled = false;
            sendButton.disabled = false;
            
            appendMessage('Xin lỗi, hiện tại tôi không thể trả lời. Vui lòng thử lại sau.', 'bot-message error');
            chatHistory.push({ role: 'bot', content: 'Xin lỗi, hiện tại tôi không thể trả lời. Vui lòng thử lại sau.' });
            saveChatHistory();
            scrollToBottom();
        }
    }

    function appendMessage(content, className, isHtml = false) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${className}`;
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';

        if (isHtml) {
            contentDiv.innerHTML = content;
        } else {
            contentDiv.textContent = content;
        }
        
        // Add timestamp
        const timestampDiv = document.createElement('div');
        timestampDiv.className = 'message-timestamp';
        timestampDiv.textContent = new Date().toLocaleTimeString();
        
        messageDiv.appendChild(contentDiv);
        messageDiv.appendChild(timestampDiv);
        messagesContainer.appendChild(messageDiv);
        scrollToBottom();
    }

    function showTypingIndicator(show) {
        const typingIndicator = document.getElementById('typing-indicator');
        typingIndicator.classList.toggle('visible', show);
    }

    // Event listeners for sending messages
    sendButton.addEventListener('click', sendMessage);
    
    chatInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // Auto-resize textarea
    chatInput.addEventListener('input', () => {
        chatInput.style.height = 'auto';
        chatInput.style.height = Math.min(chatInput.scrollHeight, 100) + 'px';
    });

    // Initialize
    loadChatHistory();
});

function handleKeyPress(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        sendMessage();
    }
}

function showTypingIndicator(show) {
    const typingIndicator = document.getElementById('typing-indicator');
    typingIndicator.style.display = show ? 'flex' : 'none';
}

function scrollToBottom() {
    const messagesDiv = document.getElementById('chat-messages');
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

document.addEventListener("DOMContentLoaded", function () {
  const chatBody = document.querySelector(".chat-body");
  const scrollBtn = document.getElementById("scroll-bottom-btn");

  if (!chatBody || !scrollBtn) return;

  chatBody.addEventListener("scroll", () => {
    const nearBottom = chatBody.scrollHeight - chatBody.scrollTop - chatBody.clientHeight < 100;
    if (nearBottom) {
      scrollBtn.classList.remove("visible");
    } else {
      scrollBtn.classList.add("visible");
    }
  });

  scrollBtn.addEventListener("click", () => {
    chatBody.scrollTo({
      top: chatBody.scrollHeight,
      behavior: "smooth"
    });
  });
})