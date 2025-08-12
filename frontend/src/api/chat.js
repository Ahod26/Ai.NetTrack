import { API_BASE_URL, API_ENDPOINTS } from "./config";



// Create a new chat
export async function createChat(title) {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.CREATECHAT}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify({
          title: title,
        }),
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to create chat: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error creating chat:", error);
    throw error;
  }
}

// Get all user chats
export async function getUserChats() {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.GETCHATS}`,
      {
        method: "GET",
        credentials: "include",
      }
    );

    if (!response.ok) {
      throw new Error(`Failed to get chats: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error getting user chats:", error);
    throw error;
  }
}

// Get specific chat by ID
export async function getChatById(chatId) {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.GETCHATBYID}/${chatId}`,
      {
        method: "GET",
        credentials: "include",
      }
    );

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error("Chat not found");
      }
      throw new Error(`Failed to get chat: ${response.status}`);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error getting chat by ID:", error);
    throw error;
  }
}
