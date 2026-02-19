/**
 * FormButtons - Submit and Cancel buttons.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../../shared";
import { formButtonsStyles as styles } from "./styles";
import type { FormButtonsProps } from "./types";

export class FormButtons extends Component<FormButtonsProps> {
  render(): ReactNode {
    const { onSubmit, onCancel, isSubmitting, submitDisabled } = this.props;

    return (
      <div style={styles.container}>
        <button
          type="button"
          onClick={onSubmit}
          disabled={submitDisabled || isSubmitting}
          style={{
            ...baseStyles.button,
            ...(submitDisabled || isSubmitting
              ? baseStyles.buttonDisabled
              : {}),
          }}
        >
          {isSubmitting ? "Creating..." : "Create"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={isSubmitting}
          style={{
            ...baseStyles.buttonSecondary,
            ...(isSubmitting ? baseStyles.buttonDisabled : {}),
          }}
        >
          Cancel
        </button>
      </div>
    );
  }
}
