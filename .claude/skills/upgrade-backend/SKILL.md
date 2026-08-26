---
name: review-document
description: >
  Review a business document for source accuracy, scope consistency, wording, structure, formatting, and quality-control issues.
---

# Review Document

## Objective
Execute this task consistently and safely across projects while adapting to the current repository/workspace.

## Workflow
1. Understand the user's goal and scope.
2. Inspect existing files, conventions, and relevant sources before changing anything.
3. Identify the smallest correct approach.
4. Reuse existing patterns where they are sound.
5. Implement or produce the requested output.
6. Validate correctness, consistency, and regressions.
7. Report key changes, assumptions, and items requiring verification.

## Constraints
- Do not invent project-specific facts or contracts.
- Do not rewrite unrelated areas.
- Do not add unnecessary dependencies or abstractions.
- Prefer explicit, maintainable solutions over clever shortcuts.
- Follow applicable project rules in `.claude/rules/`.

## Project Adaptation
When first used in a new project, inspect the real codebase/documents and adapt to the project's actual conventions rather than assuming this template is authoritative.
