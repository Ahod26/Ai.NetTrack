import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { createChat } from "../api/chat";
import chatHubService from "../api/chatHub";
import { chatSliceActions } from "../store/chat";
import { sidebarActions } from "../store/sidebarSlice";

export function useInitialChatLogic() {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { isUserLoggedIn, user } = useSelector((state) => state.userAuth);
  const [isCreatingChat, setIsCreatingChat] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  // Open sidebar when component mounts
  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

  const handleSendMessage = async (messageText) => {
    if (!isUserLoggedIn || isCreatingChat) {
      return;
    }

    setIsCreatingChat(true);

    try {
      // Create new chat via REST API
      const newChat = await createChat(messageText);
      console.log("Created new chat:", newChat);

      // Navigate to the new chat immediately to show the UI, passing the initial message
      navigate(`/chat/${newChat.id}`, {
        replace: true,
        state: { initialMessage: messageText },
      });

      // Join the chat via SignalR
      await chatHubService.joinChat(newChat.id);

      // Send the initial message (this will be shown in the chat UI)
      await chatHubService.sendMessage(newChat.id, messageText);

      // Trigger chat list refresh in sidebar
      dispatch(chatSliceActions.triggerChatRefresh());
    } catch (error) {
      console.error("Error creating chat or sending message:", error);
      setIsCreatingChat(false);
      // Show error message in top right for 3 seconds
      let msg = error?.message || "Failed to create chat";
      setErrorMessage(msg);
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  // Extract first name from full name
  const getFirstName = () => {
    if (!isUserLoggedIn || !user?.userName) {
      return "";
    }

    const firstName = user.userName.split(" ")[0];
    return firstName;
  };

  const getPersonalizedGreeting = () => {
    const firstName = getFirstName();
    if (firstName) {
      return `How can I help you today, ${firstName}?`;
    }
    return "How can I help you today?";
  };

  return {
    isUserLoggedIn,
    isCreatingChat,
    handleSendMessage,
    getPersonalizedGreeting,
    errorMessage,
  };
}
