import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { vscDarkPlus } from "react-syntax-highlighter/dist/esm/styles/prism";
import styles from "./MarkdownRenderer.module.css";

const MarkdownRenderer = ({ content }) => {
  const copyToClipboard = async (text) => {
    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      console.error("Failed to copy text: ", err);
    }
  };

  return (
    <div className={styles.markdown}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          // Tables
          table({ children }) {
            return (
              <div className={styles.tableWrapper}>
                <table className={styles.table}>{children}</table>
              </div>
            );
          },
          thead({ children }) {
            return <thead className={styles.tableHead}>{children}</thead>;
          },
          tbody({ children }) {
            return <tbody className={styles.tableBody}>{children}</tbody>;
          },
          tr({ children }) {
            return <tr className={styles.tableRow}>{children}</tr>;
          },
          th({ children }) {
            return <th className={styles.tableHeaderCell}>{children}</th>;
          },
          td({ children }) {
            return <td className={styles.tableCell}>{children}</td>;
          },
          // Code blocks (```language)
          code({ inline, className, children, ...props }) {
            const match = /language-(\w+)/.exec(className || "");
            const language = match ? match[1] : "";
            const codeString = String(children).replace(/\n$/, "");

            // Force inline for short code without newlines and common method patterns
            const shouldForceInline =
              codeString.length < 50 &&
              !codeString.includes("\n") &&
              !codeString.includes(";") &&
              !language; // No explicit language means it's likely inline

            return !inline && !shouldForceInline ? (
              <div className={styles.codeBlock}>
                <div className={styles.codeHeader}>
                  <span className={styles.codeLanguage}>
                    {language || "text"}
                  </span>
                  <button
                    className={styles.copyButton}
                    onClick={() => copyToClipboard(codeString)}
                    title="Copy code"
                  >
                    <svg
                      width="16"
                      height="16"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                    >
                      <rect
                        x="9"
                        y="9"
                        width="13"
                        height="13"
                        rx="2"
                        ry="2"
                      ></rect>
                      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                    </svg>
                  </button>
                </div>
                <SyntaxHighlighter
                  style={vscDarkPlus}
                  language={language}
                  PreTag="div"
                  customStyle={{
                    margin: 0,
                    borderRadius: "0 0 8px 8px",
                    background: "#1e1e1e",
                  }}
                  codeTagProps={{
                    style: {
                      fontSize: "14px",
                      fontFamily:
                        "'Fira Code', 'Monaco', 'Cascadia Code', 'Roboto Mono', monospace",
                    },
                  }}
                  {...props}
                >
                  {codeString}
                </SyntaxHighlighter>
              </div>
            ) : (
              <code className={styles.inlineCode} {...props}>
                {children}
              </code>
            );
          },
          // Paragraphs
          p({ children }) {
            return <p className={styles.paragraph}>{children}</p>;
          },
          // Headings
          h1({ children }) {
            return <h1 className={styles.heading1}>{children}</h1>;
          },
          h2({ children }) {
            return <h2 className={styles.heading2}>{children}</h2>;
          },
          h3({ children }) {
            return <h3 className={styles.heading3}>{children}</h3>;
          },
          // Lists
          ul({ children }) {
            return <ul className={styles.unorderedList}>{children}</ul>;
          },
          ol({ children }) {
            return <ol className={styles.orderedList}>{children}</ol>;
          },
          li({ children }) {
            return <li className={styles.listItem}>{children}</li>;
          },
          // Links
          a({ href, children }) {
            return (
              <a
                href={href}
                className={styles.link}
                target="_blank"
                rel="noopener noreferrer"
              >
                {children}
              </a>
            );
          },
          // Blockquotes
          blockquote({ children }) {
            return (
              <blockquote className={styles.blockquote}>{children}</blockquote>
            );
          },
          // Strong (bold)
          strong({ children }) {
            return <strong className={styles.strong}>{children}</strong>;
          },
          // Emphasis (italic)
          em({ children }) {
            return <em className={styles.emphasis}>{children}</em>;
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
};

export default MarkdownRenderer;
