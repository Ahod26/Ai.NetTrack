import { useRef, useCallback, useEffect, useMemo } from "react";

export function useAutoScroll(messages) {
  const messagesContainerRef = useRef(null);
  const isNearBottomRef = useRef(true);
  const lastMessagesLengthRef = useRef(0);

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

  // Memoize messages length to prevent unnecessary effect runs
  const messagesLength = useMemo(() => messages.length, [messages.length]);

  // Auto-scroll when new messages arrive (only if user is near bottom)
  useEffect(() => {
    // Only scroll if messages length actually changed
    if (messagesLength !== lastMessagesLengthRef.current) {
      lastMessagesLengthRef.current = messagesLength;

      if (isNearBottomRef.current) {
        scrollToBottom();
      }
    }
  }, [messagesLength, scrollToBottom]);
  return {
    messagesContainerRef,
    handleScroll,
    scrollToBottom,
  };
}
