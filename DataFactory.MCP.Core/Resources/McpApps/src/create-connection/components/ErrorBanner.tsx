/**
 * ErrorBanner - Displays error messages with optional retry button.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../shared";

export interface ErrorBannerProps {
  message: string;
  onRetry?: () => void;
}

export class ErrorBanner extends Component<ErrorBannerProps> {
  render(): ReactNode {
    const { message, onRetry } = this.props;

    return (
      <div style={baseStyles.statusError}>
        <div style={styles.message}>⚠️ {message}</div>
        {onRetry && (
          <button type="button" onClick={onRetry} style={styles.retryButton}>
            Retry
          </button>
        )}
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  message: {
    fontSize: "0.85rem",
    marginBottom: "8px",
  },
  retryButton: {
    padding: "4px 12px",
    background: "var(--vscode-button-background, #0e639c)",
    color: "var(--vscode-button-foreground, #ffffff)",
    border: "none",
    borderRadius: "4px",
    fontSize: "0.8rem",
    cursor: "pointer",
  },
};
