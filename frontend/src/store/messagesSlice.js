import { createSlice } from "@reduxjs/toolkit";

const initialState = {
  // Track pending star changes with desired final states
  pendingStarChanges: {}, // Object: { messageId: desiredStarredState }
  starredMessages: [], // Array of currently starred message IDs
  isInitialized: false,
};

const messagesSlice = createSlice({
  name: "messages",
  initialState,
  reducers: {
    initializeStarredMessages(state, action) {
      // Initialize with current starred messages from backend
      state.starredMessages = [...action.payload];
      state.isInitialized = true;
    },
    toggleMessageStarOptimistic(state, action) {
      const messageId = action.payload;

      // Toggle the star status optimistically
      const wasStarred = state.starredMessages.includes(messageId);
      const willBeStarred = !wasStarred;

      if (wasStarred) {
        state.starredMessages = state.starredMessages.filter(
          (id) => id !== messageId
        );
      } else {
        state.starredMessages.push(messageId);
      }

      // Track the desired final state (not just that it changed)
      state.pendingStarChanges[messageId] = willBeStarred;
    },
    clearPendingStarChanges(state) {
      // Called after successful sync
      state.pendingStarChanges = {};
    },
    revertOptimisticChanges(state, action) {
      // Revert changes if sync failed
      const failedMessageIds =
        action.payload || Object.keys(state.pendingStarChanges);

      failedMessageIds.forEach((messageId) => {
        // Revert the optimistic change
        if (state.starredMessages.includes(messageId)) {
          state.starredMessages = state.starredMessages.filter(
            (id) => id !== messageId
          );
        } else {
          state.starredMessages.push(messageId);
        }
      });

      state.pendingStarChanges = {};
    },
    updateStarredMessage(state, action) {
      // Update individual message star status (from backend response)
      const { messageId, isStarred } = action.payload;

      if (isStarred) {
        if (!state.starredMessages.includes(messageId)) {
          state.starredMessages.push(messageId);
        }
      } else {
        state.starredMessages = state.starredMessages.filter(
          (id) => id !== messageId
        );
      }

      // Remove from pending if it was there
      delete state.pendingStarChanges[messageId];
    },
  },
});

export const messagesSliceActions = messagesSlice.actions;
export default messagesSlice.reducer;
