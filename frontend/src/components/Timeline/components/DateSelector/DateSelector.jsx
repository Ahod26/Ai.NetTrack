import { useState } from "react";
import { useDateSelector } from "../../../../hooks/useDateSelector";
import { useDateUtils } from "../../../../hooks/useDateUtils";
import styles from "./DateSelector.module.css";

export default function DateSelector({ selectedDates, onDateChange }) {
  // Month navigation state
  const today = new Date();
  const [currentMonth, setCurrentMonth] = useState(today.getMonth());
  const [currentYear, setCurrentYear] = useState(today.getFullYear());

  const {
    isOpen,
    tempSelectedDates,
    setTempSelectedDates,
    dropdownPosition,
    dropdownRef,
    buttonRef,
    minDate,
    maxDate,
    handleToggleDropdown,
    handleApply,
    handleCancel,
    removeDate,
  } = useDateSelector(selectedDates, onDateChange);

  const {
    formatDateForTag,
    isDateDisabled,
    isDateSelected,
    isTempDateSelected,
    handleDateClick,
    monthNames,
  } = useDateUtils(selectedDates, tempSelectedDates, minDate, maxDate);

  // Month navigation functions
  const canGoPrevious = () => {
    const firstOfPreviousMonth = new Date(currentYear, currentMonth - 1, 1);
    return (
      firstOfPreviousMonth >=
      new Date(minDate.getFullYear(), minDate.getMonth(), 1)
    );
  };

  const canGoNext = () => {
    const firstOfNextMonth = new Date(currentYear, currentMonth + 1, 1);
    const firstOfMaxMonth = new Date(
      maxDate.getFullYear(),
      maxDate.getMonth(),
      1
    );
    return firstOfNextMonth <= firstOfMaxMonth;
  };

  const handlePreviousMonth = () => {
    if (canGoPrevious()) {
      if (currentMonth === 0) {
        setCurrentMonth(11);
        setCurrentYear(currentYear - 1);
      } else {
        setCurrentMonth(currentMonth - 1);
      }
    }
  };

  const handleNextMonth = () => {
    if (canGoNext()) {
      if (currentMonth === 11) {
        setCurrentMonth(0);
        setCurrentYear(currentYear + 1);
      } else {
        setCurrentMonth(currentMonth + 1);
      }
    }
  };

  // Generate calendar days for current displayed month
  const generateCurrentMonthCalendarDays = () => {
    const firstDay = new Date(currentYear, currentMonth, 1);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());

    const days = [];
    const current = new Date(startDate);

    for (let i = 0; i < 42; i++) {
      days.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }

    return days;
  };

  const calendarDays = generateCurrentMonthCalendarDays();

  return (
    <div className={styles.dateSelector} ref={dropdownRef}>
      <button
        ref={buttonRef}
        className={styles.selectorButton}
        onClick={handleToggleDropdown}
      >
        <span className={styles.icon}>📅</span>
        <span className={styles.label}>
          {selectedDates.length === 0
            ? "Select dates"
            : `${selectedDates.length} date${
                selectedDates.length > 1 ? "s" : ""
              }`}
        </span>
        <span className={`${styles.arrow} ${isOpen ? styles.open : ""}`}>
          ▼
        </span>
      </button>

      {selectedDates.length > 0 && (
        <div className={styles.selectedDates}>
          {selectedDates.map((date, index) => (
            <span key={index} className={styles.dateTag}>
              {formatDateForTag(date)}
              <button
                className={styles.removeDate}
                onClick={() => removeDate(index)}
                title="Remove date"
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}

      {isOpen && (
        <div
          className={styles.dropdown}
          style={{
            top: `${dropdownPosition.top}px`,
            left: `${dropdownPosition.left}px`,
          }}
        >
          <div className={styles.calendar}>
            <div className={styles.calendarHeader}>
              <button
                className={styles.navButton}
                onClick={handlePreviousMonth}
                disabled={!canGoPrevious()}
                title="Previous month"
              >
                ‹
              </button>
              <h3 className={styles.monthYear}>
                {monthNames[currentMonth]} {currentYear}
              </h3>
              <button
                className={styles.navButton}
                onClick={handleNextMonth}
                disabled={!canGoNext()}
                title="Next month"
              >
                ›
              </button>
            </div>

            <div className={styles.weekdays}>
              {["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map((day) => (
                <div key={day} className={styles.weekday}>
                  {day}
                </div>
              ))}
            </div>

            <div className={styles.calendarGrid}>
              {calendarDays.map((date, index) => {
                const isCurrentMonth = date.getMonth() === currentMonth;
                const isCurrentlySelected = isDateSelected(date);
                const isTempSelected = isTempDateSelected(date);
                const isDisabled = isDateDisabled(date);

                // Determine final display state
                let isHighlighted = false;
                if (isCurrentlySelected && !isTempSelected) {
                  // Currently selected, not in temp (will remain selected)
                  isHighlighted = true;
                } else if (!isCurrentlySelected && isTempSelected) {
                  // Not currently selected, but in temp (will be added)
                  isHighlighted = true;
                }
                // If currently selected AND in temp, it will be removed (not highlighted)
                // If not currently selected AND not in temp, it's not selected (not highlighted)

                return (
                  <button
                    key={index}
                    className={`
                      ${styles.calendarDay} 
                      ${!isCurrentMonth ? styles.otherMonth : ""}
                      ${isHighlighted ? styles.selected : ""}
                      ${isDisabled ? styles.disabled : ""}
                    `}
                    onClick={() => handleDateClick(date, setTempSelectedDates)}
                    disabled={isDisabled}
                  >
                    {date.getDate()}
                  </button>
                );
              })}
            </div>
          </div>

          <div className={styles.actionButtons}>
            <button className={styles.cancelButton} onClick={handleCancel}>
              Cancel
            </button>
            <button
              className={styles.applyButton}
              onClick={handleApply}
              disabled={tempSelectedDates.length === 0}
            >
              Apply
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
