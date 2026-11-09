---
name: finish-spec
description: Finish one microservice spec from _agenticdev/microservices (steps 5–9 of the cycle in _agenticdev/WOW.md). On the spec's feature branch it implements the approved plan, verifies the Definition of Done, marks the spec Done, then — after explicit confirmation — merges to main with --no-ff, deletes the branch and pushes main. Never pushes a feature branch; never resolves merge conflicts.
argument-hint: <service> <NN>
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Bash(git status:*), Bash(git branch:*), Bash(git log:*), Bash(git diff:*), Bash(git grep:*), Bash(git show:*), Bash(git rev-parse:*), Bash(git add:*), Bash(git commit:*), Bash(git checkout:*), Bash(git merge:*), Bash(git push origin main), Bash(dotnet build:*)
---

# /finish-spec

## Repository state at invocation

- Current branch: !`git branch --show-current`
- Commits so far (`git log --oneline main..HEAD`):

```
!`git log --oneline main..HEAD`
```

- Working tree (`git status --porcelain`, empty means clean):

```
!`git status --porcelain`
```

Arguments: `$ARGUMENTS` → `<service> <NN>`. `<service>` is a folder under `_agenticdev/microservices/` (e.g. `catalog-api`). `<NN>` is the spec number; pad a single digit to two (`4` → `04`). If either argument is missing, list the service folders and stop.

## Hard rules
- Never run on `main`. Implementation and the Done commit happen on the feature branch only.
- Never push a feature branch. The only push is `git push origin main`, after the merge, in step 7.
- Never use `git add -A`, `git add .` or `git commit -a`. Stage every file by explicit path.
- Never use `git reset`, `git stash`, `git rebase`, `git merge --abort`, `git push --force` or `git branch -D`.
- Stay inside the spec. Only files in its **Files touched** table change. Every **Explicit non-goal** holds. CLAUDE.md conventions hold (no shared contracts project, no EF migrations, nothing from PROJECT.md §10). An out-of-scope finding becomes a note in the final report for a new spec, not a change on this branch.
- If any check fails, stop. Report **which check failed**, the evidence, and **what to do next**. Leave the branch and working tree as they are; do not try to undo anything.
- Step 7 (merge, delete branch, push main) is hard to reverse. Run it only after the user explicitly confirms in this invocation. Earlier approvals (the plan, Definition of Done answers) do not count.

## Steps

### 1. Preflight
1. **Resolve the spec**: Glob `_agenticdev/microservices/<service>/<NN>-*.md`.
   - No match → fail. Next: list the specs that exist for that service and say to run `/new-spec`.
   - More than one → fail and list them.
   - Exactly one → `<spec>` is that path, `<slug>` is the file name minus `<NN>-` and `.md`.
2. **Derive the branch**: `feature/<short>-<NN>-<slug>`, where `<short>` is `<service>` with the `-api` suffix dropped. Example: `catalog-api/04-get-product.md` → `feature/catalog-04-get-product`. Read `_agenticdev/microservices/README.md`; the spec's row must exist and its Branch column must equal `<branch>`. Otherwise fail and show both.
3. **Branch exists**: `git branch --list <branch>`. Prints nothing → fail. Next: run `/start-spec <service> <NN>` first.
4. **On the branch**: the injected current branch must equal `<branch>`.
   - It is `main` → fail: this skill never runs on `main`. Next: `git switch <branch>`, then rerun.
   - It is anything else → fail, show the current branch and `<branch>`. Next: commit your work there, `git switch <branch>`, then rerun.
5. **Branch started by /start-spec**: the injected commit list is not empty and its oldest (last) line is the spec commit (`Add spec ...` or `Start spec ...`). Otherwise fail. Next: run `/start-spec <service> <NN>`.
6. **Not already Done**: the spec's `**Status:**` line and the README row's Status do not start with `Done`. Otherwise fail. Next: nothing to finish; pick the next spec per WOW.md "Pick".
7. **Working tree**: every path in the injected porcelain output must be in the spec's Files touched table (earlier implementation work in progress). Any other path → fail, list it. Next: commit it on its own branch or discard it, then rerun.

