import { configureStore } from "@reduxjs/toolkit";
import userAuthReducer from "./userAuth";
import chatReducer from "./chat";

const store = configureStore({
  reducer: {
    userAuth: userAuthReducer,
    chat: chatReducer,
  },
});

export default store;
