export const API_BASE_URL = "http://localhost:5170";

export const API_ENDPOINTS = {
  AUTH: {
    REGISTER: "/auth",
    LOGIN: "/auth/login",
    LOGOUT: "/auth/logout",
    STATUS: "/auth/status",
  },
  CHAT: {
    CHATHUB: "/chathub",
    CREATECHAT: "/chat",
    GETCHATS: "/chat",
    GETCHATBYID: "/chat", // Base route, append /{chatId} when calling
    DELETECHATBYID: "/chat" // Base route, append /{chatId} when calling
  },
};
