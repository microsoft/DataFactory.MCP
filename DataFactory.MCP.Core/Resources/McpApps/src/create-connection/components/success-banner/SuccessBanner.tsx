/**
 * SuccessBanner - Displays success message after connection creation.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../../shared";
import { successBannerStyles as styles } from "./styles";
import type { SuccessBannerProps } from "./types";

export class SuccessBanner extends Component<SuccessBannerProps> {
  render(): ReactNode {
    const { connectionId, connectionName } = this.props;

    return (
      <div style={baseStyles.statusSuccess}>
        <div style={styles.title}>✅ Connection Created Successfully</div>
        <div style={styles.detail}>
          <strong>Name:</strong> {connectionName}
        </div>
        <div style={styles.detail}>
          <strong>ID:</strong> {connectionId}
        </div>
      </div>
    );
  }
}
