import { useState } from "react";
import { Link } from "react-router-dom";
import { validateSignupForm } from "../../../utils/validation";
import { registerUser } from "../../../api/auth";
import styles from "./Signup.module.css";

export default function Signup() {
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    password: "",
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
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
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
        username: formData.fullName,
        email: formData.email,
        password: formData.password,
      };

      const response = await registerUser(signupData);

      console.log("Signup response:", response);
      console.log("Response success:", response.success);
      console.log("Response errors:", response.errors);

      if (response.success) {
        // Show success message
        setSuccessMessage("Account created successfully! You can now log in.");

        // Clear form
        setFormData({ fullName: "", email: "", password: "" });
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

          <button
            type="submit"
            className={styles.submitBtn}
            disabled={isLoading}
          >
            {isLoading ? "Creating Account..." : "Create Account"}
          </button>
        </form>

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
