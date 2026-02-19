/**
 * WizardStepIndicator - breadcrumb-style step tracker.
 * Past steps are clickable links; current step is bold; future steps are muted.
 */

import { CSSProperties } from "react";
import { WizardStep } from "./types";

interface WizardStepIndicatorProps {
  currentStep: WizardStep;
  onStepClick: (step: WizardStep) => void;
}

const STEPS: [WizardStep, string][] = [
  [0, "Mode"],
  [1, "Details"],
  [2, "Credentials"],
];

const dividerStyle: CSSProperties = {
  color: "var(--vscode-descriptionForeground, #888)",
  margin: "0 3px",
  fontSize: "0.7rem",
};

export function WizardStepIndicator({
  currentStep,
  onStepClick,
}: WizardStepIndicatorProps) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        marginBottom: "12px",
        fontSize: "0.75rem",
      }}
    >
      {STEPS.map(([idx, label], i) => (
        <span key={idx} style={{ display: "flex", alignItems: "center" }}>
          {i > 0 && <span style={dividerStyle}>›</span>}
          <button
            type="button"
            onClick={() => onStepClick(idx)}
            disabled={idx >= currentStep}
            style={{
              background: "none",
              border: "none",
              padding: "2px 4px",
              cursor: idx < currentStep ? "pointer" : "default",
              color:
                idx === currentStep
                  ? "var(--vscode-foreground, #ccc)"
                  : idx < currentStep
                    ? "var(--vscode-textLink-foreground, #3794ff)"
                    : "var(--vscode-descriptionForeground, #888)",
              fontWeight: idx === currentStep ? 600 : 400,
              fontSize: "0.75rem",
              textDecoration: idx < currentStep ? "underline" : "none",
            }}
          >
            {label}
          </button>
        </span>
      ))}
    </div>
  );
}
