---
name: new-spec
description: Create or complete one microservice spec under _agenticdev/microservices for the spec-driven workflow in _agenticdev/WOW.md. Drafts a new spec from the template, or audits an existing one and fills its gaps. Markdown only; never branches, commits or touches src/.
argument-hint: <service> <NN> [slug] ["title"]
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob
---

# /new-spec

Arguments: `$ARGUMENTS` → `<service> <NN> [slug] ["title"]`. `<service>` is a folder under `_agenticdev/microservices/` (e.g. `catalog-api`); `<NN>` is two digits. If an argument is missing or unknown, ask for it before doing anything else.

## Hard rules
- Edit only Markdown under `_agenticdev/`. No branch, no commit, no code in `src/`.
- Never overwrite an existing spec: use `Write` only for a new file, `Edit` for everything else.
- `**Status:**` stays `Not started`, in the spec and in the README row.
- PROJECT.md wins over PLAN.md. Nothing from PROJECT.md §10 goes into Scope. A spec changes one service plus its own AppHost registration.

## Steps
1. **Resolve** `_agenticdev/microservices/<service>/<NN>-*.md` with Glob.
2. **Missing** → needs `slug` and `title` (ask if absent). Create `<NN>-<slug>.md` from `_agenticdev/templates/spec-template.md`, heading `# <Service>.Api <NN> — <title>`, Status `Not started`. Draft Goal, Scope, Explicit non-goals and References from the matching sections of `_agenticdev/PROJECT.md` and `_agenticdev/PLAN.md`.
3. **Exists** → read it and report every section as **Complete**, **Stub** (template `>` placeholder, empty, TBD) or **Vague** (no types, no file names, a Done line without a command and expected result). Then continue to fill only the gaps; keep complete text as is.
4. **Status table**: read `_agenticdev/microservices/README.md`. Fill `## Depends on` from it (earlier specs of this service plus any cross-service spec PROJECT.md requires). If the spec has no row, add one in service/number order: `| <service> | [NN — Title](<service>/NN-slug.md) | \`feature/<short>-NN-<slug>\` | <deps> | Not started |`, where `<short>` drops `-api`.
5. **Ask** before writing: every design decision the documents don't settle — field names and CLR/DB types, how the schema is created, how the service gets its connection string, anything else open. One question per decision, offer the spec-backed options, and wait for the answers.
6. **Write** the spec, following the section order of the Done specs in `catalog-api/` (use `catalog-api/01-persistence.md` as the model):
   - **Context**: starting point on `main`, connection string source, startup order, schema creation, naming.
   - **Data model**: table per entity/DTO (column, CLR type, DB type, constraints). Leave it out when the spec adds no stored or wire shape.
   - **Tasks**: numbered, one file or concern each, `dotnet build src/EShopClaude.sln` after each.
   - **Files touched**: table of path + change, then a "Not touched" line.
   - **Definition of Done**: rewrite every line as a numbered check with the exact command or request and its expected result (`GET /api/products/999` → `404`). Always start with Build and Run (`aspire run`, all Running/Healthy) and end with Diff scope (`git diff --stat main` lists only Files touched) and Non-goals held (a `git grep` with no matches).
7. **Report**: print the spec path, whether the README row was added, and a bulleted list of everything still to decide by hand (unanswered questions, assumptions you made, Done checks you could not make concrete).
