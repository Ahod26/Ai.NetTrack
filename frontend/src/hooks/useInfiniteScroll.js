import { useState, useEffect, useCallback } from "react";

export const useInfiniteScroll = (hasMore, loadMore, threshold = 100) => {
  const [isFetching, setIsFetching] = useState(false);

  const handleScroll = useCallback(() => {
    if (isFetching || !hasMore) return;

    const { scrollTop, scrollHeight, clientHeight } = document.documentElement;
    const isNearBottom = scrollHeight - scrollTop - clientHeight < threshold;

    if (isNearBottom) {
      setIsFetching(true);
    }
  }, [isFetching, hasMore, threshold]);

  useEffect(() => {
    if (!isFetching) return;

    const fetchMoreData = async () => {
      await loadMore();
      setIsFetching(false);
    };

    fetchMoreData();
  }, [isFetching, loadMore]);

  useEffect(() => {
    if (!hasMore) return;

    window.addEventListener("scroll", handleScroll);
    return () => window.removeEventListener("scroll", handleScroll);
  }, [handleScroll, hasMore]);

  // Reset fetching state when hasMore changes
  useEffect(() => {
    if (!hasMore) {
      setIsFetching(false);
    }
  }, [hasMore]);

  return {
    isFetching,
    setIsFetching,
  };
};
