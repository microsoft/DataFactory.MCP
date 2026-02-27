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

Evals run automatically as a **GitHub Actions workflow** on PRs that touch `evals/`, `claude-skills/`, or `DataFactory.MCP.Core/Tools/`.

### What the workflow does

| Job | Runs when | What it does |
|---|---|---|
| **Parse check** | Every matching PR | Validates all eval files parse correctly (no API key needed) |
| **Tool-selection evals** | `OPENAI_API_KEY` secret set | Runs 94 scenarios, scores tool selection + params |
| **Integration evals** | `OPENAI_API_KEY` secret set | Tests M code quality baseline vs with skills |
| **Report** | After LLM evals | Posts score summary to GitHub Actions step summary |

Results are uploaded as workflow artifacts (`tool_selection_results.json`, `integration_eval_results.json`).

### Required secret

Add `OPENAI_API_KEY` to the repo secrets to enable LLM evals. Without it, only the parse check runs.

### Running locally (optional)

```bash
# Parse check only (no API key)
python evals/run_evals.py --dry-run
python evals/integration/run_integration_evals.py --dry-run

# Full run
OPENAI_API_KEY=sk-... python evals/run_evals.py
OPENAI_API_KEY=sk-... python evals/integration/run_integration_evals.py
```

### Files

| File | Purpose |
|---|---|
| `.github/workflows/ai-evals.yml` | GitHub Actions workflow |
| `evals/run_evals.py` | Tool-selection runner (parses markdown, calls LLM, scores) |
| `evals/integration/run_integration_evals.py` | Integration runner (M code quality, baseline vs skills) |
| `evals/integration/m-code-quality.eval.md` | 20 integration eval scenarios |
| `evals/tools_schema.json` | 32 tool definitions (OpenAI function-calling format) |

---

## Total Eval Count

### Tool Selection Evals

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

### Integration Evals (M Code Quality)

| Category | Count |
|---|---|
| M Syntax | 4 |
| Destinations | 3 |
| Performance | 3 |
| Advanced (Fast Copy, Action.Sequence) | 3 |
| Pipeline JSON | 2 |
| Workflow | 3 |
| Lifecycle | 2 |
| **Total** | **20** |

**Grand total: 114 evals** (94 tool-selection + 20 integration)
