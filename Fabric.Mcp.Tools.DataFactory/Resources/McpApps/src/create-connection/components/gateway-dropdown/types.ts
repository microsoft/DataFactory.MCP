/**
 * Types for the GatewayDropdown component.
 */

import { Gateway } from "../../services/types";

export interface GatewayDropdownProps {
  gateways: Gateway[];
  selectedGatewayId: string | null;
  onSelect: (gatewayId: string) => void;
  isLoading: boolean;
  disabled?: boolean;
  label?: string;
}
