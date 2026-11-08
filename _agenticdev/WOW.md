# EShopClaude — Way of Working

How the team and Claude Code deliver work on this repo. Project facts, commands and conventions live in `CLAUDE.md`; behaviour lives in `PROJECT.md`. This file covers process only.

## Principles
- One spec = one sub-feature = one feature branch = one merge to `main`.
- A spec changes exactly one microservice plus that service's own AppHost registration.
- `main` always builds and runs. Every merge leaves a working solution.
- Every spec is self-contained: a fresh session reads that one file and nothing else to do the work.

## Branches and merges
- Branch per spec: `feature/<service>-<NN>-<short-name>`, created from latest `main`.
  - Example: `feature/catalog-01-endpoints`.
- First commit on the branch is the spec file, before any code.
- Merge with `git merge --no-ff`, then delete the branch.
- Feature branches stay local; never pushed.
- No direct commits to `main`.

## Folder layout
```
_agenticdev/
  templates/spec-template.md        # every spec follows this
  microservices/
    README.md                       # status table for all specs
    <service>/NN-<short-name>.md    # one file per spec
```
- `NN` is numbered per service in build order (`01`, `02`, …).
- Once a spec is merged, its number never changes. Numbers are never reused, even if a spec is dropped.
- Every spec follows `templates/spec-template.md`.
- New spec: copy [templates/spec-template.md](templates/spec-template.md) to `microservices/<service>/NN-<short-name>.md` and fill every section.
- Definition of Done lists checks anyone can run: the exact command or request and its expected result (e.g. `GET /api/products/999` → `404`). "It works" is not a check.
- `microservices/README.md` holds the status table for every spec. It is updated in the same change that adds, splits or renames a spec.

## The cycle, per spec
1. **Pick** the next spec: lowest-numbered not-Done spec whose dependencies are all merged.
2. **Branch** from `main`: `git switch -c feature/<service>-<NN>-<short-name>`; commit the spec file as the first commit.
3. **Fresh session**: start a new Claude Code session (or `/clear`); point it at the spec file only.
4. **Plan** in plan mode; a human reviews and approves the plan before any edit.
5. **Implement** in auto mode, staying inside the spec's scope.
6. **Verify** manually: every item in the spec's Definition of Done passes.
7. **Commit** on the branch, then `git switch main` and `git merge --no-ff feature/<service>-<NN>-<short-name>`.
8. **Mark Done** in the spec file and in the `microservices/README.md` status table.
9. **Delete** the branch: `git branch -d feature/<service>-<NN>-<short-name>`.

## Ordering rules
- Inside a service, specs run in number order.
- A spec does not start until every spec it depends on is merged to `main`.
- Dependencies are declared in the spec itself, not inferred.

## Session hygiene
- One spec, one fresh session. Clear the context between specs.
- Plan mode before implementing; a human approves the plan.
- Out-of-scope findings go into a new spec, not into the current branch.
- Before any commit:
  - `dotnet build` passes.
  - `aspire run` starts every resource Running/Healthy.
  - The spec's Definition of Done passes.
