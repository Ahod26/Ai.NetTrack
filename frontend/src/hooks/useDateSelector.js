import { useState, useRef, useEffect } from "react";

export const useDateSelector = (selectedDates, onDateChange) => {
  const [isOpen, setIsOpen] = useState(false);
  const [tempSelectedDates, setTempSelectedDates] = useState([]);
  const [dropdownPosition, setDropdownPosition] = useState({ top: 0, left: 0 });
  const dropdownRef = useRef(null);
  const buttonRef = useRef(null);

  // Date constraints
  const minDate = new Date("2025-09-05"); // Start of aggregation
  const maxDate = new Date(); // Today

  const calculateDropdownPosition = () => {
    if (!buttonRef.current) return { top: 0, left: 0 };

    const buttonRect = buttonRef.current.getBoundingClientRect();
    const dropdownHeight = 500; // Approximate dropdown height
    const viewportHeight = window.innerHeight;

    let top = buttonRect.bottom + 8; // 8px gap
    let left = buttonRect.left;

    // If dropdown would go below viewport, position it above the button
    if (top + dropdownHeight > viewportHeight) {
      top = buttonRect.top - dropdownHeight - 8;
    }

    // Ensure dropdown doesn't go off the left edge
    if (left < 16) {
      left = 16;
    }

    // Ensure dropdown doesn't go off the right edge
    const dropdownWidth = 320;
    if (left + dropdownWidth > window.innerWidth - 16) {
      left = window.innerWidth - dropdownWidth - 16;
    }

    return { top, left };
  };

  const handleToggleDropdown = () => {
    if (!isOpen) {
      const position = calculateDropdownPosition();
      setDropdownPosition(position);
    }
    setIsOpen(!isOpen);
  };

  const handleApply = () => {
    // Start with current selected dates
    let finalDates = [...selectedDates];

    // For each temp date, either add it or remove it
    tempSelectedDates.forEach((tempDate) => {
      const isCurrentlySelected = selectedDates.some(
        (date) => date.toDateString() === tempDate.toDateString()
      );

      if (isCurrentlySelected) {
        // Remove it
        finalDates = finalDates.filter(
          (date) => date.toDateString() !== tempDate.toDateString()
        );
      } else {
        // Add it
        finalDates.push(tempDate);
      }
    });

    onDateChange(finalDates);
    setTempSelectedDates([]);
    setIsOpen(false);
  };

  const handleCancel = () => {
    setTempSelectedDates([]);
    setIsOpen(false);
  };

  const removeDate = (indexToRemove) => {
    const updatedDates = selectedDates.filter(
      (_, index) => index !== indexToRemove
    );
    onDateChange(updatedDates);
  };

  // Handle click outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
        setTempSelectedDates([]); // Reset temp selection
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return {
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
  };
};
