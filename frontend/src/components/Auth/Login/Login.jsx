import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { validateLoginForm } from "../../../utils/validation";
import { loginUser } from "../../../api/auth";
import { API_BASE_URL, API_ENDPOINTS } from "../../../api/config";
import { userAuthSliceAction } from "../../../store/userAuth";
import chatHubService from "../../../api/chatHub";
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
          // Convert UserInfo class instance to plain object before dispatching
          const userPlainObject = {
            fullName: response.user.fullName,
            email: response.user.email,
            roles: response.user.roles,
          };

          dispatch(userAuthSliceAction.setUserLoggedIn(userPlainObject));

          // Initialize SignalR connection after successful login
          try {
            await chatHubService.startConnection();
            console.log("SignalR connected after login");
          } catch (signalRError) {
            console.error(
              "Failed to connect SignalR after login:",
              signalRError
            );
          }

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
