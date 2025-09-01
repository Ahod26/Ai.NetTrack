import styles from "./Skeleton.module.css";

const Skeleton = ({
  variant = "text",
  width = "100%",
  height,
  className = "",
  animation = "pulse",
}) => {
  const skeletonStyles = {
    width,
    height: height || (variant === "text" ? "1em" : "40px"),
  };

  const skeletonClass = `${styles.skeleton} ${styles[variant]} ${styles[animation]} ${className}`;

  return <div className={skeletonClass} style={skeletonStyles} />;
};

export default Skeleton;
