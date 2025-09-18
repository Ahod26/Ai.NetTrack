import { useEffect, useState } from "react";
import { useSelector, useDispatch } from "react-redux";
import { sidebarActions } from "../../store/sidebarSlice";
import { getNewsByDate } from "../../api/news";
import DateSelector from "./components/DateSelector/DateSelector";
import NewsTypeFilter from "./components/NewsTypeFilter/NewsTypeFilter";
import SearchBar from "./components/SearchBar/SearchBar";
import NewsCard from "./components/NewsCard/NewsCard";
import NewsModal from "./components/NewsModal/NewsModal";
import LoadingSpinner from "../LoadingSpinner/LoadingSpinner";
import styles from "./Timeline.module.css";

export default function Timeline() {
  const dispatch = useDispatch();
  const isSidebarOpen = useSelector((state) => state.sidebar.isOpen);

  const [newsItems, setNewsItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedDates, setSelectedDates] = useState(() => {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(today.getDate() - 1);
    return [yesterday, today];
  });
  const [selectedNewsTypes, setSelectedNewsTypes] = useState([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedNewsItem, setSelectedNewsItem] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Close sidebar when Timeline component mounts
  useEffect(() => {
    dispatch(sidebarActions.closeSidebar());
  }, [dispatch]);

  // Load news when dates change
  useEffect(() => {
    const loadNewsData = async () => {
      if (selectedDates.length === 0) {
        setNewsItems([]);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const data = await getNewsByDate(selectedDates);
        setNewsItems(data || []);
      } catch (err) {
        setError("Failed to load news. Please try again.");
        console.error("Error loading news:", err);
      } finally {
        setLoading(false);
      }
    };

    loadNewsData();
  }, [selectedDates]);

  const loadNews = async () => {
    if (selectedDates.length === 0) {
      setNewsItems([]);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const data = await getNewsByDate(selectedDates);
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

  const handleNewsTypeChange = (types) => {
    setSelectedNewsTypes(types);
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
      <div className={styles.content}>
        <div className={styles.controls}>
          <DateSelector
            selectedDates={selectedDates}
            onDateChange={handleDateChange}
          />
          <NewsTypeFilter
            selectedTypes={selectedNewsTypes}
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
              No news found for the selected dates.
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
              />
            ))}
          </div>
        )}
      </div>

      <NewsModal
        newsItem={selectedNewsItem}
        isOpen={isModalOpen}
        onClose={handleCloseModal}
      />
    </div>
  );
}
