import { Component, ReactNode } from "react";
import { App as McpApp } from "@modelcontextprotocol/ext-apps";

/**
 * Props for McpAppComponent
 */
export interface McpAppComponentProps {
  appName: string;
  appVersion?: string;
}

/**
 * State for McpAppComponent
 */
export interface McpAppComponentState {
  isConnected: boolean;
  connectionError: string | null;
}

/**
 * Base class for MCP App React components.
 * Handles MCP SDK connection lifecycle automatically.
 *
 * Extend this class and override renderContent() to build your UI.
 */
export abstract class McpAppComponent<
  P extends McpAppComponentProps = McpAppComponentProps,
  S extends McpAppComponentState = McpAppComponentState,
> extends Component<P, S> {
  protected mcpApp: McpApp;

  constructor(props: P) {
    super(props);

    // Create MCP App instance
    this.mcpApp = new McpApp({
      name: props.appName,
      version: props.appVersion || "1.0.0",
    });

    // Initialize base state - subclasses should call super and extend
    this.state = {
      isConnected: false,
      connectionError: null,
    } as S;
  }

  componentDidMount(): void {
    // Set up tool result handler
    this.mcpApp.ontoolresult = (result) => {
      console.log(`[${this.props.appName}] Received tool result:`, result);
      this.onToolResult(result);
    };

    // Connect to MCP host
    this.mcpApp
      .connect()
      .then(() => {
        console.log(`[${this.props.appName}] Connected to MCP host`);
        this.setState({
          isConnected: true,
          connectionError: null,
        } as Partial<S> as S);
        this.onConnected();
      })
      .catch((error: Error) => {
        console.error(`[${this.props.appName}] Failed to connect:`, error);
        this.setState({
          isConnected: false,
          connectionError: error.message,
        } as Partial<S> as S);
        this.onConnectionError(error);
      });
  }

  /**
   * Called when MCP host connection succeeds.
   * Override in subclass to perform actions after connection.
   */
  protected onConnected(): void {
    // Override in subclass if needed
  }

  /**
   * Called when MCP host connection fails.
   * Override in subclass to handle connection errors.
   */
  protected onConnectionError(error: Error): void {
    // Override in subclass if needed
  }

  /**
   * Called when a tool result is received from the MCP host.
   * Override in subclass to handle initial data from the tool.
   */
  protected onToolResult(result: unknown): void {
    // Override in subclass if needed
  }

  /**
   * Call an MCP server tool and extract the parsed result.
   * Handles MCP CallToolResult shape: { content: [{ type: "text", text: "JSON" }] }
   */
  protected async callServerTool(
    toolName: string,
    args: Record<string, unknown>,
  ): Promise<unknown> {
    console.log(`[${this.props.appName}] Calling tool: ${toolName}`, args);
    const result = await this.mcpApp.callServerTool({
      name: toolName,
      arguments: args,
    });

    // If SDK returned a string directly, parse it
    if (typeof result === "string") {
      try {
        return JSON.parse(result);
      } catch {
        return result;
      }
    }

    // If SDK returned a CallToolResult object with content array, extract text
    if (result && typeof result === "object") {
      const obj = result as Record<string, unknown>;
      if (Array.isArray(obj.content) && obj.content.length > 0) {
        const firstContent = obj.content[0] as Record<string, unknown>;
        if (
          firstContent?.type === "text" &&
          typeof firstContent.text === "string"
        ) {
          try {
            return JSON.parse(firstContent.text);
          } catch {
            return firstContent.text;
          }
        }
      }
    }

    return result;
  }

  /**
   * Update the model context (send data back to the agent).
   */
  protected async updateModelContext(
    data: Record<string, unknown>,
  ): Promise<void> {
    console.log(`[${this.props.appName}] Updating model context:`, data);
    await this.mcpApp.updateModelContext({
      content: [
        {
          type: "text",
          text: JSON.stringify(data, null, 2),
        },
      ],
    });
  }

  /**
   * Override this method to render your app's content.
   */
  protected abstract renderContent(): ReactNode;

  render(): ReactNode {
    const { connectionError } = this.state;

    if (connectionError) {
      return (
        <div style={styles.errorContainer}>
          <div style={styles.errorMessage}>
            Connection Error: {connectionError}
          </div>
        </div>
      );
    }

    return this.renderContent();
  }
}

const styles = {
  errorContainer: {
    padding: "20px",
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
  },
  errorMessage: {
    padding: "12px 16px",
    background: "var(--vscode-inputValidation-errorBackground, #5a1d1d)",
    border: "1px solid var(--vscode-inputValidation-errorBorder, #be1100)",
    borderRadius: "4px",
    color: "var(--vscode-foreground, #cccccc)",
  },
};
