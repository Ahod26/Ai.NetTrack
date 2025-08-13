import { configureStore } from "@reduxjs/toolkit";
import userAuthReducer from "./userAuth";
import chatReducer from "./chat";
import sidebarReducer from "./sidebarSlice";

const store = configureStore({
  reducer: {
    userAuth: userAuthReducer,
    chat: chatReducer,
    sidebar: sidebarReducer,
  },
});

export default store;
