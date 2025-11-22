import { LoginResponse, RegisterResponse, UserInfo } from "../utils/auth";
import { API_BASE_URL, API_ENDPOINTS } from "./config";

export async function registerUser(userData) {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.AUTH.REGISTER}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          fullName: userData.fullName,
          email: userData.email,
          password: userData.password,
        }),
      }
    );

    const data = await response.json();

    const result = new RegisterResponse(data);
    return result;
  } catch {
    return new RegisterResponse({
      success: false,
      message: "Network error",
      errors: ["Unable to connect to server. Please try again."],
    });
  }
}

export async function loginUser(userData) {
  const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.AUTH.LOGIN}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "include",
    body: JSON.stringify({
      email: userData.email,
      password: userData.password,
    }),
  });

  if (!response.ok) {
    throw new Error(`Login failed: ${response.status}`);
  }

  const data = await response.json();
  return new LoginResponse(data);
}

export async function logoutUser() {
  const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.AUTH.LOGOUT}`, {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(`Logout failed: ${response.status}`);
  }

  return response.text();
}

export async function checkAuthStatus() {
  const response = await fetch(`${API_BASE_URL}${API_ENDPOINTS.AUTH.STATUS}`, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    return { isAuthenticated: false, user: null };
  }

  const data = await response.json();
  return {
    isAuthenticated: data.isAuthenticated,
    user: data.user ? new UserInfo(data.user) : null,
  };
}
