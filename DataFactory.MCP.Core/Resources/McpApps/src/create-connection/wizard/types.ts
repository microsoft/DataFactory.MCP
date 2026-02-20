/**
 * Shared types for the Create Connection wizard steps.
 */

export type WizardStep = "mode" | "details" | "credentials";

/** Ordered list used for back/forward navigation. */
export const WIZARD_STEPS: WizardStep[] = ["mode", "details", "credentials"];
