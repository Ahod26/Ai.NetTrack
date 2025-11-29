import { createSlice } from "@reduxjs/toolkit";

const initialState = {
  starredMessages: [], // Array of currently starred message IDs from backend
  reportedMessages: [], // Array of reported message IDs
};

const messagesSlice = createSlice({
  name: "messages",
  initialState,
  reducers: {
    setStarredMessages(state, action) {
      state.starredMessages = [...action.payload];
    },
    setReportedMessages(state, action) {
      state.reportedMessages = [...action.payload];
    },
    toggleMessageStar(state, action) {
      const messageId = action.payload;
      if (state.starredMessages.includes(messageId)) {
        state.starredMessages = state.starredMessages.filter(
          (id) => id !== messageId
        );
      } else {
        state.starredMessages.push(messageId);
      }
    },
    markMessageReported(state, action) {
      const messageId = action.payload;
      if (!state.reportedMessages.includes(messageId)) {
        state.reportedMessages.push(messageId);
      }
    },
  },
});

export const messagesSliceActions = messagesSlice.actions;
export default messagesSlice.reducer;
