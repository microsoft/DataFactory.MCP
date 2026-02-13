/**
 * LoadingSpinner - Simple loading indicator with message.
 * Class component.
 */

import { Component, ReactNode } from "react";

export interface LoadingSpinnerProps {
  message?: string;
}

export class LoadingSpinner extends Component<LoadingSpinnerProps> {
  render(): ReactNode {
    const { message = "Loading..." } = this.props;

    return (
      <div style={styles.container}>
        <div style={styles.spinner}>⏳</div>
        <div style={styles.message}>{message}</div>
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    padding: "40px",
    color: "var(--vscode-foreground, #cccccc)",
  },
  spinner: {
    fontSize: "2rem",
    marginBottom: "12px",
  },
  message: {
    fontSize: "0.9rem",
    opacity: 0.8,
  },
};
