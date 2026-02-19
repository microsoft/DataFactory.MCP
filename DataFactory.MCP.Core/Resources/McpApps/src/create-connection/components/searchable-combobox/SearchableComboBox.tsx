/**
 * SearchableComboBox - A combobox with search/filter functionality.
 * Replaces plain <select> elements to allow users to type and filter options.
 * Class component.
 */

import { Component, ReactNode, createRef } from "react";
import { baseStyles } from "../../../shared";
import { comboBoxStyles as styles } from "./styles";
import { filterOptions, getDisplayText, clampIndex } from "./helpers";
import type {
  ComboBoxOption,
  SearchableComboBoxProps,
  SearchableComboBoxState,
} from "./types";

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

  private handleInputChange(e: React.ChangeEvent<HTMLInputElement>): void {
    this.setState({
      searchText: e.target.value,
      isOpen: true,
      highlightedIndex: 0,
    });
    if (this.props.selectedValue) {
      this.props.onSelect("");
    }
  }

  private handleInputFocus(): void {
    this.setState({ isOpen: true, highlightedIndex: -1, searchText: "" });
  }

  private handleInputKeyDown(e: React.KeyboardEvent<HTMLInputElement>): void {
    const filtered = filterOptions(this.props.options, this.state.searchText);
    const { highlightedIndex, isOpen } = this.state;

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        if (isOpen) {
          this.setState({
            highlightedIndex: clampIndex(highlightedIndex + 1, filtered.length),
          });
        } else {
          this.setState({ isOpen: true, highlightedIndex: 0 });
        }
        break;
      case "ArrowUp":
        e.preventDefault();
        this.setState({
          highlightedIndex: clampIndex(highlightedIndex - 1, filtered.length),
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
      options,
      selectedValue,
      isLoading = false,
      disabled = false,
      label,
      required = false,
      placeholder = "Search...",
      loadingText = "Loading...",
    } = this.props;
    const { searchText, isOpen, highlightedIndex } = this.state;
    const filtered = filterOptions(options, searchText);
    const displayText = getDisplayText(selectedValue, options);
    const showDropdown = isOpen && !isLoading && !disabled;

    return (
      <div style={baseStyles.formGroup}>
        <label htmlFor={id} style={baseStyles.label}>
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
              style={baseStyles.input}
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
