import { createSlice } from "@reduxjs/toolkit";

//refresh count trigger the side bar to update the chats
const initialState = {
  refreshTrigger: 0,
};

const chatSlice = createSlice({
  name: "chat",
  initialState,
  reducers: {
    triggerChatRefresh(state) {
      state.refreshTrigger += 1;
    },
  },
});

export const chatSliceActions = chatSlice.actions;
export default chatSlice.reducer;
