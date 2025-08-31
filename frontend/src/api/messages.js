import { API_BASE_URL, API_ENDPOINTS } from "./config";

export async function getAllStarredMessages() {
  try {
    const response = await fetch(
      `${API_BASE_URL}/${API_ENDPOINTS.MESSAGES.STARREDMESSAGES}`,
      {
        method: "GET",
        credentials: "include",
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to get starred messages: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error getting starred messages:", error);
    throw error;
  }
}

export async function toggleMessageStar(messageId) {
  try {
    const response = await fetch(
      `${API_BASE_URL}/${API_ENDPOINTS.MESSAGES.TOGGLESTAR}/${messageId}/starred`,
      {
        method: "PATCH",
        credentials: "include",
      }
    );

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error("Message not found");
      }
      throw new Error(`Failed to toggle star: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error toggling message star:", error);
    throw error;
  }
}
