import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useDispatch } from "react-redux";
import { checkAuthStatus } from "../../api/auth";
import { userAuthSliceAction } from "../../store/userAuth";
import chatHubService from "../../api/chatHub";
import LoadingSpinner from "../../components/LoadingSpinner/LoadingSpinner";
import ErrorPopup from "../../components/ErrorPopup";
import styles from "./AuthCallback.module.css";

export default function AuthCallback() {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const [searchParams] = useSearchParams();
  const success = searchParams.get("success");
  const errorMessage = searchParams.get("error");
  const [showError, setShowError] = useState(false);

  useEffect(() => {
    const handleAuthCallback = async () => {
      if (success === "true") {
        try {
          // Check auth status to get user info
          const authStatus = await checkAuthStatus();

          if (authStatus.isAuthenticated && authStatus.user) {
            // Convert UserInfo to plain object
            const userPlainObject = {
              fullName: authStatus.user.fullName,
              email: authStatus.user.email,
              roles: authStatus.user.roles,
              isSubscribedToNewsletter:
                authStatus.user.isSubscribedToNewsletter,
            };

            dispatch(userAuthSliceAction.setUserLoggedIn(userPlainObject));

            // Initialize SignalR connection
            try {
              await chatHubService.startConnection();
              console.log("SignalR connected after Google login");
            } catch (signalRError) {
              console.error(
                "Failed to connect SignalR after login:",
                signalRError
              );
            }

            // Redirect to chat
            navigate("/chat/new");
          } else {
            // Auth failed
            navigate("/chat/new", {
              state: {
                error: "Failed to log in with Google. Please try again.",
              },
            });
          }
        } catch (error) {
          console.error("Error handling auth callback:", error);
          navigate("/chat/new", {
            state: { error: "Failed to log in with Google. Please try again." },
          });
        }
      } else {
        // Login failed - redirect immediately with error state
        navigate("/chat/new", {
          state: {
            error:
              errorMessage || "Failed to log in with Google. Please try again.",
          },
        });
      }
    };

    handleAuthCallback();
  }, [success, navigate, dispatch, errorMessage]);

  return (
    <div className={styles.container}>
      <LoadingSpinner />
      <p className={styles.text}>Completing sign in...</p>
    </div>
  );
}
