import { useState, useEffect, useRef } from "react";
import { App as McpApp } from "@modelcontextprotocol/ext-apps";

// Styles
const styles = {
  body: {
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    background: "var(--vscode-editor-background, #1e1e1e)",
    color: "var(--vscode-foreground, #cccccc)",
    padding: "20px",
    lineHeight: 1.5,
    margin: 0,
    minHeight: "100vh",
    boxSizing: "border-box" as const,
  },
  container: {
    maxWidth: "400px",
    margin: "0 auto",
  },
  h1: {
    fontSize: "1.25rem",
    marginBottom: "16px",
  },
  formGroup: {
    marginBottom: "16px",
  },
  label: {
    display: "block",
    marginBottom: "6px",
    fontWeight: 500,
  },
  textarea: {
    width: "100%",
    padding: "10px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-input-foreground, #cccccc)",
    fontFamily: "inherit",
    fontSize: "0.875rem",
    resize: "vertical" as const,
    minHeight: "100px",
    boxSizing: "border-box" as const,
  },
  button: {
    padding: "10px 20px",
    background: "var(--vscode-button-background, #0e639c)",
    color: "var(--vscode-button-foreground, #ffffff)",
    border: "none",
    borderRadius: "4px",
    fontSize: "0.875rem",
    cursor: "pointer",
  },
  buttonDisabled: {
    opacity: 0.6,
    cursor: "not-allowed",
  },
  status: {
    marginTop: "16px",
    padding: "10px",
    borderRadius: "4px",
    fontSize: "0.875rem",
  },
  statusSuccess: {
    background: "var(--vscode-inputValidation-infoBackground, #063b49)",
    border: "1px solid var(--vscode-inputValidation-infoBorder, #007acc)",
  },
  statusError: {
    background: "var(--vscode-inputValidation-errorBackground, #5a1d1d)",
    border: "1px solid var(--vscode-inputValidation-errorBorder, #be1100)",
  },
};

export function App() {
  const [input, setInput] = useState("");
  const [status, setStatus] = useState<{
    message: string;
    type: "success" | "error";
  } | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isConnected, setIsConnected] = useState(false);
  const appRef = useRef<McpApp | null>(null);

  useEffect(() => {
    // Apply body styles
    Object.assign(document.body.style, styles.body);

    // Create and connect MCP App
    const mcpApp = new McpApp({ name: "User Input", version: "1.0.0" });
    appRef.current = mcpApp;

    mcpApp.ontoolresult = (result) => {
      console.log("[User Input] Received tool result:", result);
    };

    mcpApp
      .connect()
      .then(() => {
        console.log("[User Input] Connected to MCP host");
        setIsConnected(true);
      })
      .catch((error) => {
        console.error("[User Input] Failed to connect:", error);
      });

    return () => {
      appRef.current = null;
    };
  }, []);

  const handleSubmit = async () => {
    if (!input.trim()) {
      setStatus({ message: "Please enter some text", type: "error" });
      return;
    }

    setIsSubmitting(true);
    setStatus(null);

    try {
      const app = appRef.current;
      if (!app) {
        throw new Error("MCP App not initialized");
      }

      console.log("[User Input] Sending to agent context:", input);

      await app.updateModelContext({
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                userInput: input.trim(),
                timestamp: new Date().toISOString(),
              },
              null,
              2,
            ),
          },
        ],
      });

      setStatus({ message: "Sent to agent!", type: "success" });
      setInput("");

      // Auto-hide success after 3s
      setTimeout(() => setStatus(null), 3000);
    } catch (error) {
      console.error("[User Input] Error:", error);
      setStatus({
        message: `Error: ${error instanceof Error ? error.message : String(error)}`,
        type: "error",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div style={styles.container}>
      <h1 style={styles.h1}>Enter Your Input</h1>

      <div style={styles.formGroup}>
        <label style={styles.label}>Message for Agent</label>
        <textarea
          style={styles.textarea}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Type your message here..."
        />
      </div>

      <button
        style={{
          ...styles.button,
          ...(isSubmitting ? styles.buttonDisabled : {}),
        }}
        onClick={handleSubmit}
        disabled={isSubmitting}
      >
        {isSubmitting ? "Sending..." : "Send to Agent"}
      </button>

      {status && (
        <div
          style={{
            ...styles.status,
            ...(status.type === "success"
              ? styles.statusSuccess
              : styles.statusError),
          }}
        >
          {status.message}
        </div>
      )}

      {!isConnected && (
        <div
          style={{ ...styles.status, ...styles.statusError, marginTop: "16px" }}
        >
          Connecting to MCP host...
        </div>
      )}
    </div>
  );
}
