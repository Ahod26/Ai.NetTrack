import { useContext } from "react";
import { createContext } from "react";

export const ChatContext = createContext();

export function useChatContext() {
  const context = useContext(ChatContext);
  if (!context) {
    throw new Error("useChatContext must be used within a ChatProvider");
  }
  return context;
}
