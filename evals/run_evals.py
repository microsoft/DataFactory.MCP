#!/usr/bin/env python3
"""
AI Eval Runner for Data Factory MCP Tools

Parses eval markdown files, sends scenarios to an LLM with tool definitions,
and scores whether the model selects the correct tools with correct parameters.

Usage:
    python run_evals.py                          # Run all evals
    python run_evals.py --file authentication    # Run one file
    python run_evals.py --eval EVAL-AUTH-001     # Run one scenario
    python run_evals.py --category "Tool Selection"  # Filter by category
    python run_evals.py --dry-run                # Parse only, no LLM calls

Environment variables:
    OPENAI_API_KEY     - API key (required unless --dry-run)
    EVAL_MODEL         - Model to test (default: gpt-4o)
    EVAL_BASE_URL      - API base URL (default: https://api.openai.com/v1)
"""

import argparse
import json
import os
import re
import sys
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


# ---------------------------------------------------------------------------
# Eval scenario model
# ---------------------------------------------------------------------------

@dataclass
class ExpectedToolCall:
    tool_name: str
    parameters: dict[str, str] = field(default_factory=dict)


@dataclass
class EvalScenario:
    eval_id: str
    title: str
    category: str
    difficulty: str
    user_prompt: str
    context: Optional[str]
    expected_tools: list[ExpectedToolCall]
    assertions: list[str]
    notes: Optional[str]
    source_file: str
    # Set after evaluation
    result: Optional[str] = None  # "pass", "partial", "fail", "skip", "error"
    actual_tools: list[dict] = field(default_factory=list)
    explanation: str = ""


# ---------------------------------------------------------------------------
# Markdown parser
# ---------------------------------------------------------------------------

def parse_eval_file(filepath: Path) -> list[EvalScenario]:
    """Parse a single .eval.md file into structured scenarios."""
    text = filepath.read_text()
    scenarios = []

    # Split on ### EVAL- headers
    blocks = re.split(r"(?=^### EVAL-)", text, flags=re.MULTILINE)

    for block in blocks:
        match = re.match(r"^### (EVAL-[\w-]+):\s*(.+)", block)
        if not match:
            continue

        eval_id = match.group(1)
        title = match.group(2).strip()

        category = _extract_field(block, "Category") or "Unknown"
        difficulty = _extract_field(block, "Difficulty") or "Unknown"
        user_prompt = _extract_blockquote(block, "User prompt")
        context = _extract_blockquote(block, "Context")
        assertions = _extract_list(block, "Assertions")
        notes = _extract_blockquote(block, "Notes")
        expected_tools = _extract_expected_tools(block)

        scenarios.append(EvalScenario(
            eval_id=eval_id,
            title=title,
            category=category,
            difficulty=difficulty,
            user_prompt=user_prompt or "",
            context=context,
            expected_tools=expected_tools,
            assertions=assertions,
            notes=notes,
            source_file=filepath.name,
        ))

    return scenarios


def _extract_field(block: str, field_name: str) -> Optional[str]:
    """Extract a **Field:** value line."""
    m = re.search(rf"\*\*{field_name}:\*\*\s*(.+)", block)
    return m.group(1).strip() if m else None


def _extract_blockquote(block: str, section_name: str) -> Optional[str]:
    """Extract text after a **Section:** header, pulling > blockquote lines."""
    pattern = rf"\*\*{section_name}:\*\*.*?\n((?:>.*\n?)+)"
    m = re.search(pattern, block)
    if not m:
        return None
    lines = m.group(1).strip().split("\n")
    return "\n".join(line.lstrip("> ").rstrip() for line in lines).strip()


def _extract_list(block: str, section_name: str) -> list[str]:
    """Extract a bulleted list after a **Section:** header."""
    pattern = rf"\*\*{section_name}:\*\*\s*\n((?:\s*-\s+.+\n?)+)"
    m = re.search(pattern, block)
    if not m:
        return []
    lines = m.group(1).strip().split("\n")
    return [re.sub(r"^\s*-\s+", "", line).strip() for line in lines if line.strip()]


