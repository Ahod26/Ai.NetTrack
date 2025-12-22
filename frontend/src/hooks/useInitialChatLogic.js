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

  useEffect(() => {
    dispatch(sidebarActions.openSidebar());
  }, [dispatch]);

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

      dispatch(
        chatSliceActions.addChat({
          id: newChat.id,
          title: chatTitle,
          time: "Just now",
          lastMessageAt: new Date().toISOString(),
        })
      );

      navigate(`/chat/${newChat.id}`, {
        replace: true,
        state: { initialMessage: messageText },
      });

      await chatHubService.joinChat(newChat.id);

      await chatHubService.sendMessage(newChat.id, messageText);
    } catch (error) {
      console.error("Error creating chat or sending message:", error);
      setIsCreatingChat(false);

      if (errorTimeoutRef.current) {
        clearTimeout(errorTimeoutRef.current);
      }

      const msg = error?.message || "Failed to create chat";
      setErrorMessage(msg);
      errorTimeoutRef.current = setTimeout(() => {
        setErrorMessage("");
        errorTimeoutRef.current = null;
      }, 5000);
    }
  };

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
