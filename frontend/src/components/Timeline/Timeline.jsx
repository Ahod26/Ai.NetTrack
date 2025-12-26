import { useEffect, useState, useCallback, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { sidebarActions } from "../../store/sidebarSlice";
import { getNewsByDate, getNewsBySearch } from "../../api/news";
import { useDatePagination } from "../../hooks/useDatePagination";
import DateSelector from "./components/DateSelector/DateSelector";
import NewsTypeFilter from "./components/NewsTypeFilter/NewsTypeFilter";
import SearchBar from "./components/SearchBar/SearchBar";
import NewsCard from "./components/NewsCard/NewsCard";
import NewsModal from "./components/NewsModal/NewsModal";
import LoadingSpinner from "../LoadingSpinner/LoadingSpinner";
import ErrorPopup from "../ErrorPopup";
import styles from "./Timeline.module.css";

export default function Timeline() {
  const dispatch = useDispatch();
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  const [newsItems, setNewsItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState(null);
  const [selectedDates, setSelectedDates] = useState(() => {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(today.getDate() - 1);
    return [yesterday, today];
  });
  const [selectedNewsType, setSelectedNewsType] = useState(null); // Single news type
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedNewsItem, setSelectedNewsItem] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [, setIsSearching] = useState(false);
  const errorTimeoutRef = useRef(null);
  const searchTimeoutRef = useRef(null);

  useEffect(() => {
    return () => {
      if (errorTimeoutRef.current) {
        clearTimeout(errorTimeoutRef.current);
      }
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, []);

  const handleChatError = useCallback((errorMsg) => {
    if (errorTimeoutRef.current) {
      clearTimeout(errorTimeoutRef.current);
    }
    setErrorMessage(errorMsg);
    errorTimeoutRef.current = setTimeout(() => {
      setErrorMessage("");
    }, 5000);
  }, []);

  const {
    getCurrentDateBatch,
    loadNextBatch,
    hasMore,
    resetPagination,
    currentPage,
    dateBatches,
  } = useDatePagination(selectedDates, 5);

  const loadMore = useCallback(async () => {
    if (!hasMore || loadingMore) return;

    try {
      setLoadingMore(true);

      const nextIndex = currentPage + 1;
      const nextBatch = dateBatches[nextIndex];
      if (!nextBatch || nextBatch.length === 0) {
        return;
      }

      const newData = await getNewsByDate(nextBatch, selectedNewsType);
      setNewsItems((prev) => [...prev, ...(newData || [])]);
      loadNextBatch();
    } catch (err) {
      console.error("Error loading more news:", err);
    } finally {
      setLoadingMore(false);
    }
  }, [
    hasMore,
    loadingMore,
    currentPage,
    dateBatches,
    selectedNewsType,
    loadNextBatch,
  ]);

  const contentRef = useRef(null);
  const sentinelRef = useRef(null);
  useEffect(() => {
    const el = contentRef.current;
    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const useViewportRoot = !el || el.scrollHeight <= el.clientHeight;
    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry.isIntersecting && !loading && !loadingMore && hasMore) {
          loadMore();
        }
      },
      {
        root: useViewportRoot ? null : el,
        rootMargin: "200px 0px 200px 0px",
        threshold: 0.01,
      }
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [loadMore, loading, loadingMore, hasMore]);

  useEffect(() => {
    dispatch(sidebarActions.closeSidebar());
  }, [dispatch]);

  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (!searchQuery || searchQuery.trim().length < 3) {
      setIsSearching(false);
      return;
    }

    setIsSearching(true);

    searchTimeoutRef.current = setTimeout(async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await getNewsBySearch(searchQuery.trim());
        setNewsItems(data || []);
      } catch (err) {
        setError("Failed to search news. Please try again.");
        console.error("Error searching news:", err);
      } finally {
        setLoading(false);
        setIsSearching(false);
      }
    }, 500);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchQuery]);

  useEffect(() => {
    if (searchQuery && searchQuery.trim().length >= 3) {
      return;
    }

    const loadInitialNews = async () => {
      if (selectedDates.length === 0) {
        setNewsItems([]);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        resetPagination();

        const firstBatch = dateBatches[0] || [];
        const data = await getNewsByDate(firstBatch, selectedNewsType);
        setNewsItems(data || []);

        if (dateBatches.length > 1) {
          /* empty */
        }
      } catch (err) {
        setError("Failed to load news. Please try again.");
        console.error("Error loading news:", err);
      } finally {
        setLoading(false);
      }
    };

    loadInitialNews();
  }, [
    selectedDates,
    selectedNewsType,
    resetPagination,
    dateBatches,
    searchQuery,
  ]);

  const loadNews = async () => {
    if (selectedDates.length === 0) {
      setNewsItems([]);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      resetPagination();

      const dateBatch = getCurrentDateBatch();
      const data = await getNewsByDate(dateBatch, selectedNewsType);
      setNewsItems(data || []);
    } catch (err) {
      setError("Failed to load news. Please try again.");
      console.error("Error loading news:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (newsItem) => {
    setSelectedNewsItem(newsItem);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setSelectedNewsItem(null);
  };

  const handleDateChange = (dates) => {
    setSelectedDates(dates);
  };

  const handleNewsTypeChange = (newsType) => {
    setSelectedNewsType(newsType);
  };

  const handleSearchChange = (query) => {
    setSearchQuery(query);
  };

  return (
    <div
      className={`${styles.timelineContainer} ${
        !isSidebarOpen ? styles.sidebarClosed : ""
      }`}
    >
      <ErrorPopup message={errorMessage} />
      <div className={styles.content} ref={contentRef}>
        <div className={styles.controls}>
          <DateSelector
            selectedDates={selectedDates}
            onDateChange={handleDateChange}
          />
          <NewsTypeFilter
            selectedType={selectedNewsType}
            onTypeChange={handleNewsTypeChange}
          />
          <SearchBar
            searchQuery={searchQuery}
            onSearchChange={handleSearchChange}
          />
        </div>
        {loading && (
          <div className={styles.loadingContainer}>
            <LoadingSpinner />
            <p className={styles.loadingText}>Loading news...</p>
          </div>
        )}

        {error && (
          <div className={styles.errorContainer}>
            <p className={styles.errorText}>{error}</p>
            <button className={styles.retryButton} onClick={loadNews}>
              Try Again
            </button>
          </div>
        )}

        {!loading && !error && newsItems.length === 0 && (
          <div className={styles.emptyContainer}>
            <p className={styles.emptyText}>
              {searchQuery && searchQuery.trim().length >= 3
                ? `No news found for "${searchQuery}"`
                : "No news found for the selected dates."}
            </p>
          </div>
        )}

        {!loading && !error && newsItems.length > 0 && (
          <div className={styles.newsGrid}>
            {newsItems.map((newsItem) => (
              <NewsCard
                key={`${newsItem.id}-${newsItem.url}`}
                newsItem={newsItem}
                onOpenModal={handleOpenModal}
                onChatError={handleChatError}
              />
            ))}
          </div>
        )}

        <div ref={sentinelRef} style={{ height: 1 }} />

        {loadingMore && (
          <div className={styles.loadingMoreContainer}>
            <LoadingSpinner />
            <p className={styles.loadingText}>Loading more news...</p>
          </div>
        )}

        <div ref={sentinelRef} style={{ height: 1 }} />
      </div>

      <NewsModal
        newsItem={selectedNewsItem}
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        onChatError={handleChatError}
      />
    </div>
  );
}