def _extract_expected_tools(block: str) -> list[ExpectedToolCall]:
    """Extract expected tool calls from the **Expected tool call(s):** section."""
    pattern = r"\*\*Expected tool call\(s\):\*\*\s*\n((?:[\s\S]*?)(?=\n\*\*|$))"
    m = re.search(pattern, block)
    if not m:
        return []

    section = m.group(1)
    tools = []
    current_tool = None

    for line in section.split("\n"):
        line = line.strip()
        # Match: - Tool: `ToolName`  or  N. Tool: `ToolName`
        tool_match = re.match(r"(?:\d+\.\s*)?(?:-\s*)?Tool:\s*`([^`]+)`", line)
        if tool_match:
            current_tool = ExpectedToolCall(tool_name=tool_match.group(1))
            tools.append(current_tool)
            continue

        # Match parameter:   - `paramName`: `value` or description
        param_match = re.match(r"-\s*`(\w+)`:\s*(.+)", line)
        if param_match and current_tool:
            param_name = param_match.group(1)
            param_value = param_match.group(2).strip().strip("`")
            current_tool.parameters[param_name] = param_value

    return tools


# ---------------------------------------------------------------------------
# LLM caller
# ---------------------------------------------------------------------------

def _is_azure_openai(base_url: str) -> bool:
    """Check if the base URL points to an Azure OpenAI endpoint."""
    return "openai.azure.com" in base_url


def _build_azure_url(base_url: str, model: str) -> str:
    """Build the Azure OpenAI chat completions URL."""
    base = base_url.rstrip("/")
    # If the URL already contains /openai/deployments, use it as-is
    if "/openai/deployments/" in base:
        return f"{base}/chat/completions?api-version=2024-10-21"
    return f"{base}/openai/deployments/{model}/chat/completions?api-version=2024-10-21"


def call_llm(
    prompt: str,
    tools: list[dict],
    context: Optional[str] = None,
    model: str = "gpt-4o",
    base_url: str = "https://api.openai.com/v1",
    api_key: str = "",
) -> dict:
    """Call the LLM with tool definitions and return the response."""
    import urllib.request

    messages = []

    messages.append({
        "role": "system",
        "content": (
            "You are an AI assistant that helps users work with Microsoft Fabric Data Factory. "
            "You have access to MCP tools for authentication, workspaces, capacities, connections, "
            "gateways, dataflows, and pipelines. Use the appropriate tools to fulfill user requests. "
            "If you need more information, ask the user."
        ),
    })

    if context:
        messages.append({"role": "assistant", "content": f"[Prior context]\n{context}"})

    messages.append({"role": "user", "content": prompt})

    body = {
        "model": model,
        "messages": messages,
        "tools": tools,
        "tool_choice": "auto",
        "temperature": 0,
    }

    is_azure = _is_azure_openai(base_url)

    if is_azure:
        url = _build_azure_url(base_url, model)
        headers = {
            "Content-Type": "application/json",
            "api-key": api_key,
        }
    else:
        url = f"{base_url}/chat/completions"
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}",
        }

    req = urllib.request.Request(
        url,
        data=json.dumps(body).encode(),
        headers=headers,
    )

    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read())
    except Exception as e:
        return {"error": str(e)}


def extract_tool_calls(response: dict) -> list[dict]:
    """Extract tool calls from an LLM response."""
    if "error" in response:
        return []

    choices = response.get("choices", [])
    if not choices:
        return []

    message = choices[0].get("message", {})
    tool_calls = message.get("tool_calls", [])

    results = []
    for tc in tool_calls:
        fn = tc.get("function", {})
        name = fn.get("name", "")
        try:
            args = json.loads(fn.get("arguments", "{}"))
        except json.JSONDecodeError:
            args = {}
        results.append({"name": name, "arguments": args})

    return results


# ---------------------------------------------------------------------------
# Scoring
# ---------------------------------------------------------------------------

