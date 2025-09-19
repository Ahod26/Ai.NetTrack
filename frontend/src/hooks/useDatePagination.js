import { useState, useEffect, useMemo, useCallback } from "react";

export const useDatePagination = (selectedDates, batchSize = 5) => {
  const [currentPage, setCurrentPage] = useState(0);
  const [hasMore, setHasMore] = useState(false);

  // Create date batches from selected dates
  const dateBatches = useMemo(() => {
    if (!selectedDates || selectedDates.length === 0) return [];

    // Sort dates in descending order (newest first)
    const sortedDates = [...selectedDates].sort(
      (a, b) => new Date(b) - new Date(a)
    );

    const batches = [];
    for (let i = 0; i < sortedDates.length; i += batchSize) {
      batches.push(sortedDates.slice(i, i + batchSize));
    }
    return batches;
  }, [selectedDates, batchSize]);

  // Reset pagination when selected dates change
  useEffect(() => {
    setCurrentPage(0);
    setHasMore(dateBatches.length > 1);
  }, [dateBatches]);

  // Get current batch of dates to load
  const getCurrentDateBatch = useCallback(() => {
    return dateBatches[currentPage] || [];
  }, [dateBatches, currentPage]);

  // Get all date batches up to current page (for accumulated loading)
  const getAllLoadedDateBatches = useCallback(() => {
    return dateBatches.slice(0, currentPage + 1).flat();
  }, [dateBatches, currentPage]);

  // Load next batch
  const loadNextBatch = useCallback(() => {
    if (currentPage < dateBatches.length - 1) {
      setCurrentPage((prev) => {
        const next = prev + 1;
        // hasMore means there is at least one more batch after the next
        setHasMore(next < dateBatches.length - 1);
        return next;
      });
      return true;
    }
    setHasMore(false);
    return false;
  }, [currentPage, dateBatches.length]);

  // Reset to first page
  const resetPagination = useCallback(() => {
    setCurrentPage(0);
    setHasMore(dateBatches.length > 1);
  }, [dateBatches.length]);

  return {
    currentPage,
    hasMore,
    dateBatches,
    getCurrentDateBatch,
    getAllLoadedDateBatches,
    loadNextBatch,
    resetPagination,
    totalBatches: dateBatches.length,
    isFirstLoad: currentPage === 0,
  };
};
