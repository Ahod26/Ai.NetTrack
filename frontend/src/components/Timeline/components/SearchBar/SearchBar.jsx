import { useState } from "react";
import styles from "./SearchBar.module.css";

export default function SearchBar({ searchQuery, onSearchChange }) {
  const [isFocused, setIsFocused] = useState(false);

  const handleInputChange = (e) => {
    onSearchChange(e.target.value);
  };

  const clearSearch = () => {
    onSearchChange("");
  };

  return (
    <div className={`${styles.searchBar} ${isFocused ? styles.focused : ""}`}>
      <div className={styles.inputWrapper}>
        <div className={styles.searchIcon}></div>
        <input
          type="text"
          className={styles.searchInput}
          placeholder="Search news content..."
          value={searchQuery}
          onChange={handleInputChange}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
        />
        {searchQuery && (
          <button
            className={styles.clearButton}
            onClick={clearSearch}
            title="Clear search"
          >
            ×
          </button>
        )}
      </div>
    </div>
  );
}
