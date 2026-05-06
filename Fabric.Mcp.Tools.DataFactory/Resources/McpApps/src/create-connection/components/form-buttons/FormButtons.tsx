/**
 * FormButtons - Submit and Cancel buttons.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { BaseStyles } from "../../../shared";
import { FormButtonsStyles as styles } from "./FormButtons.styles";
import type { FormButtonsProps } from "./types";

export class FormButtons extends Component<FormButtonsProps> {
  render(): ReactNode {
    const {
      onSubmit,
      onBack,
      onCancel,
      isSubmitting,
      submitDisabled,
      submitLabel = "Create",
    } = this.props;

    const submitText = isSubmitting ? "Creating..." : submitLabel;

    return (
      <div style={styles.container}>
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            disabled={isSubmitting}
            style={{
              ...BaseStyles.buttonSecondary,
              ...(isSubmitting ? BaseStyles.buttonDisabled : {}),
            }}
          >
            Back
          </button>
        )}
        <button
          type="button"
          onClick={onSubmit}
          disabled={submitDisabled || isSubmitting}
          style={{
            ...BaseStyles.button,
            ...(submitDisabled || isSubmitting
              ? BaseStyles.buttonDisabled
              : {}),
          }}
        >
          {submitText}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={isSubmitting}
          style={{
            ...BaseStyles.buttonSecondary,
            ...(isSubmitting ? BaseStyles.buttonDisabled : {}),
          }}
        >
          Cancel
        </button>
      </div>
    );
  }
}
