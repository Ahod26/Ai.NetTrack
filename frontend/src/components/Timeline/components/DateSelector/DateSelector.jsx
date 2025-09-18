import { useDateSelector } from "../../../../hooks/useDateSelector";
import { useDateUtils } from "../../../../hooks/useDateUtils";
import styles from "./DateSelector.module.css";

export default function DateSelector({ selectedDates, onDateChange }) {
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
    generateCalendarDays,
    monthNames,
  } = useDateUtils(selectedDates, tempSelectedDates, minDate, maxDate);

  const today = new Date();
  const calendarDays = generateCalendarDays();

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
              <h3 className={styles.monthYear}>
                {monthNames[today.getMonth()]} {today.getFullYear()}
              </h3>
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
                const isCurrentMonth = date.getMonth() === today.getMonth();
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
