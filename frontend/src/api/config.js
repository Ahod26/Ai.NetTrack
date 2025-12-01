export const API_BASE_URL = "http://localhost:5170";

export const API_ENDPOINTS = {
  AUTH: {
    REGISTER: "/auth",
    LOGIN: "/auth/login",
    LOGOUT: "/auth/logout",
    STATUS: "/auth/status",
    GOOGLE_LOGIN: "/auth/google-login",
  },
  CHAT: {
    CHAT_HUB: "/chathub",
    CREATE_CHAT: "/chat",
    GET_CHATS: "/chat",
    DELETE_CHAT_BY_ID: "/chat", // Base route, append /{chatId} when calling
    CHANGE_CHAT_TITLE: "/chat", // Base route, append /{chatId}/title when calling
  },
  MESSAGES: {
    STARRED_MESSAGES: "messages/starred",
    TOGGLE_STAR: "messages", // Base route, append /{messageId}/starred
    REPORT: "messages", // Base route, append /{messageId}/report
  },
  NEWS: {
    GET_NEWS_BY_DATE: "/news",
    GET_NEWS_BY_SEARCH: "/news/search",
  },
  PROFILE: {
    UPDATE_EMAIL: "/profile/email",
    UPDATE_FULLNAME: "/profile/username",
    UPDATE_PASSWORD: "/profile/password",
    UPDATE_NEWSLETTER: "/profile/newsletter",
    DELETE_ACCOUNT: "/profile",
  },
};
