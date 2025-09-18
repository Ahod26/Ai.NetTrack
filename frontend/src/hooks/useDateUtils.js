export const useDateUtils = (
  selectedDates,
  tempSelectedDates,
  minDate,
  maxDate
) => {
  // Basic date formatting for date selector
  const formatDate = (date) => {
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    });
  };

  // Date formatting for date tags (Today/Yesterday)
  const formatDateForTag = (date) => {
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    // Reset time to compare only dates
    const dateToCheck = new Date(date);
    dateToCheck.setHours(0, 0, 0, 0);

    const todayCheck = new Date(today);
    todayCheck.setHours(0, 0, 0, 0);

    const yesterdayCheck = new Date(yesterday);
    yesterdayCheck.setHours(0, 0, 0, 0);

    if (dateToCheck.getTime() === todayCheck.getTime()) {
      return "Today";
    } else if (dateToCheck.getTime() === yesterdayCheck.getTime()) {
      return "Yesterday";
    } else {
      return formatDate(date);
    }
  };

  // Relative date formatting for news cards (e.g., "2h ago", "Yesterday")
  const formatRelativeDate = (dateString) => {
    if (!dateString) return "Unknown date";

    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now - date);
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
    const diffHours = Math.floor(diffTime / (1000 * 60 * 60));

    if (diffDays === 0) {
      if (diffHours === 0) return "Just now";
      return `${diffHours}h ago`;
    } else if (diffDays === 1) {
      return "Yesterday";
    } else if (diffDays < 7) {
      return `${diffDays}d ago`;
    } else {
      return date.toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
      });
    }
  };

  // Full date formatting for news modal (e.g., "Monday, September 18, 2025, 10:30 AM")
  const formatFullDate = (dateString) => {
    if (!dateString) return "Unknown date";

    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const isDateDisabled = (date) => {
    return date < minDate || date > maxDate;
  };

  const isDateSelected = (date, datesList = selectedDates) => {
    return datesList.some(
      (selectedDate) => selectedDate.toDateString() === date.toDateString()
    );
  };

  const isTempDateSelected = (date) => {
    return tempSelectedDates.some(
      (tempDate) => tempDate.toDateString() === date.toDateString()
    );
  };

  const handleDateClick = (date, setTempSelectedDates) => {
    if (isDateDisabled(date)) return;

    const isCurrentlySelected = isDateSelected(date);
    const isTempSelected = isTempDateSelected(date);

    if (isCurrentlySelected) {
      // If it's currently selected, add it to temp for removal
      if (!isTempSelected) {
        setTempSelectedDates((prev) => [...prev, date]);
      } else {
        // Remove from temp (cancel removal)
        setTempSelectedDates((prev) =>
          prev.filter((d) => d.toDateString() !== date.toDateString())
        );
      }
    } else if (isTempSelected) {
      // Remove from temp selection (cancel addition)
      setTempSelectedDates((prev) =>
        prev.filter((d) => d.toDateString() !== date.toDateString())
      );
    } else {
      // Add to temp selection
      setTempSelectedDates((prev) => [...prev, date]);
    }
  };

  // Generate calendar days for current month
  const generateCalendarDays = () => {
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());

    const days = [];
    const current = new Date(startDate);

    for (let i = 0; i < 42; i++) {
      // 6 weeks * 7 days
      days.push(new Date(current));
      current.setDate(current.getDate() + 1);
    }

    return days;
  };

  const monthNames = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];

  return {
    formatDate,
    formatDateForTag,
    formatRelativeDate,
    formatFullDate,
    isDateDisabled,
    isDateSelected,
    isTempDateSelected,
    handleDateClick,
    generateCalendarDays,
    monthNames,
  };
};
