import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { validateLoginForm } from "../../../utils/validation";
import { loginUser } from "../../../api/auth";
import { userAuthSliceAction } from "../../../store/userAuth";
import styles from "./Login.module.css";

export default function Login() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    email: "",
    password: "",
  });

  const [errors, setErrors] = useState({
    email: [],
    password: [],
  });

  const [touched, setTouched] = useState({
    email: false,
    password: false,
  });

  const [isLoading, setIsLoading] = useState(false);
  const [apiError, setApiError] = useState("");

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Real-time validation only if field has been touched
    if (touched[name]) {
      const validation = validateLoginForm({ ...formData, [name]: value });
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
    const validation = validateLoginForm(formData);
    setErrors(validation.errors);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Clear previous API error
    setApiError("");

    // Mark all fields as touched
    setTouched({
      email: true,
      password: true,
    });

    // Validate form
    const validation = validateLoginForm(formData);
    setErrors(validation.errors);

    if (validation.isValid) {
      setIsLoading(true);
      try {
        const response = await loginUser(formData);

        if (response.success) {
          // Update Redux store with user data
          dispatch(userAuthSliceAction.setUserLoggedIn(response.user));

          // Navigate directly to chat
          navigate("/chat/new");
        } else {
          setApiError(response.message || "Login failed");
        }
      } catch (error) {
        console.error("Login error:", error);
        setApiError(
          "Login failed. Please check your credentials and try again."
        );
      } finally {
        setIsLoading(false);
      }
    }
  };

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <h2 className={styles.title}>Welcome Back</h2>
          <p className={styles.subtitle}>Sign in to your account</p>
        </div>

        {apiError && <div className={styles.apiError}>{apiError}</div>}

        <form onSubmit={handleSubmit} className={styles.form} noValidate>
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
              placeholder="Enter your password"
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
            {isLoading ? "Signing In..." : "Sign In"}
          </button>
        </form>

        <div className={styles.footer}>
          <p className={styles.switchText}>
            Don't have an account?{" "}
            <Link to="/signup" className={styles.switchLink}>
              Sign up
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
