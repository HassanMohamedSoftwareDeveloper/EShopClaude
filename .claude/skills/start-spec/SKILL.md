---
name: start-spec
description: Start one microservice spec from _agenticdev/microservices (steps 2–4 of the cycle in _agenticdev/WOW.md). Runs preflight checks, creates the feature branch from main, commits the spec as the branch's first commit, then plans the implementation in plan mode and stops for approval. Never implements, pushes or merges.
argument-hint: <service> <NN>
disable-model-invocation: true
allowed-tools: Read, Glob, Bash(git status:*), Bash(git branch:*), Bash(git checkout:*), Bash(git add:*), Bash(git commit:*)
---

# /start-spec

## Repository state at invocation

- Current branch: !`git branch --show-current`
- Working tree (`git status --porcelain`, empty means clean):

```
!`git status --porcelain`
```

Arguments: `$ARGUMENTS` → `<service> <NN>`. `<service>` is a folder under `_agenticdev/microservices/` (e.g. `catalog-api`). `<NN>` is the spec number; pad a single digit to two (`4` → `04`). If either argument is missing, list the service folders and stop.

## Hard rules
- Git commands allowed: `git status`, `git branch`, `git checkout`, `git add`, `git commit`. Nothing else: no push, no merge, no rm, no reset, no stash, no switch.
- Never push. Feature branches stay local in this repo.
- Never edit any file. This skill branches, commits the spec, and plans. It does not implement.
- Never invent or ask for a branch name. It comes from the WOW.md rule only (step 3).
- If any check fails, change nothing. Report: **which check failed**, the evidence (the offending line, entry or section), and **what to do next**. Then stop.

## Steps

### 1. Resolve the spec
Glob `_agenticdev/microservices/<service>/<NN>-*.md`.
- No match → fail. Next: list the specs that exist for that service (or the service folders, if the folder is missing) and say to run `/new-spec <service> <NN> <slug> "<title>"` to create the spec.
- More than one match → fail and list them. Next: remove or renumber the duplicate by hand.
- Exactly one → `<spec>` is that path and `<slug>` is the file name minus `<NN>-` and `.md`.

Read `<spec>` and `_agenticdev/microservices/README.md`. Find the README row whose Spec link points to `<service>/<NN>-<slug>.md`. No row → fail. Next: run `/new-spec <service> <NN>`, which adds the row.

### 2. Preflight (all must pass, in this order)
Use the repository state injected above. Do not re-derive it unless a check below says to.

1. **On main**: current branch is `main`. Otherwise fail. Next: commit or stash your work on `<current branch>`, then `git switch main` and rerun.
2. **Clean apart from the spec**: every line of the porcelain output refers to `<spec>`. An empty output passes. If a line is an untracked directory (ends in `/`) that contains `<spec>`, run `git status --porcelain --untracked-files=all -- <that dir>` and check each expanded line instead. Any other path fails, including `_agenticdev/microservices/README.md`. Next: stash or discard those changes, or move them to the branch they belong on (never commit them to `main` directly), then rerun.
3. **Not Done**: the spec's `**Status:**` line is not `Done`, and the README row's Status is not `Done`. Otherwise fail. Next: pick the next spec per WOW.md "Pick": lowest-numbered not-Done spec whose dependencies are all merged.
4. **Dependencies Done**: read the spec's `## Depends on` section.
   - `Nothing` / `nothing` (with or without a trailing explanation) passes.
   - Each entry that names a spec file (`<svc>/<NN>-<slug>.md`) must have a README row with Status `Done`. Anything else fails; name each such entry and its README status.
   - An entry that is not a microservice spec (e.g. a PLAN.md step) cannot be checked against the README, so it fails. Next: confirm that step is merged to `main` and record it as Done where the team tracks it, then rerun.
   - Next on failure: run `/start-spec` for the missing dependency first.
5. **Spec complete**: the spec has each of these sections, each with real content: `**Status:**` line, `## Goal`, `## Depends on`, `## Scope`, `## Tasks`, `## Explicit non-goals`, `## Definition of Done`, `## References`. A section fails if it is missing, empty, still holds template text (a `> ...` placeholder line from `_agenticdev/templates/spec-template.md`, `<Service>`, `<Short name>`, `NN`), or says `TBD` / `TODO` / `...` in place of content. A Definition of Done item without a concrete command or request and its expected result also fails. Next: run `/new-spec <service> <NN>` to complete it, then rerun.

### 3. Derive the branch name
WOW.md rule: `feature/<service>-<NN>-<short-name>`, where `<service>` is the folder name with the `-api` suffix dropped and `<short-name>` is `<slug>`.
Example: `catalog-api/04-get-product.md` → `feature/catalog-04-get-product`.

Then check:
- The README row's Branch column must equal the derived name. If it differs → fail, show both. Next: fix the README row (or the spec file name) so they agree, then rerun.
- `git branch --list <branch>` must print nothing. If the branch exists → fail. Next: if it holds earlier work, `git switch <branch>` and continue there by hand; if it is stale, delete it yourself, then rerun.

### 4. Create the branch from main
`git checkout -b <branch> main`
Uncommitted spec changes carry over onto the new branch.

### 5. Commit the spec on its own
- If `<spec>` had changes in the porcelain output:
  - `git add -- <spec>`
  - `git commit -m "Add spec <service>/<NN>-<slug> before implementation" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" -- <spec>`
- If `<spec>` had no changes (it is already complete on `main`), the branch still starts with a spec commit:
  - `git commit --allow-empty -m "Start spec <service>/<NN>-<slug> before implementation" -m "Spec file already on main; no changes to it." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"`
- Confirm with `git status --porcelain` (must be empty) and `git branch --show-current` (must be `<branch>`). If the commit failed, report the exact git error and stop. Leave the branch as it is; do not try to undo it.

### 6. Plan in plan mode
Enter plan mode (EnterPlanMode). Plan from `<spec>` only, per WOW.md "fresh session" rule. Read source files only where the spec's Context, Tasks or Files touched names them, to confirm the starting point.

The plan contains:
- **Spec**: `<spec>`, branch `<branch>`, spec commit hash.
- **Starting point**: what the spec's Context says is on `main`, and any place the code disagrees (do not resolve it, flag it).
- **Steps**: one per spec Task, in the spec's order, each naming the file(s) it changes and the change. Include `dotnet build src/EShopClaude.sln` after each Task when the spec says so. No step that the spec's Tasks do not require.
- **Files**: the spec's Files touched table. Any file outside it is out of scope.
- **Guardrails**: every Explicit non-goal, restated as "do not ...". Also CLAUDE.md rules that apply (no shared contracts project, no migrations, PROJECT.md §10).
- **Verification**: the Definition of Done items, numbered as in the spec, to run after implementing.
- **Open questions**: anything the spec leaves undecided. Ask, don't guess. Out-of-scope findings become a note for a new spec, not a plan step.

### 7. Stop for approval
Present the plan with ExitPlanMode and stop. Do not implement, edit, build, run, merge or push. After approval, implementation happens in auto mode under WOW.md step 5. It is not part of this skill.
