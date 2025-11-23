import { API_BASE_URL, API_ENDPOINTS } from "./config";

// Get user's timezone offset in minutes
const getTimezoneOffset = () => {
  return new Date().getTimezoneOffset();
};

// Create a new chat
export async function createChat(
  firstMessage,
  timezoneOffset = null,
  relatedNewsSource = null
) {
  try {
    const actualTimezoneOffset = timezoneOffset ?? getTimezoneOffset();
    let url = `${API_BASE_URL}${API_ENDPOINTS.CHAT.CREATE_CHAT}?timezoneOffset=${actualTimezoneOffset}`;

    if (relatedNewsSource) {
      url += `&relatedNewsSource=${encodeURIComponent(relatedNewsSource)}`;
    }

    const response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      credentials: "include",
      body: JSON.stringify({
        firstMessage: firstMessage,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json();
      const errorMessage = errorData.error;
      throw new Error(errorMessage);
    }

    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error creating chat:", error);
    throw error;
  }
}

// Get all user chats
export async function getUserChatsMetaData() {
  try {
    const timezoneOffset = getTimezoneOffset();
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.GET_CHATS}?timezoneOffset=${timezoneOffset}`,
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

// Delete specific chat by ID
export async function deleteChatById(chatId) {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.DELETE_CHAT_BY_ID}/${chatId}`,
      {
        method: "DELETE",
        credentials: "include",
      }
    );

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error("Chat not found");
      }
      throw new Error(`Failed to delete chat: ${response.status}`);
    }
    return { success: true };
  } catch (error) {
    console.error("Error deleting chat:", error);
    throw error;
  }
}

//Change chat title by chat ID
export async function changeChatTitle(chatId, newTitle) {
  try {
    const response = await fetch(
      `${API_BASE_URL}${API_ENDPOINTS.CHAT.CHANGE_CHAT_TITLE}/${chatId}/title`,
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
        },
        credentials: "include",
        body: JSON.stringify(newTitle),
      }
    );

    if (!response.ok) {
      // Try to parse error response
      let errorMessage = `Failed to change chat title: ${response.status}`;

      try {
        const errorData = await response.json();
        if (errorData.message) {
          errorMessage = errorData.message;
        } else if (errorData.errors) {
          // Handle validation errors
          if (typeof errorData.errors === "string") {
            errorMessage = errorData.errors;
          } else if (
            errorData.errors.title &&
            Array.isArray(errorData.errors.title)
          ) {
            errorMessage = errorData.errors.title[0];
          } else if (errorData.errors.$ && Array.isArray(errorData.errors.$)) {
            // Handle model-level validation errors
            errorMessage = errorData.errors.$[0];
          }
        }
      } catch {
        // If we can't parse the error response, use status-based message
        if (response.status === 404) {
          errorMessage = "Chat not found";
        } else if (response.status === 400) {
          errorMessage = "Invalid title format";
        }
      }

      const error = new Error(errorMessage);
      error.status = response.status;
      throw error;
    }

    return { success: true };
  } catch (error) {
    console.error("Error changing chat title:", error);
    throw error;
  }
}