### 2. DEVELOP
1. Read `<spec>` in full: Context, Tasks, Files touched, Explicit non-goals, Definition of Done.
2. **Approved plan**: implement the plan approved earlier in this session (from `/start-spec`). If this session has no approved plan, enter plan mode (EnterPlanMode), plan from `<spec>` only as `/start-spec` step 6 describes, and wait for approval before any edit. Where the approved plan and the spec disagree, the spec wins; say so.
3. Work through the spec's Tasks in order. After each Task:
   - `dotnet build src/EShopClaude.sln`. Any error → fix it within the Task's files; if it cannot be fixed inside scope, stop and report.
   - Stage only the files that Task changed, by explicit path (`git add -- <path> <path>`), and commit:
     `git commit -m "<what the Task did> (<service>/<NN>)" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"`
   - Commits that touch already-committed files from an earlier Task are fine; keep them in Task order.
4. Update the API's `.http` file when the spec adds endpoints (CLAUDE.md convention), as part of the Task that adds them, if the spec lists it in Files touched.
5. When all Tasks are committed, `git status --porcelain` must be empty. Any leftover path → stop and report it.

### 3. VERIFY
1. `dotnet build src/EShopClaude.sln`. Any error, or any new warning → stop. Report the exact output. Nothing is merged.
2. Walk the spec's `## Definition of Done` line by line, in its order. For each line decide:
   - **Run it yourself** when it is a build, a `git diff` / `git grep` / `git log` check, a file read, or anything else your tools can run without a live AppHost. Run it and compare against the expected result. Mismatch → stop and report the line, the command, expected vs actual.
   - **Ask the user** when it needs something you cannot run or observe yourself: `aspire run` with every resource Running/Healthy, the Aspire dashboard, pgAdmin / Redis Insight (a table, a row, a key), a request sent from the `.http` file or against an endpoint taken from the dashboard. Ask one line at a time with AskUserQuestion: quote the Done line, give the exact command/request and the expected result, options **Yes, passes** / **No, fails**. Wait for the answer before the next line.
   - Any **No** (or a failure you see) → stop here. Report which line failed and the evidence. Leave the branch as it is. Nothing is marked Done, nothing is merged.
3. Keep a record per line: number, text, `ran: <command> → <result>` or `user confirmed`. It goes into the final report.

### 4. Mark the spec Done
1. `git rev-parse HEAD` → `<sha>` (the last implementation commit).
2. In `<spec>`, set the Status line to exactly: `**Status:** Done (<sha>)`. Change nothing else in the file.

### 5. Update the status table
In `_agenticdev/microservices/README.md`, set the Status cell of the spec's row (found in step 1.2) to the same value: `Done (<sha>)`. Change no other row or cell.

### 6. Commit the documentation
- `git add -- <spec> _agenticdev/microservices/README.md`
- `git commit -m "Mark <service>/<NN>-<slug> Done" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" -- <spec> _agenticdev/microservices/README.md`
- `git status --porcelain` must be empty. Otherwise stop and report.

### 7. Confirm, then merge, delete and push
1. Show the user, then ask with AskUserQuestion (options **Merge, delete branch and push main** / **Stop here**):
   - Source branch: `<branch>`
   - Target: `main`, merged with `git merge --no-ff`
   - The commits to be merged: `git log --oneline main..<branch>`
   - Then: `git branch -d <branch>` and `git push origin main`
   Anything but explicit confirmation → stop. Report that the branch is complete and unmerged, and that rerunning `/finish-spec <service> <NN>` resumes at this step.
2. `git checkout main`
3. `git merge --no-ff --no-edit <branch>`
   - Conflict or any failure → stop. Do not resolve, abort or edit anything. Report the conflicted files (`git diff --name-only --diff-filter=U`) and the exact git output. Next for the user: resolve by hand, or `git merge --abort` to back out.
4. `git rev-parse HEAD` → `<merge-sha>`.
5. `git branch -d <branch>`. If git refuses, stop and report its output. Never use `-D`.
6. `git push origin main`. If it is rejected (e.g. `main` moved on the remote), stop and report the output. Never force, pull or rebase.

### 8. Report
- **Spec**: `<spec>`, Status `Done (<sha>)`.
- **Merge commit**: `<merge-sha>` on `main`; branch `<branch>` deleted; `main` pushed to `origin` (or why not).
- **Files changed**: `git diff --stat <merge-sha>^1 <merge-sha>`.
- **Definition of Done**: table with columns `#`, `Line`, `Verified by` (`ran: <command> → <result>` or `user confirmed`).
- **Out-of-scope findings**: notes for new specs, or "none".
