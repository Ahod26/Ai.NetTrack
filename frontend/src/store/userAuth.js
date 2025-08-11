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
      state.user = action.payload; // Expecting user object with userName, email, etc.
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
