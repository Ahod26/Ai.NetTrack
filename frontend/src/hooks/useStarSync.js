import { useEffect, useRef, useCallback } from "react";
import { useSelector, useDispatch } from "react-redux";
import { useLocation } from "react-router-dom";
import { messagesSliceActions } from "../store/messagesSlice";
import { toggleMessageStar } from "../api/messages";

export const useStarSync = () => {
  const dispatch = useDispatch();
  const location = useLocation();
  const pendingChanges = useSelector(
    (state) => state.messages.pendingStarChanges
  );
  const prevLocationRef = useRef(location.pathname);
  const syncInProgressRef = useRef(false);

  const syncPendingStarChanges = useCallback(async () => {
    const pendingIds = Object.keys(pendingChanges);

    if (syncInProgressRef.current) {
      return;
    }

    if (pendingIds.length === 0) {
      return;
    }

    syncInProgressRef.current = true;
    const changesToSync = { ...pendingChanges };

    try {
      // Send star changes to backend based on desired final state
      const results = await Promise.all(
        pendingIds.map(async (messageId) => {
          const desiredStarredState = changesToSync[messageId];

          // Call the API with the desired state (might need to call multiple times to reach desired state)
          const response = await toggleMessageStar(messageId);

          // Check if we got the desired state, if not, call again
          if (response.isStarred !== desiredStarredState) {
            const secondResponse = await toggleMessageStar(messageId);
            return { messageId, isStarred: secondResponse.isStarred };
          }

          return { messageId, isStarred: response.isStarred };
        })
      );

      // Update the starred messages list with actual backend state
      results.forEach(({ messageId, isStarred }) => {
        dispatch(
          messagesSliceActions.updateStarredMessage({ messageId, isStarred })
        );
      });

      // Clear pending changes on success
      dispatch(messagesSliceActions.clearPendingStarChanges());
    } catch (error) {
      console.error("Failed to sync star changes:", error);
      // Optionally revert optimistic changes
      dispatch(messagesSliceActions.revertOptimisticChanges(pendingIds));
    } finally {
      syncInProgressRef.current = false;
    }
  }, [pendingChanges, dispatch]);

  useEffect(() => {
    // Detect route change
    if (prevLocationRef.current !== location.pathname) {
      // Sync pending changes when leaving the route
      syncPendingStarChanges();
      prevLocationRef.current = location.pathname;
    }
  }, [location.pathname, syncPendingStarChanges]);

  // Also sync on component unmount (when component is destroyed)
  useEffect(() => {
    return () => {
      const pendingIds = Object.keys(pendingChanges);
      if (pendingIds.length > 0) {
        syncPendingStarChanges();
      }
    };
  }, [pendingChanges, syncPendingStarChanges]);

  return { syncPendingStarChanges };
};
