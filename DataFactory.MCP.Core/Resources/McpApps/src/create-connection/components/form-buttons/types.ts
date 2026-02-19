/**
 * Types for the FormButtons component.
 */

export interface FormButtonsProps {
  onSubmit: () => void;
  onCancel: () => void;
  isSubmitting: boolean;
  submitDisabled: boolean;
}
