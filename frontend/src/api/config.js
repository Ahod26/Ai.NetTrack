// Use HTTP for development to avoid certificate issues
export const API_BASE_URL = "http://localhost:5170";

export const API_ENDPOINTS = {
  AUTH: {
    REGISTER: "/auth",
    LOGIN: "/auth/login",
    LOGOUT: "/auth/logout",
    STATUS: "/auth/status",
  },
};
