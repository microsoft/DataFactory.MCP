/**
 * WizardStepIndicator - breadcrumb-style step tracker.
 * Past steps are clickable links; current step is bold; future steps are muted.
 */

import { WizardStep, WIZARD_STEPS } from "./types";
import { WizardStepIndicatorStyles as styles } from "./WizardStepIndicator.styles";

interface WizardStepIndicatorProps {
  currentStep: WizardStep;
  onStepClick: (step: WizardStep) => void;
}

const STEPS: [WizardStep, string][] = [
  ["mode", "Mode"],
  ["details", "Details"],
  ["credentials", "Credentials"],
];

export function WizardStepIndicator({
  currentStep,
  onStepClick,
}: WizardStepIndicatorProps) {
  const currentIdx = WIZARD_STEPS.indexOf(currentStep);
  return (
    <div style={styles.container}>
      {STEPS.map(([step, label], i) => {
        const stepIdx = WIZARD_STEPS.indexOf(step);
        const isCurrent = step === currentStep;
        const isPast = stepIdx < currentIdx;
        return (
          <span key={step} style={styles.stepWrapper}>
            {i > 0 && <span style={styles.divider}>›</span>}
            <button
              type="button"
              onClick={() => onStepClick(step)}
              disabled={!isPast}
              style={styles.stepButton(isCurrent, isPast)}
            >
              {label}
            </button>
          </span>
        );
      })}
    </div>
  );
}
