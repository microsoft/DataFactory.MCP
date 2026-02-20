/**
 * Types for the ConnectionDetailFields component.
 */

import { DataSourceLabel } from "../../services/types";

export interface ConnectionDetailFieldsProps {
  labels: DataSourceLabel[];
  values: Record<string, string>;
  onChange: (fieldName: string, value: string) => void;
  disabled?: boolean;
}
