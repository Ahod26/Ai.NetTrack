import { useState, useRef } from "react";
import { useSelector, useDispatch } from "react-redux";
import { useNavigate } from "react-router-dom";
import {
  updateEmail,
  updateFullName,
  updatePassword,
  deleteAccount,
} from "../api/user";
import { userAuthSliceAction } from "../store/userAuth";
import { logoutUser } from "../api/auth";
import chatHubService from "../api/chatHub";

export const useAccountSettings = () => {
  
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { user } = useSelector((state) => state.userAuth);
  const errorTimeoutRef = useRef(null);

  // Rate limit error popup
  const [rateLimitError, setRateLimitError] = useState("");

  // Form states
  const [fullName, setFullName] = useState(user?.fullName || "");
  const [email, setEmail] = useState(user?.email || "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  // Password visibility states
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);

  // Loading states
  const [isUpdatingFullName, setIsUpdatingFullName] = useState(false);
  const [isUpdatingEmail, setIsUpdatingEmail] = useState(false);
  const [isUpdatingPassword, setIsUpdatingPassword] = useState(false);

  // Success/Error messages
  const [fullNameMessage, setFullNameMessage] = useState({
    type: "",
    text: "",
  });
  const [emailMessage, setEmailMessage] = useState({ type: "", text: "" });
  const [passwordMessage, setPasswordMessage] = useState({
    type: "",
    text: "",
  });

  // Delete account modal
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const clearMessage = (setter) => {
    setTimeout(() => setter({ type: "", text: "" }), 3000);
  };

  const showRateLimitError = () => {
    // Clear any existing timeout
    if (errorTimeoutRef.current) {
      clearTimeout(errorTimeoutRef.current);
    }

    // Show rate limit error popup for 5 seconds
    setRateLimitError("Too many requests. Please try again later.");
    errorTimeoutRef.current = setTimeout(() => {
      setRateLimitError("");
      errorTimeoutRef.current = null;
    }, 5000);
  };

  const handleUpdateFullName = async (e) => {
    e.preventDefault();
    if (!fullName.trim()) {
      setFullNameMessage({ type: "error", text: "Full name cannot be empty" });
      clearMessage(setFullNameMessage);
      return;
    }

    setIsUpdatingFullName(true);
    try {
      const updatedUser = await updateFullName(fullName);

      // Update Redux store with new user data
      dispatch(
        userAuthSliceAction.setUserLoggedIn({
          fullName: updatedUser.fullName,
          email: updatedUser.email,
          roles: updatedUser.roles,
        })
      );

      setFullNameMessage({
        type: "success",
        text: "Full name updated successfully",
      });
      clearMessage(setFullNameMessage);
    } catch (error) {
      if (error.isRateLimitError) {
        showRateLimitError();
      } else {
        setFullNameMessage({
          type: "error",
          text: error.message || "Failed to update full name",
        });
        clearMessage(setFullNameMessage);
      }
    } finally {
      setIsUpdatingFullName(false);
    }
  };

  const handleUpdateEmail = async (e) => {
    e.preventDefault();
    if (!email.trim() || !email.includes("@")) {
      setEmailMessage({ type: "error", text: "Please enter a valid email" });
      clearMessage(setEmailMessage);
      return;
    }

    setIsUpdatingEmail(true);
    try {
      const updatedUser = await updateEmail(email);

      // Update Redux store with new user data
      dispatch(
        userAuthSliceAction.setUserLoggedIn({
          fullName: updatedUser.fullName,
          email: updatedUser.email,
          roles: updatedUser.roles,
        })
      );

      setEmailMessage({ type: "success", text: "Email updated successfully" });
      clearMessage(setEmailMessage);
    } catch (error) {
      if (error.isRateLimitError) {
        showRateLimitError();
      } else {
        setEmailMessage({
          type: "error",
          text: error.message || "Failed to update email",
        });
        clearMessage(setEmailMessage);
      }
    } finally {
      setIsUpdatingEmail(false);
    }
  };

  const handleUpdatePassword = async (e) => {
    e.preventDefault();

    if (!currentPassword || !newPassword) {
      setPasswordMessage({
        type: "error",
        text: "All password fields are required",
      });
      clearMessage(setPasswordMessage);
      return;
    }

    if (newPassword.length < 6) {
      setPasswordMessage({
        type: "error",
        text: "Password must be at least 6 characters",
      });
      clearMessage(setPasswordMessage);
      return;
    }

    setIsUpdatingPassword(true);
    try {
      await updatePassword(currentPassword, newPassword);
      setPasswordMessage({
        type: "success",
        text: "Password updated successfully",
      });
      setCurrentPassword("");
      setNewPassword("");
      setShowCurrentPassword(false);
      setShowNewPassword(false);
      clearMessage(setPasswordMessage);
    } catch (error) {
      if (error.isRateLimitError) {
        showRateLimitError();
      } else {
        setPasswordMessage({
          type: "error",
          text: error.message || "Failed to update password",
        });
        clearMessage(setPasswordMessage);
      }
    } finally {
      setIsUpdatingPassword(false);
    }
  };

  const handleDeleteAccount = async () => {
    try {
      await deleteAccount();

      // Logout and cleanup
      await logoutUser();
      await chatHubService.stopConnection();
      dispatch(userAuthSliceAction.setUserLoggedOut());

      navigate("/chat/new");
    } catch (error) {
      console.error("Failed to delete account:", error);
      alert(error.message || "Failed to delete account");
    }
  };

  const handleBack = () => {
    navigate("/chat/new");
  };

  return {
    user,
    fullName,
    setFullName,
    email,
    setEmail,
    currentPassword,
    setCurrentPassword,
    newPassword,
    setNewPassword,
    showCurrentPassword,
    setShowCurrentPassword,
    showNewPassword,
    setShowNewPassword,
    isUpdatingFullName,
    isUpdatingEmail,
    isUpdatingPassword,
    fullNameMessage,
    emailMessage,
    passwordMessage,
    showDeleteModal,
    setShowDeleteModal,
    handleUpdateFullName,
    handleUpdateEmail,
    handleUpdatePassword,
    handleDeleteAccount,
    handleBack,
    rateLimitError,
  };
};
