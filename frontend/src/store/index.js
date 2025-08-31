import { configureStore } from "@reduxjs/toolkit";
import userAuthReducer from "./userAuth";
import chatReducer from "./chat";
import sidebarReducer from "./sidebarSlice";
import messagesReducer from "./messagesSlice";

const store = configureStore({
  reducer: {
    userAuth: userAuthReducer,
    chat: chatReducer,
    sidebar: sidebarReducer,
    messages: messagesReducer,
  },
});

export default store;
