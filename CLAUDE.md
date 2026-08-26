# CLAUDE.md

## Project Identity
Backend/API service. Inspect the repository before assuming framework version, architecture, persistence, auth, or deployment model.

## Default Goals
- Clear domain/application/infrastructure responsibilities where justified.
- Stable API contracts.
- Explicit validation and authorization.
- Maintainable database migrations.
- Production-oriented error handling and observability.
- No unnecessary enterprise patterns.

## Working Method
Understand requirement → Inspect codebase → Define contract → Implement smallest coherent change → Test → Review security/regression.

## Project Configuration
Update this file with:
- Actual backend stack/version.
- Folder/solution structure.
- Database/provider.
- Auth mechanism.
- Migration strategy.
- Test/build/run commands.
- Deployment constraints.

Detailed rules live in `.claude/rules/`.
Task workflows live in `.claude/skills/`.
