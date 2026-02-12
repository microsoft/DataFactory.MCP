import { ReactNode } from "react";
import {
  McpAppComponent,
  McpAppComponentProps,
  McpAppComponentState,
  baseStyles,
  applyBodyStyles,
} from "../shared";

/**
 * State for CreateConnectionApp
 */
interface CreateConnectionAppState extends McpAppComponentState {
  inputText: string;
  isSubmitting: boolean;
  statusMessage: string | null;
  statusType: "success" | "error" | null;
}

/**
 * Create Connection MCP App - Class Component
 *
 * Form for creating data source connections.
 */
export class CreateConnectionApp extends McpAppComponent<
  McpAppComponentProps,
  CreateConnectionAppState
> {
  constructor(props: McpAppComponentProps) {
    super(props);

    // Extend base state with our state
    this.state = {
      ...this.state,
      inputText: "",
      isSubmitting: false,
      statusMessage: null,
      statusType: null,
    };

    // Bind methods
    this.handleInputChange = this.handleInputChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
  }

  componentDidMount(): void {
    super.componentDidMount();
    applyBodyStyles();
  }

  private handleInputChange(
    event: React.ChangeEvent<HTMLTextAreaElement>,
  ): void {
    this.setState({ inputText: event.target.value });
  }

  private async handleSubmit(): Promise<void> {
    const { inputText } = this.state;
    const trimmedInput = inputText.trim();

    if (!trimmedInput) {
      this.setState({
        statusMessage: "Please enter some text",
        statusType: "error",
      });
      return;
    }

    this.setState({
      isSubmitting: true,
      statusMessage: null,
      statusType: null,
    });

    try {
      // Send to agent context
      await this.updateModelContext({
        userInput: trimmedInput,
        timestamp: new Date().toISOString(),
      });

      this.setState({
        inputText: "",
        statusMessage: "Sent to agent!",
        statusType: "success",
        isSubmitting: false,
      });

      // Auto-hide success message after 3 seconds
      setTimeout(() => {
        this.setState({ statusMessage: null, statusType: null });
      }, 3000);
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      this.setState({
        statusMessage: `Error: ${errorMessage}`,
        statusType: "error",
        isSubmitting: false,
      });
    }
  }

  protected renderContent(): ReactNode {
    const { inputText, isSubmitting, statusMessage, statusType } = this.state;

    return (
      <div style={baseStyles.container}>
        <h1 style={baseStyles.h1}>Enter Your Input</h1>

        <div style={baseStyles.formGroup}>
          <label style={baseStyles.label}>Message for Agent</label>
          <textarea
            style={baseStyles.textarea}
            value={inputText}
            onChange={this.handleInputChange}
            placeholder="Type your message here..."
            disabled={isSubmitting}
          />
        </div>

        <button
          style={{
            ...baseStyles.button,
            ...(isSubmitting ? baseStyles.buttonDisabled : {}),
          }}
          onClick={this.handleSubmit}
          disabled={isSubmitting}
        >
          {isSubmitting ? "Sending..." : "Send to Agent"}
        </button>

        {statusMessage && (
          <div
            style={
              statusType === "success"
                ? baseStyles.statusSuccess
                : baseStyles.statusError
            }
          >
            {statusMessage}
          </div>
        )}
      </div>
    );
  }
}