def score_scenario(scenario: EvalScenario, actual_calls: list[dict]) -> tuple[str, str]:
    """Score: pass / partial / fail with explanation."""
    if not scenario.expected_tools:
        # Behavioral expectation only (no explicit tool calls expected)
        return "skip", "No expected tool calls defined — manual review needed"

    if not actual_calls:
        return "fail", "No tool calls made"

    expected_names = [t.tool_name for t in scenario.expected_tools]
    actual_names = [c["name"] for c in actual_calls]

    # Check tool selection
    all_tools_match = all(en in actual_names for en in expected_names)
    any_tools_match = any(en in actual_names for en in expected_names)

    if not any_tools_match:
        return "fail", f"Expected {expected_names}, got {actual_names}"

    if not all_tools_match:
        missing = [n for n in expected_names if n not in actual_names]
        return "partial", f"Missing tool calls: {missing}"

    # Check parameters for matching tools
    param_issues = []
    for expected in scenario.expected_tools:
        matching = [c for c in actual_calls if c["name"] == expected.tool_name]
        if not matching:
            continue
        actual_args = matching[0]["arguments"]

        for param, expected_val in expected.parameters.items():
            # Skip descriptive values (not literal)
            if expected_val.startswith("any ") or expected_val.startswith("the "):
                continue
            if expected_val in ("null", "null / omitted"):
                if param in actual_args and actual_args[param] is not None:
                    param_issues.append(f"{expected.tool_name}.{param}: expected null, got '{actual_args[param]}'")
                continue

            actual_val = actual_args.get(param)
            if actual_val is None:
                param_issues.append(f"{expected.tool_name}.{param}: missing (expected '{expected_val}')")
            elif str(actual_val) != expected_val:
                param_issues.append(f"{expected.tool_name}.{param}: expected '{expected_val}', got '{actual_val}'")

    if param_issues:
        return "partial", "Parameter mismatches: " + "; ".join(param_issues)

    return "pass", "All tools and parameters match"


# ---------------------------------------------------------------------------
# Reporter
# ---------------------------------------------------------------------------

COLORS = {
    "pass": "\033[92m✅ Pass\033[0m",
    "partial": "\033[93m⚠️  Partial\033[0m",
    "fail": "\033[91m❌ Fail\033[0m",
    "skip": "\033[90m⏭  Skip\033[0m",
    "error": "\033[91m💥 Error\033[0m",
}


def print_result(scenario: EvalScenario):
    badge = COLORS.get(scenario.result, scenario.result)
    print(f"  {badge}  {scenario.eval_id}: {scenario.title}")
    if scenario.result in ("partial", "fail", "error"):
        print(f"         → {scenario.explanation}")
    if scenario.actual_tools:
        names = [t["name"] for t in scenario.actual_tools]
        print(f"         Tools called: {names}")


def print_summary(scenarios: list[EvalScenario]):
    total = len(scenarios)
    counts = {"pass": 0, "partial": 0, "fail": 0, "skip": 0, "error": 0}
    for s in scenarios:
        counts[s.result or "error"] += 1

    print("\n" + "=" * 60)
    print("EVAL SUMMARY")
    print("=" * 60)
    print(f"  Total:   {total}")
    print(f"  ✅ Pass:    {counts['pass']}")
    print(f"  ⚠️  Partial: {counts['partial']}")
    print(f"  ❌ Fail:    {counts['fail']}")
    print(f"  ⏭  Skip:    {counts['skip']}")
    print(f"  💥 Error:   {counts['error']}")

    scored = counts["pass"] + counts["partial"] + counts["fail"]
    if scored > 0:
        score = (counts["pass"] + 0.5 * counts["partial"]) / scored * 100
        print(f"\n  Score: {score:.1f}% ({scored} scored)")
    print("=" * 60)

    # Per-file breakdown
    files = sorted(set(s.source_file for s in scenarios))
    print("\nPer-file breakdown:")
    for f in files:
        file_scenarios = [s for s in scenarios if s.source_file == f]
        p = sum(1 for s in file_scenarios if s.result == "pass")
        t = len(file_scenarios)
        print(f"  {f}: {p}/{t} pass")


