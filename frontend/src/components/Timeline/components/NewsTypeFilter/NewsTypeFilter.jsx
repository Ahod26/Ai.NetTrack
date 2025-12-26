import { useState, useRef, useEffect } from "react";
import styles from "./NewsTypeFilter.module.css";

const newsTypes = [
  {
    id: "all",
    label: "All Updates",
    color: "#6366f1",
    icon: "",
  },
  {
    id: 1,
    label: "GitHub",
    color: "#24292e",
    icon: "",
  },
  {
    id: 2,
    label: "RSS",
    color: "#ee802f",
    icon: "",
  },
  {
    id: 3,
    label: "YouTube",
    color: "#ff0000",
    icon: "",
  },
];

export default function NewsTypeFilter({ selectedType, onTypeChange }) {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef(null);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const toggleType = (typeId) => {
    // If clicking the same type, clear selection (show all)
    // Otherwise, select the new type
    const newSelectedType = selectedType === typeId ? null : typeId;
    onTypeChange(newSelectedType);
  };

  const clearAll = () => {
    onTypeChange(null); // null means show all types
  };

  const getDisplayText = () => {
    if (!selectedType) return "All news types";
    const selectedTypeObj = newsTypes.find((type) => type.id === selectedType);
    return selectedTypeObj ? selectedTypeObj.label : "All news types";
  };

  return (
    <div className={styles.newsTypeFilter} ref={dropdownRef}>
      <button
        className={styles.filterButton}
        onClick={() => setIsOpen(!isOpen)}
      >
        <span className={styles.label}>{getDisplayText()}</span>
        <span className={`${styles.arrow} ${isOpen ? styles.open : ""}`}>
          ▼
        </span>
      </button>

      {isOpen && (
        <div className={styles.dropdown}>
          <div className={styles.dropdownHeader}>
            <h3 className={styles.dropdownTitle}>News Types</h3>
            <div className={styles.headerActions}>
              <button
                className={styles.headerAction}
                onClick={clearAll}
                disabled={!selectedType}
              >
                Clear
              </button>
            </div>
          </div>

          <div className={styles.typesList}>
            {newsTypes.map((type) => (
              <label key={type.id} className={styles.typeItem}>
                <input
                  type="radio"
                  name="newsType"
                  className={styles.checkbox}
                  checked={selectedType === type.id}
                  onChange={() => toggleType(type.id)}
                />
                <div className={styles.typeContent}>
                  <div className={styles.typeHeader}>
                    <span className={styles.typeIcon}>{type.icon}</span>
                    <span className={styles.typeName}>{type.label}</span>
                  </div>
                </div>
              </label>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
