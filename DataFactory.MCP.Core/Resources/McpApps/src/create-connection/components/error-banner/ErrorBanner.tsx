/**
 * ErrorBanner - Displays error messages with optional retry button.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../../shared";
import { errorBannerStyles as styles } from "./styles";
import type { ErrorBannerProps } from "./types";

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
