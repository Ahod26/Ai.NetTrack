import { useState, useEffect } from "react";
import { motion, AnimatePresence } from "framer-motion";
import styles from "./AnimatedGreeting.module.css";

const greetings = [
  "How can I help you build with .NET today?",
  "Explore the latest in AI for .NET developers.",
  "Need help debugging your C# code?",
  "Let's architect your next microservice.",
  "Ask me about Semantic Kernel integration.",
];

export default function AnimatedGreeting() {
  const [index, setIndex] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setIndex((prev) => (prev + 1) % greetings.length);
    }, 4000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className={styles.container}>
      <h1 className={styles.staticTitle}>Welcome to .Net AI Hub</h1>
      <div className={styles.dynamicWrapper}>
        <AnimatePresence mode="wait">
          <motion.p
            key={index}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            transition={{ duration: 0.5 }}
            className={styles.dynamicText}
          >
            {greetings[index]}
          </motion.p>
        </AnimatePresence>
      </div>
    </div>
  );
}
