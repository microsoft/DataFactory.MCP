/**
 * Types for the FormButtons component.
 */

export interface FormButtonsProps {
  onSubmit: () => void;
  onCancel: () => void;
  isSubmitting: boolean;
  submitDisabled: boolean;
  /** If provided, renders a Back button before the submit button */
  onBack?: () => void;
  /** Label for the submit button. Defaults to "Create" */
  submitLabel?: string;
}
