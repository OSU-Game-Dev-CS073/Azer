<<<<<<< HEAD
# Contributing (Azer)
- Branch off: `feature/<short-name>`
- Open PR to `main` (or `dev` if using dev-first)
- Require ≥1 approval; resolve all comments
- Squash merge with conventional commit title
- Link Issues/Project cards for traceability
=======
# Contributing Guide — Azer: The Path of Salvation

Welcome! This guide explains how to set up, code, test, review and release so contributions meet our **Definition of Done (DoD)** (see Team Charter).

## Code of Conduct
We follow the OSU class standards and professional conduct. Report concerns privately to the Sprint Lead or Coordinator; escalate per Team Charter (Conflict & Accountability).

## Getting Started
- **Unity:** Install Unity 2022 LTS (2D). Open the project once to generate `.sln/.csproj`.  
- **Dotnet tools:** `dotnet tool restore` (ensures **CSharpier** is available).  
- **Folder conventions:** Scenes in `Assets/Scenes/`, scripts in `Assets/Scripts/`, tests in `Assets/Tests/{EditMode|PlayMode}`.

## Branching & Workflow
- **Default branch:** `main`.  
- **Feature branches:** `feature/<short-topic>` (e.g., `feature/attack-hitbox`).  
- **Flow:** feature → PR → ≥1 review → squash‑merge. Rebase locally as needed.  
- **Issue linking:** Include `Fixes #<issue-id>` or `Refs #<issue-id>` in PRs.

## Issues & Planning
- Use issue templates. Add **AC** (Given/When/Then), labels (`feature`, `bug`, `docs`), and estimate (S/M/L).  
- Keep tasks small (≤1–2 days).

## Commit Messages
Use **Conventional Commits**:
- `feat: add enemy patrol AI`  
- `fix: correct jump coyote time`  
- `docs: update ATTRIBUTIONS`  
Reference issues where applicable.

## Code Style, Linting & Formatting
- **Formatter:** CSharpier.  
  - Run locally: `dotnet tool restore && dotnet csharpier .`  
  - CI gate: `lint-format` job (see `.github/workflows/unity-ci.yml`).  
- **EditorConfig:** code style rules in `.editorconfig`. Fix warnings before review.

## Testing
- **Unity Test Runner:** put tests in `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`.  
- **Local run (Editor):** Window → Test Runner → Run.  
- **CLI (example):**  
<UnityPath>/Unity -batchmode -projectPath . -runTests -testResults results.xml -testPlatform editmode -quit
<UnityPath>/Unity -batchmode -projectPath . -runTests -testResults results_play.xml -testPlatform playmode -quit

- **Coverage thresholds:** add/maintain tests for changed gameplay logic; ensure all tests pass.

## Pull Requests & Reviews
- Must include: linked issue, passing CI checks, updated docs/attributions if applicable.  
- Keep PRs small (<300 lines changed when possible).  
- Reviewer SLA: **≤24h**. Resolve all comments; re‑request review after changes.

## CI/CD
**Workflows:**  
- `/.github/workflows/unity-ci.yml` — jobs: `lint-format`, `editmode-tests`, `playmode-tests`.
- `/.github/workflows/codeql.yml` — job: `codeql`.  
- **Required to merge:** all required jobs green (see Team Charter — DoD).

## Security & Secrets
- Never commit secrets or license files.  
- Use GitHub Secrets for tokens.  
- Security scans via **CodeQL**; report vulnerabilities to the Sprint Lead privately.

## Documentation Expectations
- Update `README.md`, `/docs/`, and `/docs/ATTRIBUTIONS.md` for any new assets or user‑visible changes.  
- Log changes in `CHANGELOG.md` under `Added/Changed/Fixed`.

## Release Process
- Tag releases `vMAJOR.MINOR.PATCH`.  
- Include build link and commit hash in the release notes; ensure CI is green.

## Support & Contact
- Questions: open a Discussion or ask in Discord `#general`.  
- Response windows: ≤12h (Discord), ≤24h (PR review).  
>>>>>>> cda978e8d09053e161e1d8f777664c3acc57018f
