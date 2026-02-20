/**
 * LoadingSpinner - Simple loading indicator with message.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { LoadingSpinnerStyles as styles } from "./LoadingSpinner.styles";
import type { LoadingSpinnerProps } from "./types";

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
