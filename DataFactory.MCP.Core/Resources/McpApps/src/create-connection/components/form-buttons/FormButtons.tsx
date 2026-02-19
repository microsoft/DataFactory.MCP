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
              ...baseStyles.buttonSecondary,
              ...(isSubmitting ? baseStyles.buttonDisabled : {}),
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
            ...baseStyles.button,
            ...(submitDisabled || isSubmitting
              ? baseStyles.buttonDisabled
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
