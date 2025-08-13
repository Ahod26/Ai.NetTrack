import { useRef, useCallback, useEffect } from "react";

export function useAutoScroll(messages) {
  const messagesContainerRef = useRef(null);
  const isNearBottomRef = useRef(true);

  const scrollToBottom = useCallback(() => {
    if (messagesContainerRef.current) {
      messagesContainerRef.current.scrollTop =
        messagesContainerRef.current.scrollHeight;
    }
  }, []);

  const checkIfNearBottom = useCallback(() => {
    if (messagesContainerRef.current) {
      const { scrollTop, scrollHeight, clientHeight } =
        messagesContainerRef.current;
      const threshold = 100; // pixels from bottom
      isNearBottomRef.current =
        scrollTop + clientHeight >= scrollHeight - threshold;
    }
  }, []);

  const handleScroll = useCallback(() => {
    checkIfNearBottom();
  }, [checkIfNearBottom]);

  // Auto-scroll when new messages arrive (only if user is near bottom)
  useEffect(() => {
    if (isNearBottomRef.current) {
      scrollToBottom();
    }
  }, [messages, scrollToBottom]);

  return {
    messagesContainerRef,
    handleScroll,
    scrollToBottom,
  };
}
