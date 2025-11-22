import { useState, useEffect, useCallback, useRef, useMemo } from "react";
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
  const errorTimeoutRef = useRef(null);

  // Open sidebar when component mounts
  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (errorTimeoutRef.current) {
        clearTimeout(errorTimeoutRef.current);
        errorTimeoutRef.current = null;
      }
    };
  }, []);

  const handleSendMessage = async (messageText) => {
    if (!isUserLoggedIn || isCreatingChat) {
      return;
    }

    setIsCreatingChat(true);

    try {
      const newChat = await createChat(messageText);

      const chatTitle =
        newChat.title ||
        (messageText.length > 50
          ? messageText.slice(0, 50) + "..."
          : messageText);

      // Add chat optimistically to Redux store
      dispatch(
        chatSliceActions.addChat({
          id: newChat.id,
          title: chatTitle,
          time: "Just now",
          lastMessageAt: new Date().toISOString(),
        })
      );

      // Navigate to the new chat immediately to show the UI, passing the initial message
      navigate(`/chat/${newChat.id}`, {
        replace: true,
        state: { initialMessage: messageText },
      });

      // Join the chat via SignalR
      await chatHubService.joinChat(newChat.id);

      // Send the initial message (this will be shown in the chat UI)
      await chatHubService.sendMessage(newChat.id, messageText);
    } catch (error) {
      console.error("Error creating chat or sending message:", error);
      setIsCreatingChat(false);

      // Clear any existing timeout
      if (errorTimeoutRef.current) {
        clearTimeout(errorTimeoutRef.current);
      }

      // Show error message for 5 seconds
      const msg = error?.message || "Failed to create chat";
      setErrorMessage(msg);
      errorTimeoutRef.current = setTimeout(() => {
        setErrorMessage("");
        errorTimeoutRef.current = null;
      }, 5000);
    }
  };

  // Memoize personalized greeting to avoid recalculation
  const personalizedGreeting = useMemo(() => {
    if (!isUserLoggedIn || !user?.fullName) {
      return "How can I help you today?";
    }

    const firstName = user.fullName.split(" ")[0];
    return firstName
      ? `How can I help you today, ${firstName}?`
      : "How can I help you today?";
  }, [isUserLoggedIn, user?.fullName]);

  const getPersonalizedGreeting = useCallback(() => {
    return personalizedGreeting;
  }, [personalizedGreeting]);

  return {
    isUserLoggedIn,
    isCreatingChat,
    handleSendMessage,
    getPersonalizedGreeting,
    errorMessage,
  };
}
