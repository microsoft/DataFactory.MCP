# AI Evals for Data Factory MCP Tools

Structured evaluation scenarios for testing how well an LLM selects, parameterizes, and orchestrates MCP tools.

## Structure

Each `*.eval.md` file covers a functional area and contains three eval categories:

| Category | What it tests |
|---|---|
| **Tool Selection** | Given a user prompt, does the model pick the correct tool? |
| **Parameter Extraction** | Does the model supply correct arguments from conversational context? |
| **Edge Cases** | Ambiguous prompts, missing context, error recovery, validation boundaries |

Cross-tool workflows live in `multi-step.eval.md`.

## Eval File Index

| File | Tools Covered |
|---|---|
| [authentication.eval.md](authentication.eval.md) | `AuthenticateServicePrincipalAsync`, `AuthenticateInteractiveAsync`, `StartDeviceCodeAuthAsync`, `CheckDeviceAuthStatusAsync`, `GetAuthenticationStatus`, `SignOutAsync`, `GetAccessTokenAsync` |
| [workspace-capacity.eval.md](workspace-capacity.eval.md) | `ListWorkspacesAsync`, `ListCapacitiesAsync` |
| [connections.eval.md](connections.eval.md) | `ListSupportedConnectionTypesAsync`, `ListConnectionsAsync`, `GetConnectionAsync`, `CreateConnectionAsync`, `create_connection_ui` |
| [gateways.eval.md](gateways.eval.md) | `ListGatewaysAsync`, `GetGatewayAsync`, `create_virtualnetwork_gateway` |
| [dataflows.eval.md](dataflows.eval.md) | `ListDataflowsAsync`, `CreateDataflowAsync`, `AddConnectionToDataflowAsync`, `AddOrUpdateQueryInDataflowAsync`, `get_dataflow_definition`, `save_dataflow_definition`, `ExecuteQueryAsync`, `RefreshDataflowBackground`, `RefreshDataflowStatus` |
| [pipelines.eval.md](pipelines.eval.md) | `ListPipelinesAsync`, `CreatePipelineAsync`, `GetPipelineAsync`, `GetPipelineDefinitionAsync`, `UpdatePipelineAsync`, `UpdatePipelineDefinitionAsync` |
| [multi-step.eval.md](multi-step.eval.md) | Cross-tool orchestration scenarios |

## Scenario Format

Each scenario follows this template:

```
### EVAL-{AREA}-{NUMBER}: {Title}

**Category:** Tool Selection | Parameter Extraction | Edge Case
**Difficulty:** Easy | Medium | Hard

**User prompt:**
> {What the user says}

**Context:** (optional)
> {Prior conversation, returned data, or system state}

**Expected tool call(s):**
- Tool: `{ToolName}`
  - `param1`: {expected value or description}
  - `param2`: {expected value or description}

**Assertions:**
- {What must be true about the model's response}

**Notes:** (optional)
> {Why this scenario matters, common failure modes}
```

## Scoring Guide

| Result | Meaning |
|---|---|
| ✅ Pass | Correct tool selected with valid parameters |
| ⚠️ Partial | Right tool, wrong/missing parameters — or unnecessary extra tool calls |
| ❌ Fail | Wrong tool selected, hallucinated parameters, or skipped tool call entirely |

## Running Evals

### Prerequisites

- Python 3.11+
- An OpenAI-compatible API key (set `OPENAI_API_KEY`)

### Commands

```bash
# Dry run — parse all scenarios, no LLM calls
python evals/run_evals.py --dry-run

# Run all 94 evals
OPENAI_API_KEY=sk-... python evals/run_evals.py

# Run a specific file
python evals/run_evals.py --file authentication

# Run a single scenario
python evals/run_evals.py --eval EVAL-AUTH-001

# Filter by category or difficulty
python evals/run_evals.py --category "Tool Selection"
python evals/run_evals.py --difficulty Hard

# Use a different model or API
EVAL_MODEL=gpt-4o-mini python evals/run_evals.py
EVAL_BASE_URL=http://localhost:11434/v1 EVAL_MODEL=llama3 python evals/run_evals.py
```

### Output

Results are saved to `eval_results.json` with per-scenario details:
```json
{
  "eval_id": "EVAL-AUTH-001",
  "result": "pass",
  "expected_tools": [{"name": "AuthenticateInteractiveAsync", "params": {}}],
  "actual_tools": [{"name": "AuthenticateInteractiveAsync", "arguments": {}}]
}
```

### Files

| File | Purpose |
|---|---|
| `run_evals.py` | Runner script — parses markdown, calls LLM, scores results |
| `tools_schema.json` | Tool definitions in OpenAI function-calling format (32 tools) |
| `eval_results.json` | Generated after a run — detailed per-scenario results |

---

## Total Eval Count

| Area | Tool Selection | Param Extraction | Edge Cases | Total |
|---|---|---|---|---|
| Authentication | 7 | 3 | 4 | 14 |
| Workspace & Capacity | 3 | 3 | 3 | 9 |
| Connections | 5 | 5 | 5 | 15 |
| Gateways | 3 | 3 | 3 | 9 |
| Dataflows | 9 | 7 | 6 | 22 |
| Pipelines | 6 | 5 | 4 | 15 |
| Multi-step | — | — | — | 10 |
| **Total** | **33** | **26** | **25** | **94** |
