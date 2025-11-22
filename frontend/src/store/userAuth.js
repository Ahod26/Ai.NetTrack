import { createSlice } from "@reduxjs/toolkit";

const initialState = {
  isUserLoggedIn: false,
  user: null,
};

const userAuthSlice = createSlice({
  name: "userAuthSlice",
  initialState,
  reducers: {
    setUserLoggedIn(state, action) {
      state.isUserLoggedIn = true;
      // Convert UserInfo class instance to plain object for Redux serialization
      const userPayload = action.payload;

      // Handle both plain objects and UserInfo class instances
      if (userPayload && typeof userPayload === "object") {
        state.user = {
          fullName: userPayload.fullName,
          email: userPayload.email,
          roles: Array.isArray(userPayload.roles) ? [...userPayload.roles] : [],
        };
      } else {
        state.user = null;
      }

      console.log("User logged in:", state.user);
    },
    setUserLoggedOut(state) {
      state.isUserLoggedIn = false;
      state.user = null;
      console.log("User logged out");
    },
  },
});

export const userAuthSliceAction = userAuthSlice.actions;
export default userAuthSlice.reducer;
