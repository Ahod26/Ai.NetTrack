import { createSlice } from "@reduxjs/toolkit";

const initialState = {
  chats: [],
  isLoading: false,
  hasInitialized: false, // Track if I fetched initial data
  lastUpdated: null,
};

const chatSlice = createSlice({
  name: "chat",
  initialState,
  reducers: {
    setChats(state, action) {
      state.chats = action.payload;
      state.lastUpdated = Date.now();
      state.isLoading = false;
      state.hasInitialized = true;
    },
    setLoading(state, action) {
      state.isLoading = action.payload;
    },
    resetInitialization(state) {
      state.hasInitialized = false;
    },
    addChat(state, action) {
      state.chats.unshift(action.payload);
      state.lastUpdated = Date.now();
    },
    updateChat(state, action) {
      const { chatId, updates } = action.payload;
      const chatIndex = state.chats.findIndex((chat) => chat.id === chatId);
      if (chatIndex !== -1) {
        state.chats[chatIndex] = { ...state.chats[chatIndex], ...updates };
        state.lastUpdated = Date.now();
      }
    },
    removeChat(state, action) {
      const chatId = action.payload;
      state.chats = state.chats.filter((chat) => chat.id !== chatId);
      state.lastUpdated = Date.now();
    },
    updateChatOrder(state, action) {
      const chatId = action.payload;
      const chatIndex = state.chats.findIndex((chat) => chat.id === chatId);
      if (chatIndex !== -1) {
        const chat = state.chats[chatIndex];
        const updatedChat = {
          ...chat,
          time: "Just now",
          lastMessageAt: new Date().toISOString(),
        };
        state.chats.splice(chatIndex, 1);
        state.chats.unshift(updatedChat);
        state.lastUpdated = Date.now();
      }
    },
  },
});

export const chatSliceActions = chatSlice.actions;
export default chatSlice.reducer;
