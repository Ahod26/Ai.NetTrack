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
      // Generate a simple title for now - you can make this AI-generated later
      const chatCounter = Date.now() % 1000;
      const chatTitle = `Chat ${chatCounter}`;

      // Create new chat via REST API
      const newChat = await createChat(chatTitle);
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
  };
}
