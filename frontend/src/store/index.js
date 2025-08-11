import {configureStore} from "@reduxjs/toolkit"
import userAuthReducer from "./userAuth"

const store = configureStore({
  reducer: {userAuth: userAuthReducer}
})

export default store
