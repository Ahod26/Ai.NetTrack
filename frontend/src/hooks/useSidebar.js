import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useSelector, useDispatch } from "react-redux";
import { userAuthSliceAction } from "../store/userAuth";
import { chatSliceActions } from "../store/chat";
import { logoutUser } from "../api/auth";
import {
  getUserChatsMetaData,
  deleteChatById,
  changeChatTitle,
} from "../api/chat";

export const useSidebar = () => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { chatId: currentChatId } = useParams();
  const { isUserLoggedIn } = useSelector((state) => state.userAuth);
  const { chats, isLoading: isLoadingChats } = useSelector(
    (state) => state.chat
  );

  const [openDropdown, setOpenDropdown] = useState(null);
  const [deleteModal, setDeleteModal] = useState({
    isOpen: false,
    chatId: null,
  });
  const [renameModal, setRenameModal] = useState({
    isOpen: false,
    chatId: null,
    chatTitle: "",
  });

  // Format chat time utility
  const formatChatTime = (dateString) => {
    if (!dateString) return "Unknown";

    const chatDate = new Date(dateString);
    const now = new Date();

    // Validate the date
    if (isNaN(chatDate.getTime())) return "Unknown";

    const diffInMs = now.getTime() - chatDate.getTime();
    const diffInMinutes = Math.floor(diffInMs / (1000 * 60));
    const diffInHours = Math.floor(diffInMs / (1000 * 60 * 60));
    const diffInDays = Math.floor(diffInMs / (1000 * 60 * 60 * 24));
    const diffInWeeks = Math.floor(diffInDays / 7);
    const diffInMonths = Math.floor(diffInDays / 30);

    // Handle edge cases
    if (diffInMs < 0) {
      return "Just now"; // Future dates
    } else if (diffInMinutes < 5) {
      return "Just now";
    } else if (diffInMinutes < 10) {
      return "5 min ago";
    } else if (diffInMinutes < 15) {
      return "10 min ago";
    } else if (diffInMinutes < 30) {
      return "15 min ago";
    } else if (diffInMinutes < 60) {
      return "30 min ago";
    } else if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours > 1 ? "s" : ""} ago`;
    } else if (diffInDays === 1) {
      return "Yesterday";
    } else if (diffInDays < 7) {
      return `${diffInDays} day${diffInDays > 1 ? "s" : ""} ago`;
    } else if (diffInWeeks === 1) {
      return "1 week ago";
    } else if (diffInWeeks < 4) {
      return `${diffInWeeks} weeks ago`;
    } else if (diffInMonths === 1) {
      return "1 month ago";
    } else {
      return "1 month ago"; // Max display is 1 month
    }
  };

  // Fetch user chats only once when user logs in
  useEffect(() => {
    const fetchUserChats = async () => {
      try {
        dispatch(chatSliceActions.setLoading(true));
        const userChats = await getUserChatsMetaData();

        const formattedChats = userChats.map((chat) => ({
          id: chat.Id || chat.id,
          title: chat.Title || chat.title,
          time: formatChatTime(chat.LastMessageAt || chat.lastMessageAt),
          lastMessageAt: chat.LastMessageAt || chat.lastMessageAt,
        }));

        dispatch(chatSliceActions.setChats(formattedChats));
      } catch (error) {
        console.error("Error fetching chats:", error);
        dispatch(chatSliceActions.setLoading(false));
      }
    };

    if (isUserLoggedIn && chats.length === 0) {
      // Only fetch if we don't have chats already
      fetchUserChats();
    } else if (!isUserLoggedIn) {
      // Clear chats when user logs out
      dispatch(chatSliceActions.setChats([]));
    }
  }, [isUserLoggedIn, dispatch, chats.length]);

  // Navigation handlers
  const handleNewChat = () => {
    navigate("/chat/new");
  };

  const handleLogout = async () => {
    try {
      await logoutUser();
      dispatch(userAuthSliceAction.setUserLoggedOut());
      navigate("/chat/new");
    } catch (error) {
      console.error("Logout error:", error);
    }
  };

  // Chat action handlers
  const handleDeleteChat = (chatId) => {
    setDeleteModal({ isOpen: true, chatId });
    setOpenDropdown(null);
  };

  const handleRenameChat = (chatId) => {
    const chat = chats.find((c) => c.id === chatId);
    setRenameModal({
      isOpen: true,
      chatId: chatId,
      chatTitle: chat ? chat.title : "",
    });
    setOpenDropdown(null);
  };

  const confirmRenameChat = async (newTitle) => {
    const { chatId } = renameModal;

    if (!newTitle.trim() || newTitle.trim() === renameModal.chatTitle.trim()) {
      setRenameModal({ isOpen: false, chatId: null, chatTitle: "" });
      return;
    }

    try {
      await changeChatTitle(chatId, newTitle.trim());

      // Update chat in Redux store
      dispatch(
        chatSliceActions.updateChat({
          chatId,
          updates: { title: newTitle.trim() },
        })
      );

      setRenameModal({ isOpen: false, chatId: null, chatTitle: "" });
    } catch (error) {
      console.error("Error renaming chat:", error);
      throw error;
    }
  };

  const cancelRenameChat = () => {
    setRenameModal({ isOpen: false, chatId: null, chatTitle: "" });
  };

  const confirmDeleteChat = async () => {
    const { chatId } = deleteModal;

    try {
      await deleteChatById(chatId);

      if (currentChatId === chatId.toString()) {
        navigate("/chat/new");
      }

      // Remove chat from Redux store
      dispatch(chatSliceActions.removeChat(chatId));
      setDeleteModal({ isOpen: false, chatId: null });
    } catch (error) {
      console.error("Error deleting chat:", error);
      alert("Failed to delete chat. Please try again.");
    }
  };

  const cancelDeleteChat = () => {
    setDeleteModal({ isOpen: false, chatId: null });
  };

  const toggleDropdown = (chatId, event) => {
    event.preventDefault();
    event.stopPropagation();
    setOpenDropdown(openDropdown === chatId ? null : chatId);
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = () => {
      setOpenDropdown(null);
    };

    document.addEventListener("click", handleClickOutside);
    return () => {
      document.removeEventListener("click", handleClickOutside);
    };
  }, []);

  return {
    chats,
    isLoadingChats,
    openDropdown,
    deleteModal,
    renameModal,
    currentChatId,
    isUserLoggedIn,

    handleNewChat,
    handleLogout,
    handleDeleteChat,
    handleRenameChat,
    confirmRenameChat,
    cancelRenameChat,
    confirmDeleteChat,
    cancelDeleteChat,
    toggleDropdown,
  };
};
