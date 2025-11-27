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
  const pendingChangesRef = useRef(pendingChanges);

  // Keep ref updated with latest pendingChanges
  useEffect(() => {
    pendingChangesRef.current = pendingChanges;
  }, [pendingChanges]);

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
      // Send star changes to backend based on final state
      const results = await Promise.all(
        pendingIds.map(async (messageId) => {
          const desiredStarredState = changesToSync[messageId];

          // Call the API with the desired state
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
    } catch {
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
  }, [location.pathname, pendingChanges, syncPendingStarChanges]);

  // Sync on component unmount (using ref to avoid re-running cleanup on every render)
  useEffect(() => {
    return () => {
      // This cleanup only runs when component actually unmounts
      const currentPendingChanges = pendingChangesRef.current;
      const pendingIds = Object.keys(currentPendingChanges);
      if (pendingIds.length > 0) {
        // Call sync directly instead of using the callback
        // to avoid stale closure issues
        const syncChanges = async () => {
          if (syncInProgressRef.current) return;

          syncInProgressRef.current = true;
          try {
            const results = await Promise.all(
              pendingIds.map(async (messageId) => {
                const desiredStarredState = currentPendingChanges[messageId];
                const response = await toggleMessageStar(messageId);

                if (response.isStarred !== desiredStarredState) {
                  const secondResponse = await toggleMessageStar(messageId);
                  return { messageId, isStarred: secondResponse.isStarred };
                }

                return { messageId, isStarred: response.isStarred };
              })
            );

            results.forEach(({ messageId, isStarred }) => {
              dispatch(
                messagesSliceActions.updateStarredMessage({
                  messageId,
                  isStarred,
                })
              );
            });

            dispatch(messagesSliceActions.clearPendingStarChanges());
          } catch (error) {
            console.error("[useStarSync] Error in unmount sync:", error);
          } finally {
            syncInProgressRef.current = false;
          }
        };

        syncChanges();
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty deps means cleanup only runs on actual unmount

  return { syncPendingStarChanges };
};
