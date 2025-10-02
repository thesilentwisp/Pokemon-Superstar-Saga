# Contributing

## Prereqs
- Unity 6 (2025.x)
- C# 10
- macOS/Windows

## Git Workflow
- Branch names: `feature/*`, `fix/*`, `chore/*`
- Small commits with clear messages: `feat:`, `fix:`, `refactor:`
- Open a Pull Request when ready; prefer 200–400 line diffs or smaller.

## Style
- Descriptive names, comments for non-obvious math (e.g., DEF mitigation).
- Avoid allocations in per-frame paths; coroutines are fine for flows.
- Prefer explicit code over clever ternaries when clarity helps.

## Testing / Checks
- Manual: Run the battle scene; verify UI, QTE timings, chain rules.
- Optional EditMode tests for pure functions (e.g., `ComputeBaseDamage`).

## Unity Settings
- Asset Serialization: Force Text
- Meta files: (Unity 6: always on)

## Issue Reports
- Include Unity version, steps to reproduce, expected vs actual, and logs.