def save_results(scenarios: list[EvalScenario], output_path: Path):
    """Save detailed results to JSON."""
    results = []
    for s in scenarios:
        results.append({
            "eval_id": s.eval_id,
            "title": s.title,
            "category": s.category,
            "difficulty": s.difficulty,
            "source_file": s.source_file,
            "result": s.result,
            "explanation": s.explanation,
            "expected_tools": [{"name": t.tool_name, "params": t.parameters} for t in s.expected_tools],
            "actual_tools": s.actual_tools,
        })

    output_path.write_text(json.dumps(results, indent=2))
    print(f"\nDetailed results saved to {output_path}")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Run AI evals for Data Factory MCP tools")
    parser.add_argument("--file", help="Run evals from a specific file (e.g., 'authentication')")
    parser.add_argument("--eval", help="Run a single eval by ID (e.g., 'EVAL-AUTH-001')")
    parser.add_argument("--category", help="Filter by category (e.g., 'Tool Selection')")
    parser.add_argument("--difficulty", help="Filter by difficulty (e.g., 'Easy', 'Medium', 'Hard')")
    parser.add_argument("--dry-run", action="store_true", help="Parse only, no LLM calls")
    parser.add_argument("--model", default=os.environ.get("EVAL_MODEL", "gpt-4o"), help="Model to evaluate")
    parser.add_argument("--base-url", default=os.environ.get("EVAL_BASE_URL", "https://api.openai.com/v1"))
    parser.add_argument("--output", default="eval_results.json", help="Output file for results")
    parser.add_argument("--delay", type=float, default=1.0, help="Delay between API calls (seconds)")
    args = parser.parse_args()

    evals_dir = Path(__file__).parent
    schema_path = evals_dir / "tools_schema.json"

    # Load tool schemas
    tools = json.loads(schema_path.read_text())["tools"]
    print(f"Loaded {len(tools)} tool definitions from {schema_path.name}")

    # Parse eval files
    if args.file:
        files = list(evals_dir.glob(f"*{args.file}*.eval.md"))
    else:
        files = sorted(evals_dir.glob("*.eval.md"))

    if not files:
        print("No eval files found", file=sys.stderr)
        sys.exit(1)

    all_scenarios: list[EvalScenario] = []
    for f in files:
        scenarios = parse_eval_file(f)
        all_scenarios.extend(scenarios)
        print(f"Parsed {len(scenarios)} scenarios from {f.name}")

    # Apply filters
    if args.eval:
        all_scenarios = [s for s in all_scenarios if s.eval_id == args.eval]
    if args.category:
        all_scenarios = [s for s in all_scenarios if args.category.lower() in s.category.lower()]
    if args.difficulty:
        all_scenarios = [s for s in all_scenarios if s.difficulty.lower() == args.difficulty.lower()]

    print(f"\n{'=' * 60}")
    print(f"Running {len(all_scenarios)} evals with model: {args.model}")
    print(f"{'=' * 60}\n")

    if args.dry_run:
        for s in all_scenarios:
            exp = [f"{t.tool_name}({', '.join(f'{k}={v}' for k, v in t.parameters.items())})" for t in s.expected_tools]
            print(f"  {s.eval_id}: {s.title}")
            print(f"    Category: {s.category} | Difficulty: {s.difficulty}")
            print(f"    Prompt: {s.user_prompt[:80]}...")
            print(f"    Expected: {exp or '(behavioral)'}")
            print()
        print(f"Dry run complete. {len(all_scenarios)} scenarios parsed.")
        return

    # Validate API key
    api_key = os.environ.get("OPENAI_API_KEY", "")
    if not api_key:
        print("Error: OPENAI_API_KEY environment variable not set", file=sys.stderr)
        print("Set it or use --dry-run to parse without LLM calls", file=sys.stderr)
        sys.exit(1)

    # Run evals
    current_file = None
    for i, scenario in enumerate(all_scenarios):
        if scenario.source_file != current_file:
            current_file = scenario.source_file
            print(f"\n--- {current_file} ---")

        try:
            response = call_llm(
                prompt=scenario.user_prompt,
                tools=tools,
                context=scenario.context,
                model=args.model,
                base_url=args.base_url,
                api_key=api_key,
            )

            if "error" in response:
                scenario.result = "error"
                scenario.explanation = response["error"]
            else:
                actual_calls = extract_tool_calls(response)
                scenario.actual_tools = actual_calls
                scenario.result, scenario.explanation = score_scenario(scenario, actual_calls)

        except Exception as e:
            scenario.result = "error"
            scenario.explanation = str(e)

        print_result(scenario)

        if i < len(all_scenarios) - 1 and args.delay > 0:
            time.sleep(args.delay)

    # Report
    print_summary(all_scenarios)
    save_results(all_scenarios, Path(args.output))


if __name__ == "__main__":
    main()
