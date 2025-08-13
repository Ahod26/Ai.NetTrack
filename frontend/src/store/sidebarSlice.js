import { createSlice } from "@reduxjs/toolkit";

const initialState = {
  isOpen: true, // Default to open
};

const sidebarSlice = createSlice({
  name: "sidebar",
  initialState,
  reducers: {
    openSidebar(state) {
      state.isOpen = true;
    },
    closeSidebar(state) {
      state.isOpen = false;
    },
    toggleSidebar(state) {
      state.isOpen = !state.isOpen;
    },
  },
});

export const sidebarActions = sidebarSlice.actions;
export default sidebarSlice.reducer;
