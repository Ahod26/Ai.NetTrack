import { useEffect } from "react";
import { useDispatch } from "react-redux";
import { checkAuthStatus } from "../api/auth";
import { userAuthSliceAction } from "../store/userAuth";
import chatHubService from "../api/chatHub";

export default function AuthProvider({ children }) {
  const dispatch = useDispatch();

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        // Check if user is authenticated via cookie
        const authStatus = await checkAuthStatus();

        if (authStatus.isAuthenticated && authStatus.user) {
          // Convert UserInfo class instance to plain object before dispatching
          const userPlainObject = {
            userName: authStatus.user.userName,
            email: authStatus.user.email,
            roles: authStatus.user.roles,
          };

          // Update Redux store
          dispatch(userAuthSliceAction.setUserLoggedIn(userPlainObject));

          // Initialize SignalR connection for existing session
          try {
            await chatHubService.startConnection();
            console.log("SignalR connected for existing session");
          } catch (error) {
            console.error("Failed to initialize SignalR connection:", error);
          }
        } else {
          // User is not authenticated, ensure they're logged out
          dispatch(userAuthSliceAction.setUserLoggedOut());
        }
      } catch (error) {
        console.error("Auth status check failed:", error);
        dispatch(userAuthSliceAction.setUserLoggedOut());
      }
    };

    initializeAuth();
  }, [dispatch]);

  return children;
}
