import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useDispatch } from "react-redux";
import { checkAuthStatus } from "../../api/auth";
import { userAuthSliceAction } from "../../store/userAuth";
import chatHubService from "../../api/chatHub";
import LoadingSpinner from "../../components/LoadingSpinner/LoadingSpinner";
import styles from "./AuthCallback.module.css";

export default function AuthCallback() {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const [searchParams] = useSearchParams();
  const success = searchParams.get("success");

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
            navigate("/login");
          }
        } catch (error) {
          console.error("Error handling auth callback:", error);
          navigate("/login");
        }
      } else {
        // Login failed
        navigate("/login");
      }
    };

    handleAuthCallback();
  }, [success, navigate, dispatch]);

  return (
    <div className={styles.container}>
      <LoadingSpinner />
      <p className={styles.text}>Completing sign in...</p>
    </div>
  );
}
