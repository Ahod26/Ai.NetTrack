// Validation functions matching backend Identity constraints

export const validateEmail = (email) => {
  const errors = [];

  if (!email || email.trim() === "") {
    errors.push("Email is required");
    return { isValid: false, errors };
  }

  // Basic email format validation
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    errors.push("Please enter a valid email address");
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
};

export const validatePassword = (password) => {
  const errors = [];

  if (!password || password.trim() === "") {
    errors.push("Password is required");
    return { isValid: false, errors };
  }

  // Backend constraints:
  // RequiredLength = 6
  if (password.length < 6) {
    errors.push("Password must be at least 6 characters long");
  }

  // RequireDigit = true
  if (!/\d/.test(password)) {
    errors.push("Password must contain at least one digit");
  }

  // RequireLowercase = true
  if (!/[a-z]/.test(password)) {
    errors.push("Password must contain at least one lowercase letter");
  }

  // RequireUppercase = false (no validation needed)
  // RequireNonAlphanumeric = false (no validation needed)

  return {
    isValid: errors.length === 0,
    errors,
  };
};

export const validateUserName = (userName) => {
  const errors = [];

  if (!userName || userName.trim() === "") {
    errors.push("Full name is required");
    return { isValid: false, errors };
  }

  // Backend allowed characters: "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"
  const allowedCharsRegex = /^[a-zA-Z0-9\-._@+\s]+$/;
  if (!allowedCharsRegex.test(userName)) {
    errors.push(
      "Name contains invalid characters. Only letters, numbers, spaces, and -._@+ are allowed"
    );
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
};

// Form validation function for login
export const validateLoginForm = (formData) => {
  const emailValidation = validateEmail(formData.email);
  const passwordValidation = validatePassword(formData.password);

  return {
    isValid: emailValidation.isValid && passwordValidation.isValid,
    errors: {
      email: emailValidation.errors,
      password: passwordValidation.errors,
    },
  };
};

// Form validation function for signup
export const validateSignupForm = (formData) => {
  const fullNameValidation = validateUserName(formData.fullName);
  const emailValidation = validateEmail(formData.email);
  const passwordValidation = validatePassword(formData.password);

  return {
    isValid:
      fullNameValidation.isValid &&
      emailValidation.isValid &&
      passwordValidation.isValid,
    errors: {
      fullName: fullNameValidation.errors,
      email: emailValidation.errors,
      password: passwordValidation.errors,
    },
  };
};
