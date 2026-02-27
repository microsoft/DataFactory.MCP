#!/usr/bin/env python3
"""
Integration Eval Runner for Data Factory MCP Tools

Tests content quality: M code syntax, destination patterns, pipeline JSON.
Runs each scenario twice — without skills (baseline) and with skills (full system) —
then reports the delta to measure skill ROI.

Usage:
    python run_integration_evals.py --dry-run           # Parse only
    python run_integration_evals.py                      # Run all
    python run_integration_evals.py --eval EVAL-INT-M-001
    python run_integration_evals.py --baseline-only      # Skip skills run
    python run_integration_evals.py --skills-only        # Skip baseline run

Environment variables:
    OPENAI_API_KEY     - API key (required unless --dry-run)
    EVAL_MODEL         - Model to test (default: gpt-4o)
    EVAL_BASE_URL      - API base URL (default: https://api.openai.com/v1)
                         For Azure OpenAI, use the deployment endpoint URL
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
# Skill loader
# ---------------------------------------------------------------------------

SKILLS_DIR = Path(__file__).parent.parent / "claude-skills"

SKILL_FILES = {
    "datafactory-core": "datafactory-core.md",
    "datafactory-destinations": "datafactory-destinations.md",
    "datafactory-performance": "datafactory-performance.md",
    "datafactory-advanced": "datafactory-advanced.md",
    "datafactory-pipelines": "datafactory-pipelines.md",
}

# Always-loaded skill tip file
SKILL_TIPS_FILE = "SKILL.md"


def load_skill(name: str) -> str:
    path = SKILLS_DIR / SKILL_FILES.get(name, "")
    if path.exists():
        return path.read_text()
    return ""


def load_tips() -> str:
    path = SKILLS_DIR / SKILL_TIPS_FILE
    return path.read_text() if path.exists() else ""


def build_system_prompt(skill_names: list[str]) -> str:
    base = (
        "You are an AI assistant that helps users work with Microsoft Fabric Data Factory. "
        "You write M (Power Query) code, configure data destinations, and build pipeline definitions. "
        "When asked to write M code, always produce a complete, valid M section document unless told otherwise."
    )

    if not skill_names:
        return base

    tips = load_tips()
    skills_text = "\n\n---\n\n".join(
        load_skill(name) for name in skill_names if load_skill(name)
    )

    return f"{base}\n\n## Reference Knowledge\n\n{tips}\n\n{skills_text}"


# ---------------------------------------------------------------------------
# Scenario model
# ---------------------------------------------------------------------------

@dataclass
class ValidationRule:
    description: str
    check_type: str  # "contains", "not_contains", "regex", "json_valid", "custom"
    pattern: str = ""

    def evaluate(self, text: str) -> bool:
        text_lower = text.lower() if self.check_type != "regex" else text
        if self.check_type == "contains":
            return self.pattern.lower() in text_lower
        elif self.check_type == "not_contains":
            return self.pattern.lower() not in text_lower
        elif self.check_type == "regex":
            return bool(re.search(self.pattern, text, re.IGNORECASE | re.MULTILINE))
        elif self.check_type == "json_valid":
            return _is_valid_json(text)
        elif self.check_type == "m_validator":
            return _m_validator_pass(text)
        return False


@dataclass
class IntegrationScenario:
    eval_id: str
    title: str
    category: str
    difficulty: str
    skills: list[str]  # skill names to load
    user_prompt: str
    validation_rules: list[ValidationRule]
    source_file: str
    # Results per mode
    baseline_result: Optional[str] = None
    baseline_output: str = ""
    baseline_passed: list[str] = field(default_factory=list)
    baseline_failed: list[str] = field(default_factory=list)
    skills_result: Optional[str] = None
    skills_output: str = ""
    skills_passed: list[str] = field(default_factory=list)
    skills_failed: list[str] = field(default_factory=list)


# ---------------------------------------------------------------------------
# M validation helpers
# ---------------------------------------------------------------------------

def _is_valid_json(text: str) -> bool:
    # Extract JSON from markdown code blocks if present
    json_text = _extract_code_block(text, "json") or text
    try:
        json.loads(json_text)
        return True
    except (json.JSONDecodeError, ValueError):
        return False


def _m_validator_pass(text: str) -> bool:
    """Replicate MDocumentValidator logic in Python."""
    m_text = _extract_code_block(text, "m") or _extract_code_block(text, "") or text

    if "section " not in m_text:
        return False
    if not re.search(r"section\s+\w+\s*;", m_text):
        return False
    if not re.search(r"\bshared\s+", m_text, re.IGNORECASE):
        return False

    # Balanced brackets
    for open_c, close_c in [("(", ")"), ("{", "}"), ("[", "]")]:
        if m_text.count(open_c) != m_text.count(close_c):
            return False

    return True


def _extract_code_block(text: str, lang: str) -> Optional[str]:
    """Extract content from a markdown fenced code block."""
    if lang:
        pattern = rf"```{lang}\s*\n([\s\S]*?)```"
    else:
        pattern = r"```\s*\n([\s\S]*?)```"
    m = re.search(pattern, text)
    return m.group(1).strip() if m else None


# ---------------------------------------------------------------------------
# Markdown parser
# ---------------------------------------------------------------------------

def parse_integration_eval_file(filepath: Path) -> list[IntegrationScenario]:
    text = filepath.read_text()
    scenarios = []

    blocks = re.split(r"(?=^### EVAL-)", text, flags=re.MULTILINE)

    for block in blocks:
        match = re.match(r"^### (EVAL-[\w-]+):\s*(.+)", block)
        if not match:
            continue

        eval_id = match.group(1)
        title = match.group(2).strip()

        category = _extract_field(block, "Category") or "Unknown"
        difficulty = _extract_field(block, "Difficulty") or "Unknown"
        skills_str = _extract_field(block, "Skills") or "none"
        user_prompt = _extract_blockquote(block, "User prompt")

        # Parse skills: "none → datafactory-core + datafactory-performance"
        skills = []
        if "→" in skills_str:
            after_arrow = skills_str.split("→")[1].strip()
            skills = [s.strip() for s in after_arrow.split("+")]
        elif skills_str.strip() != "none":
            skills = [s.strip() for s in skills_str.split("+")]

        # Parse validation rules from checkbox list
        validation_rules = _extract_validation_rules(block)

        scenarios.append(IntegrationScenario(
            eval_id=eval_id,
            title=title,
            category=category,
            difficulty=difficulty,
            skills=skills,
            user_prompt=user_prompt or "",
            validation_rules=validation_rules,
            source_file=filepath.name,
        ))

    return scenarios


def _extract_field(block: str, field_name: str) -> Optional[str]:
    m = re.search(rf"\*\*{field_name}:\*\*\s*(.+)", block)
    return m.group(1).strip() if m else None


def _extract_blockquote(block: str, section_name: str) -> Optional[str]:
    pattern = rf"\*\*{section_name}:\*\*.*?\n((?:>.*\n?)+)"
    m = re.search(pattern, block)
    if not m:
        return None
    lines = m.group(1).strip().split("\n")
    return "\n".join(line.lstrip("> ").rstrip() for line in lines).strip()


RULE_PATTERNS = {
    # M syntax checks
    "section Section1;": ("regex", r"section\s+\w+\s*;"),
    "section": ("regex", r"section\s+\w+\s*;"),
    "shared GetCustomers =": ("regex", r"shared\s+GetCustomers\s*="),
    "shared": ("regex", r"\bshared\s+"),
    "let ... in": ("regex", r"\blet\b[\s\S]*?\bin\b"),
    "Sql.Database": ("contains", "Sql.Database"),
    "Table.SelectRows": ("contains", "Table.SelectRows"),
    "Table.Group": ("contains", "Table.Group"),
    "Table.Sort": ("contains", "Table.Sort"),
    "Table.SelectColumns": ("contains", "Table.SelectColumns"),
    "Lakehouse.Contents": ("contains", "Lakehouse.Contents"),
    "Web.Contents": ("contains", "Web.Contents"),
    "Action.Sequence": ("contains", "Action.Sequence"),
    # Destinations
    "DataDestinations": ("contains", "DataDestinations"),
    "_DataDestination": ("regex", r"\w+_DataDestination"),
    "IsNewTarget = true": ("contains", "IsNewTarget = true"),
    "IsNewTarget = false": ("contains", "IsNewTarget = false"),
    'Kind = "Automatic"': ("contains", '"Automatic"'),
    'Kind = "Manual"': ("contains", '"Manual"'),
    "?[Data]?": ("contains", "?[Data]?"),
    "[Data]": ("contains", "[Data]"),
    "EnableFolding = false": ("contains", "EnableFolding = false"),
    "[AllowCombine = true]": ("contains", "[AllowCombine = true]"),
    # Negative checks
    "Table.FirstN": ("not_contains", "Table.FirstN"),
    "[StagingDefinition": ("contains", "StagingDefinition"),
    "NOT contain Table.Group": ("not_contains", "Table.Group"),
    "NOT contain Action.Sequence": ("not_contains", "Action.Sequence"),
    "NOT use Fast Copy": ("not_contains", "StagingDefinition"),
    "NOT contain hardcoded year": ("not_contains", "2025"),
    # Pipeline JSON
    "Valid JSON": ("json_valid", ""),
    "properties.activities": ("contains", '"activities"'),
    "DataflowActivity": ("contains", "DataflowActivity"),
    "dependsOn": ("contains", "dependsOn"),
    "Succeeded": ("contains", "Succeeded"),
    # Validator
    "Passes MDocumentValidator": ("m_validator", ""),
    # Workflow checks
    "ApplyChangesIfNeeded": ("contains", "ApplyChangesIfNeeded"),
    "add_connection_to_dataflow": ("contains", "add_connection"),
    "list_connections": ("contains", "list_connections"),
    "SkipApplyChanges": ("not_contains", "SkipApplyChanges"),
    # Date patterns
    "DateTime.LocalNow": ("regex", r"DateTime\.LocalNow|DateTimeZone\.LocalNow"),
    "Date.AddMonths": ("contains", "Date.AddMonths"),
    # Quoted names
    '#"Get Sales Data"': ("regex", r'#"[^"]+\s+[^"]+"'),
}


def _extract_validation_rules(block: str) -> list[ValidationRule]:
    """Extract validation rules from - [ ] checkbox lines."""
    pattern = r"\*\*Validation rules:\*\*\s*\n((?:\s*-\s*\[[ x]\].+\n?)+)"
    m = re.search(pattern, block)
    if not m:
        return []

    rules = []
    for line in m.group(1).strip().split("\n"):
        line = line.strip()
        desc_match = re.match(r"-\s*\[[ x]\]\s*(.+)", line)
        if not desc_match:
            continue

        desc = desc_match.group(1).strip()
        rule = _match_rule(desc)
        if rule:
            rules.append(rule)

    return rules


def _match_rule(description: str) -> Optional[ValidationRule]:
    """Map a human-readable rule description to a ValidationRule."""
    # Try exact keyword matches first
    for keyword, (check_type, pattern) in RULE_PATTERNS.items():
        if keyword.lower() in description.lower():
            return ValidationRule(description=description, check_type=check_type, pattern=pattern)

    # Fallback: contains check on the description itself
    # Extract backticked content as the search pattern
    backtick = re.search(r"`([^`]+)`", description)
    if backtick:
        target = backtick.group(1)
        if "NOT" in description or "not" in description.split("`")[0]:
            return ValidationRule(description=description, check_type="not_contains", pattern=target)
        return ValidationRule(description=description, check_type="contains", pattern=target)

    return None


# ---------------------------------------------------------------------------
# LLM caller
# ---------------------------------------------------------------------------

def call_llm(
    prompt: str,
    system_prompt: str,
    model: str = "gpt-4o",
    base_url: str = "https://api.openai.com/v1",
    api_key: str = "",
) -> str:
    import urllib.request

    body = {
        "model": model,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": prompt},
        ],
        "temperature": 0,
        "max_tokens": 4096,
    }

    req = urllib.request.Request(
        f"{base_url}/chat/completions",
        data=json.dumps(body).encode(),
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {api_key}",
        },
    )

    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = json.loads(resp.read())
            return data["choices"][0]["message"]["content"]
    except Exception as e:
        return f"[ERROR] {e}"


# ---------------------------------------------------------------------------
# Scoring
# ---------------------------------------------------------------------------

def score_output(scenario: IntegrationScenario, output: str) -> tuple[list[str], list[str]]:
    """Returns (passed_rules, failed_rules)."""
    passed = []
    failed = []
    for rule in scenario.validation_rules:
        if rule.evaluate(output):
            passed.append(rule.description)
        else:
            failed.append(rule.description)
    return passed, failed


def result_label(passed: list, failed: list) -> str:
    total = len(passed) + len(failed)
    if total == 0:
        return "skip"
    if len(failed) == 0:
        return "pass"
    if len(passed) > 0:
        return "partial"
    return "fail"


# ---------------------------------------------------------------------------
# Reporter
# ---------------------------------------------------------------------------

COLORS = {
    "pass": "\033[92m✅\033[0m",
    "partial": "\033[93m⚠️\033[0m",
    "fail": "\033[91m❌\033[0m",
    "skip": "\033[90m⏭\033[0m",
    "error": "\033[91m💥\033[0m",
}


def print_scenario_result(scenario: IntegrationScenario):
    b_badge = COLORS.get(scenario.baseline_result, "?")
    s_badge = COLORS.get(scenario.skills_result, "?")
    b_score = f"{len(scenario.baseline_passed)}/{len(scenario.baseline_passed) + len(scenario.baseline_failed)}"
    s_score = f"{len(scenario.skills_passed)}/{len(scenario.skills_passed) + len(scenario.skills_failed)}"

    # Delta
    b_pct = _pct(scenario.baseline_passed, scenario.baseline_failed)
    s_pct = _pct(scenario.skills_passed, scenario.skills_failed)
    delta = s_pct - b_pct
    delta_str = f"+{delta:.0f}%" if delta > 0 else f"{delta:.0f}%" if delta < 0 else "0%"
    delta_color = "\033[92m" if delta > 0 else "\033[91m" if delta < 0 else "\033[90m"

    print(f"  {scenario.eval_id}: {scenario.title}")
    if scenario.baseline_result:
        print(f"    Baseline: {b_badge} {b_score}  |  With skills: {s_badge} {s_score}  |  Delta: {delta_color}{delta_str}\033[0m")
    elif scenario.skills_result:
        print(f"    With skills: {s_badge} {s_score}")
    else:
        print(f"    Baseline: {b_badge} {b_score}")

    # Show failures
    for label, failures in [("baseline", scenario.baseline_failed), ("skills", scenario.skills_failed)]:
        if failures:
            print(f"    {label} failures: {failures[:3]}{'...' if len(failures) > 3 else ''}")


def _pct(passed: list, failed: list) -> float:
    total = len(passed) + len(failed)
    return (len(passed) / total * 100) if total > 0 else 0


def print_summary(scenarios: list[IntegrationScenario]):
    print("\n" + "=" * 70)
    print("INTEGRATION EVAL SUMMARY")
    print("=" * 70)

    b_total_pass = sum(len(s.baseline_passed) for s in scenarios)
    b_total_fail = sum(len(s.baseline_failed) for s in scenarios)
    s_total_pass = sum(len(s.skills_passed) for s in scenarios)
    s_total_fail = sum(len(s.skills_failed) for s in scenarios)

    b_total = b_total_pass + b_total_fail
    s_total = s_total_pass + s_total_fail

    if b_total > 0:
        print(f"\n  Baseline (no skills):   {b_total_pass}/{b_total} rules passed ({b_total_pass/b_total*100:.1f}%)")
    if s_total > 0:
        print(f"  With skills:            {s_total_pass}/{s_total} rules passed ({s_total_pass/s_total*100:.1f}%)")
    if b_total > 0 and s_total > 0:
        b_pct = b_total_pass / b_total * 100
        s_pct = s_total_pass / s_total * 100
        delta = s_pct - b_pct
        color = "\033[92m" if delta > 0 else "\033[91m" if delta < 0 else "\033[90m"
        print(f"  Skill ROI (delta):      {color}{delta:+.1f}%\033[0m")

    # Per-category
    categories = sorted(set(s.category for s in scenarios))
    print("\nPer-category:")
    for cat in categories:
        cat_scenarios = [s for s in scenarios if s.category == cat]
        bp = sum(len(s.baseline_passed) for s in cat_scenarios)
        bf = sum(len(s.baseline_failed) for s in cat_scenarios)
        sp = sum(len(s.skills_passed) for s in cat_scenarios)
        sf = sum(len(s.skills_failed) for s in cat_scenarios)
        b = f"{bp}/{bp+bf}" if bp + bf > 0 else "—"
        s = f"{sp}/{sp+sf}" if sp + sf > 0 else "—"
        print(f"  {cat:20s}  baseline: {b:8s}  skills: {s}")

    print("=" * 70)


def save_results(scenarios: list[IntegrationScenario], output_path: Path):
    results = []
    for s in scenarios:
        results.append({
            "eval_id": s.eval_id,
            "title": s.title,
            "category": s.category,
            "difficulty": s.difficulty,
            "skills": s.skills,
            "baseline": {
                "result": s.baseline_result,
                "passed": s.baseline_passed,
                "failed": s.baseline_failed,
                "output_preview": s.baseline_output[:500],
            },
            "with_skills": {
                "result": s.skills_result,
                "passed": s.skills_passed,
                "failed": s.skills_failed,
                "output_preview": s.skills_output[:500],
            },
        })
    output_path.write_text(json.dumps(results, indent=2))
    print(f"\nResults saved to {output_path}")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Run integration evals for M code quality")
    parser.add_argument("--eval", help="Run a single eval by ID")
    parser.add_argument("--category", help="Filter by category")
    parser.add_argument("--dry-run", action="store_true", help="Parse only, no LLM calls")
    parser.add_argument("--baseline-only", action="store_true", help="Skip skills run")
    parser.add_argument("--skills-only", action="store_true", help="Skip baseline run")
    parser.add_argument("--model", default=os.environ.get("EVAL_MODEL", "gpt-4o"))
    parser.add_argument("--base-url", default=os.environ.get("EVAL_BASE_URL", "https://api.openai.com/v1"))
    parser.add_argument("--output", default="integration_eval_results.json")
    parser.add_argument("--delay", type=float, default=1.0)
    args = parser.parse_args()

    evals_dir = Path(__file__).parent

    # Parse eval files
    files = sorted(evals_dir.glob("*.eval.md"))
    if not files:
        print("No integration eval files found", file=sys.stderr)
        sys.exit(1)

    all_scenarios: list[IntegrationScenario] = []
    for f in files:
        scenarios = parse_integration_eval_file(f)
        all_scenarios.extend(scenarios)
        print(f"Parsed {len(scenarios)} scenarios from {f.name}")

    # Apply filters
    if args.eval:
        all_scenarios = [s for s in all_scenarios if s.eval_id == args.eval]
    if args.category:
        all_scenarios = [s for s in all_scenarios if args.category.lower() in s.category.lower()]

    print(f"\n{'=' * 70}")
    print(f"Running {len(all_scenarios)} integration evals with model: {args.model}")
    modes = []
    if not args.skills_only:
        modes.append("baseline")
    if not args.baseline_only:
        modes.append("with_skills")
    print(f"Modes: {', '.join(modes)}")
    print(f"Skills dir: {SKILLS_DIR}")
    print(f"{'=' * 70}\n")

    if args.dry_run:
        for s in all_scenarios:
            rules_count = len(s.validation_rules)
            print(f"  {s.eval_id}: {s.title}")
            print(f"    Category: {s.category} | Difficulty: {s.difficulty}")
            print(f"    Skills: {s.skills or ['none']}")
            print(f"    Validation rules: {rules_count}")
            print(f"    Prompt: {s.user_prompt[:80]}...")
            print()
        print(f"Dry run complete. {len(all_scenarios)} scenarios, "
              f"{sum(len(s.validation_rules) for s in all_scenarios)} total rules.")
        return

    api_key = os.environ.get("OPENAI_API_KEY", "")
    if not api_key:
        print("Error: OPENAI_API_KEY not set", file=sys.stderr)
        sys.exit(1)

    for i, scenario in enumerate(all_scenarios):
        print(f"\n--- {scenario.eval_id}: {scenario.title} ---")

        # Baseline (no skills)
        if "baseline" in modes:
            sys_prompt = build_system_prompt([])
            output = call_llm(scenario.user_prompt, sys_prompt,
                              model=args.model, base_url=args.base_url, api_key=api_key)
            scenario.baseline_output = output
            scenario.baseline_passed, scenario.baseline_failed = score_output(scenario, output)
            scenario.baseline_result = result_label(scenario.baseline_passed, scenario.baseline_failed)

            if args.delay > 0:
                time.sleep(args.delay)

        # With skills
        if "with_skills" in modes:
            sys_prompt = build_system_prompt(scenario.skills)
            output = call_llm(scenario.user_prompt, sys_prompt,
                              model=args.model, base_url=args.base_url, api_key=api_key)
            scenario.skills_output = output
            scenario.skills_passed, scenario.skills_failed = score_output(scenario, output)
            scenario.skills_result = result_label(scenario.skills_passed, scenario.skills_failed)

            if args.delay > 0 and i < len(all_scenarios) - 1:
                time.sleep(args.delay)

        print_scenario_result(scenario)

    print_summary(all_scenarios)
    save_results(all_scenarios, Path(args.output))


if __name__ == "__main__":
    main()
