import { API_BASE_URL, API_ENDPOINTS } from "./config";

export async function updateEmail(email) {
  const response = await fetch(
    `${API_BASE_URL}${API_ENDPOINTS.PROFILE.UPDATE_EMAIL}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      credentials: "include",
      body: JSON.stringify({ email }),
    }
  );

  if (!response.ok) {
    if (response.status === 429) {
      const error = new Error("Too many requests. Please try again later.");
      error.isRateLimitError = true;
      throw error;
    }
    const error = await response.json();
    const errorMsg = Array.isArray(error)
      ? error[0]?.description || "Failed to update email"
      : error.message || "Failed to update email";
    throw new Error(errorMsg);
  }

  // Backend returns updated UserInfoDTO
  return response.json();
}

export async function updateFullName(fullName) {
  const response = await fetch(
    `${API_BASE_URL}${API_ENDPOINTS.PROFILE.UPDATE_FULLNAME}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      credentials: "include",
      body: JSON.stringify({ fullName }),
    }
  );

  if (!response.ok) {
    if (response.status === 429) {
      const error = new Error("Too many requests. Please try again later.");
      error.isRateLimitError = true;
      throw error;
    }
    const error = await response.json();
    const errorMsg = Array.isArray(error)
      ? error[0]?.description || "Failed to update full name"
      : error.message || "Failed to update full name";
    throw new Error(errorMsg);
  }

  // Backend returns updated UserInfoDTO
  return response.json();
}

export async function updatePassword(currentPassword, newPassword) {
  const response = await fetch(
    `${API_BASE_URL}${API_ENDPOINTS.PROFILE.UPDATE_PASSWORD}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      credentials: "include",
      body: JSON.stringify({ currentPassword, newPassword }),
    }
  );

  if (!response.ok) {
    if (response.status === 429) {
      const error = new Error("Too many requests. Please try again later.");
      error.isRateLimitError = true;
      throw error;
    }
    const error = await response.json();
    const errorMsg = Array.isArray(error)
      ? error[0]?.description || "Failed to update password"
      : error.message || "Failed to update password";
    throw new Error(errorMsg);
  }

  return response.ok ? {} : response.json();
}

export async function toggleNewsletterSubscription() {
  const response = await fetch(
    `${API_BASE_URL}${API_ENDPOINTS.PROFILE.UPDATE_NEWSLETTER}`,
    {
      method: "PUT",
      credentials: "include",
    }
  );

  if (!response.ok) {
    if (response.status === 429) {
      const error = new Error("Too many requests. Please try again later.");
      error.isRateLimitError = true;
      throw error;
    }
    const error = await response.json();
    const errorMsg = Array.isArray(error)
      ? error[0]?.description || "Failed to update newsletter preference"
      : error.message || "Failed to update newsletter preference";
    throw new Error(errorMsg);
  }

  // Backend returns updated UserInfoDTO
  return response.json();
}

export async function deleteAccount() {
  const response = await fetch(
    `${API_BASE_URL}${API_ENDPOINTS.PROFILE.DELETE_ACCOUNT}`,
    {
      method: "DELETE",
      credentials: "include",
    }
  );

  if (!response.ok) {
    const error = await response.json();
    const errorMsg = Array.isArray(error)
      ? error[0]?.description || "Failed to delete account"
      : error.message || "Failed to delete account";
    throw new Error(errorMsg);
  }

  return response.ok ? {} : response.json();
}
