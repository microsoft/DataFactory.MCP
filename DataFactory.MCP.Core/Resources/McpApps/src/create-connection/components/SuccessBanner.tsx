/**
 * SuccessBanner - Displays success message after connection creation.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../shared";

export interface SuccessBannerProps {
  connectionId: string;
  connectionName: string;
}

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

const styles: Record<string, React.CSSProperties> = {
  title: {
    fontWeight: 600,
    marginBottom: "8px",
    fontSize: "1rem",
  },
  detail: {
    fontSize: "0.85rem",
    marginBottom: "4px",
    color: "var(--vscode-foreground, #cccccc)",
  },
};
