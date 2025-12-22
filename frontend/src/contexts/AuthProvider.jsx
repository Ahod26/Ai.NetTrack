import { useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import { checkAuthStatus } from "../api/auth";
import { userAuthSliceAction } from "../store/userAuth";
import chatHubService from "../api/chatHub";

export default function AuthProvider({ children }) {
  const dispatch = useDispatch();
  const hasInitialized = useRef(false);

  useEffect(() => {
    const initializeAuth = async () => {
      if (hasInitialized.current) {
        return;
      }
      hasInitialized.current = true;

      try {
        const authStatus = await checkAuthStatus();

        if (authStatus.isAuthenticated && authStatus.user) {
          const userPlainObject = {
            fullName: authStatus.user.fullName,
            email: authStatus.user.email,
            roles: authStatus.user.roles,
            isSubscribedToNewsletter: authStatus.user.isSubscribedToNewsletter,
          };

          dispatch(userAuthSliceAction.setUserLoggedIn(userPlainObject));

          try {
            await chatHubService.startConnection();
            console.log("SignalR connected for existing session");
          } catch (error) {
            console.error("Failed to initialize SignalR connection:", error);
          }
        } else {
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
