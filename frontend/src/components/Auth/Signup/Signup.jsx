import { useState } from "react";
import { Link } from "react-router-dom";
import { validateSignupForm } from "../../../utils/validation";
import { registerUser } from "../../../api/auth";
import { API_BASE_URL, API_ENDPOINTS } from "../../../api/config";
import styles from "./Signup.module.css";

export default function Signup() {
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
    isSubscribedToNewsletter: true,
  });

  const [errors, setErrors] = useState({
    fullName: [],
    email: [],
    password: [],
  });

  const [touched, setTouched] = useState({
    fullName: false,
    email: false,
    password: false,
  });

  const [isLoading, setIsLoading] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [apiErrors, setApiErrors] = useState([]);

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));

    // Real-time validation only if field has been touched
    if (touched[name]) {
      const validation = validateSignupForm({ ...formData, [name]: value });
      setErrors(validation.errors);
    }
  };

  const handleInputBlur = (e) => {
    const { name } = e.target;
    setTouched((prev) => ({
      ...prev,
      [name]: true,
    }));

    // Validate when field loses focus
    const validation = validateSignupForm(formData);
    setErrors(validation.errors);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Clear previous API errors
    setApiErrors([]);

    // Mark all fields as touched
    setTouched({
      fullName: true,
      email: true,
      password: true,
    });

    // Validate form
    const validation = validateSignupForm(formData);
    setErrors(validation.errors);

    if (validation.isValid) {
      setIsLoading(true);

      // Map formData to API expected format
      const signupData = {
        fullName: formData.fullName,
        email: formData.email,
        password: formData.password,
        isSubscribedToNewsletter: formData.isSubscribedToNewsletter,
      };

      const response = await registerUser(signupData);

      console.log("Signup response:", response);
      console.log("Response success:", response.success);
      console.log("Response errors:", response.errors);

      if (response.success) {
        // Show success message
        setSuccessMessage("Account created successfully! You can now log in.");

        // Clear form
        setFormData({
          fullName: "",
          email: "",
          password: "",
          isSubscribedToNewsletter: true,
        });
        setTouched({ fullName: false, email: false, password: false });
        setErrors({ fullName: [], email: [], password: [] });

        // Hide success message after 3 seconds
        setTimeout(() => {
          setSuccessMessage("");
        }, 3000);
      } else {
        // Handle API errors from the response
        setApiErrors(
          response.errors || [response.message || "Registration failed"]
        );
      }

      setIsLoading(false);
    }
  };
  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <h2 className={styles.title}>Create Account</h2>
          <p className={styles.subtitle}>Join the .NET AI Developer Hub</p>
        </div>

        {successMessage && (
          <div className={styles.successMessage}>{successMessage}</div>
        )}

        {apiErrors.length > 0 && (
          <div className={styles.apiError}>
            {apiErrors.map((error, index) => (
              <div key={index}>{error}</div>
            ))}
          </div>
        )}

        <form onSubmit={handleSubmit} className={styles.form} noValidate>
          <div className={styles.inputGroup}>
            <label htmlFor="fullName" className={styles.label}>
              Full Name
            </label>
            <input
              type="text"
              id="fullName"
              name="fullName"
              value={formData.fullName}
              onChange={handleInputChange}
              onBlur={handleInputBlur}
              className={`${styles.input} ${
                touched.fullName && errors.fullName.length > 0
                  ? styles.inputError
                  : ""
              }`}
              placeholder="Enter your full name"
            />
            {touched.fullName && errors.fullName.length > 0 && (
              <div className={styles.errorMessages}>
                {errors.fullName.map((error, index) => (
                  <span key={index} className={styles.errorMessage}>
                    {error}
                  </span>
                ))}
              </div>
            )}
          </div>

          <div className={styles.inputGroup}>
            <label htmlFor="email" className={styles.label}>
              Email
            </label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleInputChange}
              onBlur={handleInputBlur}
              className={`${styles.input} ${
                touched.email && errors.email.length > 0
                  ? styles.inputError
                  : ""
              }`}
              placeholder="Enter your email"
            />
            {touched.email && errors.email.length > 0 && (
              <div className={styles.errorMessages}>
                {errors.email.map((error, index) => (
                  <span key={index} className={styles.errorMessage}>
                    {error}
                  </span>
                ))}
              </div>
            )}
          </div>

          <div className={styles.inputGroup}>
            <label htmlFor="password" className={styles.label}>
              Password
            </label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleInputChange}
              onBlur={handleInputBlur}
              className={`${styles.input} ${
                touched.password && errors.password.length > 0
                  ? styles.inputError
                  : ""
              }`}
              placeholder="Create a password"
            />
            {touched.password && errors.password.length > 0 && (
              <div className={styles.errorMessages}>
                {errors.password.map((error, index) => (
                  <span key={index} className={styles.errorMessage}>
                    {error}
                  </span>
                ))}
              </div>
            )}
          </div>

          <div className={styles.checkboxGroup}>
            <input
              type="checkbox"
              id="newsletter"
              name="isSubscribedToNewsletter"
              checked={formData.isSubscribedToNewsletter}
              onChange={handleInputChange}
              className={styles.checkbox}
            />
            <label htmlFor="newsletter" className={styles.checkboxLabel}>
              Subscribe to our newsletter for updates and tips
            </label>
          </div>

          <button
            type="submit"
            className={styles.submitBtn}
            disabled={isLoading}
          >
            {isLoading ? "Creating Account..." : "Create Account"}
          </button>
        </form>

        <div className={styles.divider}>
          <span className={styles.dividerText}>OR</span>
        </div>

        <button
          type="button"
          className={styles.googleBtn}
          onClick={() =>
            (window.location.href = `${API_BASE_URL}${API_ENDPOINTS.AUTH.GOOGLE_LOGIN}`)
          }
        >
          <svg
            className={styles.googleIcon}
            viewBox="0 0 24 24"
            width="20"
            height="20"
          >
            <path
              fill="#4285F4"
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
            />
            <path
              fill="#34A853"
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
            />
            <path
              fill="#FBBC05"
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
            />
            <path
              fill="#EA4335"
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
            />
          </svg>
          Continue with Google
        </button>

        <div className={styles.footer}>
          <p className={styles.switchText}>
            Already have an account?{" "}
            <Link to="/login" className={styles.switchLink}>
              Sign in
            </Link>
          </p>
        </div>

        <Link to="/chat/new" className={styles.closeBtn}>
          <svg
            width="24"
            height="24"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
          >
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </Link>
      </div>
    </div>
  );
}
