/**
 * SearchableComboBox - A combobox with search/filter functionality.
 * Replaces plain <select> elements to allow users to type and filter options.
 * Class component.
 */

import { Component, ReactNode, createRef } from "react";
import { BaseStyles } from "../../shared";

export interface ComboBoxOption {
  value: string;
  label: string;
}

export interface SearchableComboBoxProps {
  id: string;
  options: ComboBoxOption[];
  selectedValue: string;
  onSelect: (value: string) => void;
  placeholder?: string;
  loadingText?: string;
  isLoading?: boolean;
  disabled?: boolean;
  label: string;
  required?: boolean;
}

interface SearchableComboBoxState {
  searchText: string;
  isOpen: boolean;
  highlightedIndex: number;
}

export class SearchableComboBox extends Component<
  SearchableComboBoxProps,
  SearchableComboBoxState
> {
  private readonly containerRef = createRef<HTMLDivElement>();
  private readonly inputRef = createRef<HTMLInputElement>();

  constructor(props: SearchableComboBoxProps) {
    super(props);
    this.state = {
      searchText: "",
      isOpen: false,
      highlightedIndex: -1,
    };
    this.handleInputChange = this.handleInputChange.bind(this);
    this.handleInputFocus = this.handleInputFocus.bind(this);
    this.handleInputKeyDown = this.handleInputKeyDown.bind(this);
    this.handleOptionClick = this.handleOptionClick.bind(this);
    this.handleDocumentClick = this.handleDocumentClick.bind(this);
    this.handleClear = this.handleClear.bind(this);
  }

  componentDidMount(): void {
    document.addEventListener("mousedown", this.handleDocumentClick);
  }

  componentWillUnmount(): void {
    document.removeEventListener("mousedown", this.handleDocumentClick);
  }

  private handleDocumentClick(e: MouseEvent): void {
    if (
      this.containerRef.current &&
      !this.containerRef.current.contains(e.target as Node)
    ) {
      this.setState({ isOpen: false, highlightedIndex: -1 });
    }
  }

  private getFilteredOptions(): ComboBoxOption[] {
    const { options } = this.props;
    const { searchText } = this.state;
    if (!searchText) return options;
    const lower = searchText.toLowerCase();
    return options.filter((opt) => opt.label.toLowerCase().includes(lower));
  }

  private getDisplayText(): string {
    const { selectedValue, options } = this.props;
    if (!selectedValue) return "";
    const found = options.find((o) => o.value === selectedValue);
    return found ? found.label : selectedValue;
  }

  private handleInputChange(e: React.ChangeEvent<HTMLInputElement>): void {
    this.setState({
      searchText: e.target.value,
      isOpen: true,
      highlightedIndex: 0,
    });
    // Clear selection when user types
    if (this.props.selectedValue) {
      this.props.onSelect("");
    }
  }

  private handleInputFocus(): void {
    this.setState({ isOpen: true, highlightedIndex: -1, searchText: "" });
  }

  private handleInputKeyDown(e: React.KeyboardEvent<HTMLInputElement>): void {
    const filtered = this.getFilteredOptions();
    const { highlightedIndex, isOpen } = this.state;

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        if (isOpen) {
          this.setState({
            highlightedIndex: Math.min(
              highlightedIndex + 1,
              filtered.length - 1,
            ),
          });
        } else {
          this.setState({ isOpen: true, highlightedIndex: 0 });
        }
        break;
      case "ArrowUp":
        e.preventDefault();
        this.setState({
          highlightedIndex: Math.max(highlightedIndex - 1, 0),
        });
        break;
      case "Enter":
        e.preventDefault();
        if (isOpen && highlightedIndex >= 0 && filtered[highlightedIndex]) {
          this.selectOption(filtered[highlightedIndex]);
        }
        break;
      case "Escape":
        this.setState({ isOpen: false, highlightedIndex: -1 });
        break;
      case "Tab":
        this.setState({ isOpen: false, highlightedIndex: -1 });
        break;
    }
  }

  private handleOptionClick(option: ComboBoxOption): void {
    this.selectOption(option);
  }

  private selectOption(option: ComboBoxOption): void {
    this.props.onSelect(option.value);
    this.setState({ isOpen: false, searchText: "", highlightedIndex: -1 });
    this.inputRef.current?.blur();
  }

  private handleClear(): void {
    this.props.onSelect("");
    this.setState({ searchText: "", isOpen: false, highlightedIndex: -1 });
    this.inputRef.current?.focus();
  }

  render(): ReactNode {
    const {
      id,
      isLoading = false,
      disabled = false,
      label,
      required = false,
      placeholder = "Search...",
      loadingText = "Loading...",
    } = this.props;
    const { searchText, isOpen, highlightedIndex } = this.state;
    const filtered = this.getFilteredOptions();
    const displayText = this.getDisplayText();
    const showDropdown = isOpen && !isLoading && !disabled;

    return (
      <div style={BaseStyles.formGroup}>
        <label htmlFor={id} style={BaseStyles.label}>
          {label} {required && <span style={styles.required}>*</span>}
        </label>
        <div ref={this.containerRef} style={styles.container}>
          <div style={styles.inputWrapper}>
            <input
              ref={this.inputRef}
              id={id}
              type="text"
              value={displayText && !isOpen ? displayText : searchText}
              onChange={this.handleInputChange}
              onFocus={this.handleInputFocus}
              onKeyDown={this.handleInputKeyDown}
              disabled={disabled || isLoading}
              placeholder={isLoading ? loadingText : placeholder}
              style={BaseStyles.input}
              aria-busy={isLoading}
              autoComplete="off"
            />
            {displayText && !disabled && !isLoading && (
              <button
                type="button"
                onClick={this.handleClear}
                style={styles.clearButton}
                aria-label="Clear selection"
                tabIndex={-1}
              >
                ✕
              </button>
            )}
          </div>
          {showDropdown && (
            <div id={`${id}-listbox`} style={styles.dropdown}>
              {filtered.length === 0 ? (
                <div style={styles.noResults}>No matches found</div>
              ) : (
                filtered.map((option, index) => (
                  <button
                    type="button"
                    key={option.value}
                    tabIndex={-1}
                    data-selected={index === highlightedIndex}
                    style={{
                      ...styles.option,
                      ...(index === highlightedIndex
                        ? styles.optionHighlighted
                        : {}),
                    }}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      this.handleOptionClick(option);
                    }}
                    onMouseEnter={() =>
                      this.setState({ highlightedIndex: index })
                    }
                  >
                    {option.label}
                  </button>
                ))
              )}
            </div>
          )}
        </div>
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  required: {
    color: "var(--vscode-errorForeground, #f48771)",
  },
  container: {
    position: "relative",
  },
  inputWrapper: {
    position: "relative",
    display: "flex",
    alignItems: "center",
  },
  clearButton: {
    position: "absolute",
    right: "8px",
    background: "none",
    border: "none",
    color: "var(--vscode-input-foreground, #cccccc)",
    cursor: "pointer",
    fontSize: "0.75rem",
    padding: "4px",
    opacity: 0.7,
    lineHeight: 1,
  },
  dropdown: {
    position: "absolute",
    top: "100%",
    left: 0,
    right: 0,
    maxHeight: "200px",
    overflowY: "auto",
    background: "var(--vscode-dropdown-background, #3c3c3c)",
    border: "1px solid var(--vscode-dropdown-border, #3c3c3c)",
    borderRadius: "0 0 4px 4px",
    margin: 0,
    padding: 0,
    listStyle: "none",
    zIndex: 1000,
    boxShadow: "0 4px 8px rgba(0,0,0,0.3)",
  },
  option: {
    display: "block",
    width: "100%",
    padding: "8px 10px",
    cursor: "pointer",
    fontSize: "0.875rem",
    color: "var(--vscode-dropdown-foreground, #cccccc)",
    background: "none",
    border: "none",
    textAlign: "left" as const,
    fontFamily: "inherit",
    whiteSpace: "nowrap",
    overflow: "hidden",
    textOverflow: "ellipsis",
    boxSizing: "border-box" as const,
  },
  optionHighlighted: {
    background: "var(--vscode-list-activeSelectionBackground, #094771)",
    color: "var(--vscode-list-activeSelectionForeground, #ffffff)",
  },
  noResults: {
    padding: "8px 10px",
    fontSize: "0.875rem",
    color: "var(--vscode-descriptionForeground, #888)",
    fontStyle: "italic",
  },
};
